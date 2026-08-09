# Task 7C2: Secure Flutter Commercial Receiving and Offline Sync

- Status: `TASK 7C2B2 = COMPLETE`; Task 7C2B3 not started
- Date: 2026-08-01
- Scope: Task 7A mobile identity, encrypted local-first Commercial Receiving,
  authenticated sync, and read-only Owner monitoring

Task 7C2A freezes the implemented Task 7C1 routes, cursor shapes, result/error
vocabulary, reference policy, immutable event audiences, `Manual` capture
source, and offline pre-Start behavior before the secure mobile foundation is
implemented. Task 7C2A does not implement the production client.

The encrypted-storage compatibility evidence and explicit no-selection result
are recorded in
`docs/assessments/TASK-7C2A-Encrypted-Storage-Compatibility-Spike.md`.
Task 7C2B1 then froze candidate configuration/key syntax and added host
tamper, interruption, API-24/API-35 engine, repeated physical performance,
native-provenance, notice, SBOM, and reviewed SCA evidence. It also corrected
the pinned secure-store clean-install bootstrap and passed its fail-closed,
force-stop, and same-data replacement matrix on API 24 and physical API 35
with both candidates. ADR-0011 accepted SQLite3MultipleCiphers 2.3.6 through
`sqlite3` 3.5.0 with explicit ChaCha20-Poly1305 and a true random 256-bit raw
key. Task 7C2B2 now implements the isolated secure-store key, fail-closed
provisioning state machine, encrypted Commercial schema 1, production runtime,
Android backup/release controls, production SBOM/SCA, and separate POC
entrypoint. API-24, force-stop, replacement, host, and unsigned ARM release
gates pass. The production physical API-35 matrix, force-stop/replacement, and
temporary externally signed release-posture checks also pass. The Project/Legal
Owner approved the frozen exact-text notice bundle on 2026-08-08; see
`docs/assessments/TASK-7C2B2-Secure-Commercial-Local-Foundation.md`.

## Status

Task 7C2B2 secure local foundation and its required host, API-24, physical
API-35, release, backup-declaration, native, SBOM, and SCA technical gates pass.
Owner notice approval is recorded for the frozen B2 graph. The approved future
surface is About / Legal / Third-Party Notices with the SQLite public-domain
dedication/provenance included; that presentation UI is a later release
requirement and is not implemented in B2. B3 authentication/context binding
and all business tables/UI remain unimplemented. No plaintext pilot path is
approved. ADR-0012 is accepted. `TASK 7C2B2 = COMPLETE`; Task 7C2B3 is `NOT
STARTED`.

## Reviewed milestone split

The implementation sequence is deliberately separated from the full Receiving
client:

| Milestone | Entry gate | Deliverables | Exit gate |
| --- | --- | --- | --- |
| Task 7C2B1: Production encrypted-storage selection | Merged 7C2A evidence and unchanged production dependency graph | Frozen candidate parameters/key syntax, API/runtime/lifecycle/performance evidence, provenance, notices, locked SBOM, SCA review, and ADR-0011 only after one candidate passes every gate | **Complete subject to commit review:** SQLite3MultipleCiphers accepted in ADR-0011 |
| Task 7C2B2: Secure Android/Flutter foundation | Accepted ADR-0011 and approved Android backup/release policy | Pinned production packages, narrow OS secure-store and database-key ports, separate encrypted Commercial database/opener, fail-closed startup, foundation metadata schema 1, backup/data-extraction controls, redaction, runtime ownership, and architecture/test seams | **Complete:** host/API-24/API-35/release/SBOM/SCA/static review pass; Owner notice approval recorded 2026-08-08; ADR-0012 accepted |
| Task 7C2B3: Production authentication and context binding | Accepted B2 foundation and stable Task 7A contracts | Activation, credential storage, workspace login, `/auth/me` binding, serialized refresh, logout/reactivation/context rules, and HTTPS-only authenticated client foundation | Identity/context/key-loss matrices pass without introducing Receiving capture or sync |
| Later Task 7C2 delivery slices | Accepted B3 and relevant product gates | Master sync, local Receiving/outbox, capture UI, operation sync, event projections, Owner monitoring, and field validation in separately reviewed slices | Existing backlog acceptance criteria and final pilot gates pass |

B2 schema 1 contains only immutable installation/database identity and schema
metadata needed to prove the secure opener. Receiving Sessions, Entries,
outbox, masters, cursors, projections, attention, Inventory, and Finance do not
belong in the secure-foundation milestone. No POC schema/data is migrated.

## Outcome

Deliver an Android-first production Commercial Receiving client that activates
and authenticates a pre-created Device, stores credentials/key material safely,
saves immutable physical facts locally before network use, synchronizes with
Task 7C1 under one-editor generation/lease rules, resumes commercial cursors,
and supports read-only Owner monitoring through submission.

