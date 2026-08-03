# TraderPro AgriSuite

## Product summary

TraderPro AgriSuite is an Android-first agricultural trading and operations product. Its foundation combines a Flutter mobile application with a .NET 10 modular-monolith backend, PostgreSQL 18, one deployable API, and one background worker. Business data is cloud-authoritative; the mobile architecture will preserve locally captured physical facts before synchronisation.

This repository contains a buildable foundation with the Platform database,
production backend identity/device-session boundary, and focused mobile
offline-store and two-device proofs of concept. It establishes project,
module, dependency, test, and local-infrastructure boundaries without
implementing complete production business workflows.

## Repository map

```text
.
|-- AGENTS.md
|-- README.md
|-- Directory.Build.props
|-- TraderPro.sln
|-- apps/
|   `-- mobile/                         Flutter application
|-- contracts/
|   |-- events/                         Event contract placeholders
|   |-- golden-vectors/                 Cross-platform contract test vectors
|   `-- openapi/                        HTTP API contract placeholders
|-- docs/
|   |-- architecture/                   System and module architecture
|   |-- assessments/                    Technical and product assessments
|   |-- decisions/                      Architecture decision records
|   |-- functional-specs/               Functional specifications
|   |-- plans/                           Delivery and migration plans
|   `-- technical-specs/                Technical specifications
|-- infrastructure/
|   |-- database/                       Database infrastructure placeholders
|   |-- deployment/                     Deployment infrastructure placeholders
|   `-- docker/                         Local Docker Compose foundation
|-- scripts/
|   |-- build/
|   |-- migration/
|   |-- setup/
|   `-- test/
|-- services/
|   `-- backend/
|       |-- src/
|       |   |-- TraderPro.Api/
|       |   |-- TraderPro.Application/
|       |   |-- TraderPro.Domain/
|       |   |-- TraderPro.Infrastructure/
|       |   `-- TraderPro.Worker/
|       `-- tests/
|           |-- TraderPro.ArchitectureTests/
|           |-- TraderPro.IntegrationTests/
|           `-- TraderPro.UnitTests/
`-- tests/
    |-- concurrency/
    |-- end-to-end/
    `-- failure-injection/
```

## Architecture boundaries

The backend is a modular monolith with one API process and one worker process. Dependency direction is intentionally constrained:

- `TraderPro.Domain` has no dependency on ASP.NET Core, EF Core, Infrastructure, PostgreSQL, SignalR, or API projects.
- `TraderPro.Application` may depend on Domain.
- `TraderPro.Infrastructure` may depend on Application and Domain.
- `TraderPro.Api` may depend on Application and Infrastructure.
- `TraderPro.Worker` may depend on Application and Infrastructure.
- Architecture tests enforce these dependency rules.

The initial module registration boundaries are Platform, MasterData, Procurement, Inventory, Sales, Finance, Production, Documents, and Reporting. They are registration seams only; this scaffold does not implement those modules.

## Prerequisites

- .NET SDK 10.x (LTS)
- Flutter 3.44.4 with Dart 3.12.2
- Android SDK/toolchain for Android builds
- Docker Engine or Docker Desktop with Docker Compose v2
- PowerShell 7+ for the commands below, or an equivalent shell

Confirm the installed toolchain:

```powershell
dotnet --version
flutter --version
docker compose version
```

## Setup commands

Run these commands from the repository root:

```powershell
Copy-Item .\infrastructure\docker\.env.example .\infrastructure\docker\.env
dotnet tool restore
dotnet restore .\TraderPro.sln

Push-Location .\apps\mobile
flutter pub get
Pop-Location
```

The copied `.env` is for local development only and is ignored by Git. Replace
its placeholder password on any shared or long-lived environment. Backend and
migration commands read the PostgreSQL connection only from
`ConnectionStrings:TraderPro` or the
`ConnectionStrings__TraderPro` environment variable. No connection string or
password is committed.

## Build commands

```powershell
dotnet build .\TraderPro.sln --configuration Debug --no-restore

Push-Location .\apps\mobile
flutter build apk --debug
Pop-Location
```

Generate the checked-in Drift source after changing the local schema:

```powershell
Push-Location .\apps\mobile
dart run build_runner build
Pop-Location
```

## Test commands

```powershell
dotnet test .\TraderPro.sln --configuration Debug --no-build

Push-Location .\apps\mobile
flutter analyze
flutter test
Pop-Location

