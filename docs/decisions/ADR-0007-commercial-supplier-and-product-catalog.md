# ADR-0007: Commercial supplier and product catalog

- Status: Accepted for Task 7B2
- Date: 2026-07-31
- Scope: Production Supplier and Product catalog before commercial Receiving

## Context

Commercial Receiving needs durable supplier and product identities,
procurement eligibility, optional supplier restrictions, and exact
product-specific bag-content standards. Task 7A supplies authenticated
commercial authority and Task 7B1 supplies operational masters including Bag
Type tare. The temporary Task 6 POC strings and MobileSync cursor are not
production authority.

## Decision

Supplier is a Procurement aggregate but remains separate from payable and
financial state. It records who may supply goods, not balances, credit,
settlement, bank details, or accounting postings.

Product Group and Product live in a shared Catalog boundary because later
Procurement, Inventory, Sales, and Production designs may reference the same
stable identity. Task 7B2 exposes procurement-facing eligibility through
`IsPurchasable` without implementing those later modules.

Product Type is controlled. Processing Family is optional and deterministically
normalized, allowing products such as Aman, H Aman, and P Aman to share an
`AMAN` classification without introducing a production recipe or Milling Run.

Supplier product scope defaults to Unrestricted so future Active purchasable
products do not require repetitive supplier maintenance. Restricted mode is
explicit and requires at least one Active immutable Supplier/Product
association. Scope rows are preserved when returning to Unrestricted.

Product Standard Bag Weight stores exact net content separately from Bag Type
tare. Multiple standards are allowed, but at most one Active default exists
per Product. Default selection is an explicit versioned command; deactivation
clears the default and reactivation does not infer one.

Master ownership and code, and association ownership/identity, are immutable.
Records deactivate instead of delete. PostgreSQL triggers repeat these rules,
revision transitions, cross-table active-reference checks, and in-use
protections for direct SQL.

Supplier-scope changes and Product-default changes use deterministic
transaction-scoped advisory locks in application paths and database triggers.
Partial uniqueness and deferred constraint triggers remain final invariants
under concurrency.

Commands reuse authenticated Task 7A context, Task 5 database idempotency,
optimistic versions, audit events, and the transactional outbox. Catalog facts
use only the `Internal` stream. They remain outside the temporary unauthenticated
Task 5/6 MobileSync cursor pending a separately reviewed commercial sync
contract.

Update transport contracts are presence-aware. Supplier, Product Group, and
Product codes are rejected when supplied to update routes; Product Standard
Bag Weight updates likewise reject Product, Bag Type, and default-selection
fields. These requests fail before idempotency begins. Default selection stays
an explicit command.

Audit snapshots and Internal event payloads are intentionally separate.
Catalog audits retain safe material before/after facts while Internal events
carry minimal identity and state. Supplier protected values never enter either
projection; Supplier audits use a deterministic `changedFields` list of field
names when those values change. Product audits retain safe Description values
and represent raw Notes changes only with a deterministic `notes` field name;
Product Internal events remain minimal. Initial Supplier Product Scopes,
including those supplied for an Unrestricted Supplier, each receive a safe
audit and Internal event in Product ID order. Supplier creation, all initial
scopes, all facts, and idempotency completion form one transaction, so failure
of any initial-scope event rolls back the complete composite.

The follow-up `HardenCommercialSupplierProductCatalogContracts` migration
strengthens direct-SQL Supplier email validation without rewriting the original
Task 7B2 migration. Canonical emails are lowercase and trimmed, contain exactly
one non-edge `@`, and contain no whitespace or control characters.

## Consequences

- Task 7C receives stable selection identities without prematurely creating
  financial or Inventory state.
- Supplier eligibility can remain open-ended or be explicitly constrained.
- Exact content standards and physical tare cannot be accidentally conflated.
- Historical consumers can snapshot stable IDs/codes while later descriptive
  edits advance revisions.
- In-use masters must have associations moved or deactivated before their own
  deactivation.
- Owner mutations are retry-safe; Operators remain read-only.
- Rejected immutable fields cannot create partial mutation or idempotency state.
- Protected Supplier changes remain auditable without persisting PII values.
- Initial scope facts are complete, deterministic, and transactionally atomic.
- Direct SQL cannot bypass deletion, identity, revision, scope, default, or
  active-reference invariants.

## Rejected alternatives

- Combining Supplier with accounts payable was rejected because posting and
  settlement require a separate balanced-finance design.
- Reusing Task 6 POC product/supplier strings was rejected because they lack
  authenticated commercial ownership and stable catalog identity.
- Restricting all suppliers by default was rejected because it makes future
  catalog additions silently unusable without an explicit business decision.
- Storing standard content as Bag Type tare was rejected because content is
  Product-specific while tare is a physical Bag Type fact.
- Automatically restoring a default on reactivation was rejected as hidden
  state selection.
- Mutable codes, physical deletion, process-local locks, and MobileSync event
  publication were rejected for historical, distributed-correctness, and
  contract-isolation reasons.
