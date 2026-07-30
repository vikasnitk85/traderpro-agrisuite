# TPTECH-001.17: Flutter Sync and Two-Device Field POC

## Objective and scope

Task 6B is a development-only Flutter proof that two independent app
installations can exercise the frozen Task 6A Procurement POC:

- Mobile A captures one local session and five manual immutable weights,
  survives restart, synchronizes Start before dependent operations, and
  submits.
- Mobile B discovers committed events, monitors a read-only projection,
  approves, finalizes, and replays the same completed finalization command.

This is not the production Active Receiving Console. It adds no BLE,
background service, SignalR, authentication, subscription enforcement,
masters, Inventory, supplier payable, Purchase Bill, Sales, Finance, PDF, or
commercial posting behavior.

## Feature gate and debug network boundary

The compile-time flag is:

```text
TRADERPRO_PROCUREMENT_POC
```

It defaults to false. `ProcurementPocFeatureConfiguration.isEnabled` requires
the flag and `releaseMode == false`. The named route
`/development/procurement-poc` applies the same guard, so direct route
navigation fails closed. Tests inject a feature configuration explicitly.

The main Android manifest does not enable cleartext traffic. Only
`android/app/src/debug/AndroidManifest.xml` declares
`android:usesCleartextTraffic="true"`. The typed client accepts HTTP only
because this debug POC must reach a LAN development server. Release-mode
navigation remains unavailable even if the define is supplied.

Temporary `X-TraderPro-Workspace-ID` and `X-TraderPro-Device-ID` values are
development request context only. Setup and every POC screen state that these
IDs are not authentication.

## Immutable Task 4 facts

Schema-version-1 tables are not recreated or rewritten. Task 4 continues to
own:

- local receiving-session and UUIDv7 identity;
- immutable entry facts and raw input strings;
- captured precision/method/source;
- deterministic outbox operation identity and local sequence;
- exact `payloadJson` and lowercase SHA-256 `payloadHash`;
- delivery state and local restart recovery.

Task 6B adds local submission to the same transactional repository. Submission
inserts one immutable `SubmitReceivingSession` operation and closes the local
session in one transaction. It does not change any entry or earlier operation.

For `/api/v1/mobile/sync/operations`, the client inserts the stored
`payloadJson` string directly as the envelope string value and sends the
stored hash. It never parses and re-encodes that payload. `leaseId` is a
sibling envelope property. Start omits lease metadata; records and submit
use the current persisted lease. A local/cloud session uses the same
mobile-generated UUIDv7 aggregate ID. Task 4 payloads captured before Start
retain their original nullable `cloudSessionId`.

## Local schema version 3

Schema version 2 originally added the Task 6B tables. The corrective schema
version 3 is an additive, non-destructive follow-up. A direct `1 -> 3`
migration creates the final tables without touching the four Task 4 tables. A
`2 -> 3` migration rebuilds only cloud state and control commands, copies
every version-2 field exactly, and backfills execution context from the one
active profile and its matching persisted source. If rows cannot be bound
unambiguously, migration fails with `POC_CONTEXT_BACKFILL_REQUIRED`; it never
guesses, deletes, or recreates the database.

| Table | Purpose |
| --- | --- |
| `poc_device_profiles` | One active development profile and persisted automatic-sync pause |
| `receiving_session_cloud_states` | Source/device-bound direct-response cloud reference, version, status, lease, expiry, editor, and safe errors |
| `poc_sync_sources` | Backend/workspace-scoped durable cursor and polling diagnostics |
| `mobile_sync_event_inbox` | Exact received events, application state, and timestamps |
| `remote_receiving_session_projections` | Read-only owner session projection |
| `remote_receiving_entry_summaries` | Maximum five recent entry summaries |
| `poc_control_commands` | Source/device-bound, restart-safe Heartbeat, Approve, and Finalize commands |

Foreign keys and immutable-context triggers bind cloud state and commands to
their source and acting device. Checks enforce local/cloud identity, lease
shape, and command-type version/lease requirements. Cloud versions cannot
regress. Control commands cannot be physically deleted. Task 4 immutability
triggers and bytes remain unchanged.

Migration tests cover `1 -> 3`, `2 -> 3`, and harmless schema-3 reopening.
They preserve exact Task 4 payload strings/hashes and representative rows from
every version-2 POC table, including raw event payload and successful command
response JSON.

## Development device profile

The profile contains normalized backend base URL, canonical Workspace and
Device UUIDs, Operator/Owner display role, optional label, persisted automatic
sync pause, and UTC timestamps. It contains no password, token, or secret.

The role is presentation only; backend device rules remain authoritative.
Changing only label, role, or pause state is allowed. Base URL, Workspace, or
Device identity cannot change while an open local session, unresolved mobile
operation, active cloud lease/session, or incomplete control command exists.
This prevents Device A's work from being sent with Device B headers.

