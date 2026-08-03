# TPRUN-005: Commercial Receiving Backend

- Status: Task 7C1 local verification runbook
- Date: 2026-08-01

## Safety

Use only local/development PostgreSQL. Do not reset volumes, use production
credentials, enable POC headers as authority, or log access tokens, refresh
tokens, Device secrets, lease IDs, signing keys, or connection strings.

## Configure and migrate

Set `ConnectionStrings__TraderPro` privately for the existing local PostgreSQL
18 database. Restore the repository tool, inspect migrations, and apply both
forward-only migrations explicitly. The applied original migration must remain
unchanged; hardening is migration `HardenCommercialReceivingBackendContracts`:

```powershell
dotnet tool restore
dotnet ef migrations list --project .\services\backend\src\TraderPro.Infrastructure --startup-project .\services\backend\src\TraderPro.Api
dotnet ef database update --project .\services\backend\src\TraderPro.Infrastructure --startup-project .\services\backend\src\TraderPro.Api
```

Never delete/reset the PostgreSQL volume. API/Worker startup does not migrate.

## Focused validation

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\test\test-commercial-receiving-backend.ps1
```

The script builds the solution and runs focused domain/hash tests, PostgreSQL
migration/integrity tests, and architecture tests. Run the Task 7B2, 7B1, 7A,
6A, Task 5, mobile POC, weight parity, and offline-store scripts before release.

## Operational behavior

Canonical routes use the committed endpoint map: operations/events/masters are
under `/api/v1/mobile/commercial-sync`; Session list/live/lease/ownership are
under `/api/v1/procurement/receiving-sessions`; reference policy is under
`/api/v1/procurement/receiving-reference-policy`. Event and master reads accept
only the opaque `cursor` plus `limit`. Do not translate older documented
`after`, bootstrap-high-water, or `commercial-receiving-sessions` paths.

- Lease duration defaults to 60 minutes under
  `TraderPro:CommercialReceiving:LeaseMinutes`.
- Routine heartbeat targets 10 minutes while foregrounded and online.
- An expired lease is recovered only by the same durable editor and generation.
- Transfer is Owner-only and requires a safe reason plus optimistic Session and
  generation facts. It clears the lease; the authenticated target Operator must
  acquire its own lease, and an active valid lease cannot be rotated.
- Start, Entry, Submit, heartbeat, and lease acquisition/reacquisition are
  Operator-only. Owners retain monitoring, policy management, and transfer.
- `NeedsAttention` commits the durable operation identity but no business
  mutation or completed result; exact retry is allowed, changed reuse conflicts.
- A blocked same-Session follower is the special unattempted
  `RECEIVING_OPERATION_WAITING_FOR_PRIOR_SEQUENCE`: it is retryable and owns no
  claim, idempotency, audit, event, reference, or aggregate write. Expired lease
  reacquisition is `RECEIVING_LEASE_REACQUISITION_REQUIRED`; stale generation
  is `NeedsAttention`.
- A Start reserves its reference before Session execution. A failed Start may
  leave a permanent gap; another operation never receives that reservation.
- Receiving transactions lock selected masters. Direct SQL snapshots are also
  protected by same-company keys and validation triggers.
- Every Commercial outbox row must have exactly one valid audience by commit;
  Internal and POC rows must have none.
- Master changes use explicit camel-case version-1 payloads and canonical
  six-decimal strings; never inspect them as raw table-row JSON.
- Submission is terminal for Task 7C1 and creates no posting effect.
- The default reference is `RCV-{SEQ:000000}` with `Never` reset/start 1.
- `OwnerBroadcast` and `TargetDevice` are immutable issuance audiences. A
  former editor receives its transfer-away TargetDevice row; subsequent events
  target only the new editor. Do not add transfer reason, lease expiry, lease
  ID, or secrets to the version-1 ownership-change event.

## Diagnostics

Use correlation IDs and stable error codes. Inspect aggregate/audit/outbox
counts without printing payload secrets. Commercial event rows may contain
safe operational snapshots but never lease IDs or Supplier protected data.
Master sync reads only `sync.commercial_master_changes`.

When validating an upgrade, preserve and fingerprint existing Identity,
Task 7B masters, POC Receiving, and Internal/POC outbox rows before applying
all Task 7C1 migrations. Do not reset or delete the PostgreSQL volume.

## Finalization and recovery

Apply `20260802120000_FinalizeCommercialReceivingBackendContracts` after
`20260802090000_HardenCommercialReceivingBackendContracts` without resetting
the volume. The finalizer restores all Session user triggers, reconciles only
provable legacy Start/Entry identities, marks unsafe identities as stable
non-replayable facts, then installs serialized claim guards, locked snapshot
validation, the exact ownership state machine, Disabled-vehicle enforcement,
monitoring corrections, and UUIDv7 checks.

Apply `20260802130000_SealCommercialReceivingReferenceContracts` immediately
after the finalizer. Before application, verify there is no duplicate
`(workspace_id, company_id, session_id)` reservation; the migration fails
closed rather than discarding an immutable reservation. After application,
verify the unique index, exact retry across a reference-policy format update,
and invalid typed Start rejection without counter advancement.

An installation still exactly at the original Task 7C1 migration with existing
Sessions must use a maintenance window and database-owner preflight because the
immutable hardening migration performs a version-neutral Session backfill:

```sql
ALTER TABLE procurement.commercial_receiving_sessions DISABLE TRIGGER USER;
```

Apply the hardening migration and then the finalizer immediately. The finalizer
executes `ENABLE TRIGGER USER`; verify all Receiving triggers are enabled before
reopening writes. Never leave the database between those steps.

Task 7C1 migrations are forward-only. Do not run `dotnet ef database update
<older migration>` after finalization. Roll back the application and restore the
database from a known-good backup. Recursive payload rejection, retryable batch
waiting, exact claim/ownership transitions, master locking, and the later
Session/Ownership monitoring timestamp are required post-migration smoke tests.

For concurrency diagnosis, preserve the canonical orders. Start is Operation
claim, company reference series, reservation/policy/counter, Session/defaults,
Supplier, settings, destination, Weight Policy, optional Vehicle. Entry is
Operation claim, Session, Ownership, Product, optional scope, Bag Type,
optional standard weight. Do not work around waits by changing this order or by
depending on PostgreSQL deadlock detection.
