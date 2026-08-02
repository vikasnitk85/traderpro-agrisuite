# Task 7C1: Commercial Receiving Backend

- Status: Implemented
- Date: 2026-08-01
- Scope: Production backend from commercial Start through submission for settlement review

## Status

Implemented as a separate authenticated production Receiving boundary through
`SubmittedForSettlementReview`. Settlement and posting remain outside scope.

## Outcome

Implement the Task 7C0 production contracts behind Task 7A authenticated
commercial context, using new production entities/tables/routes and preserving
atomic idempotent command behavior. The milestone ends when a Session is
`SubmittedForSettlementReview` and exposes no settlement or posting effects.

## Scope

Domain/Application/Infrastructure/API artifacts, reviewed PostgreSQL migration,
aggregate and ownership persistence, cloud reference allocation, authenticated
operation/event/master sync, Owner list/live view, submission, approved
ownership recovery/transfer only where source decisions exist, and focused
tests.

## Non-goals

No Flutter work, Purchase Settlement, bag/empty-bag/DoublePlastic deduction,
Purchase Bill, supplier payable, Inventory, Sales, Finance, official purchase
finalization, POC refactor/promotion, package addition, or speculative open-
question behavior.

## Authoritative sources

- `AGENTS.md`
- TPRC-101 baseline/open questions
- TPTECH-001.21
- ADR-0001 through ADR-0009
- TPSEC-001
- Existing Task 3, 5, 7A, 7B1, and 7B2 production foundations

## Entry gates

- Branch and expected baseline are reviewed for the implementation task.
- OQ-01 is resolved with configurable automatic numbering and default
  `RCV-{SEQ:000000}`.
- OQ-06 is resolved with positive Entry facts and a positive, non-empty
  submission requirement.
- OQ-08 is resolved with a configurable 60-minute lease and 10-minute
  foreground heartbeat target.
- Different-device transfer is Owner-only, requires a safe reason and expected
  Session/generation, and is audited and evented atomically.
- PostgreSQL RLS remains outside Task 7C1 scope; workspace/company ownership is
  enforced by authenticated context, query filters, foreign keys, locks, and
  database constraints/triggers.

## Backlog

