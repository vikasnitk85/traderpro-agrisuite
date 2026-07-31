# TPTECH-001.19: Commercial Operational Master Data

## Scope

Task 7B1 introduces the first production commercial masters required before
Supplier, Product, and commercial Receiving work:

- Operations Business Locations;
- Procurement Receiving Vehicles;
- Procurement Bag Types;
- Procurement Weight Processing Policies;
- one Company Procurement Settings record.

It does not add Suppliers, Product Groups, Products, product-specific bag
weights, Receiving Sessions, settlement, Inventory, Sales, Finance,
Production, fleet operations, Flutter screens, or commercial mobile sync.

## Authenticated commercial authority

Every route declares Task 7A commercial endpoint metadata. Mutations require
`TraderProOwner`; reads require `TraderProOperatorOrOwner`.
`IAuthenticatedTraderProContext` supplies the authoritative Workspace, User,
Device, Company, default Branch, role, and correlation ID after current
database revalidation.

Request bodies never accept authority fields. Temporary
`X-TraderPro-Workspace-ID` and `X-TraderPro-Device-ID` POC headers are ignored
by these routes. Guessed IDs outside the authenticated workspace/company and
default branch return isolated 404 responses.

## Module boundaries

`Operations` owns `BusinessLocation`, a shared operational destination
concept. `Procurement/MasterData` owns receiving vehicles, bag types, weight
policies, and company settings. Procurement may query Operations destinations.
Operations uses the Application abstraction
`IBusinessLocationDefaultUsageReader` for default-destination protection and
does not depend directly on Procurement persistence.

Domain types contain no EF Core, ASP.NET Core, or Npgsql dependency.
Application contains contracts, input rules, canonical hashing, and service
abstractions. Infrastructure contains EF mappings, queries, PostgreSQL
transactions/locking, and persistence error translation. API endpoints only
bind transport inputs and call Application services.

## Shared master rules

Commercial master IDs are server-generated UUIDv7 values. Records have
immutable Workspace/Company ownership, `Active` or `Inactive` status, UTC
create/update timestamps, and optimistic-concurrency Version starting at 1.
Locations additionally bind immutably to the authenticated default Branch.

Every successful update, deactivate, reactivate, or configured-settings
update advances the aggregate's persisted Version exactly once and writes the
current injected UTC server time to UpdatedAtUtc. Responses, audit snapshots,
and outbox versions come from that mutated entity. PostgreSQL revision
triggers reject unchanged versions, jumps, regressions, timestamp regressions,
creation-time changes, and revision-only corruption.

Codes are 2-32 characters, reject surrounding whitespace, contain only
letters, digits, and hyphens, and normalize with invariant uppercase. The
stored Code and NormalizedCode are identical and immutable. Codes are unique
per Workspace + Company + master type. Names and optional local names are
trimmed safe text; HTML delimiters and control characters are rejected.
Physical DELETE routes do not exist, and PostgreSQL triggers reject direct
deletion and ownership/code mutation.

## Business Location

`operations.business_locations` stores Code, Name, optional LocalName,
controlled LocationType (`Warehouse`, `Yard`, `Mill`, `Office`, `Other`),
optional AddressLine and Notes, authenticated default Branch ownership,
status, timestamps, and Version. It contains no inventory balance or stock
logic.

The unique code is company-scoped. The active destination index begins with
Workspace, Company, Branch, and Status. Company settings may select one active
same-company/default-branch location as the procurement destination.

## Receiving Vehicle

`procurement.receiving_vehicles` is a minimal selection master, not Fleet
Management. It stores immutable Code, corrected-through-version
RegistrationNumber, deterministic uppercase alphanumeric normalized
registration, optional DisplayName/OwnerName/ContactNumber/Notes, controlled
VehicleType (`Truck`, `Tractor`, `Van`, `Other`), status, timestamps, and
Version.

Normalized registration is unique per company. The model contains no driver,
trip, GPS, fuel, maintenance, routing, or accounting behavior. Future
commercial Receiving may omit a vehicle.

PostgreSQL also requires the normalized value to equal the uppercase
alphanumeric normalization of the trimmed display registration after removing
only spaces and hyphens.

## Bag Type

`procurement.bag_types` stores immutable Code, Name, optional LocalName,
controlled ConstructionClass (`Jute`, `SinglePlastic`, `DoublePlastic`,
`Other`), exact `numeric(20,6)` StandardTareWeightKg, IsReturnable, optional
Notes, status, timestamps, and Version.

`isReturnable` is a required presence-aware API boolean. Callers must send
either `true` or `false`; omission returns `BAG_TYPE_INVALID`.

The API accepts tare as a decimal string with at most 14 integer and 6
fractional digits. It never passes through `double`. `DoublePlastic` is
classified for later settlement design; Task 7B1 posts no 0.2/0.4 kg rule,
bag stock, deduction, or supplier settlement.

## Weight Processing Policy

`procurement.weight_processing_policies` stores immutable Code, Name,
DecimalPlaces (1, 2, or 3), ProcessingMethod (`Standard`, `Floor`, or
`Ceiling`), optional Notes, status, timestamps, and Version.

The aggregate validates through the Task 3 pure weight-processing contract.
Standard remains non-negative decimal half-up; Floor and Ceiling retain the
same exact scaled-integer behavior. Future Receiving Entries must snapshot the
selected precision/method. Updating a policy never rewrites a captured
historical fact.

