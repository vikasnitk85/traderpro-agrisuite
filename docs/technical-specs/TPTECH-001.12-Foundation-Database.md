# TPTECH-001.12: Foundation Database

## Scope

This specification records the first PostgreSQL and EF Core foundation for
TraderPro AgriSuite. It introduces only Platform persistence. No Procurement,
Inventory, Sales, Finance, Production, subscription billing, authentication,
sync, PDF, or posting behavior is implemented.

## Runtime and dependencies

- .NET 10
- Entity Framework Core Design 10.0.10
- Npgsql Entity Framework Core provider 10.0.3
- PostgreSQL 18
- EF Core health checks 10.0.10
- Testcontainers.PostgreSql 4.13.0
- xUnit v3 (the existing 3.2.2 test baseline)
- repository-local `dotnet-ef` 10.0.10

All package and tool versions are pinned. No global tool is required.

## Schema and tables

`TraderProDbContext` owns the `platform` schema:

| Table | Purpose |
| --- | --- |
| `platform.workspaces` | Tenant roots |
| `platform.companies` | Workspace-owned legal/trading companies |
| `platform.branches` | Workspace- and company-owned branches |
| `platform.users` | Identity placeholders without credentials |
| `platform.devices` | Registered-device placeholders |
| `platform.idempotency_records` | Command idempotency outcomes |
| `platform.outbox_messages` | Mutable delivery lifecycle for transactional events |
| `platform.audit_events` | Append-only audit evidence |

The migration history table is
`platform.__ef_migrations_history`.

## Data conventions

- IDs are .NET `Guid` values generated with `Guid.CreateVersion7()`.
- IDs map to PostgreSQL `uuid`.
- Timestamps are `DateTimeOffset` values with a required zero UTC offset.
- Timestamp columns use `timestamp with time zone` (`timestamptz`).
- Versioned records start at `1`; Version maps to `bigint` and is an EF Core
  concurrency token.
- Every successful tracked modification increments Version exactly once.
- Payload and snapshot JSON maps to `jsonb` and is validated as JSON during
  controlled entity construction.
- Technical statuses are short-backed enums with database check constraints.
- No binary floating-point money or quantity field is introduced.

## Workspace scoping

Company, Branch, PlatformUser, Device, IdempotencyRecord, OutboxMessage, and
AuditEvent implement `IWorkspaceScoped`.

Global EF Core filters restrict ordinary queries to
`ICurrentWorkspaceAccessor.WorkspaceId`. A focused SaveChanges interceptor
assigns an omitted WorkspaceId on insert and rejects conflicting inserts,
cross-workspace updates/deletes, and ownership changes. Workspace itself is
unfiltered because it is the tenant root.

When no active workspace is present, normal workspace-owned queries return no
rows and every workspace-owned insert, update, or delete is rejected. An
explicit `Guid.Empty` WorkspaceId on a new record is assigned only when an
active workspace exists.

These are application controls, not the final tenant security boundary. Raw
SQL and `IgnoreQueryFilters` can bypass query filtering. PostgreSQL RLS remains
a mandatory pre-production blocker; see
`docs/decisions/ADR-0001-workspace-isolation-foundation.md`.

## Keys, constraints, and indexes

- Workspace Code is unique.
- Company Code is unique per workspace.
- non-null Company TaxRegistrationNumber is unique per workspace.
- Branch Code is unique per workspace and company.
- a PostgreSQL partial unique index permits only one `is_default = true`
  branch per company.
- a default Branch must remain Active until `IsDefault` is explicitly cleared.
- PlatformUser Username is unique per workspace.
- Device InstallationId is unique per workspace.
- IdempotencyRecord is unique by WorkspaceId, CommandType, and
  IdempotencyKey. RequestHash is retained so future command logic can detect a
  reused key with a different payload.
- Outbox EventVersion and AggregateVersion must be positive.
- Outbox AttemptCount cannot be negative.
- composite foreign keys ensure Branch, optional audit company/branch, and
  optional audit actors belong to the same workspace.
- an AuditEvent with BranchId must also have CompanyId, and its
  WorkspaceId + CompanyId + BranchId must reference the same Branch.
- document-history audit lookups use the
  WorkspaceId + AggregateType + AggregateId + OccurredAtUtc index.

## Audit immutability

The initial migration creates
`platform.reject_audit_event_mutation()` and the
`audit_events_append_only` trigger. PostgreSQL raises SQLSTATE `55000` before
any UPDATE or DELETE against `platform.audit_events`. Inserts remain allowed.

Audit generation is not automatic in this task. Callers must explicitly create
AuditEvent records when future reviewed workflows require them.

## Idempotency and outbox lifecycle

IdempotencyRecord begins Pending and has controlled Completed or Failed
terminal transitions. Re-executing a terminal transition is rejected.

OutboxMessage begins Pending and is deliberately mutable. Only Pending can
start Processing; Processing and terminal Processed/Failed records cannot
start again. A failed attempt with a retry schedule returns the message to
Pending. Attempt and retry metadata remain updateable because the publishing
worker is deferred.

## Registration and health

`AddTraderProPersistence(configuration)` registers the context, Npgsql,
current-workspace scope, system clock, interceptors, and database health check.
API and Worker both call this extension.

The API exposes:

- `/health/live`, which does not probe dependencies;
- `/health/ready`, which verifies the registered database context.

Neither host applies migrations automatically.

## Connection configuration

Supply the development connection string through:

```powershell
$env:ConnectionStrings__TraderPro = '<local PostgreSQL connection string>'
```

Do not commit the value. The design-time factory fails with a clear message
when the variable is absent. It does not contain a fallback password.

## Migration workflow

From the repository root:

```powershell
.\scripts\migration\restore-tools.ps1
.\scripts\migration\list.ps1
.\scripts\migration\new.ps1 -Name ExampleMigration
.\scripts\migration\generate-script.ps1 `
    -Output .\artifacts\migrations\traderpro.sql
.\scripts\migration\apply-local.ps1
```

The first migration is `InitialPlatformFoundation`. The reviewed follow-up
`HardenPlatformFoundationIntegrity` adds audit company/branch integrity and the
document-history lookup index without rewriting applied migration history.
Generated SQL under `artifacts` is ignored and is not a committed production
deployment artifact.

## Integration-test behavior

The integration fixture starts a disposable `postgres:18` Testcontainer with a
random test-only password. Each test creates a fresh database, applies all EF
Core migrations, runs against that database, and drops it with forced
connection cleanup. The container is disposed after the collection.

Tests require an available Docker engine and never use the developer's
long-lived Compose database.

## Deferred capabilities

The following remain explicitly deferred:

- PostgreSQL Row-Level Security and authenticated session context;
- authentication, authorization, passwords, PINs, and tokens;
- mobile device activation and capacity enforcement;
- outbox publishing worker;
- business-module commands, entities, and posting;
- automatic migration at host startup;
- production deployment scripts and credentials.