Event source keys are SHA-256 over normalized base URL plus canonical
Workspace ID. A different backend or workspace starts at cursor zero.

## Focused typed API client

`ProcurementPocApiClient` uses `dart:io` `HttpClient`; no networking package
was added. It implements:

- `POST /api/v1/mobile/sync/operations`;
- `GET /api/v1/mobile/sync/events`;
- session list and live view;
- heartbeat, approve, and finalize;
- development bootstrap.

It sends JSON content type, canonical workspace/device headers, and a UUIDv7
correlation ID. Direct commands send the persisted `Idempotency-Key`; approval
and finalization send the persisted `X-Expected-Version`. Decimal values stay
strings.

Connect and read timeouts are bounded. Disposing a client force-closes pending
HTTP work. The client does not retry mutations. It parses the TraderPro error
envelope into stable code, safe message, retryability, HTTP status, and
correlation ID. Non-JSON/HTML failures become a generic safe error. Malformed
mutation responses are ambiguous and retain the command for same-key replay.

The event response reader extracts the lexical JSON span of every `payload`
value before semantic decoding. The inbox therefore stores the exact payload
text received rather than a reconstructed equivalent.

## Mobile synchronization algorithm

`ProcurementPocSyncEngine` depends on profile, sync-store, API, feature, and
clock abstractions. It selects and recovers only `ReceivingSession` rows whose
operation type is Start, Record, or Submit. Future module rows are neither
recovered, sent, blocked, nor counted in POC diagnostics.

1. Fail closed when the feature or profile is unavailable.
2. Recover durable `Sending` rows to retryable `Pending` after ambiguous
   process/network loss.
3. Read deterministic aggregate/local-sequence candidates.
4. Send one Start alone, with no lease.
5. Transactionally mark Start accepted and store its direct cloud reference,
   version, lease ID, expiry, editor, and timestamps.
6. Reload candidates. For each aggregate, require a known unexpired lease.
7. Batch up to 50 dependent operations, preserving per-aggregate sequence.
8. Mark the batch `Sending` before network I/O.
9. Validate response identity and order against durable operations, reject
   regressive versions, require Start/record lease state, and require Submit
   to clear the lease.
10. Commit accepted results and cloud state together.
11. Treat `PreviouslyProcessed` exactly like accepted delivery.
12. Retain `NeedsAttention` and `Rejected` operations with stable errors; a
    lower unresolved sequence blocks later work for that aggregate.
13. Continue unrelated aggregates.
14. On timeout, connection loss, or malformed response, return the exact same
    operations to Pending. Do not generate a new UUID or payload.

Submission success stores `SubmittedForReview` and clears the local lease.

## Response-loss and restart recovery

The deterministic fake commits a dependent batch and drops its response. The
client leaves the six dependent operations retryable, closes the database,
reopens it, and sends the same operation IDs, sequences, payload strings,
hashes, and envelope lease. The fake returns `PreviouslyProcessed`; all rows
complete without a second local entry or total increment.

## Foreground heartbeat

The coordinator checks only cloud states with `ReceivingInProgress` and a
persisted lease. No command exists before Start succeeds. When expiry is
within the configurable lead time, one Heartbeat row is created. Ambiguous
failure returns that row to Pending; restart reuses its command UUID. Success
updates lease expiry and cloud version. Submission removes the lease and makes
the session ineligible. Expiry records
`RECEIVING_POC_LEASE_EXPIRED` and does not create a replacement or force
recovery.

One app-level `ProcurementPocRuntime` owns the database, repository, guarded
API factories, sync engine, event poller, control service, coordinator, and
controller. Its lifecycle observer exists for the whole enabled runtime, not
for one route. Only `resumed` enables cycles; `inactive`, `hidden`, `paused`,
and `detached` disable them. Popping and re-entering the POC route does not
alter this ownership.

The coordinator acquires one shared automatic/manual cycle gate before any
await. Event polling, control execution, and heartbeat scheduling do the same.
Timer failures become safe diagnostics and cannot escape as unhandled
asynchronous errors. Runtime disposal cancels the timer once, prevents later
API starts and notifications, disposes the controller once, and closes the
database once.

After every completed or safely failed foreground cycle, a neutral callback
reloads the controller's local session, entries, POC outbox, bound cloud
state, remote and selected projections, commands, and diagnostics. Start,
entry delivery, owner events, and heartbeat renewal therefore become visible
without Manual Sync Now. Application has no Presentation dependency.

The default automatic foreground cycle is three seconds and may be paused in
the profile. Manual Sync Now remains explicitly available. No Android/iOS
background package is used.

## Event inbox, cursor, and owner projection

Polling requests events strictly after the scoped durable cursor and follows
server pages. Events are sorted by increasing sequence. One Drift transaction
inserts the exact event, applies its projection, marks it applied, and advances
the cursor. Duplicate pages match existing inbox facts and do nothing.

