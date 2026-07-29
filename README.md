# TraderPro AgriSuite

## Product summary

TraderPro AgriSuite is an Android-first agricultural trading and operations product. Its foundation combines a Flutter mobile application with a .NET 10 modular-monolith backend, PostgreSQL 18, one deployable API, and one background worker. Business data is cloud-authoritative; the mobile architecture will preserve locally captured physical facts before synchronisation.

This repository contains a buildable foundation with the first Platform
database migration and a focused mobile offline-store proof of concept. It
establishes project, module, dependency, test, and local-infrastructure
boundaries without implementing complete production business workflows.

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

Run either backend only after setting the local connection variable:

```powershell
dotnet run --project .\services\backend\src\TraderPro.Api
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
- The Flutter UI provides only a minimal foundation startup experience and
  does not access Drift; the core layer includes the pure weight-processing
  contract and local database foundation.
- PostgreSQL 18 can run locally through Docker Compose and disposable
  Testcontainers.
- Contract, documentation, infrastructure, script, and cross-system test directories are tracked with placeholders.
- No production deployment credential or automatic startup migration exists.

## Explicitly unimplemented capabilities

The following capabilities are intentionally outside this scaffold:

- Complete Receiving Session workflows and UI
- Inventory movements
- Financial posting
- Sales workflows
- Production workflows
- Subscription billing
- Full authentication and authorisation
- Cloud synchronisation
- PDF generation
- Business-module database migrations and domain entities
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
