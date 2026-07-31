# ADR-0006: Commercial operational master data

- Status: Accepted for Task 7B1
- Date: 2026-07-31
- Scope: Production commercial operational masters before Supplier/Product
  setup and commercial Receiving

## Context

Commercial Receiving needs stable destinations, optional vehicle selection,
bag construction/tare classification, a captured weight-processing policy,
and explicit company defaults. Task 6 POC strings cannot become commercial
authority, while Task 7A now provides authenticated Workspace/Company/default
Branch/User/Device context.

## Decision

Business Locations belong to an `Operations` boundary because warehouses,
yards, mills, and offices are shared operational identities beyond
Procurement. Procurement settings reference an Operations location rather
than duplicating it.

Receiving Vehicles remain a minimal Procurement selection master. They are
not Fleet Management and contain no trips, drivers, fuel, maintenance, GPS,
route planning, or cost accounting.

Bag Types classify Jute, SinglePlastic, DoublePlastic, and Other now and store
exact standard tare. DoublePlastic identifies later settlement design, but no
special deduction or settlement behavior is implemented in Task 7B1.

Weight Processing Policies use the exact Task 3 DecimalPlaces and
Standard/Floor/Ceiling semantics. Future physical facts snapshot the selected
policy; policy edits cannot reprocess history.

One explicit Company Procurement Settings row selects the authenticated
default Branch destination, active weight policy, and Optional/Disabled
vehicle mode. GET does not create defaults. Future Receiving fails closed if
settings are absent.

Task 7A commercial context is the sole authority. Caller-controlled POC
workspace/device headers and request-body ownership fields cannot select
commercial data.

Master codes normalize invariantly, are company/type unique, and become
immutable after creation. Records transition between Active and Inactive and
never physically delete. A default destination or policy must be replaced in
settings before it can deactivate.

Aggregate mutation methods own revision advancement. A successful material
change persists Version + 1 and the injected UTC timestamp; services cannot
synthesize a newer response without mutating the entity. PostgreSQL enforces
the same one-step revision contract.

Settings assignment and default deactivation serialize on one
transaction-scoped advisory lock per Workspace + Company. Application paths
and triggers use the deterministic `TraderPro.ProcurementDefaults.v1` scope
and reload state after locking.

Commands reuse Task 5 PostgreSQL idempotency, optimistic concurrency,
transactional audit, and outbox behavior. Commercial master events use only
the `Internal` stream until an authenticated commercial mobile-sync contract
is reviewed. They are never added to the frozen temporary POC cursor.
Task 5/6 retains its global MobileSync commit-order lock; Internal Task 7B1
commands opt out because they do not produce that cursor.

List cursors are versioned and scope-bound to master kind, authority, filters,
normalized code, and ID. Bag returnability is explicit, registration is
database-normalized consistently, and hashes reflect persisted values.

## Consequences

- Operations can be reused by later Inventory and Production designs without
  duplicating location identity.
- Procurement owns receiving-specific configuration without claiming fleet,
  settlement, or posting behavior.
- Company readiness is explicit and queryable rather than inferred from
  whichever master was created first.
- Direct SQL triggers reinforce ownership/code immutability, no-delete rules,
  revision transitions, registration consistency, active defaults, and
  default-in-use protections.
- Owner mutations are retry-safe and versioned; Operators remain read-only.
- PostgreSQL RLS and authenticated commercial mobile sync remain separate
  pre-production/future decisions.

## Rejected alternatives

- Reusing Task 6 POC product/bag strings was rejected because they are
  development-only facts without commercial identity or authority.
- Putting Locations under Procurement was rejected because they are shared
  operational masters.
- Expanding Vehicle into Fleet Management was rejected as unrelated scope.
- Persisting implicit settings during GET was rejected because missing
  commercial readiness must remain visible.
- Mutable codes and physical deletion were rejected because historical
  references require stable identity.
- Publishing through `MobileSync` was rejected because that cursor is a
  temporary Task 5/6 POC contract without authenticated commercial sync.
