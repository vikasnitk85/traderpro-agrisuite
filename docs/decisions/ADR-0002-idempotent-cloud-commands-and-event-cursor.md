# ADR-0002: Idempotent cloud commands and event cursor

- Status: Accepted for the Task 5 platform spike
- Date: 2026-07-29
- Scope: Platform command proof of concept, not a production business API

## Context

Mobile clients can lose an HTTP response after the cloud has committed a
command. Networks can also deliver retries concurrently. A command therefore
needs a stable result even when a caller cannot know whether the first request
committed. Separately, mobile recovery needs an ordered source of committed
events that does not depend on transient real-time delivery.

Process-local locks cannot provide correctness across API replicas, process
restarts, or concurrent workers. UUID order and device timestamps are not a
safe event cursor.

## Decision

TraderPro uses a database-backed idempotency record scoped by workspace,
command type, and idempotency key. The command pipeline canonicalizes typed
business values, hashes them with SHA-256, and takes a transaction-scoped
PostgreSQL advisory lock for the scoped key. The existing unique database
index remains the final claim invariant.

The first request commits its aggregate change, one audit event, one outbox
message, and a completed idempotency record in one PostgreSQL transaction. The
record stores both the successful response data and its HTTP status. An
identical retry returns that stored outcome; a changed hash returns
`IDEMPOTENCY_PAYLOAD_CONFLICT`. A rolled-back transaction leaves no completed
claim.

A completed record is replayable only when its payload has the required stored
result structure. The hardening migration classifies unknown legacy
completions as `Failed` while preserving their payload evidence. An existing
failed or otherwise non-replayable record returns
`IDEMPOTENCY_PREVIOUS_ATTEMPT_FAILED` and requires a new caller key; it is
never treated as a successful replay.

Outbox messages receive a PostgreSQL-generated, globally increasing `bigint`
sequence and a controlled event stream. Existing and ordinary outbox records
default to `Internal`; mobile-recoverable probe events are explicitly
`MobileSync`. Workspace event reads select only committed `MobileSync` rows
with `sequence > after`, ordered by sequence. A workspace sees a strictly
increasing subsequence, so gaps from other workspaces, internal events, and
rolled-back sequence allocations are expected.

Event-producing spike transactions also hold one transaction-scoped database
commit-order lock before adding their outbox row. This prevents a transaction
with a later allocated sequence from committing and becoming cursor-visible
before an earlier sequence. Future event writers must share this ordering
contract or replace it with an equivalently safe database design.

The cursor reads committed outbox event records even when a future publisher
has not delivered them. It returns the payload captured when the command
committed and never reconstructs an event from later aggregate state.

Outbox event facts are immutable after insert at both the EF model and
PostgreSQL trigger layers. Only delivery metadata remains mutable. The mobile
cursor has its own Application abstraction and Infrastructure implementation,
and both it and the probe reader require workspace context directly below the
HTTP boundary.

## Consequences

- Database serialization, not API-process memory, governs identical command
  concurrency.
- Persisting the original result makes a lost-response retry deterministic.
- Unknown legacy completion placeholders cannot become false successful
  replays.
- Internal integration events cannot leak into the mobile sync contract.
- Event payload and identity facts cannot drift while publisher delivery state
  can still advance.
- Optimistic aggregate concurrency remains enabled in addition to the explicit
  expected-version check.
- SignalR may later reduce latency, but it is not a recovery source and cannot
  advance the durable cursor.
- External publishing remains deferred to a future outbox worker and never
  occurs inside the command transaction.
- The current cursor reads the outbox table directly. Retention, archival,
  replay windows, publisher ownership, poison-message policy, and cursor
  compatibility must be reviewed with the future worker.
- The temporary workspace header is not authentication. Authentication,
  authorization, trustworthy request-to-workspace resolution, and PostgreSQL
  RLS remain mandatory pre-production gates.

## Alternatives rejected

- A process-local lock does not coordinate multiple processes or survive a
  restart.
- Returning a newly constructed response on retry can drift from the committed
  response and cannot preserve the original status.
- UUID or timestamp ordering does not provide a stable, strictly increasing
  recovery cursor.
- SignalR-only recovery loses events while a client is disconnected.
