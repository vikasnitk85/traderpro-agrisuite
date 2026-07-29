# TraderPro AgriSuite

## Product summary

TraderPro AgriSuite is an Android-first agricultural trading and operations product. Its foundation combines a Flutter mobile application with a .NET 10 modular-monolith backend, PostgreSQL 18, one deployable API, and one background worker. Business data is cloud-authoritative; the mobile architecture will preserve locally captured physical facts before synchronisation.

This repository currently contains a buildable foundation only. It establishes project, module, dependency, test, and local-infrastructure boundaries without implementing production business workflows.

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
dotnet restore .\TraderPro.sln

Push-Location .\apps\mobile
flutter pub get
Pop-Location
```

The copied `.env` is for local development only and is ignored by Git. Replace its placeholder password on any shared or long-lived environment.

## Build commands

```powershell
dotnet build .\TraderPro.sln --configuration Debug --no-restore

Push-Location .\apps\mobile
flutter build apk --debug
Pop-Location
```

## Test commands

```powershell
dotnet test .\TraderPro.sln --configuration Debug --no-build

Push-Location .\apps\mobile
flutter analyze
flutter test
Pop-Location
```

The .NET test command runs unit, integration, and architecture test projects. Integration tests remain scaffold-level until database-backed behaviour is introduced.

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

The optional `future-apps` Compose profile documents future API and Worker runtime configuration. Its image names are deliberate placeholders and the profile must not be started until deployable application images exist.

## Current scaffold status

- The .NET solution establishes Domain, Application, Infrastructure, API, and Worker projects.
- Empty module-registration boundaries establish the planned modular-monolith seams.
- Architecture, unit, and integration test projects establish test locations.
- The Flutter app provides only a minimal foundation startup experience and placeholder feature folders.
- PostgreSQL 18 can run locally through Docker Compose.
- Contract, documentation, infrastructure, script, and cross-system test directories are tracked with placeholders.
- No production database migration or detailed domain entity exists.

## Explicitly unimplemented capabilities

The following capabilities are intentionally outside this scaffold:

- Receiving Session business logic
- Inventory movements
- Financial posting
- Sales workflows
- Production workflows
- Subscription billing
- Full authentication and authorisation
- Cloud synchronisation
- PDF generation
- Production database migrations and detailed domain entities
- Drift/SQLite persistence and SignalR integration

These omissions are intentional. Future work should introduce each capability through reviewed specifications and tests while preserving the rules in `AGENTS.md`.