powershell -ExecutionPolicy Bypass `
  -File .\scripts\test\test-weight-processing-parity.ps1

powershell -ExecutionPolicy Bypass `
  -File .\scripts\test\test-mobile-offline-store.ps1

powershell -ExecutionPolicy Bypass `
  -File .\scripts\test\test-production-identity-foundation.ps1
```

The .NET test command runs unit, PostgreSQL integration, and architecture test
projects. Database integration tests start a disposable PostgreSQL 18
Testcontainer and create a fresh migrated database for every test. Docker must
be available; the tests do not use the long-lived Compose database.

The parity script runs the targeted C# and Dart weight-processing suites
against the same repository-level JSON golden vectors.

The offline-store script regenerates Drift source, runs the real-SQLite schema,
transaction, restart, projection, ordering, duplicate, and outbox-transition
tests, and then runs the weight-processing parity script. The targeted mobile
tests can also be run directly:

```powershell
Push-Location .\apps\mobile
flutter test .\test\core\database
flutter test .\test\features\receiving
Pop-Location
```

Run the focused cloud-command, PostgreSQL 18 API, concurrency, rollback,
cursor, and architecture suite with:

```powershell
powershell -ExecutionPolicy Bypass `
  -File .\scripts\test\test-cloud-command-spike.ps1
```

## Docker Compose commands

Validate the Compose model without creating a local `.env`:

```powershell
docker compose --env-file .\infrastructure\docker\.env.example -f .\infrastructure\docker\compose.yml config
```

Start, inspect, and stop PostgreSQL after completing the setup step:

```powershell
docker compose --env-file .\infrastructure\docker\.env -f .\infrastructure\docker\compose.yml up -d postgres
docker compose --env-file .\infrastructure\docker\.env -f .\infrastructure\docker\compose.yml ps
docker compose --env-file .\infrastructure\docker\.env -f .\infrastructure\docker\compose.yml down
```

Do not remove the `postgres-data` volume as part of routine development.

The optional `future-apps` Compose profile documents future API and Worker runtime configuration. Its image names are deliberate placeholders and the profile must not be started until deployable application images exist.

## Database connection and migrations

Start PostgreSQL as shown above, then set a local development connection string
in the current shell without committing or printing it:

```powershell
$env:ConnectionStrings__TraderPro = '<local PostgreSQL connection string>'
```

Restore the repository-local EF Core tool, list the migration, generate an
ignored idempotent SQL script, and apply it to the explicitly configured local
database:

```powershell
.\scripts\migration\restore-tools.ps1
.\scripts\migration\list.ps1
.\scripts\migration\generate-script.ps1 `
    -Output .\artifacts\migrations\traderpro.sql
.\scripts\migration\apply-local.ps1
```

Create a future reviewed migration with:

```powershell
.\scripts\migration\new.ps1 -Name DescriptiveMigrationName
```

The API and Worker register persistence but never call
`Database.Migrate()` during startup. The API exposes `/health/live` and
database-backed `/health/ready`.

The API also requires the explicit authentication settings documented below;
it has no committed or fallback signing secret. Run the Worker after setting
the local connection variable:

```powershell
dotnet run --project .\services\backend\src\TraderPro.Worker
```

## Cloud-command spike (non-production)

The Task 5 `CommandProbe` endpoints are a Platform proof of concept, not
production business APIs. They are disabled in committed configuration and
are mapped only in Development or Testing when explicitly enabled:

```powershell
$env:DOTNET_ENVIRONMENT = 'Development'
$env:TraderPro__Spikes__Enabled = 'true'
dotnet run --project .\services\backend\src\TraderPro.Api
```

Spike and temporary event-cursor requests require
`X-TraderPro-Workspace-ID` containing an existing workspace UUID. This header
is temporary development/testing context and **is not authentication**.
`X-Correlation-ID` is optional but must be a canonical UUID when supplied.
Commands require `Idempotency-Key`; increments also require positive
`X-Expected-Version`.

Apply the reviewed schema through the ordinary migration workflow:

```powershell
dotnet ef database update `
  --project .\services\backend\src\TraderPro.Infrastructure `
  --startup-project .\services\backend\src\TraderPro.Api
```

The reviewed migrations are `AddCloudCommandAndEventCursorSpike` followed by
`HardenCloudCommandAndEventCursorSpike`. API and Worker startup still never
apply migrations automatically.

## Production identity and device sessions

