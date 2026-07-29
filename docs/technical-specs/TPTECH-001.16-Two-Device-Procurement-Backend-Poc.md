# TPTECH-001.16: Two-Device Procurement Backend POC

## Objective and explicit scope

Task 6A is a development/testing-only backend proof that Mobile A can start,
exclusively edit, synchronize five ordered receiving weights, and submit a POC
session while Mobile B monitors it read-only, approves it, and idempotently
finalizes it.

Every implemented type is suffixed `Poc`. The completion record is not a
Purchase Bill or commercial Procurement document. The workflow creates no
Inventory movement, supplier payable, settlement, Sales record, journal, or
financial posting.

## Feature gate and temporary context

Both flags must be true in Development or Testing:

```text
TraderPro:Spikes:Enabled
TraderPro:Spikes:ProcurementPoc:Enabled
```

Committed values are false. Production never maps the endpoints. Lease length
comes from `TraderPro:Spikes:ProcurementPoc:LeaseMinutes`, default 5, and is
validated only while the POC is enabled.

When the Procurement POC flag is false, its spike routes and
`/api/v1/mobile/sync/operations` are not mapped and return 404 before temporary
workspace or device context is evaluated. The Task 5
`/api/v1/mobile/sync/events` route remains mapped under the general spike flag
and retains its original workspace-only context behavior.

POC requests use canonical UUID headers
`X-TraderPro-Workspace-ID` and `X-TraderPro-Device-ID`. The workspace and
device must exist, the device must be Active, and ownership must match. The
scope is new per request and fails closed. These temporary headers are not
authentication or authorization.

## Aggregate and PostgreSQL tables

The `procurement` schema contains:

| Table | Purpose |
| --- | --- |
| `receiving_session_pocs` | Leased aggregate, exact projection, status, and PostgreSQL-generated cloud reference |
| `receiving_entry_pocs` | Immutable accepted physical facts and captured weight policy |
| `receiving_finalization_pocs` | One immutable non-posting completion snapshot |

`ReceivingSessionPoc` starts at version 1, count 0, total `0.000000`, and next
sequence 2. Supported states are `ReceivingInProgress`,
`SubmittedForReview`, `Approved`, and `Finalized`.

The mobile UUIDv7 is the session primary key. PostgreSQL generates a globally
unique bigint reference sequence formatted `RS-POC-000001`. This sequence is
not the production official-number service.

Composite foreign keys enforce workspace ownership for sessions, entries,
devices, approvers, finalizers, and finalizations. Unique indexes protect
workspace/operation and workspace/session/sequence, and allow one finalization
per session. Checks protect status, versions, positive sequences/counts,
precision 1-3, controlled method/source values, lifecycle shape, and
non-negative totals. PostgreSQL triggers reject entry/finalization update or
delete and session workspace/editor changes or physical session deletion. The
follow-up `HardenTwoDeviceProcurementPocContracts` migration requires:

- `ReceivingInProgress` to have all lease fields and no submission or approval;
- `SubmittedForReview` to have submission, no lease, and no approval;
- `Approved` and `Finalized` to have submission and approval with no lease.

EF configuration also marks workspace and editor identity immutable after
insert.

## Mobile operation endpoint and ordering

`POST /api/v1/mobile/sync/operations` accepts 1-50 operations and processes
them in request order. Supported types are:

1. `StartReceivingSession` at sequence 1.
2. `RecordReceivingEntry` at the aggregate's exact next sequence.
3. `SubmitReceivingSession` at the exact next sequence.

The request envelope carries optional `leaseId` transport metadata beside the
immutable Task 4 fields. Start rejects lease metadata. Record and submit require
it. Their typed payloads contain no lease. The exact Task 4
`RecordReceivingEntry` `payloadJson` and `payloadHash` therefore pass to Task 6A
unchanged, including nullable `cloudSessionId`. A supplied cloud session ID
must be one canonical UUID equal to the aggregate ID.

Task 6B will read the current lease from local session state and enrich only the
network envelope. It will not rewrite the immutable Drift outbox payload.

The endpoint verifies SHA-256 over the exact UTF-8 `payloadJson` string before
typed parsing. It then hashes canonical typed values, including authoritative
current device, actual operation type, and envelope lease when applicable. All
mobile operations share command type
`Procurement.Poc.MobileSyncOperation`, so one operation UUID has one global
mobile identity. Reusing it for another operation type conflicts.

Only operation/aggregate UUIDs and positive sequence are structural batch
preflight. Type, payload presence, hash, typed JSON, lease, version, weight,
and business validation run inside the ordered loop. Each operation commits in
its own transaction. Earlier accepted work survives a later malformed or
rejected operation. One failure blocks later work for that aggregate only;
other aggregates continue.

Results are `Accepted`, `PreviouslyProcessed`, `NeedsAttention`, or `Rejected`.
Lower reused sequences with a new operation ID conflict; higher sequences are
gaps. Optional expected cloud versions must match when supplied. Identical
retries return the stored original result, while changed typed payloads return
`IDEMPOTENCY_PAYLOAD_CONFLICT`. Because current device ID is part of every POC
command hash, another device cannot replay a successful start, entry,
heartbeat, approval, or finalization and receive its stored result or lease.

