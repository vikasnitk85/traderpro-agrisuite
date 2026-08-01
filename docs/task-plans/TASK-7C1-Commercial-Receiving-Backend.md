# Task 7C1: Commercial Receiving Backend

- Status: Planned; not implemented
- Date: 2026-08-01
- Scope: Production backend from commercial Start through submission for settlement review

## Status

Planned and not implemented. Entry gates must be satisfied before production
routes are enabled.

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
- OQ-01 is an explicit Task 7C1 enablement gate: an approved renderer
  configuration exists or the production Start route remains disabled.
- OQ-06 is an explicit Task 7C1 enablement gate before production Entry/Submit
  validation is enabled.
- OQ-08 is an explicit Task 7C1 enablement gate: reviewed lease duration,
  renewal, grace, and escalation configuration exists before route activation.
- OQ-04 is resolved before a different-device transfer/recovery route is
  enabled. Same-device reacquisition does not require that business decision.
- PostgreSQL RLS has either been implemented for the new tables or remains an
  explicit production-blocking gate with test ownership assigned.

## Backlog

| ID | Artifact/work | Acceptance criteria | Dependencies |
| --- | --- | --- | --- |
| 7C1-01 | Domain lifecycle and snapshot value objects | New `CommercialReceivingSession`, immutable `CommercialReceivingEntry`, and `CommercialReceivingOwnership` implement exactly the two states and fields in TPTECH-001.21, including Procurement Settings ID/version and vehicle-mode snapshots; Domain has no API/EF/Npgsql dependency; no POC type is renamed/reused. | TPTECH-001.21; OQ-06 for final validation. |
| 7C1-02 | Domain ownership/generation rules | Start assigns authenticated editor/generation 1; same-device reacquisition preserves generation; transfer increments once; submission closes lease; stale Device/generation errors are stable; race unit tests cover all four scenarios. | ADR-0008; OQ-04 before different-device transfer enablement. |
| 7C1-03 | Application operation contracts | Define versioned Start/Record/Submit envelopes/results, typed payload parsers, max batch 50, exact decimal strings, and hash verification. Start requires null/absent generation, expected cloud version, and lease; Record/Submit require generation and lease transport metadata and null/absent expected cloud version. Canonical hashing includes actual type/authenticated Device and excludes mutable cloud version, lease, and token metadata. | Task 3; Task 7A; TPTECH-001.21. |
| 7C1-04 | Application query/monitor contracts | Define company-scoped Session list and live-view results with safe snapshots, exact totals, recent Entries, editor/generation/lease health, attention, last update, and no mutation controls. | 7C1-01. |
| 7C1-05 | Reviewed production migration | Create new `commercial_receiving_sessions` with Procurement Settings snapshots, Entries, ownership, reference allocator, idempotency/sequence constraints, state/immutability triggers, and `commercial_mobile_master_changes`. Deterministically backfill one safe current row for every existing 7B1/7B2 Active/Inactive master, association, and settings record. POC tables/data and prior migrations are unchanged. | 7C1-01/02; Task 7B1/7B2; approved naming/schema review. |
| 7C1-06 | Event audience migration | Add controlled `CommercialMobileSync` stream with required audience kind/company metadata and Device target required only for editor events; preserve `Internal` and POC `MobileSync`; scope commit-order locking by stream contract + Workspace + Company, not globally. | ADR-0009; outbox migration review. |
| 7C1-07 | Cloud reference allocator/renderer | PostgreSQL serializes one company/series allocation per accepted Start; stores numeric value, opaque rendered value, and format version once; replay returns same reference; concurrent Starts remain unique; no POC/Purchase Bill/final-posting format is reused. | 7C1-05; OQ-01. |
| 7C1-08 | Master validation/snapshot service | Start requires the exact Procurement Settings ID/version, snapshots authoritative mode, and accepts only that revision's configured default destination and Weight Policy; no alternate route/flag/payload behavior until OQ-11. Entry validates Product/scope/Bag Type/standard. Stale settings/master is `NeedsAttention` without substitution. | Task 7B1/7B2; 7C1-03; OQ-11 for any override. |
| 7C1-09 | Exact Entry acceptance | Reprocess unchanged raw string using captured Session Task 3 policy; require exact processed/display match; store `numeric(20,6)` and deterministic display text; exact total overflow rolls back Entry, projection, audit, event, and result. | Task 3; 7C1-01/05/08; OQ-06. |
| 7C1-10 | PostgreSQL command orchestration | Extend/reuse the focused idempotent executor. All three mobile types claim `Procurement.CommercialReceiving.MobileSyncOperation`; actual type is hashed and one UUID reused as another type returns `IDEMPOTENCY_PAYLOAD_CONFLICT` without execution. Aggregate/Entry, audit, audience-specific commercial outbox, and result commit atomically; deterministic replay returns the original result. | Task 5 executor; 7C1-03/05/06. |
| 7C1-11 | Ordered operation endpoint | Map `POST /api/v1/mobile/commercial-sync/operations` with Task 7A authority. Concurrency uses authenticated Device + generation + exact sequence + Session status + aggregate/ownership lock, never expected cloud version or process lock. Malformed requests write nothing; a per-op failure blocks only that Session and unrelated Sessions continue. | 7C1-03/08/09/10. |
| 7C1-12 | Lease renewal and same-device reacquisition | Add reviewed authenticated direct commands/routes with server time, PostgreSQL serialization, stable lease errors, no ordered-operation payload rewrite, and restart/replay tests. Routine renewal policy and audit noise are documented. | 7C1-02/05/10; OQ-08 configuration gate. |
| 7C1-13 | Different-device transfer/recovery (conditional) | Only after OQ-04 resolution: Owner/approved actor, expected Session/ownership versions where approved, mandatory approved reason, idempotency, generation increment, old lease invalidation, audit and ownership event in one transaction; no physical payload mutation. Otherwise route is absent. | OQ-04; 7C1-02/10/12. |
| 7C1-14 | Submission | Exact next sequence/editor/generation/lease validation; approved entry-minimum rule; transition to `SubmittedForSettlementReview`; clear edit lease/capability; no later Entry; immutable snapshot/state-shape DB enforcement; audit/event/result atomic. | 7C1-01/02/09/10; OQ-06. |
| 7C1-15 | Authenticated commercial event cursor | Map `GET /api/v1/mobile/commercial-sync/events`; Owners read safe company broadcasts, and any authenticated active editor Device reads rows targeted to it; Operators have no broader entitlement. Prove unrelated same-company Operators cannot read, cursor gaps are valid, reads do not mutate delivery, and all three streams remain mutually exclusive. | 7C1-06/10; OQ-12 for broader Operator visibility. |
| 7C1-16 | Receiving event payloads | Emit only four version-1 event types with safe snapshots from TPTECH-001.21; no Supplier protected data, tokens, Device secrets, lease ID, or Internal payload; event is captured at commit and not reconstructed. | 7C1-06/09/10/14; TPSEC-001. |
| 7C1-17 | Dedicated commercial master sync | Implement only the migration-backed change-log protocol: deterministic safe backfill of every existing 7B1/7B2 master/association/settings row, Active and Inactive; first bootstrap captures scoped `H`, pages unique rows through `H`, then deltas strictly after `H`; future mutations append atomically under the scoped master-stream lock. No raw `Internal` data. | Task 7B1/7B2; ADR-0009; 7C1-05. |
| 7C1-18 | Owner list/live-view routes | Owner-authenticated read-only routes return active/submitted Sessions, safe header snapshots, exact totals, recent Entries, editor/generation/lease health, attention/submission state; queries mutate no Session/outbox/cursor; guessed cross-company IDs do not disclose existence. | 7C1-04/05. |
| 7C1-19 | Audit, observability, and redaction | Material Start/Entry/Submit/reacquisition/transfer actions use authenticated actor/correlation; safe diagnostics expose stable codes/counts only; logs/audits/events contain no credential, lease capability, Supplier protected fields, raw SQL, or stack trace. | TPSEC-001; 7C1-10/12/13/14. |
| 7C1-20 | Unit/architecture tests | Cover lifecycle, settings snapshots/default-only validation, operation field matrix, shared-scope/type-conflict hashing, exact weight, ownership errors/races, safe audience serializers, and dependency direction. No test claims settlement/posting. | 7C1-01 through 7C1-19. |
| 7C1-21 | PostgreSQL 18/API/concurrency/failure tests | Start from an upgrade database containing existing 7B1/7B2 Active/Inactive masters, associations, and settings; prove complete safe deterministic backfill, no raw Internal data, bootstrap/delta concurrency with no skip/duplicate, and cursor after `H`. Also prove same-company event commit order, different-company concurrent writers and valid gaps, Owner/editor-only visibility, cross-type UUID conflict, sequence/status/generation locks, response loss, rollback, immutability, references, and RLS. | 7C1-05 through 7C1-19; Docker/Testcontainers. |
| 7C1-22 | Contract/runbook and feature readiness | Update API contract/docs and safe local test runbook; production configuration has no POC flags/credentials/fallback; migration remains explicit; no route exposes unresolved cancellation/correction/transfer behavior. | All prior items. |

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