## Scope

Authentication UI/services, serialized refresh, secure secret storage,
encrypted database, isolated production Drift schema, master cache, capture UI,
operation sync, commercial event polling, Owner monitoring, attention/recovery
presentation, restart/replay tests, and a two-device commercial field test.

## Non-goals

No POC promotion, plaintext pilot database, BLE/scale integration unless later
separately approved, background execution promise, cancellation/correction,
settlement, bag deduction, Purchase Bill, payable, Inventory, Sales, Finance,
or official purchase finalization.

## Authoritative sources

- `AGENTS.md`
- TPRC-101 baseline/open questions
- TPTECH-001.13, TPTECH-001.18, TPTECH-001.21
- ADR-0004, ADR-0005, ADR-0008, ADR-0009
- TPSEC-001
- Final Task 7C1 versioned contracts

## Entry gates

- Task 7C1 operation/event/master contracts are implemented and testable.
- An encrypted SQLite + OS secure-storage compatibility spike is reviewed.
- The supported Android version/ABI matrix and backup behavior are approved.
- OQ-06 is resolved before enabling physical capture/submission.
- Different-device transfer UI remains deferred even though the Task 7C1 Owner
  command is implemented.

## Task 7C2A frozen mobile contract

- The exact routes are the committed Task 7A/7C1 routes. Commercial event and
  master reads use the server-issued opaque `cursor`; clients do not construct
  `after` or bootstrap-high-water query parameters.
- Local event cursor identity is source/stream + contract version + Workspace
  + Company + Device. Server cursors contain no independent authority.
- Operation results are exactly `Accepted`, `PreviouslyProcessed`,
  `NeedsAttention`, and `Rejected`. A same-batch follower uses
  `RECEIVING_OPERATION_WAITING_FOR_PRIOR_SEQUENCE`; an expired owner lease uses
  `RECEIVING_LEASE_REACQUISITION_REQUIRED`; stale ownership generation is
  `NeedsAttention`.
- The default automatic reference policy is `RCV-{SEQ:000000}`, `Never`, start
  1. Mobile never constructs an official reference.
- `OwnerBroadcast` and `TargetDevice` are immutable issuance audiences. An
  authenticated active Device may read historical events targeted to it;
  mutation authority still revalidates current editor/generation. Transfer
  emits the old-Device row needed to communicate transfer-away, and later
  Receiving events target only the new editor.
- Task 7C2 capture emits `weightSource: "Manual"`. POC sources, BLE/scale, and
  imported-memory sources are not production values.
- A securely bound authenticated Operator may capture a local Session and
  immutable Entries offline using valid cached Active master/settings
  revisions. Start remains sequence 1; dependent operations queue behind it
  and are not transmitted until Start returns reference, generation, and
  lease. Start rejection preserves every fact and makes the Session attention-
  required without rewriting any Entry or payload.
- Owner monitoring is read-only. Owner transfer UI, voice selection, BLE/scale,
  settlement, Purchase Bill, Inventory, Finance, Sales, Production,
  cancellation, correction, reopen, and SignalR remain outside Task 7C2.

## Backlog