## Weight revalidation

Entries preserve the raw decimal string unchanged. The server invokes the Task
3 C# `WeightProcessor` with the submitted decimal places and method. Submitted
canonical six-decimal processed text and display text must exactly match the
server result. A mismatch returns
`RECEIVING_POC_WEIGHT_PROCESSING_MISMATCH` without entry, projection, audit,
outbox, version, or completed-idempotency changes. Stored processed values and
session totals use `numeric(20,6)` and .NET `decimal`, never `double`.
Exceeding total `numeric(20,6)` capacity returns
`RECEIVING_POC_TOTAL_WEIGHT_EXCEEDED` with the same all-or-nothing behavior.

Product and bag type are bounded temporary POC strings. No master-data entity
is introduced.

## Lease and device separation

Start validates the active current device, assigns it as editor, creates a
UUIDv7 lease, and sets expiry from the server clock. The explicit heartbeat
renews only a matching active lease for the editor. Heartbeat is idempotent and
does not add routine audit or MobileSync noise.

Record and submit require the editor, lease ID, non-expired lease, and
`ReceivingInProgress`. Mobile B can read but receives
`RECEIVING_POC_EDITOR_DEVICE_MISMATCH` when writing. Forced recovery is
deferred.

## Read-only monitoring

`GET .../{id}/live-view` returns reference, state, editor, lease expiry, exact
count/total, version, last cloud update, and at most five recent entries. The
list endpoint supports optional exact status plus reference-sequence cursor
pagination. Both are workspace scoped and do not mutate session, outbox
delivery state, or cursor state. Cross-workspace guessed IDs return 404.

The existing `GET /api/v1/mobile/sync/events` cursor remains the durable
recovery source and exposes only committed `MobileSync` payloads. The
`ReceivingSessionPocStarted` event deliberately omits `leaseId`; lease
capability data is returned only in the direct start-operation response to the
requesting device.

## Submission, approval, and finalization

Submission requires at least one accepted entry, records UTC submission,
clears lease/heartbeat/expiry, transitions to `SubmittedForReview`, and emits
one audit/event.

Approval requires a positive expected version and a different active device
from the original editor. It records device/time and transitions to `Approved`.

Finalization requires `Approved`, positive expected version, and a different
device from the editor. It copies count/total into one
`ReceivingFinalizationPoc`, transitions to `Finalized`, and emits one
audit/event. Identical concurrent requests use the shared PostgreSQL
idempotency transaction and create one result. A different key after
finalization returns `RECEIVING_POC_ALREADY_FINALIZED`.

## Transaction, audit, and event contract

The shared Task 5/6A executor takes a transaction-scoped advisory lock for
workspace, command type, and key, plus the existing MobileSync commit-order
lock. Aggregate change, immutable entry/finalization, one material audit, one
captured outbox payload, and completed idempotency result commit atomically.
Database failure rolls back all of them. No process-local correctness lock is
used.

Event order is:

```text
ReceivingSessionPocStarted
ReceivingEntryPocAccepted (one per entry)
ReceivingSessionPocSubmitted
ReceivingSessionPocApproved
ReceivingSessionPocFinalized
```

Audit actions use the current workspace/device and are created only for first
successful execution. Heartbeats are intentionally quiet.

## Insecure development bootstrap

`POST /api/v1/spikes/procurement-poc/bootstrap` idempotently creates a stable
POC workspace, company, default branch, active operator device, and active
owner device. It returns IDs only, creates no credentials, is not production
onboarding, and is unavailable when the feature gate is off or in Production.

## Verification

Run:

```powershell
powershell -ExecutionPolicy Bypass `
  -File .\scripts\test\test-two-device-procurement-backend-poc.ps1
```

Focused unit tests cover states, sequences, leases, exact totals, capacity,
device separation, weight facts, canonical hashes, and finalization invariants.
PostgreSQL 18 Testcontainers/API tests cover fresh/upgrade/follow-up migration,
bootstrap, feature-disabled 404 routing and Task 5 cursor regression, two
independent clients, five entries, exact Task 4 payload compatibility,
same-/cross-device replay, operation-type identity conflict, mixed-aggregate
batch continuation, stale approval, different-key post-finalization conflict,
payload/sequence/weight/capacity errors, deterministic lease time, event lease
omission, live view, cursor paging/order, concurrency, rollback triggers,
session update/delete/state controls, and workspace/device isolation.

## Removal and evolution plan

Before commercial Procurement:

- replace temporary headers and bootstrap with authenticated, authorized device
  and workspace resolution;
- add PostgreSQL RLS and subscription enforcement;
- replace temporary product/bag strings and POC reference sequencing;
- review recovery, correction, cancellation, retention, and lease transfer;
- connect the Task 6B Flutter Drift outbox and real devices;
- optionally add SignalR only as a latency layer over durable cursor recovery;
- introduce Inventory, supplier obligations, settlement, documents, Sales, and
  Finance only through their own reviewed atomic posting design;
- remove or migrate all `Poc` endpoints, entities, and tables before shipping.

Flutter HTTP sync, real-device restart tests, SignalR, authentication, RLS,
subscriptions, commercial masters, Inventory, Sales, Finance, Purchase Bills,
and production outbox publishing are explicitly deferred.
