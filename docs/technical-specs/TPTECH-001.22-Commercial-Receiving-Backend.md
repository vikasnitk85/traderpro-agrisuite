# TPTECH-001.22: Commercial Receiving Backend

- Status: Implemented for Task 7C1
- Date: 2026-08-01

## Scope and boundary

Task 7C1 implements production Commercial Receiving from authenticated Start
through `SubmittedForSettlementReview`. The module is isolated under
`Procurement/Receiving` and does not reuse the Task 6 POC. It creates no
settlement, Purchase Bill, payable, Inventory, Sales, Finance, or Production
effect.

All capture mutations (Start, Entry, Submit, heartbeat, and lease acquisition/
reacquisition) require the focused `TraderProOperator` policy in endpoint
metadata and again at the service boundary. Owners retain read-only list/live
monitoring, reference-policy management, ownership transfer, and safe
OwnerBroadcast access; Owner mutation attempts are forbidden before any
Session, Entry, ownership, audit, outbox, claim completion, or result write.

## Aggregate and snapshots

`CommercialReceivingSession` owns the mobile UUIDv7 aggregate ID, immutable
cloud reference and reference-policy version, authenticated Workspace/Company/
default Branch, Supplier/settings/destination/weight-policy/optional-vehicle
snapshots, exact sequence, count, total, and two-state lifecycle.
`CommercialReceivingEntry` is append-only and snapshots Product, Restricted
Supplier scope evidence, Bag Type, optional Product Standard Bag Weight, raw
text, exact processed/display values, source, and capture/acceptance times.
PostgreSQL stores exact weights as `numeric(20,6)` and rejects Entry update or
delete.

## Ownership and lease

One `CommercialReceivingOwnership` row stores durable editor Device,
generation, renewable lease, heartbeat/reacquisition/transfer evidence, and an
independent optimistic version. Generation begins at 1. The lease defaults to
60 minutes; mobile targets a 10-minute foreground heartbeat. Expiry preserves
ownership and physical facts. The same Device and generation may reacquire a
new UUIDv7 lease. Owner transfer requires a target active credentialed Device,
safe reason, expected Session version, and expected generation; it advances the
generation once, changes the durable editor, and clears the old lease. The
Owner receives no lease capability. The target authenticates as an Operator on
that Device and acquires a new lease without changing generation. Acquisition
is allowed only when transfer left the lease missing or the prior lease expired;
an active valid lease returns `RECEIVING_LEASE_STILL_ACTIVE`.

## Reference policy

One automatic company policy uses default `RCV-{SEQ:000000}`. Templates permit
exactly one `{SEQ:0...}` token and optional `{YYYY}`/`{MM}`. Reset policies are
`Never`, `CalendarYear`, and `Monthly`; required date tokens are validated.
Counters are company/policy/period scoped and PostgreSQL serialized. Sessions
store the numeric sequence, rendered immutable reference, and policy version.
Before the Session transaction, Start commits a durable reservation keyed by
the immutable operation ID and request hash under the same reference-series
lock. Exact retry reuses its reservation; a different operation cannot. A
failed Session transaction therefore leaves a documented gap and the next
Start receives the next number. Session creation consumes its one matching
reservation atomically. Starting-number restrictions consider both consumed
and reserved values; later format versions affect only future Sessions.

## Mobile operations and idempotency

`POST /api/v1/mobile/commercial-sync/operations` accepts 1-50 Start, Record,
and Submit operations. Every UUID claims the shared database scope
`Procurement.CommercialReceiving.MobileSyncOperation`; actual type,
authenticated Device/context, Session, sequence, generation, payload hash, and
exact UTF-8 payload bytes are canonical hash facts. Renewable lease metadata,
cloud version, correlation, and token identity are excluded. Each operation
commits independently; a failure blocks later operations for its aggregate but
not unrelated aggregates. Reversible master/lease/generation conditions return
`NeedsAttention` without aggregate, Entry, audit, commercial outbox, completed
idempotency result, or business mutation.

