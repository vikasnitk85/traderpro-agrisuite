# TPTECH-001.20: Commercial Supplier and Product Catalog

## Scope

Task 7B2 adds the production commercial catalog required before commercial
Receiving Sessions:

- Procurement Suppliers;
- Catalog Product Groups and Products;
- optional Supplier Product Scope;
- product-specific Standard Bag Weights.

It does not add Receiving Sessions, settlement, supplier balances, rates,
Inventory, Sales, Finance, Production, recipes, stock valuation, Flutter
screens, or commercial mobile synchronization.

## Authenticated commercial authority

All routes use Task 7A commercial endpoint metadata.
`IAuthenticatedTraderProContext` is the sole authority for Workspace, Company,
default Branch, User, Device, role, and correlation. Request bodies do not
accept authority fields, and temporary POC workspace/device headers do not
influence these endpoints. Mutations require `TraderProOwner`; reads require
`TraderProOperatorOrOwner`. Cross-company or cross-workspace IDs produce
isolated not-found responses.

## Module boundaries

Supplier behavior belongs to `Procurement/Suppliers`. Shared product identity,
classification, and bag standards belong to `Catalog`. Catalog uses the
Application abstraction `IBagTypeProductStandardUsageReader` to extend Bag
Type in-use protection without coupling Procurement to Catalog persistence.

Domain types have no EF Core, ASP.NET Core, or Npgsql dependency. Application
owns command/query contracts and input rules. Infrastructure owns EF mappings,
PostgreSQL transactions and locks, persistence error translation, and query
execution. API endpoints bind transport inputs and call Application services.

## Shared master rules and revisions

Supplier, Product Group, and Product IDs are server-generated UUIDv7 values.
Ownership and codes are immutable. Codes follow the Task 7B1 2-32 character
uppercase letter, digit, and hyphen contract and are company/master unique.
Records transition between Active and Inactive and never physically delete.

Version begins at 1. Each successful material mutation advances the affected
record exactly once and uses the injected UTC clock for UpdatedAtUtc. Existing
revision triggers reject direct SQL that skips or jumps a version, regresses a
timestamp, changes CreatedAtUtc, or performs a revision-only update.

## Supplier

A Supplier stores commercial identity, controlled `Individual` or `Business`
type, optional descriptive/contact data, and controlled `Unrestricted` or
`Restricted` product scope mode. It deliberately has no balance, opening
payable, credit limit, payment terms, bank account, or posting behavior.

Email canonicalizes to trimmed invariant lowercase. Tax registration stores a
trimmed display value and an uppercase alphanumeric normalized value; a
supplied normalized tax number is unique within the company. Contact, email,
tax, address, and notes are excluded from audit and outbox snapshots.
PostgreSQL independently rejects non-canonical email values with multiple,
leading, or trailing `@` characters, whitespace, control characters, or an
over-length value. This is deliberately conservative rather than RFC-complete.

Product scope defaults to Unrestricted. An Unrestricted supplier may use any
future Active purchasable product; retained scope rows do not restrict it. A
Restricted supplier must have at least one Active Supplier Product Scope.
Restricted creation supplies unique, canonically sorted initial Product IDs,
and supplier plus scopes commit atomically. Changing to Restricted also
requires an Active scope. Changing to Unrestricted preserves existing rows.
Every initial Product ID, including one supplied for an Unrestricted Supplier,
creates a safe scope audit fact and
`Procurement.SupplierProductScopeAdded` Internal event in Product ID order.

## Product Group and Product

Product Groups organize catalog identity and contain no stock, accounting, or
recipe state. An Active Product must reference an Active same-company group.
A group with any Active Product cannot deactivate.

Products support `RawMaterial`, `FinishedGood`, `ByProduct`, `Consumable`, and
`Other`. `IsPurchasable` is explicit. Future commercial Receiving may select
only Active purchasable Products. Product state contains no quantity, value,
price, rate, tax, account, bill of material, or production transaction.

Processing Family is an optional normalized product classification using the
master-code contract. The display code is retained while a normalized code is
stored for matching and filters. Products such as Aman, H Aman, and P Aman may
all use `AMAN`. A Product update never rewrites a future historical snapshot;
Task 7B2 creates no Milling Run or production recipe.

An Active Supplier Product Scope or Active Standard Bag Weight prevents
Product deactivation. Associations must deactivate first. Making a Product
non-purchasable is likewise rejected while these active associations exist.

## Supplier Product Scope

One immutable association may exist per Supplier and Product. Creation or
reactivation requires an Active Supplier and an Active purchasable Product in
the authenticated workspace/company. Associations deactivate and reactivate;
they are never deleted and never carry a rate, price, payable, or Receiving
Session.

Changing scope mode and every scope mutation serialize on a transaction-scoped
PostgreSQL advisory lock keyed by Workspace + Company + Supplier. Deferred
database constraint triggers acquire the same lock and reject a Restricted
supplier with zero Active scopes, including direct SQL and concurrent final
scope changes.

