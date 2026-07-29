# TPTECH-001.15: Cloud Command, Idempotency, and Event Cursor Spike

## Objective and status

Task 5 proves one authoritative cloud command path with safe retries,
optimistic concurrency, transactional audit/outbox persistence, and a
workspace-isolated event cursor. `CommandProbe` is a small development
aggregate in the Platform module, not a Receiving Session, business document,
or commercial API.

The spike is available only when `TraderPro:Spikes:Enabled` is true and the API
environment is Development or Testing. Committed configuration defaults the
flag to false. Production never maps the spike endpoints.

## CommandProbe

`CommandProbe` has a UUIDv7 ID, workspace ownership, required trimmed name
(maximum 200 characters), non-negative `long` counter, UTC create/update
timestamps, and optimistic-concurrency version. It starts at counter 0 and
version 1. Its controlled `Increment(int)` method accepts only positive whole
numbers. EF Core remains in Infrastructure; Domain has no HTTP, JSON, EF Core,
or Npgsql dependency.

The aggregate exists only to validate this pipeline and must be removed or
replaced by reviewed Platform/business commands before commercial release.

## Transaction and idempotency

Create and increment run with one scoped `TraderProDbContext` and one
PostgreSQL read-committed transaction:

1. Require the already resolved workspace.
2. Validate the idempotency key.
3. Hash a canonical representation of typed command values.
4. Acquire `pg_advisory_xact_lock` for workspace, command type, and key.
5. Read or create the uniquely scoped idempotency record.
6. Acquire the transaction-scoped outbox commit-order database lock.
7. Execute the aggregate change.
8. Add one `AuditEvent` and one `OutboxMessage`.
9. Store the successful result, original correlation ID, and HTTP status.
10. Call `SaveChangesAsync` once and commit.

The unique database index on workspace, command type, and idempotency key
remains the final invariant. The PostgreSQL lock, not a process-local lock,
coordinates concurrent identical requests.

An identical completed request returns the stored result/status with
`PreviouslyProcessed` and performs no writes. A different hash for the same
key returns `IDEMPOTENCY_PAYLOAD_CONFLICT`. A visible non-completed record
returns `IDEMPOTENCY_IN_PROGRESS`. A failed record, or a completed record
without a proven stored-result shape, returns HTTP 409
`IDEMPOTENCY_PREVIOUS_ATTEMPT_FAILED`, is not retryable, performs no writes,
and instructs the caller to use a new key. Persistence failure rolls the
entire transaction back, so a later retry can execute normally.

## Canonical request hashing and expected version

Hashes use UTF-8, SHA-256, lowercase hexadecimal, invariant UUID/numeric
formatting, explicit field labels, and a UTF-8 byte length for the normalized
name. Create includes command type and name. Increment includes command type,
route ID, expected version, and delta. Raw HTTP bytes are never hashed, so JSON
whitespace and property order do not affect a typed command hash.

Increment requires positive `X-Expected-Version`. The handler compares it with
the workspace-filtered aggregate. Mismatch returns HTTP 409
`COMMAND_PROBE_VERSION_CONFLICT` with safe expected/current details. EF
optimistic concurrency remains the final race guard and maps a database race
to the same conflict.

## Audit, outbox, and transaction rollback

First executions write one audit action:

- `Platform.CommandProbe.Created`
- `Platform.CommandProbe.Incremented`

Audit and outbox rows carry the workspace and correlation ID. Audit snapshots
are captured where useful and remain protected by the append-only trigger.

Outbox event types are `CommandProbeCreated` and
`CommandProbeIncremented`, event version 1, aggregate type `CommandProbe`.
They are explicitly assigned to the `MobileSync` event stream. All existing
and non-mobile outbox creation paths default to `Internal`; internal events
are never exposed by the mobile cursor.
Deterministic camelCase payloads contain only:

- create: `probeId`, `name`, `counter`, `version`;
- increment: `probeId`, `delta`, `counter`, `version`.

The exact stored payload is returned through the cursor. No external event is
published during the transaction. A probe change, audit insert, outbox insert,
or idempotency completion failure rolls all four writes back.

Committed outbox event facts are immutable after insert: sequence, workspace,
event stream, event type/version, aggregate type/ID/version, payload,
correlation ID, and occurrence time. EF Core rejects tracked changes and a
PostgreSQL trigger rejects direct SQL updates. Delivery status, attempt count,
next-attempt time, processed time, and last error remain mutable for the
future publisher.

## Database and migration

Migration `AddCloudCommandAndEventCursorSpike`:

- creates `platform.command_probes`;
- adds database-generated identity `bigint`
  `platform.outbox_messages.sequence`;
- adds nullable integer
  `platform.idempotency_records.result_status_code`;
- adds workspace/name uniqueness and aggregate checks;
- adds unique sequence and initial `(workspace_id, sequence)` indexes;
- adds the completed-result status/payload constraint.

The follow-up migration `HardenCloudCommandAndEventCursorSpike` leaves that
already-applied migration unchanged and:

- adds controlled `Internal` and `MobileSync` event streams with a database
  check, defaulting all existing rows to `Internal`;