`sync.commercial_receiving_operation_claims` durably claims every operation ID
before business execution. The claim stores Workspace/Company/common scope,
operation type, Session, authenticated Device, applicable generation, canonical
request hash, first-seen time, and state. `NeedsAttention` commits that identity
so an exact request can retry after resolution; payload, type, or Device reuse
conflicts. Permanent rejected payloads remain bound, and successful retry moves
the claim to completed exactly once. No process-local lock is authoritative.

Start validates exact current settings/defaults and master versions before
snapshotting and atomically assigns reference, editor, generation, lease,
audit, two audience events, and stored result. Entry reprocesses unchanged raw
text with Task 3, requires exact processed/display equality and positive
weight/bags, and advances exact count/total/version once. Submit requires exact
sequence, editor/generation/live lease, at least one Entry and a positive total;
it freezes the Session and clears lease capability.

Start takes deterministic PostgreSQL share locks on Supplier, Procurement
Settings, destination, Weight Policy, optional Vehicle, and reference policy;
Entry similarly locks Product, required Supplier Product Scope, Bag Type,
optional standard, Session, and ownership. Same-company composite foreign keys
and `BEFORE INSERT` snapshot triggers reject fictitious, cross-company, stale,
or inconsistent direct-SQL snapshots. A mutation that commits first produces
`NeedsAttention`; a mutation arriving after Receiving validation waits, and
historical snapshots remain unchanged.

## Synchronization and reads

`CommercialMobileSync` is distinct from `Internal` and POC `MobileSync`.
Commercial rows have a one-to-one immutable
`platform.commercial_outbox_audiences` companion fact requiring Company and
either `OwnerBroadcast` or `TargetDevice`. Deferred constraint triggers require
exactly one audience for every `CommercialMobileSync` row by commit, verify
Workspace/company equality and shape, prohibit audience rows on Internal/POC
streams, and make audience facts immutable. This leaves the base outbox schema
backward compatible during upgrade. Event-producing transactions take a
Workspace/Company scoped commit-order lock. The authenticated opaque cursor returns only
Owner broadcasts for Owners and TargetDevice events for the authenticated
Device.

`OwnerBroadcast` and `TargetDevice` are immutable issuance audiences. Event
reads revalidate active authenticated Device/Workspace/Company/role but do not
revalidate current Session editor/generation. This permits delivery of the
old-Device transfer-away event. Current editor/generation still controls every
mutation and subsequent Receiving events target only the new editor.

`sync.commercial_master_changes` is an immutable safe log for all Task 7B1/7B2
Receiving masters. The migration deterministically backfills Active and
Inactive rows; table triggers append future changes under a company lock.
Bootstrap captures high-water `H`, pages through `H`, then continues strictly
after it. Backfill and triggers call the same explicit `contractVersion: 1`,
camel-case builder for each of the ten master types. Exact decimals are
canonical six-decimal strings; Supplier protected and persistence-only fields,
plus raw Internal events, are excluded.

Owner list/live view reads all company Sessions. Operators read only Sessions
whose current ownership points to their authenticated Device; unrelated live
views return not found. `lastCloudUpdateUtc` is the later of Session and
ownership update time, so heartbeat and reacquisition are visible. Reference-
policy audits use aggregate type
`Procurement.CommercialReceivingReferencePolicy`. Queries do not mutate
delivery or business state.

## Persistence and migration

The immutable migration `AddCommercialReceivingBackend` creates reference
policies/counters, Sessions, ownerships, Entries, master changes, commercial
audiences, defaults, backfill, and initial PostgreSQL guards. The reviewed
follow-up `HardenCommercialReceivingBackendContracts` adds durable operation
claims and reference reservations, deterministic snapshot protection, deferred
audience integrity, exact Session/ownership transition guards, and explicit
versioned master payload builders. Upgrade coverage starts exactly at
`HardenCommercialSupplierProductCatalogContracts` with existing Identity, POC,
master, and old outbox data. API startup applies neither migration.

## Task 7C2 dependency