Task 7A adds the backend identity prerequisite for future commercial APIs:
workspace/password login bound to one pre-created active Device, one-time
device activation, short-lived signed JWT access tokens, rotating hash-only
refresh tokens, immediate database revalidation, Owner/Operator policies,
logout, lockout, and safe response-loss replay. Reactivation rotates the
credential on the same Device row, so reinstall does not consume another
licensed slot.

Commercial authority is exposed through
`IAuthenticatedTraderProContext`. It comes from validated authentication and
current workspace/user/device/company/default-branch/role state. Temporary
`X-TraderPro-Workspace-ID` and `X-TraderPro-Device-ID` POC headers cannot
change `/api/v1/auth/me` or any other commercial context. Endpoint metadata,
not URL-prefix matching, selects commercial context, revoked-family
allowances, secret-response handling, and commercial authorization policy.
POC routes remain separate even when an `Authorization` header is present.

Required configuration is under `TraderPro:Authentication`:

- `Issuer`
- `Audience`
- `AccessTokenMinutes`
- `RefreshTokenDays`
- `RefreshReplaySeconds`
- `SigningKey`
- `DataProtectionKeyRingPath`
- `LockoutFailureLimit`
- `LockoutMinutes`
- `ActivationCodeMinutes`
- `RateLimitingEnabled`
- `RequireHttps`
- `ForwardedHeadersEnabled`
- `TrustedProxyAddresses`

Environment forms use double underscores, for example
`TraderPro__Authentication__SigningKey`. The signing key must be private
Base64 representing at least 256 random bits. Never commit or print signing
keys, Data Protection keys, passwords, activation codes, device secrets,
access tokens, refresh tokens, or connection strings. Production has no
fallback secret and refuses missing/unsafe authentication configuration or
enabled development spikes. Production also refuses `RequireHttps=false`.
Forwarded TLS state is trusted only when forwarding is explicitly enabled and
the immediate proxy's exact IP is allowlisted; arbitrary
`X-Forwarded-Proto` input is ignored.

All secret-bearing identity responses set `Cache-Control: no-store` and
`Pragma: no-cache`. Valid commercial correlation UUIDs are preserved through
errors and material audits. Owner-only denials use `OWNER_ROLE_REQUIRED`;
other commercial authorization denials use `AUTHORIZATION_DENIED`.

PostgreSQL transaction-scoped advisory locks use one deterministic hierarchy:
bootstrap, user session, activation device, device credential, token family,
token identity, then command idempotency; family collections are UUID-sorted.
This serializes login/logout-all, refresh/logout, and login or refresh against
device reactivation across API instances. Refresh replay is permitted only
while the exact same-family replacement remains active, unconsumed, and
hash-verified. Once B rotates to C, presenting predecessor A revokes the family
and records reuse exactly once. Protected replay material is cleared as soon
as it is no longer needed.

`HardenProductionIdentitySessionSecurity` follows the original identity
migration. It enforces same-workspace/family token chains, one predecessor per
replacement, valid rotation timing, immutable token facts, and one active
activation code per Device. Issuance and redemption share a device lock.
Repeating a successful activation-code idempotency key returns
`DEVICE_ACTIVATION_CODE_RESPONSE_NOT_REPLAYABLE` without persisting or
returning the code again; a new key intentionally replaces the prior active
code.

The Development/Testing identity bootstrap flag is
`TraderPro:Spikes:IdentityBootstrap:Enabled`; it is false by default and never
mapped in Production. Start a safe local process with:

```powershell
powershell -ExecutionPolicy Bypass `
  -File .\scripts\poc\start-production-identity-backend.ps1 `
  -Url 'http://localhost:5000' `
  -DataProtectionKeyRingPath `
    '.\artifacts\development-identity-key-ring' `
  -GenerateTemporarySigningKey `
  -EnableIdentityBootstrap
```

Run the focused identity, PostgreSQL 18/API, architecture, and Task 5/6A
regression suite with:

```powershell
powershell -ExecutionPolicy Bypass `
  -File .\scripts\test\test-production-identity-foundation.ps1