- replaces the cursor index with
  `(workspace_id, event_stream, sequence)`;
- adds the database trigger protecting immutable event facts;
- classifies legacy completed idempotency rows without the required stored
  probe-result structure as `Failed`, clears their result status as required
  by the constraint, and preserves their payload as evidence.

The identity supplies a global increasing order. Workspace subsequences can
have gaps because of other workspaces or rolled-back allocations. Sequence is
value-generated on add and read-only after save. Event-producing spike
transactions serialize sequence allocation through commit with a PostgreSQL
transaction lock, preventing a later cursor value from becoming visible before
an earlier one. Future event writers must use the same ordering contract or an
equivalent database-safe mechanism.

## Event cursor

`GET /api/v1/mobile/sync/events` accepts non-negative `after` (default 0) and
`limit` from 1 through 100 (default 50). A dedicated
`ICloudEventCursorReader` explicitly requires a current workspace below HTTP.
It reads only that workspace's `MobileSync` stream, uses `sequence > after`,
orders ascending, and fetches `limit + 1` to calculate `hasMore`.
`nextCursor` is the last returned sequence or the supplied cursor when the
page is empty.

The response excludes outbox delivery state and errors. Reads never update
delivery state. Committed events are recoverable before a future publishing
worker processes them. SignalR is deferred because it cannot replace this
durable recovery source; publishing, retention, and archival are also deferred.

## Temporary workspace and correlation context

Development and Testing requests require `X-TraderPro-Workspace-ID` in
canonical UUID `D` format. The workspace must exist before the scoped accessor
is bound. Missing, malformed, and unknown values fail closed; request bodies
cannot choose a workspace.

Both the cursor path and its trailing-slash form bind the middleware using
segment-safe matching. This header is temporary context, not authentication.
A new request scope prevents cross-request leakage. Probe and cursor reader
services also reject direct invocation without a current workspace instead of
depending on an empty EF query result. Existing filters and write interception
remain active. Authentication, authorization, trustworthy tenant resolution,
PostgreSQL RLS, and subscription enforcement remain mandatory pre-production
gates.

`X-Correlation-ID` is optional and, when supplied, must be one non-empty UUID
in canonical `D` format. Missing values become UUIDv7 IDs. The committed value
is returned in response metadata/header and stored in audit and outbox rows.

## API and stable errors

- `POST /api/v1/spikes/command-probes` creates and returns HTTP 201.
- `POST /api/v1/spikes/command-probes/{id}/increment` returns HTTP 200.
- `GET /api/v1/spikes/command-probes/{id}` reads within one workspace.
- `GET /api/v1/mobile/sync/events` reads ordered committed events.

Command responses contain `result` and `meta` with correlation and idempotency
status. Errors contain stable code, message, category, retryable flag, field
errors, safe details, and correlation metadata.

Missing bodies, malformed JSON, and invalid JSON value types are normalized to
HTTP 400 `REQUEST_BODY_INVALID`, category `Validation`, retryable false. JSON
parser details and stack traces are not returned.

Codes include `IDEMPOTENCY_KEY_REQUIRED`, `IDEMPOTENCY_KEY_INVALID`,
`IDEMPOTENCY_PAYLOAD_CONFLICT`, `IDEMPOTENCY_IN_PROGRESS`,
`IDEMPOTENCY_PREVIOUS_ATTEMPT_FAILED`, `REQUEST_BODY_INVALID`,
`WORKSPACE_CONTEXT_REQUIRED`, `WORKSPACE_CONTEXT_INVALID`,
`WORKSPACE_NOT_FOUND`, `COMMAND_PROBE_NOT_FOUND`,
`COMMAND_PROBE_NAME_INVALID`, `COMMAND_PROBE_DELTA_INVALID`,
`COMMAND_PROBE_EXPECTED_VERSION_REQUIRED`,
`COMMAND_PROBE_VERSION_CONFLICT`, `CORRELATION_ID_INVALID`,
`EVENT_CURSOR_AFTER_INVALID`, `EVENT_CURSOR_LIMIT_INVALID`, and
`TEMPORARY_COMMAND_FAILURE`.

## Verification and replacement

Run:

```powershell
powershell -ExecutionPolicy Bypass `
  -File .\scripts\test\test-cloud-command-spike.ps1
```

Tests cover aggregate/hash rules, fresh migrations, schema generation, API
commands/replays/conflicts, actual concurrency, failure-injection rollback,
workspace isolation, disabled routing, correlation, cursor resume/gaps, and
event-fact immutability with PostgreSQL 18 Testcontainers. The migration suite
also upgrades a database stopped at `HardenPlatformFoundationIntegrity` with
existing outbox and legacy idempotency rows, proving sequence backfill,
internal-stream classification, non-replayable legacy handling, and the final
constraints/indexes.

Authentication, device enrolment, subscriptions, RLS, Receiving Sessions,
mobile HTTP sync, SignalR, outbox publishing, retention, notifications, and
commercial API lifecycle remain deferred. Before commercial release, delete
the endpoints and `CommandProbe`, or replace them through a reviewed migration
and real commands preserving these transaction/idempotency/cursor contracts.