## Company Procurement Settings

`procurement.company_procurement_settings` allows at most one row for a
Workspace + Company. It stores the immutable authenticated default Branch,
an active same-branch Business Location, an active same-company Weight
Processing Policy, VehicleSelectionMode (`Optional` or `Disabled`), UTC
timestamps, and Version.

GET does not create state. When absent it returns
`PROCUREMENT_SETTINGS_NOT_CONFIGURED`. The first PUT configures the row
without an expected version; later PUTs require positive
`X-Expected-Version`. Database foreign keys and an active-default trigger
repeat the service validation.

Settings INSERT/UPDATE and default location/policy deactivation share one
transaction-scoped PostgreSQL advisory lock derived from
`TraderPro.ProcurementDefaults.v1`, WorkspaceId, and CompanyId. Database
triggers acquire that same lock before reloading active references or settings
usage, so assignment and deactivation races cannot commit an inactive default.

The current default destination and weight policy cannot deactivate.
Application checks return `PROCUREMENT_DEFAULT_LOCATION_IN_USE` and
`PROCUREMENT_DEFAULT_WEIGHT_POLICY_IN_USE`; PostgreSQL triggers enforce the
same rule for direct SQL. Change settings first, then deactivate the former
master. Future commercial Receiving must fail closed while settings are
absent.

## Commands, idempotency, and concurrency

Creates require `Idempotency-Key`. Updates, status changes, and existing
settings updates additionally require positive `X-Expected-Version`.
`X-Correlation-ID` remains optional and uses Task 7A correlation behavior.

Commands reuse `PostgreSqlIdempotentCommandExecutor`. Canonical SHA-256 hashes
include command type, authoritative Workspace/Company/default Branch/User/
Device, expected version where applicable, and length-delimited invariant
typed values. The PostgreSQL advisory lock and unique idempotency index
serialize identical commands across API instances. Identical replay returns
the original result, HTTP status, and correlation. A changed typed payload
returns `IDEMPOTENCY_PAYLOAD_CONFLICT`.

Explicit expected-version checks and EF concurrency tokens prevent
last-write-wins. Conflicts return `MASTER_VERSION_CONFLICT` with safe expected
and current versions. Unique-index races map to master-specific 409 errors.

Optional text is canonicalized to its persisted trimmed value before hashing;
whitespace-only values hash as null. Vehicle hashes include both trimmed
display registration and normalized registration.

## Audit and Internal outbox

The first successful execution writes one material `AuditEvent` with
authenticated Workspace, Company, default Branch, User, Device, and
correlation. Actions use the `Operations.BusinessLocation.*`,
`Procurement.ReceivingVehicle.*`, `Procurement.BagType.*`,
`Procurement.WeightProcessingPolicy.*`, and
`Procurement.CompanySettings.*` names.

The same transaction writes one minimal safe outbox fact with event version 1,
aggregate identity/version/status, and `EventStream = Internal`. Settings
events contain only settings/default-branch identity and Version. No
commercial master event is visible through the temporary POC MobileSync
cursor. Aggregate change, audit, outbox, and completed idempotency result
commit or roll back together.

Legacy executor overloads retain the global MobileSync commit-order lock used
by Task 5/6 cursor sequencing. Task 7B1 explicitly selects independent
Internal ordering, so unrelated commercial commands are not serialized for a
cursor they never publish to. Idempotency locking and atomicity are unchanged.

## Queries and routes

Routes are:

- `/api/v1/operations/locations`;
- `/api/v1/procurement/vehicles`;
- `/api/v1/procurement/bag-types`;
- `/api/v1/procurement/weight-policies`;
- `/api/v1/procurement/settings`.

Master collections support optional `status=Active|Inactive|All`, optional
safe search text, an opaque cursor, and `limit` 1-100. The default is active
only. Ordering is deterministic by normalized code and ID. Reads do not
mutate masters, event delivery state, or cursors.

The versioned cursor binds master kind, authenticated Workspace/Company,
Location default Branch where applicable, status, normalized search, last
normalized code, and last ID. Reuse across endpoints, tenants, or changed
filters returns `MASTER_CURSOR_INVALID`. Keyset comparison uses code and ID.

## Migration and limitations

`AddCommercialOperationalMasterData` creates the `operations` schema and five
tables, composite ownership foreign keys, controlled-value checks, exact
numeric tare storage, unique/indexed lookups, concurrency tokens, active
settings validation, immutable-identity triggers, default-in-use triggers,
and no-delete triggers. It creates no implicit masters or settings.

`HardenCommercialOperationalMasterDataContracts` follows that applied
migration without rewriting it. It adds revision-transition triggers,
registration consistency, and the per-company Procurement-default lock.
Focused PostgreSQL tests cover lifecycles, stale-write races, default races,
direct-SQL corruption, and Task 7B1 Internal-outbox rollback.

PostgreSQL RLS, authenticated commercial mobile sync, outbox publication,
Supplier/Product masters, commercial Receiving, Inventory, settlement,
Purchase Bills, Sales, Finance, and Production remain deferred. Task 7B2
depends on these masters and will add Suppliers, Product Groups, Products,
product-specific Standard Bag Weights, and Supplier Product Scope separately.