```

Follow
[`TPRUN-002-Development-Identity-Bootstrap.md`](docs/runbooks/TPRUN-002-Development-Identity-Bootstrap.md).
The design is documented in
[`TPTECH-001.18-Production-Identity-and-Device-Sessions.md`](docs/technical-specs/TPTECH-001.18-Production-Identity-and-Device-Sessions.md)
and
[`ADR-0005-authenticated-commercial-context.md`](docs/decisions/ADR-0005-authenticated-commercial-context.md).
Flutter activation, secure token storage, login screens, and authenticated
mobile networking are explicitly deferred.

## Commercial operational master data

Task 7B1 adds the first production commercial masters behind the authenticated
Task 7A context:

- shared Operations Business Locations;
- minimal Procurement Receiving Vehicles;
- Procurement Bag Types with exact `numeric(20,6)` tare;
- Task 3-aligned Weight Processing Policies;
- explicit Company Procurement Settings for destination, policy, and optional
  vehicle selection.

Owner-authenticated routes create, update, deactivate, reactivate, and
configure these records. Owner and Operator routes read them:

```text
/api/v1/operations/locations
/api/v1/procurement/vehicles
/api/v1/procurement/bag-types
/api/v1/procurement/weight-policies
/api/v1/procurement/settings
```

Commercial authority comes only from `IAuthenticatedTraderProContext`.
Temporary POC workspace/device headers cannot change it. Codes are normalized
and immutable, mutations require PostgreSQL-backed idempotency, updates use
expected versions, physical deletion is prohibited, and material changes
commit with immutable audit facts and `Internal` outbox events. The current
default destination and weight policy must be changed in settings before they
can deactivate.

`HardenCommercialOperationalMasterDataContracts` persists each real
aggregate revision, guards one-step Version/timestamp transitions in
PostgreSQL, validates vehicle registration normalization, requires explicit
Bag Type returnability, scope-binds list cursors, and serializes Procurement
default assignment/deactivation per company. Internal events do not acquire
the Task 5/6 global MobileSync cursor-order lock.

Run the focused unit, PostgreSQL 18/API, architecture, Task 7A, and Task 6A
suite with:

```powershell
powershell -ExecutionPolicy Bypass `
  -File .\scripts\test\test-commercial-operational-master-data.ps1
```

Follow
[`TPRUN-003-Commercial-Operational-Master-Data.md`](docs/runbooks/TPRUN-003-Commercial-Operational-Master-Data.md).
The design is documented in
[`TPTECH-001.19-Commercial-Operational-Master-Data.md`](docs/technical-specs/TPTECH-001.19-Commercial-Operational-Master-Data.md)
and
[`ADR-0006-commercial-operational-master-data.md`](docs/decisions/ADR-0006-commercial-operational-master-data.md).

Task 7B1 does not add commercial Receiving, settlement, Inventory, Sales,
Finance, Production, Fleet Management, Flutter screens, or mobile commercial
sync.

## Commercial Supplier and Product Catalog

Task 7B2 adds production Suppliers, Product Groups, Products, optional
Supplier Product Scope, and product-specific Standard Bag Weights behind the
authenticated Task 7A context. Product Types and explicit purchasability
prepare selection for Task 7C. Optional normalized Processing Family allows
Aman, H Aman, and P Aman products to share `AMAN` without introducing
Production behavior.

Owner-authenticated routes manage records; Owners and Operators may read:

```text
/api/v1/procurement/suppliers
/api/v1/procurement/suppliers/{supplierId}/product-scopes
/api/v1/catalog/product-groups
/api/v1/catalog/products
/api/v1/catalog/products/{productId}/bag-standards
```

Supplier scope defaults to Unrestricted. Restricted suppliers require an
Active Product association and cannot lose their final Active scope. Standard
bag content is an exact product-specific net weight and remains separate from
Task 7B1 Bag Type tare. At most one Active default standard exists per
Product. Active associations protect Product, Product Group, and Bag Type
deactivation.

Codes and ownership are immutable, physical DELETE is prohibited, successful
mutations advance persisted versions, and PostgreSQL advisory locks protect
scope/default and in-use races. Commands reuse database idempotency and commit
safe audit plus `Internal` outbox facts atomically. Presence-aware update
contracts reject immutable codes and bag-standard Product, Bag Type, or
default fields before idempotency begins. Audit snapshots explain safe material
changes separately from minimal Internal event payloads. Supplier protected
values never enter either projection; audits identify protected changes only
through deterministic field names. Product audits retain safe Description
before/after values and identify Notes changes through a deterministic `notes`
field name without storing raw Notes; Product Internal events keep their
minimal shape. Initial Supplier scopes, including Unrestricted initial scopes,
each receive an audit and event in Product ID order, and the entire
Supplier/scope/fact/result composite rolls back if any scope event fails.
Commercial catalog events remain outside the temporary POC MobileSync cursor.