Handled events are:

1. `ReceivingSessionPocStarted`
2. `ReceivingEntryPocAccepted`
3. `ReceivingSessionPocSubmitted`
4. `ReceivingSessionPocApproved`
5. `ReceivingSessionPocFinalized`

Started rejects any payload containing `leaseId`. Known malformed payloads
roll back and stop progress. Unknown types are stored as `SkippedUnknown`.
Exact totals are validated as canonical six-decimal strings through BigInt
parsing. Projection versions never move backward, and authoritative event
count/total values prevent duplicate increments.

Owner discovery combines the local event projection with explicit server list
and live-view refresh. The Owner UI shows status, editor, lease expiry, count,
exact total, cloud version, last update, and no more than five recent entries.
It contains no entry form or Submit control.

## Direct control-command outbox

Heartbeat stores lease identity. Approve and Finalize store the live-view
expected version. Identity fields cannot change after insert:

- command UUID/idempotency key;
- source key and acting Device ID;
- type;
- session;
- expected version;
- lease;
- creation time.

Mutable delivery state records Pending, Sending, Completed, Needs Attention,
attempts, safe errors, and the successful raw response. Heartbeat must return
the matching in-progress session and a renewed lease, Approve must return
`Approved`, Finalize must return `Finalized` with a finalization ID, and no
successful result may regress version. Malformed 2xx responses remain
retryable.

The explicit finalization replay is available only when the selected session
has a completed Finalize command bound to the active source and device. It
requeues that exact command without changing its key, version, or stored
evidence. A remotely Finalized projection alone is not replay authority.

## POC UI

The small Material 3 UI contains:

1. POC Setup
2. Operator Session
3. Owner Session List
4. Owner Live View
5. Sync Diagnostics

Setup supports base URL, bootstrap, manual IDs, local role, label, selectable
bootstrap JSON, connection test, and explicit identity warnings.

Operator shows local/cloud identity and status, lease/heartbeat, sequence,
three recent entries, exact total, outbox counts, manual synchronization,
pause toggle, shared Task 3 processing preview, local save, and Submit.
Manual capture requires valid ISO-8601 text with an explicit UTC designator or
zero offset. Missing, malformed, or offset-free text produces
`POC_CAPTURE_TIMESTAMP_INVALID` and creates no local fact or outbox row; no
epoch fallback or silent conversion is used.

Owner shows read-only list/live view, refresh, conditional Approve/Finalize,
and explicit same-finalization replay. Operator has no owner controls.
Diagnostics shows safe counts, durable cursor, stable error code, and
persistent control-command keys/status.

Widgets call only `ProcurementPocController`; they do not access Drift tables
or construct HTTP requests.

## Automated verification

Tests cover:

- non-destructive Drift migration and Task 4 byte preservation;
- version-2 POC preservation and source/device context backfill;
- automatic operator/owner/heartbeat UI refresh and app-wide lifecycle;
- shared coordinator, poller, command, and heartbeat-scheduling gates;
- POC-only outbox recovery, selection, and diagnostics;
- cloud-state/command context isolation and cloud-contract checks;
- exact envelope payload/hash and header serialization;
- raw event payload preservation;
- offline capture, restart, Start-only first request, five-entry ordering,
  exact total, submission, and response-loss replay;
- heartbeat preconditions, same-key restart recovery, renewal, and expiry;
- event atomicity, duplicate pages, unknown/malformed events, projection
  versions/totals, and restart cursor;
- finalization same-key timeout/restart and completed-command replay;
- malformed successful response retryability, matching-device replay
  visibility, and strict UTC input rejection;
- profile-change blocking and cursor-source isolation;
- default/release feature gating;
- owner read-only and operator control separation;
- source dependency boundaries and absence of posting implementations;
- two independent repositories/profiles completing one logical session, five
  entries, one exact total, one finalization, and same result on replay.

Run:

```powershell
powershell -ExecutionPolicy Bypass `
  -File .\scripts\test\test-two-device-procurement-mobile-poc.ps1
```

The manual procedure is
`docs/runbooks/TPRUN-001-Two-Device-Procurement-Poc.md`.

## Limitations and evolution/removal

Ordinary SQLite remains unencrypted and is not suitable for sensitive pilot
data. Polling, foreground heartbeat, temporary IDs, bootstrap, debug HTTP,
manual product/bag strings, and POC completion are not production
capabilities. Authentication, authorization, secure provisioning, RLS,
subscriptions, encrypted local storage, background policy, recovery/lease
transfer, master data, commercial receiving correction, Inventory, supplier
obligations, documents, Sales, and Finance require separate reviewed work.

Before commercial release, remove the POC route/tables or migrate them through
a reviewed schema, replace temporary context with secure identity, disable
cleartext, and evolve the frozen `Poc` contracts without rewriting captured
physical facts.