| ID | Artifact/work | Acceptance criteria | Dependencies |
| --- | --- | --- | --- |
| 7C2-01 | Encrypted SQLite/secure-storage compatibility and selection | Evaluate SQLCipher Community and SQLite3MultipleCiphers with OS-backed secure storage; freeze key/cipher parameters; verify API 24/35, build/release ABI, licensing, performance, wrong-key/tamper failure, WAL/journal encryption, known-plaintext absence, restart/interruption, backup behavior, provenance, SBOM, SCA, and test injection. Automatic/in-place rekey is explicitly deferred from V1 and is not a selection gate. Accept ADR-0011 before any production dependency change. | ADR-0009; TPSEC-001; TPRUN-006; Android matrix. |
| 7C2-02 | Secure key/secret services | Narrow abstractions store Device secret, rotating refresh token, and DEK/key reference outside SQLite; redact logs; no production default key; key loss fails closed; cross-device restore is unsupported in V1; test fake is explicit. | 7C2-01. |
| 7C2-03 | Encrypted commercial database opener | Open a separate production file only with the reviewed encrypted executor/key; release startup proves encryption; no plaintext fallback; POC file/schema is untouched; ciphertext is retained on key failure. Destructive discard/reinitialization is a separate approved recovery workflow that must first classify/preserve unsynced facts. | 7C2-01/02. |
| 7C2-04 | Production Drift schema version 1 | Implement isolated tables/constraints/triggers for account/credential metadata, typed masters, Sessions, immutable Entries, immutable outbox, cloud state, event inbox, cursor, Owner projection, recent Entries, attention; exact weights use TEXT/BigInt, never REAL; generated source checked in. | 7C2-03; TPTECH-001.21. |
| 7C2-05 | Drift migration strategy | Explicit non-destructive future `onUpgrade`; schema-1 creation and reopen tests; no automatic POC import/in-place conversion; no automatic rekey in V1; backup/recovery hooks follow encrypted engine. | 7C2-04. |
| 7C2-06 | Device activation UI/service | Redeem one-time Task 7A code for a pre-created Device; store returned Device secret once; no secret in SQLite/logs/screens after confirmation; reactivation uses same Device slot and explains local-data implications. | 7C2-02; Task 7A. |
| 7C2-07 | Workspace-code login and context binding | Login using workspace code, credentials, Device ID/secret over HTTPS; store refresh safely; keep access token memory-scoped; call `/auth/me`; bind exact Workspace/Company/default Branch/User/Device/role; mismatch fails closed. | 7C2-02/06; Task 7A. |
| 7C2-08 | Serialized refresh coordinator | One in-flight refresh shared by all API calls; rotating token stored durably before callers resume; lost response retries same predecessor in replay window; reuse/revocation forces login; business operation IDs remain unchanged. | 7C2-02/07. |
| 7C2-09 | Logout/logout-all/reactivation/context refresh | Current/all family flows clear credentials appropriately but preserve encrypted pending work; re-login must match bound context; role/context refresh on login/refresh/resume/authorization failure; no silent commercial identity switch while work exists. | 7C2-07/08. |
| 7C2-10 | Authenticated commercial API client | HTTPS-only release client for operation/event/master/list/live/reacquire routes; bearer injection after refresh; bounded timeouts; exact payload string/hash serialization; decimal strings; safe error/correlation parsing; no mutation auto-retry outside durable engines. | 7C2-08; Task 7C1 contracts. |
| 7C2-11 | Commercial master cache engine | Bootstrap captures and persists server high-water `H`, applies unique change rows through `H`, then deltas strictly after `H`; page application and cursor are atomic/idempotent. Cache every required Active/Inactive master, association, and settings version; never consume raw `Internal`; cache refresh never rewrites Receiving snapshots. | 7C2-04/10; 7C1 master endpoint. |
| 7C2-12 | Local Receiving repository | Create mobile UUIDv7 Session/Start op atomically with exact Procurement Settings ID/version and vehicle-mode snapshot; allow approved offline immutable Entry capture queued behind Start; capture Entry/Record and Submit/local close atomically. Immutable operation rows contain the operation-appropriate generation but never expected cloud version; cloud version remains mutable projection state. Start rejection preserves all facts in attention. | 7C2-04; Task 3; frozen Task 7C2A offline policy. |
| 7C2-13 | Commercial capture UI | Select Active Supplier/Product/Bag/standard/optional vehicle from cache and use only the exact captured settings revision's configured default destination and Weight Policy; expose no override until OQ-11. Snapshot IDs/versions/safe facts, preserve raw text, send only `weightSource: "Manual"`, show exact preview/cache age, and keep widgets outside database access. | 7C2-11/12; frozen Task 7C2A capture contract; OQ-11 for any override. |
| 7C2-14 | Operation sync engine | Recover ambiguous Sending rows and preserve Session order/max 50. Start sends null/absent generation, expected cloud version, and lease; Record/Submit send required generation/current lease transport metadata and null/absent expected cloud version. All retain the shared scope and immutable actual type; lease enrichment never rewrites payload/hash. | 7C2-10/12; Task 7C1 endpoint. |
| 7C2-15 | Lease renewal/reacquisition | One foreground coordinator queues renewable lease controls; same Device/generation reacquires after expiry and retries same operation; another Device never auto-acquires; server time wins; lease stored outside payload. Exact production timing is configured from reviewed policy. | 7C2-14; 7C1 reacquire route; OQ-08. |
| 7C2-16 | Attention and recovery presentation | Stable master/lease/device/generation errors link to original immutable operation/fact; no delete/edit/reclassification shortcut; same-device reacquire action is explicit; stale-master/old-generation actions remain absent until approved. | 7C2-14/15; OQ-09/OQ-10 for future commands. |
| 7C2-17 | `CommercialMobileSync` event inbox/cursor | Cursor key includes source/workspace/company/device/contract; Owner profiles apply safe company broadcasts, and a bound Device accepts immutable rows targeted to it, including transfer-away history. Mutation authority remains current-editor/generation bound and future events follow the new editor. Operator profiles have no broader list/live entitlement. Unrelated Session events fail closed. Exact event/inbox/projection/cursor apply is atomic; gaps and duplicate pages are safe. | 7C2-04/10; frozen 7C1 cursor/events; OQ-12 for broader discovery. |
| 7C2-18 | Owner read-only monitoring | Owner discovers active/submitted Sessions, sees header snapshots/count/total/recent Entries/editor/generation/lease health/update/attention/submission; refresh combines events with list/live view; no Entry/Submit/settlement controls or repository port. | 7C2-17; 7C1 read routes. |
| 7C2-19 | Runtime/lifecycle coordination | One commercial runtime owns database, credential coordinator, API factories, sync/master/event/lease engines, controller, timers, and lifecycle observer; one gate prevents overlaps before async lookup; disposal stops clients/notifications and closes DB once. Foreground behavior is honest; no background guarantee. | 7C2-08/10/14/15/17. |
| 7C2-20 | Restart/response-loss tests | File-backed encrypted tests prove settings snapshot/default-only Start, Start-first envelope fields, Record/Submit generation/lease with no cloud version, shared-scope type-conflict safety, exact sequence, dropped responses, same ID/payload/hash replay after cloud progress/reacquisition, master high-water resume, event audience/gaps, and no duplicate Entry/total. | 7C2-03 through 7C2-19. |
| 7C2-21 | Security/reinstall/backup tests | Verify wrong/missing key, no plaintext header/known strings, no secret in DB/logs, no fallback, uninstall/reactivation behavior, backup/cross-device restore failure, revoked family/context mismatch, POC separation, debug HTTP exclusion, and release guards. | 7C2-01 through 7C2-10; TPSEC-001. |
| 7C2-22 | Two-device commercial field test | On independent authenticated Devices: editor captures offline/restarts/syncs/submits; Owner monitors live/read-only; lease expiry/reacquire and response loss use same operations; another Operator Device cannot edit, take over, or read unrelated Session events; exact settings/weight/master snapshots, total, reference, and status match cloud. Use non-production data and no settlement claims. | All prior items; Task 7C1 field environment. |
| 7C2-23 | Documentation and pilot gate | Document setup, secret-safe diagnostics, key loss, backup, logout/reactivation, attention, release config, and known rooted-device limitation. Pilot checklist blocks on encryption, HTTPS, RLS, and threat-model tests. | All prior items. |