## Failure behavior

Rollback leaves no partial aggregate, Entry, projection, audit, event, or
completed result. `NeedsAttention` never rewrites/consumes an immutable local
payload. Stale generation/wrong Device fail without takeover. Response-loss
replay returns the original stored success.

## Security implications

Every route uses endpoint metadata and database-revalidated context. New tables
require tenant constraints and RLS coverage before production. Event/master
payload allowlists and negative isolation tests are release gates. HTTPS and
Task 7A configuration remain mandatory.

## Migration implications

Create forward-only reviewed migrations; do not edit applied migration history,
run migrations at host startup, or touch POC tables. Upgrade tests must begin at
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

OQ-01, OQ-06, and OQ-08 remain explicit Task 7C1 production enablement gates;
OQ-04 gates different-device transfer. OQ-11 and OQ-12 gate only default-
override and non-editor Operator visibility behavior, which remain absent.
Cancellation, correction, additional headers, stale-master recovery, and old-
generation fact recovery also remain absent unless separately approved.

## Exit criteria

- All acceptance criteria above pass on PostgreSQL 18.
- No POC type/table/header/reference/cursor is used as commercial authority.
- No Purchase Bill, payable, Inventory, Sales, Finance, or finalization effect
  exists.
- Task 7C2 receives frozen versioned JSON/OpenAPI/error/event/master contracts.