Task 7C2 must implement Task 7A mobile authentication, secure secret storage,
an encrypted production SQLite database, immutable operation persistence,
lease enrichment outside payload hashes, and the frozen operation/event/master
cursor contracts. No Dart source changes are part of Task 7C1.

## Task 7C2A contract freeze

Canonical paths are the committed endpoint paths:

```text
POST /api/v1/auth/device-activations/redeem
POST /api/v1/auth/login
POST /api/v1/auth/refresh
POST /api/v1/auth/logout
POST /api/v1/auth/logout-all
GET  /api/v1/auth/me
POST /api/v1/mobile/commercial-sync/operations
GET  /api/v1/mobile/commercial-sync/events?cursor=<opaque>&limit=<1-100>
GET  /api/v1/mobile/commercial-sync/masters?cursor=<opaque>&limit=<1-100>
GET  /api/v1/procurement/receiving-sessions
GET  /api/v1/procurement/receiving-sessions/{id}/live-view
POST /api/v1/procurement/receiving-sessions/{id}/lease/heartbeat
POST /api/v1/procurement/receiving-sessions/{id}/lease/reacquire
POST /api/v1/procurement/receiving-sessions/{id}/ownership/transfer
GET  /api/v1/procurement/receiving-reference-policy
PUT  /api/v1/procurement/receiving-reference-policy
```

Operation statuses are exactly `Accepted`, `PreviouslyProcessed`,
`NeedsAttention`, and `Rejected`. Blocked followers use
`RECEIVING_OPERATION_WAITING_FOR_PRIOR_SEQUENCE`; expired owners use
`RECEIVING_LEASE_REACQUISITION_REQUIRED`; stale generation is
`NeedsAttention`. The implemented default reference is
`RCV-{SEQ:000000}`/`Never`/1. Task 7C2 capture emits `Manual` and no POC source.

A securely bound authenticated Operator may create a local Session and capture
immutable Entries offline using valid cached Active master/settings revisions.
Start is sequence 1; dependent operations are not transmitted until Start
returns official reference, generation, and lease. Start rejection preserves
all physical facts in attention without rewriting. Owner monitoring stays
read-only; transfer UI, voice, BLE/scale, settlement/posting, cancellation,
correction, reopen, and SignalR are deferred.

## Finalization migration contract

`20260802120000_FinalizeCommercialReceivingBackendContracts` replaces the
claim, snapshot-validation, and ownership-state functions without changing the
two earlier migration files. Snapshot functions lock reference reservation and
policy, Supplier, settings, destination, Weight Policy, optional Vehicle,
Session, Ownership, Product, optional Supplier scope, Bag Type, and optional
standard weight before comparison. Disabled vehicle mode requires every vehicle
identity/snapshot column to be null. Operation claim/session and reservation
identity columns have UUIDv7 checks.

Legacy Start and Entry claims are reconstructed only from matching completed
idempotency and audit facts. Exact retries return `PreviouslyProcessed`; unsafe
identities become terminal
`COMMERCIAL_MOBILE_OPERATION_LEGACY_NON_REPLAYABLE` facts and cannot reserve a
second reference. Claim completion is valid only with matching completed
idempotency facts. Monitoring uses the later Session/Ownership update and the
explicit acquisition/reacquisition attention states.

The final migration `Down()` always throws. Task 7C1 schema rollback is an
application rollback plus backup restore; EF migration downgrade is unsupported.

## Reference-contract seal

`20260802130000_SealCommercialReceivingReferenceContracts` adds one
reservation per workspace/company/Session and retains immutable reservation
policy/period/sequence/rendered-reference facts. The Session trigger validates
claim ownership and counter issuance but deliberately does not compare the
current reference-policy version to the reservation snapshot. Thus an exact
retry after a policy update uses the original reference and a new Start uses
the updated format.

Typed Start deserialization and structural validation occur after claim but
before reservation. Malformed structure, missing or non-UUIDv7 identities,
non-positive master versions, non-UTC timestamps, invalid external references,
or identity/sequence mismatch reject the claim without a reservation, counter
advance, Session, audit, event, or idempotency completion. The migration and
all preceding Task 7C1 migrations remain forward-only.