The follow-up
`20260801030919_HardenCommercialSupplierProductCatalogContracts` migration
strengthens direct-SQL Supplier email validation to require canonical lowercase
trimmed values, exactly one non-edge `@`, and no whitespace or control
characters. The original Task 7B2 migration remains unchanged.

Run the focused unit, PostgreSQL 18/API, architecture, Task 7B1, Task 7A, and
Task 6A suite with:

```powershell
powershell -ExecutionPolicy Bypass `
  -File .\scripts\test\test-commercial-supplier-product-catalog.ps1
```

Follow
[`TPRUN-004-Commercial-Supplier-and-Product-Catalog.md`](docs/runbooks/TPRUN-004-Commercial-Supplier-and-Product-Catalog.md).
The design is documented in
[`TPTECH-001.20-Commercial-Supplier-and-Product-Catalog.md`](docs/technical-specs/TPTECH-001.20-Commercial-Supplier-and-Product-Catalog.md)
and
[`ADR-0007-commercial-supplier-and-product-catalog.md`](docs/decisions/ADR-0007-commercial-supplier-and-product-catalog.md).

Task 7B2 does not implement supplier balances, opening
payables, stock, prices, rates, settlement, accounting, Purchase Bills,
Inventory movements, Sales, Finance, Production, recipes, Flutter screens, or
commercial mobile sync.

## Task 7C0 Commercial Receiving design

Task 7C0 freezes the architecture, contracts, and mobile-security boundaries
for production Commercial Receiving from local-first immutable capture through
`SubmittedForSettlementReview`. The accepted design separates durable editor
ownership, ownership generation, and renewable lease; assigns a distinct cloud
Receiving reference; snapshots validated commercial masters; defines
authenticated operation, event, and master synchronization; and requires a
separate encrypted production mobile database.

Task 7C1 now implements the authenticated production backend in a separate
Domain/Application/Infrastructure/API boundary. It adds immutable Session,
Entry, ownership, reference policy/counter, commercial event audience, and
commercial master-change persistence through the reviewed
`AddCommercialReceivingBackend` migration and the reviewed
`HardenCommercialReceivingBackendContracts` follow-up. Receiving mutations are
Operator-only; durable operation claims retain `NeedsAttention` identity;
Start reserves non-reusable references before aggregate execution; and
deterministic master locks, snapshot triggers, deferred audience constraints,
and Session/ownership transition guards protect the commit boundary. Owner
transfer clears the lease, and the authenticated target Operator must acquire
its own lease. Master sync uses explicit camel-case `contractVersion: 1`
selection payloads rather than table-row JSON. Task 7C2 Flutter work remains
deferred. The Task 5/6 Procurement POC remains separate and unchanged.

The current Flutter/Drift database still uses ordinary, **unencrypted SQLite**.
It is not approved for pilot or production customer data. Task 7C2 must complete
a reviewed encrypted-SQLite and OS secure-storage compatibility spike before
selecting packages or enabling Commercial Receiving.

Commercial Receiving documents:

- [TPRC-101 requirements baseline](docs/product-specs/TPRC-101-Commercial-Receiving-Requirements-Baseline.md)
- [TPRC-101 open questions](docs/product-specs/TPRC-101-Open-Questions.md)
- [TPTECH-001.21 Commercial Receiving contract and mobile security](docs/technical-specs/TPTECH-001.21-Commercial-Receiving-Contract-and-Mobile-Security-Design.md)
- [TPTECH-001.22 Commercial Receiving backend](docs/technical-specs/TPTECH-001.22-Commercial-Receiving-Backend.md)
- [ADR-0008 ownership and offline synchronization](docs/decisions/ADR-0008-commercial-receiving-ownership-and-offline-sync.md)
- [ADR-0009 authenticated mobile sync and storage](docs/decisions/ADR-0009-authenticated-commercial-mobile-sync-and-storage.md)
- [ADR-0010 references and sync streams](docs/decisions/ADR-0010-commercial-receiving-reference-and-sync-streams.md)
- [TPRUN-005 Commercial Receiving backend runbook](docs/runbooks/TPRUN-005-Commercial-Receiving-Backend.md)
- [TPSEC-001 Commercial Mobile Receiving threat model](docs/threat-models/TPSEC-001-Commercial-Mobile-Receiving-Threat-Model.md)
- [Task 7C1 Commercial Receiving Backend plan](docs/task-plans/TASK-7C1-Commercial-Receiving-Backend.md)
- [Task 7C2 Secure Flutter Commercial Receiving plan](docs/task-plans/TASK-7C2-Secure-Flutter-Commercial-Receiving.md)

Task 7C1 is the implemented authenticated production backend. The next
milestone is Task 7C2 for secure Flutter authentication, encrypted local storage, offline
capture/sync, and read-only Owner monitoring. Missing TPCL-101/TPFS-101 details
remain explicit gates; Task 7C0 does not invent cancellation, correction,
or settlement/posting behavior. Task 7C1 resolves focused Owner transfer and
automatic Receiving-reference behavior only.

## Two-device Procurement backend POC (non-production)

Task 6A adds a development/testing-only backend proof of concept. It is not a
commercial Procurement workflow and creates no Purchase Bill, Inventory,
supplier payable, Sales, or Finance effects. Both spike flags must be enabled;
committed configuration keeps them false and Production never maps the routes:

```powershell
$env:DOTNET_ENVIRONMENT = 'Development'
$env:TraderPro__Spikes__Enabled = 'true'
$env:TraderPro__Spikes__ProcurementPoc__Enabled = 'true'
$env:TraderPro__Spikes__ProcurementPoc__LeaseMinutes = '5'
dotnet run --project .\services\backend\src\TraderPro.Api
```

Create the stable insecure local POC setup (IDs only, no credentials) with:

```powershell
Invoke-RestMethod `
  -Method Post `
  -Uri 'http://localhost:5000/api/v1/spikes/procurement-poc/bootstrap'