## Data ownership

Secure storage owns small secrets/key references. The encrypted database owns
local capture/outbound evidence and cached projections. Cloud state is
authoritative after acceptance. Widgets use application/controller ports and
never access Drift tables or construct HTTP requests.

## Transaction boundaries

Activation/login token replacement, local capture, local submit, operation
response apply, event apply/cursor, master apply/cursor, and key switch each
have explicit atomic boundaries. Network I/O occurs outside SQLite
transactions; durable Sending state is committed before mutation calls.

## Failure behavior

Network ambiguity returns the same operation/token to its safe replay path.
`NeedsAttention` retains immutable facts. Cross-context identity, revoked
family, wrong key, insecure transport, malformed response/event, and cursor
scope mismatch fail closed. No error path deletes POC/customer data or creates
plaintext storage.

## Security implications

Encrypted SQLite, OS-backed secrets, HTTPS, serialized refresh, context binding,
event/master isolation, and release redaction are pilot gates. Rooted Devices
and compromised live-process memory remain documented limitations. ADR-0011
completed the technical selection, and Task 7C2B2 implements only the reviewed
secure local foundation. The Project/Legal Owner approved the frozen B2 notice
bundle on 2026-08-08. Any production dependency or binary-graph change reopens
the notice/SBOM review gate.

## Migration implications

The production encrypted schema starts separately at version 1. POC schema
versions/rows remain untouched and do not become production data. Future
migrations are forward-only and non-destructive and must account for encrypted
engine backup/rekey behavior. Task 7C2 V1 does not implement automatic rekey.

## Future implementation dependencies

Production background policy, BLE/scale integration, key-rotation operations,
remote wipe, attestation/integrity APIs, settlement/posting, cancellation, and
correction require later reviewed work.

## Unresolved questions

Encrypted engine choice is resolved by ADR-0011. Supported Android/OEM backup
behavior beyond the validated B1 matrix, production lease anomaly thresholds,
and all remaining TPRC-101 business gaps remain unresolved until their entry
gates are satisfied. Positive Entry/submission validation and lease timing are
already frozen by Task 7C1; Owner transfer UI is deferred.

## Exit criteria

- All backlog acceptance criteria and two-device field evidence pass.
- Commercial SQLite is demonstrably encrypted and has no plaintext fallback.
- Credentials/key material never enter SQLite, logs, diagnostics, or payloads.
- No POC identity/header/stream/table/reference is commercial authority.
- No settlement, Purchase Bill, payable, Inventory, Sales, Finance, or
  finalization behavior exists.