| ID | Artifact/work | Acceptance criteria | Dependencies |
| --- | --- | --- | --- |
| 7C1-01 | Domain lifecycle and snapshot value objects | New `CommercialReceivingSession`, immutable `CommercialReceivingEntry`, and `CommercialReceivingOwnership` implement exactly the two states and fields in TPTECH-001.21, including Procurement Settings ID/version and vehicle-mode snapshots; Domain has no API/EF/Npgsql dependency; no POC type is renamed/reused. | TPTECH-001.21; resolved OQ-06. |
| 7C1-02 | Domain ownership/generation rules | Start assigns authenticated editor/generation 1; same-device reacquisition preserves generation; transfer increments once; submission closes lease; stale Device/generation errors are stable; race tests cover the supported transitions. | ADR-0008; focused OQ-04 Owner-transfer resolution. |
| 7C1-03 | Application operation contracts | Define versioned Start/Record/Submit envelopes/results, typed payload parsers, max batch 50, exact decimal strings, and hash verification. Start requires null/absent generation, expected cloud version, and lease; Record/Submit require generation and lease transport metadata and null/absent expected cloud version. Canonical hashing includes actual type/authenticated Device and excludes mutable cloud version, lease, and token metadata. | Task 3; Task 7A; TPTECH-001.21. |
| 7C1-04 | Application query/monitor contracts | Define company-scoped Session list and live-view results with safe snapshots, exact totals, recent Entries, editor/generation/lease health, attention, last update, and no mutation controls. | 7C1-01. |
| 7C1-05 | Reviewed production migration | Create new `commercial_receiving_sessions` with Procurement Settings snapshots, Entries, ownership, reference allocator, idempotency/sequence constraints, state/immutability triggers, and `sync.commercial_master_changes`. Deterministically backfill one safe current row for every existing 7B1/7B2 Active/Inactive master, association, and settings record. POC tables/data and prior migrations are unchanged. | 7C1-01/02; Task 7B1/7B2; approved naming/schema review. |
| 7C1-06 | Event audience migration | Add controlled `CommercialMobileSync` stream with required audience kind/company metadata and Device target required only for editor events; preserve `Internal` and POC `MobileSync`; scope commit-order locking by stream contract + Workspace + Company, not globally. | ADR-0009; outbox migration review. |
| 7C1-07 | Cloud reference allocator/renderer | PostgreSQL serializes one company/series allocation per accepted Start; stores numeric value, opaque rendered value, and format version once; replay returns same reference; concurrent Starts remain unique; no POC/Purchase Bill/final-posting format is reused. | 7C1-05; OQ-01. |
| 7C1-08 | Master validation/snapshot service | Start requires the exact Procurement Settings ID/version, snapshots authoritative mode, and accepts only that revision's configured default destination and Weight Policy; no alternate route/flag/payload behavior until OQ-11. Entry validates Product/scope/Bag Type/standard. Stale settings/master is `NeedsAttention` without substitution. | Task 7B1/7B2; 7C1-03; OQ-11 for any override. |
| 7C1-09 | Exact Entry acceptance | Reprocess unchanged raw string using captured Session Task 3 policy; require exact processed/display match; store `numeric(20,6)` and deterministic display text; exact total overflow rolls back Entry, projection, audit, event, and result. | Task 3; 7C1-01/05/08; OQ-06. |
| 7C1-10 | PostgreSQL command orchestration | Extend/reuse the focused idempotent executor. All three mobile types claim `Procurement.CommercialReceiving.MobileSyncOperation`; actual type is hashed and one UUID reused as another type returns `IDEMPOTENCY_PAYLOAD_CONFLICT` without execution. Aggregate/Entry, audit, audience-specific commercial outbox, and result commit atomically; deterministic replay returns the original result. | Task 5 executor; 7C1-03/05/06. |
| 7C1-11 | Ordered operation endpoint | Map `POST /api/v1/mobile/commercial-sync/operations` with Task 7A authority. Concurrency uses authenticated Device + generation + exact sequence + Session status + aggregate/ownership lock, never expected cloud version or process lock. Malformed requests write nothing; a per-op failure blocks only that Session and unrelated Sessions continue. | 7C1-03/08/09/10. |
| 7C1-12 | Lease renewal and same-device reacquisition | Add authenticated direct commands/routes with server time, PostgreSQL serialization, stable lease errors, no ordered-operation payload rewrite, and replay tests. Routine heartbeat is not audited; material reacquisition is audited. | 7C1-02/05/10; resolved OQ-08. |
| 7C1-13 | Different-device Owner transfer | Owner authority, expected Session version and ownership generation, mandatory safe reason, idempotency, generation increment, old lease invalidation, audit, and old/new/Owner audience events commit in one transaction; no physical payload mutation. | Focused OQ-04 resolution; 7C1-02/10/12. |
| 7C1-14 | Submission | Exact next sequence/editor/generation/lease validation; approved entry-minimum rule; transition to `SubmittedForSettlementReview`; clear edit lease/capability; no later Entry; immutable snapshot/state-shape DB enforcement; audit/event/result atomic. | 7C1-01/02/09/10; OQ-06. |
| 7C1-15 | Authenticated commercial event cursor | Map `GET /api/v1/mobile/commercial-sync/events`; Owners read safe company broadcasts, and any authenticated active editor Device reads rows targeted to it; Operators have no broader entitlement. Prove unrelated same-company Operators cannot read, cursor gaps are valid, reads do not mutate delivery, and all three streams remain mutually exclusive. | 7C1-06/10; OQ-12 for broader Operator visibility. |
| 7C1-16 | Receiving event payloads | Emit only four version-1 event types with safe snapshots from TPTECH-001.21; no Supplier protected data, tokens, Device secrets, lease ID, or Internal payload; event is captured at commit and not reconstructed. | 7C1-06/09/10/14; TPSEC-001. |
| 7C1-17 | Dedicated commercial master sync | Implement only the migration-backed change-log protocol: deterministic safe backfill of every existing 7B1/7B2 master/association/settings row, Active and Inactive; first bootstrap captures scoped `H`, pages unique rows through `H`, then deltas strictly after `H`; future mutations append atomically under the scoped master-stream lock. No raw `Internal` data. | Task 7B1/7B2; ADR-0009; 7C1-05. |
| 7C1-18 | Owner list/live-view routes | Owner-authenticated read-only routes return active/submitted Sessions, safe header snapshots, exact totals, recent Entries, editor/generation/lease health, attention/submission state; queries mutate no Session/outbox/cursor; guessed cross-company IDs do not disclose existence. | 7C1-04/05. |
| 7C1-19 | Audit, observability, and redaction | Material Start/Entry/Submit/reacquisition/transfer actions use authenticated actor/correlation; safe diagnostics expose stable codes/counts only; logs/audits/events contain no credential, lease capability, Supplier protected fields, raw SQL, or stack trace. | TPSEC-001; 7C1-10/12/13/14. |
| 7C1-20 | Unit/architecture tests | Cover lifecycle, settings snapshots/default-only validation, operation field matrix, shared-scope/type-conflict hashing, exact weight, ownership errors/races, safe audience serializers, and dependency direction. No test claims settlement/posting. | 7C1-01 through 7C1-19. |
| 7C1-21 | PostgreSQL 18/API/concurrency/failure tests | Start from an upgrade database containing existing 7B1/7B2 Active/Inactive masters, associations, and settings; prove complete safe deterministic backfill, no raw Internal data, bootstrap/delta concurrency with no skip/duplicate, and cursor after `H`. Also prove same-company event commit order, different-company valid gaps, Owner/editor-only visibility, sequence/status/generation locks, replay, rollback, immutability, and references. PostgreSQL RLS remains an explicit non-goal. | 7C1-05 through 7C1-19; Docker/Testcontainers. |
| 7C1-22 | Contract/runbook and feature readiness | Update API contract/docs and safe local test runbook; production configuration has no POC flags/credentials/fallback; migration remains explicit; cancellation/correction remain absent while the resolved Owner transfer route is documented. | All prior items. |