```

POC requests require both `X-TraderPro-Workspace-ID` and
`X-TraderPro-Device-ID`. These temporary headers are **not authentication**.
The primary routes are:

- `POST /api/v1/mobile/sync/operations`
- `GET /api/v1/mobile/sync/events`
- `GET /api/v1/spikes/procurement-poc/receiving-sessions`
- `GET /api/v1/spikes/procurement-poc/receiving-sessions/{id}/live-view`
- `POST /api/v1/spikes/procurement-poc/receiving-sessions/{id}/heartbeat`
- `POST /api/v1/spikes/procurement-poc/receiving-sessions/{id}/approve`
- `POST /api/v1/spikes/procurement-poc/receiving-sessions/{id}/finalize`

Run the focused unit, PostgreSQL 18/API, concurrency, rollback, architecture,
and Task 5 regression suite with:

```powershell
powershell -ExecutionPolicy Bypass `
  -File .\scripts\test\test-two-device-procurement-backend-poc.ps1
```

The reviewed migrations are `AddTwoDeviceProcurementPocFoundation` followed by
`HardenTwoDeviceProcurementPocContracts`. Task 6B connects the Flutter Drift
outbox and enriches its network envelope with the current lease without
rewriting immutable payloads. Task 6A itself does not modify the mobile schema
or implement Flutter HTTP sync, SignalR, authentication, RLS, subscriptions,
or production posting.

## Flutter two-device Procurement POC (non-production)

Task 6B adds a **Development Only** foreground Flutter sync client for the
frozen Task 6A backend. It preserves Task 4 payload JSON/hashes, stores leases
outside immutable physical payloads, resumes the MobileSync cursor, and gives
an independent owner device a read-only approval/finalization view. It creates
no Purchase Bill, Inventory, supplier payable, Sales, or Finance effect.

One app-level runtime owns the Task 6B database, repository, guarded API
factories, engines, coordinator, controller, timer, and lifecycle observer.
Only `resumed` permits foreground work, and one gate serializes automatic and
manual cycles before any asynchronous lookup. Completed or safely failed
cycles reload all Drift-backed operator/owner/controller state, so Start,
entry, event, and heartbeat results appear without a manual refresh.

Drift schema version 3 binds cloud lease state and control commands immutably
to their source/device. Direct schema `1 -> 3` and follow-up `2 -> 3`
migrations preserve Task 4 bytes and existing POC rows; ambiguous context
backfill fails explicitly. The POC engine filters only its three
ReceivingSession operation types and validates successful operation/command
responses semantically. Finalization replay requires this active
source/device's completed command, and manual capture requires explicit valid
UTC text.

The Flutter route is disabled by default and is unavailable in release mode
even if a define is supplied. Build the debug POC explicitly:

```powershell
Push-Location .\apps\mobile
flutter pub get
flutter build apk `
  --debug `
  --dart-define=TRADERPRO_PROCUREMENT_POC=true
Pop-Location
```

Start the backend with process-scoped development flags and a configurable LAN
binding:

```powershell
powershell -ExecutionPolicy Bypass `
  -File .\scripts\poc\start-two-device-procurement-backend.ps1 `
  -BindAddress 0.0.0.0 `
  -Port 5000
```

The launcher does not create or print a database credential, reset PostgreSQL,
or create a Windows Firewall rule. Temporary Workspace/Device headers and
debug HTTP are insecure development mechanisms and **are not
authentication**.

Run the focused mobile/client suite and regressions:

```powershell
powershell -ExecutionPolicy Bypass `
  -File .\scripts\test\test-two-device-procurement-mobile-poc.ps1
```

Follow the physical Android procedure in
[`TPRUN-001-Two-Device-Procurement-Poc.md`](docs/runbooks/TPRUN-001-Two-Device-Procurement-Poc.md).
The local design is documented in
[`TPTECH-001.17-Flutter-Sync-Two-Device-Field-Poc.md`](docs/technical-specs/TPTECH-001.17-Flutter-Sync-Two-Device-Field-Poc.md)
and
[`ADR-0004-mobile-sync-envelope-and-projection-poc.md`](docs/decisions/ADR-0004-mobile-sync-envelope-and-projection-poc.md).

## Current scaffold status

- The .NET solution establishes Domain, Application, Infrastructure, API, and Worker projects.
- Module-registration boundaries establish the planned modular-monolith seams.
- EF Core and Npgsql map the Platform schema, including the temporary
  `command_probes` spike table.
- `InitialPlatformFoundation` is the first reviewed migration.
- `AddCloudCommandAndEventCursorSpike` adds stored idempotent HTTP outcomes and
  the database event sequence.
- `HardenCloudCommandAndEventCursorSpike` adds controlled event streams,
  immutable outbox event facts, and conservative legacy replay repair.
- `AddTwoDeviceProcurementPocFoundation` adds the disabled-by-default
  Procurement POC aggregate, immutable weight facts/completion record, leased
  two-device API workflow, and development bootstrap.
- `HardenTwoDeviceProcurementPocContracts` preserves immutable Task 4 payloads,
  binds POC idempotency to device and operation scope, and hardens session
  identity, lifecycle shape, and deletion rules.
- `AddProductionIdentityAndDeviceSessions` adds immutable commercial workspace
  codes, password/device credentials, activation codes, rotating
  refresh-token families, and database transition controls.
- `HardenProductionIdentitySessionSecurity` constrains token chains to one
  workspace/family, protects immutable security facts and replay clearing, and
  enforces one active activation code per Device.
- `AddCommercialOperationalMasterData` adds production Business Locations,
  Receiving Vehicles, Bag Types, Weight Processing Policies, and the
  company-level procurement defaults that bind them.
- Task 7C0 documents the production Commercial Receiving aggregate, two-state
  lifecycle, ownership generation/lease recovery, authenticated commercial
  operation/event/master sync, secure mobile storage strategy, threat model,
  and Task 7C1/7C2 backlogs.
- `AddCommercialReceivingBackend` implements the separate authenticated
  production Session/Entry/ownership/reference boundary, commercial event
  audiences/cursor, and safe commercial master change log through submission
  for settlement review, with no settlement or posting effects.
- `HardenCommercialReceivingBackendContracts` adds durable mobile-operation
  claims, non-reusable reference reservations, same-company master foreign
  keys and snapshot validation, deferred commercial-audience completeness,
  serialized transfer/acquisition guards, and explicit versioned master
  payload builders without changing the POC or adding posting effects.
- The API validates signed access tokens and revalidates current commercial
  workspace/user/device/company/default-branch/role authority on every
  protected request; temporary POC context remains separate.
- Workspace query filters and write validation provide application-level
  tenant scoping; PostgreSQL Row-Level Security remains a mandatory
  pre-production gate.
- Audit records are protected from UPDATE and DELETE by a PostgreSQL trigger.
- Architecture, unit, and integration test projects establish test locations.
- The pure weight-processing contract is shared across .NET and Flutter through
  versioned JSON golden vectors and exact scaled-integer implementations.
- The focused Flutter/Drift offline-store spike persists local Receiving
  Sessions, immutable processed-weight facts, rebuildable exact projections,
  and future-sync outbox operations in atomic SQLite transactions.
- File-backed tests prove restart recovery, deterministic duplicate handling,
  independent session ordering, projection rebuilding, and explicit outbox
  transitions without network transmission.