## Product-specific Standard Bag Weights

A Product Standard Bag Weight associates an immutable Product and Bag Type
with an optional label and exact positive net content weight. The API accepts
the weight as a decimal string and PostgreSQL stores `numeric(20,6)`; no value
passes through floating point. Product, Bag Type, and association ownership
must match. Product + Bag Type + exact weight is unique.

Standard content weight is net product content. It does not replace or alter
`BagType.StandardTareWeightKg`, which remains the separate physical tare fact.
Task 7B2 performs no tare deduction or supplier settlement.

Multiple standards may exist, but a partial unique index permits at most one
Active default per Product. Selecting a default is explicit. The prior default
is cleared, all changed rows advance revision once, and audit/outbox facts
identify only the selected default. Deactivating a default clears it;
reactivation never silently restores it.

Default creation, update, selection, and deactivation serialize on a
transaction-scoped advisory lock keyed by Workspace + Company + Product.
Application services and database triggers use the same lock. An Active
standard prevents its Product or Bag Type from deactivating.

## Commands, idempotency, audit, and events

Creates require `Idempotency-Key`. Updates, status changes, scope changes, and
default selection also require positive `X-Expected-Version`. Commands reuse
`PostgreSqlIdempotentCommandExecutor`; no process-local correctness lock or
second executor exists. Canonical hashes include authenticated authority,
command type, expected version, persisted canonical values, sorted unique
initial Product IDs, and canonical exact decimals. Identical retries return
the stored result; changed meaning returns `IDEMPOTENCY_PAYLOAD_CONFLICT`.

Presence-aware update request contracts reject immutable `code` fields and
the bag-standard `productId`, `bagTypeId`, and command-only `isDefault` fields
with `MASTER_IMMUTABLE_FIELD_SUPPLIED`. Rejection occurs before command
execution, so no row, revision, audit, outbox, or idempotency completion is
created. The set-default endpoint remains the only way to select a default.

Each winning mutation atomically commits aggregate/association changes, one
immutable AuditEvent, one `Internal` OutboxMessage, and the completed
idempotency record for each changed aggregate or association. Audit snapshots
and Internal-event payloads are separate projections: audits contain safe
material before/after facts, while events remain minimal. Supplier facts never
contain contact number, email, address, tax, or notes values; protected-only
updates instead record a deterministic `changedFields` name list. Product audit
snapshots include safe Description values and use `changedFields: ["notes"]`
when raw Product Notes change; raw Notes never enter audits or outbox payloads,
and Product Internal-event payloads remain minimal. Initial Supplier creation
commits the Supplier, every initial scope (including scopes supplied in
Unrestricted mode), their audits and events in Product ID order, and
idempotency completion in one transaction. A failure on any scope event rolls
back the complete composite. Internal events use independent ordering and never
enter the temporary Task 5/6 MobileSync cursor. Replay creates no duplicate
audit or outbox fact.

## Queries and cursors

Collections default to Active records and support explicit status, safe
search, opaque cursor pagination, deterministic normalized-code/ID order, and
page sizes 1-100. Supplier filters include type and scope mode. Product filters
include Product Group, Product Type, purchasable flag, and normalized
Processing Family. Nested scope and bag-standard lists bind their cursor to
the parent ID.

Cursors bind master kind, authenticated Workspace/Company, every normalized
filter, normalized search, ordering key, ID, and parent where applicable.
Endpoint, tenant, parent, search, or filter reuse returns
`MASTER_CURSOR_INVALID`. Queries are read-only.

## Database migration

`AddCommercialSupplierAndProductCatalog` creates the `catalog` schema and:

- `procurement.suppliers`;
- `catalog.product_groups`;
- `catalog.products`;
- `catalog.product_standard_bag_weights`;
- `procurement.supplier_product_scopes`.

It adds composite ownership keys, controlled-value checks, unique and active
lookup indexes, exact numeric storage, partial default uniqueness, immutable
identity/no-delete/revision triggers, active-reference validation, advisory
lock functions, deferred Restricted-scope validation, and Group/Product/Bag
Type in-use protection. It creates no implicit commercial rows and upgrades a
Task 7B1 database without changing existing identity, POC, master, audit, or
outbox data.

`HardenCommercialSupplierProductCatalogContracts` replaces only the Supplier
email check with the strengthened canonical single-`@`, non-whitespace,
non-control contract while preserving existing valid rows. The original
`AddCommercialSupplierAndProductCatalog` migration remains unchanged.

## Task 7C dependency and deferred behavior

Task 7C may use only an Active Supplier and Active purchasable Product, apply
Restricted scope when configured, select product-specific bag standards, keep
Bag Type tare separate from content weight, and snapshot relevant master
facts. Task 7C must design Receiving Session concurrency and physical-fact
reconciliation separately. Supplier settlement, balances, bag deductions,
Inventory movements, Purchase Bills, Sales, Finance, Production, and recipes
remain explicitly deferred.