## Data ownership

All production tables are Workspace/Company scoped, Sessions are authenticated
default-Branch scoped, and Device/User/role authority comes only from current
commercial context. Masters remain owned by existing modules. Receiving stores
snapshots. Outbox/audit are Platform infrastructure but commit with the
Procurement command.

## Transaction boundaries

Start, Entry, Submit, lease reacquisition, and approved transfer each commit
their full state/audit/commercial-event/idempotent result atomically. Master
change-log rows join the relevant master transaction. Commercial event sequence
allocation holds the stream-contract/Workspace/Company lock through commit;
master mutations use their separately named scoped stream lock. Cursor/list
reads perform no server mutation. External delivery remains outside the command
transaction.

Every mobile operation first commits its immutable durable claim. Start also
commits its non-reusable reference reservation before aggregate execution; the
Session transaction consumes that reservation or leaves an intentional gap.
Receiving holds deterministic share locks on validated masters through commit,
and transfer advances Session and ownership together while clearing the lease.

## Failure behavior

Rollback leaves no partial aggregate, Entry, projection, audit, event, or
completed result. `NeedsAttention` commits the safe durable claim but never
rewrites/consumes an immutable local payload; exact retry is permitted while
changed/type/Device reuse conflicts. A failed Start retains its reservation and
another operation receives the next number. Stale generation/wrong Device fail
without takeover. Response-loss replay returns the original stored success.

## Security implications

Every route uses endpoint metadata and database-revalidated context. Capture
mutations require Operator at both boundaries; Owner remains read-only except
for policy management and explicit transfer. New tables use tenant constraints,
foreign keys, locks, and triggers; PostgreSQL RLS is outside this task.
Event/master payload allowlists and negative isolation tests are release gates.
HTTPS and Task 7A configuration remain mandatory.

## Migration implications

Keep the applied `AddCommercialReceivingBackend` migration unchanged and add
only the reviewed `HardenCommercialReceivingBackendContracts` follow-up; do not
run migrations at host startup or touch POC tables. Upgrade tests must begin at
the Task 7B2 baseline populated with existing Active and Inactive commercial
masters, associations, and Procurement Settings. They verify deterministic safe
change-log backfill, then concurrent bootstrap/delta behavior, while all old
identity/master/POC/outbox rows remain unchanged and raw `Internal` payloads
remain outside the mobile log.

## Future implementation dependencies

Task 7C2 depends on the final operation/event/master/OpenAPI contracts and a
testable backend. Settlement/posting work depends on separate product and
atomic-posting decisions.

## Unresolved questions

OQ-01, OQ-06, OQ-08, and the focused Owner-transfer authority are resolved for
Task 7C1. OQ-11 and OQ-12 gate only default-
override and non-editor Operator visibility behavior, which remain absent.
Cancellation, correction, additional headers, stale-master recovery, and old-
generation fact recovery also remain absent unless separately approved.

## Exit criteria

- All acceptance criteria above pass on PostgreSQL 18.
- Rollback coverage proves transfer/submission/audience failures leave no
  partial state; deterministic master races cover Supplier, Vehicle, Product,
  scope, Bag Type, and standard.
- The exact Task 7B2-with-data upgrade preserves Identity, POC, masters, and old
  outbox rows while producing safe Active/Inactive version-1 backfill.
- No POC type/table/header/reference/cursor is used as commercial authority.
- No Purchase Bill, payable, Inventory, Sales, Finance, or finalization effect
  exists.
- Task 7C2 receives frozen versioned JSON/OpenAPI/error/event/master contracts.

## Applied corrective migrations

`20260802120000_FinalizeCommercialReceivingBackendContracts` follows the
original and hardening migrations. It provides recursive
payload capability rejection, retryable waiting results without durable
claims, Operation-scoped claim serialization and terminal guards, locked
Session/Entry snapshot validation, Disabled-vehicle enforcement, the exact
ownership state machine and transfer target revalidation, original-schema
legacy reconstruction/non-replayable classification, monitoring timestamp and
attention corrections, and UUIDv7 claim/reservation checks.

The migration is forward-only: its `Down()` throws `NotSupportedException`.
Application rollback plus database restore is the supported recovery path;
EF downgrade to an older migration is not supported. The original and first
hardening migrations remain byte-for-byte unchanged.

`20260802130000_SealCommercialReceivingReferenceContracts` is the reviewed
forward-only reference seal. It adds workspace/company/Session reservation
uniqueness, preserves historical reservation format/version facts across live
policy updates, validates typed Start structure before allocation, and aligns
application and trigger locks. Canonical Start order is Operation claim,
company reference series, reservation/policy/counter, Session/defaults,
Supplier, settings, destination, Weight Policy, optional Vehicle. Canonical
Entry order is Operation claim, Session, Ownership, Product, optional scope,
Bag Type, optional standard weight.