- The production Flutter entry remains a minimal foundation startup
  experience. An explicitly compiled debug-only route provides the Task 6B
  two-device POC through injected services; widgets do not access Drift or
  construct HTTP requests.
- Drift schema version 3 provides non-destructive `1 -> 3` and `2 -> 3`
  migration paths, source/device-bound cloud state and commands, and preserves
  every Task 4 immutable fact, payload, and operation.
- The Task 6B client uses `dart:io` without a new dependency, synchronizes
  Start before leased dependent operations, filters only POC receiving rows,
  rejects semantically invalid successful responses, replays ambiguous
  responses with stable context-bound keys, and polls the durable event cursor
  in the foreground.
- The app-level Task 6B runtime owns lifecycle and resources; shared
  orchestration gates prevent overlaps, while automatic cycles refresh visible
  Drift state and disposal prevents later notifications or API starts.
- PostgreSQL 18 can run locally through Docker Compose and disposable
  Testcontainers.
- Contract, documentation, infrastructure, script, and cross-system test directories are tracked with placeholders.
- No production deployment credential or automatic startup migration exists.

## Explicitly unimplemented capabilities

The following capabilities are intentionally outside this scaffold:

- Commercial Receiving Flutter workflows and UI (Task 7C2)
- Task 7C2 secure Flutter Commercial Receiving, authentication, encrypted
  local database, and commercial offline synchronization
- Inventory movements
- Financial posting
- Sales workflows
- Production workflows
- Subscription billing
- Flutter authentication/activation UI and secure token storage
- MFA, password reset, SSO, and production onboarding
- Production cloud synchronisation (Task 6B is a debug-only POC)
- PDF generation
- Business-module migrations and domain entities beyond the Task 7B2
  commercial Supplier and Product catalog
- SignalR integration and cloud synchronisation

The implemented database foundation is documented in
`docs/technical-specs/TPTECH-001.12-Foundation-Database.md`. Workspace
isolation decisions and the remaining RLS security gate are documented in
`docs/decisions/ADR-0001-workspace-isolation-foundation.md`.

The focused mobile store is documented in
`docs/technical-specs/TPTECH-001.14-Mobile-Offline-Store-Spike.md`.
It currently uses ordinary, unencrypted SQLite and is **not production-ready
for sensitive customer data**. Encrypted local storage selection, key
management, and migration remain mandatory pre-pilot security work.

These omissions are intentional. Future work should introduce each capability through reviewed specifications and tests while preserving the rules in `AGENTS.md`.

### Task 7C1 final contract corrections

`20260802120000_FinalizeCommercialReceivingBackendContracts` completes the
Commercial Receiving backend contract. Immutable mobile payloads are scanned
recursively and reject lease/cloud-version capabilities at any depth; a later
same-Session batch item blocked by an earlier item returns retryable
`RECEIVING_OPERATION_WAITING_FOR_PRIOR_SEQUENCE` without a claim or
idempotency record. Every claim transition is serialized by the commercial
command scope plus `OperationId`, and terminal claims are database-immutable.

PostgreSQL now locks every Session/Entry master row while validating snapshots,
rejects vehicle facts when vehicle selection is Disabled, enforces the explicit
heartbeat/reacquire/transfer/submission ownership state machine, and validates
mobile claim/reservation UUIDv7 identities. Monitoring uses the later of the
Session and Ownership update times and exposes acquisition/reacquisition
attention. Reconstructible original-Task-7C1 operations retain exact replay;
unknown legacy identities are explicitly non-replayable and cannot reserve a
second reference.

All Task 7C1 migrations are forward-only. After finalization, `dotnet ef
database update <older migration>` is unsupported; rollback means application
rollback plus database restore from backup.

### Task 7C1 reference-contract seal

`20260802130000_SealCommercialReceivingReferenceContracts` enforces one
durable reference reservation per workspace/company/Session. Start validates
and rejects its typed immutable payload after claiming the Operation but before
locking the reference series or advancing its counter. Invalid commands never
allocate; a valid Start that later needs business attention may retain its
reservation and intentional numbering gap.

Reservations are historical numbering-policy snapshots. Exact retry consumes
the original rendered reference even after the current policy format/version
changes, while a new Session uses the new format. Start lock order is claim,
reference series, reservation/policy/counter, Session/defaults, Supplier,
settings, destination, Weight Policy, and optional Vehicle. Entry lock order is
claim, Session, Ownership, Product, optional scope, Bag Type, and optional
standard weight. The seal is also forward-only.
