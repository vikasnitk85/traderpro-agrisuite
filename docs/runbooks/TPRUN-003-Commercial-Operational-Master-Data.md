# TPRUN-003: Commercial Operational Master Data

## Development-only prerequisites

Use a local PostgreSQL 18 database, the .NET 10 SDK, and the Task 7A
development identity bootstrap. Do not use production data, credentials,
tokens, signing keys, activation codes, device secrets, or connection
strings. Keep all secret values in private shell variables and never print
them.

## Start the database and backend

From the repository root, start PostgreSQL without deleting or recreating its
volume:

```powershell
docker compose `
  --env-file .\infrastructure\docker\.env `
  -f .\infrastructure\docker\compose.yml `
  up -d postgres
```

Privately set `ConnectionStrings__TraderPro`, then apply the reviewed
migrations:

```powershell
.\scripts\migration\apply-local.ps1
```

Start the authenticated Development backend with the identity bootstrap only:

```powershell
powershell -ExecutionPolicy Bypass `
  -File .\scripts\poc\start-production-identity-backend.ps1 `
  -Url 'http://localhost:5000' `
  -DataProtectionKeyRingPath `
    '.\artifacts\development-identity-key-ring' `
  -GenerateTemporarySigningKey `
  -EnableIdentityBootstrap
```

This launcher keeps Task 5/6 spike routes disabled. Follow
`TPRUN-002-Development-Identity-Bootstrap.md` to bootstrap, activate the
pre-created Owner and Operator Devices, and log in. Store Owner and Operator
access tokens only in private variables.

## Configure representative masters

Use `Authorization: Bearer <Owner access token>`. Give every logical mutation
a new `Idempotency-Key`; retain the same key only for an identical retry after
an ambiguous response. Use a positive `X-Expected-Version` from the last
successful result for updates and status changes.

1. Create one Warehouse or Yard with
   `POST /api/v1/operations/locations`.
2. Create one optional receiving truck with
   `POST /api/v1/procurement/vehicles`.
3. Create Jute, SinglePlastic, and DoublePlastic bag types with
   `POST /api/v1/procurement/bag-types`. Supply
   `standardTareWeightKg` as a decimal string, never a JSON number, and always
   supply `isReturnable` explicitly as `true` or `false`.
4. Create Standard, Floor, and Ceiling policies with
   `POST /api/v1/procurement/weight-policies`, using precision 1-3.
5. Configure the created destination and one active policy with
   `PUT /api/v1/procurement/settings`. The first configuration has no expected
   version. Later changes require `X-Expected-Version`.

Do not put WorkspaceId, CompanyId, BranchId, UserId, or DeviceId in request
bodies. If temporary POC headers are present accidentally, confirm they do not
change the returned Branch or visible records.

## Verify access and protections

Using the Operator token:

- GET each collection and item succeeds;
- POST, PUT, deactivate, reactivate, and settings PUT return 403
  `OWNER_ROLE_REQUIRED`.

Without a bearer token, commercial routes return 401. A stale or revoked Task
7A session also returns 401.

Using the Owner token:

- GET collection defaults to Active records;
- `status=Inactive` explicitly returns inactive records;
- search, opaque cursor, and `limit` up to 100 work deterministically;
- a cursor cannot be reused for another master, tenant, status, or search;
- stale expected versions return `MASTER_VERSION_CONFLICT`;
- each successful mutation persists and returns exactly the next Version and
  current server UpdatedAtUtc; GET returns those same revision facts;
- identical idempotent retry returns the original result;
- changed payload with the same key returns
  `IDEMPOTENCY_PAYLOAD_CONFLICT`;
- the selected destination cannot deactivate until settings point elsewhere;
- the selected policy cannot deactivate until settings point elsewhere.

The temporary `/api/v1/mobile/sync/events` POC cursor, when separately enabled
for development evidence, must not return these Internal commercial events.
Do not enable Task 5/6 spikes merely for routine commercial master setup.

The follow-up migration
`HardenCommercialOperationalMasterDataContracts` must immediately follow
`AddCommercialOperationalMasterData`. It adds revision guards, registration
consistency, and the per-company default lock. Never replace or regenerate the
earlier applied migration.

## Automated verification

Run:

```powershell
powershell -ExecutionPolicy Bypass `
  -File .\scripts\test\test-commercial-operational-master-data.ps1
```

The script runs focused unit, PostgreSQL/API, architecture, Task 7A identity,
and Task 6A backend regression tests. Testcontainers use a disposable
PostgreSQL 18 database and do not touch the Compose volume.

Stop the backend after the exercise. Clear password, activation, device
secret, access-token, refresh-token, and header variables from the shell.
