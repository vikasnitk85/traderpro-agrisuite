# Task 7C2: Secure Flutter Commercial Receiving and Offline Sync

- Status: Planned; not implemented
- Date: 2026-08-01
- Scope: Task 7A mobile identity, encrypted local-first Commercial Receiving,
  authenticated sync, and read-only Owner monitoring

## Status

Planned and not implemented. No mobile package or plaintext pilot path is
approved by this plan.

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
- Different-device transfer UI is absent unless OQ-04 and Task 7C1 support are
  approved.

## Backlog

| ID | Artifact/work | Acceptance criteria | Dependencies |
| --- | --- | --- | --- |
| 7C2-01 | Encrypted SQLite/secure-storage compatibility spike | Evaluate at least a Drift-compatible SQLCipher/page-encrypted option and OS-backed secure-storage abstraction on supported Android; verify build/release ABI, licensing, performance, wrong-key failure, WAL/journal encryption, known-plaintext absence, restart, rekey feasibility, backup behavior, and test injection. Document package decision before dependency change. | ADR-0009; TPSEC-001; Android matrix. |
| 7C2-02 | Secure key/secret services | Narrow abstractions store Device secret, rotating refresh token, and DEK/key reference outside SQLite; redact logs; no production default key; key loss and cross-device restore fail closed; test fake is explicit. | 7C2-01. |
| 7C2-03 | Encrypted commercial database opener | Open a separate production file only with the reviewed encrypted executor/key; release startup proves encryption; no plaintext fallback; POC file/schema is untouched; explicit discard flow preserves/warns about ciphertext and unsynced facts. | 7C2-01/02. |
| 7C2-04 | Production Drift schema version 1 | Implement isolated tables/constraints/triggers for account/credential metadata, typed masters, Sessions, immutable Entries, immutable outbox, cloud state, event inbox, cursor, Owner projection, recent Entries, attention; exact weights use TEXT/BigInt, never REAL; generated source checked in. | 7C2-03; TPTECH-001.21. |
| 7C2-05 | Drift migration strategy | Explicit non-destructive future `onUpgrade`; schema-1 creation and reopen tests; no automatic POC import/in-place conversion; backup/recovery hooks follow encrypted engine. | 7C2-04. |
| 7C2-06 | Device activation UI/service | Redeem one-time Task 7A code for a pre-created Device; store returned Device secret once; no secret in SQLite/logs/screens after confirmation; reactivation uses same Device slot and explains local-data implications. | 7C2-02; Task 7A. |
| 7C2-07 | Workspace-code login and context binding | Login using workspace code, credentials, Device ID/secret over HTTPS; store refresh safely; keep access token memory-scoped; call `/auth/me`; bind exact Workspace/Company/default Branch/User/Device/role; mismatch fails closed. | 7C2-02/06; Task 7A. |
| 7C2-08 | Serialized refresh coordinator | One in-flight refresh shared by all API calls; rotating token stored durably before callers resume; lost response retries same predecessor in replay window; reuse/revocation forces login; business operation IDs remain unchanged. | 7C2-02/07. |
| 7C2-09 | Logout/logout-all/reactivation/context refresh | Current/all family flows clear credentials appropriately but preserve encrypted pending work; re-login must match bound context; role/context refresh on login/refresh/resume/authorization failure; no silent commercial identity switch while work exists. | 7C2-07/08. |
| 7C2-10 | Authenticated commercial API client | HTTPS-only release client for operation/event/master/list/live/reacquire routes; bearer injection after refresh; bounded timeouts; exact payload string/hash serialization; decimal strings; safe error/correlation parsing; no mutation auto-retry outside durable engines. | 7C2-08; Task 7C1 contracts. |
| 7C2-11 | Commercial master cache engine | Bootstrap captures and persists server high-water `H`, applies unique change rows through `H`, then deltas strictly after `H`; page application and cursor are atomic/idempotent. Cache every required Active/Inactive master, association, and settings version; never consume raw `Internal`; cache refresh never rewrites Receiving snapshots. | 7C2-04/10; 7C1 master endpoint. |
| 7C2-12 | Local Receiving repository | Create mobile UUIDv7 Session/Start op atomically with exact Procurement Settings ID/version and vehicle-mode snapshot; capture Entry/Record and Submit/local close atomically. Immutable operation rows contain the operation-appropriate generation but never expected cloud version; cloud version remains mutable projection state. | 7C2-04; Task 3; OQ-06. |
| 7C2-13 | Commercial capture UI | Select Active Supplier/Product/Bag/standard/optional vehicle from cache and use only the exact captured settings revision's configured default destination and Weight Policy; expose no override until OQ-11. Snapshot IDs/versions/safe facts, preserve raw text, show exact preview/cache age, and keep widgets outside database access. | 7C2-11/12; OQ-06; OQ-11 for any override. |
| 7C2-14 | Operation sync engine | Recover ambiguous Sending rows and preserve Session order/max 50. Start sends null/absent generation, expected cloud version, and lease; Record/Submit send required generation/current lease transport metadata and null/absent expected cloud version. All retain the shared scope and immutable actual type; lease enrichment never rewrites payload/hash. | 7C2-10/12; Task 7C1 endpoint. |
| 7C2-15 | Lease renewal/reacquisition | One foreground coordinator queues renewable lease controls; same Device/generation reacquires after expiry and retries same operation; another Device never auto-acquires; server time wins; lease stored outside payload. Exact production timing is configured from reviewed policy. | 7C2-14; 7C1 reacquire route; OQ-08. |
| 7C2-16 | Attention and recovery presentation | Stable master/lease/device/generation errors link to original immutable operation/fact; no delete/edit/reclassification shortcut; same-device reacquire action is explicit; stale-master/old-generation actions remain absent until approved. | 7C2-14/15; OQ-09/OQ-10 for future commands. |
| 7C2-17 | `CommercialMobileSync` event inbox/cursor | Cursor key includes source/workspace/company/device/contract; Owner profiles apply safe company broadcasts, and any active editor profile accepts rows targeted to its Device; Operator profiles have no broader entitlement. Unrelated Session events fail closed. Exact event/inbox/projection/cursor apply is atomic; gaps and duplicate pages are safe. | 7C2-04/10; 7C1 cursor/events; OQ-12 for broader visibility. |
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
remain a documented limitation. No package is added before the spike.

## Migration implications

The production encrypted schema starts separately at version 1. POC schema
versions/rows remain untouched and do not become production data. Future
migrations are forward-only and non-destructive and must account for encrypted
engine backup/rekey behavior.

## Future implementation dependencies

Production background policy, BLE/scale integration, key-rotation operations,
remote wipe, attestation/integrity APIs, settlement/posting, cancellation, and
correction require later reviewed work.

## Unresolved questions

Package choice, supported Android/backup matrix, production lease timing,
zero-weight/submission validation, transfer UI authority, and all TPRC-101
business gaps remain unresolved until their entry gates are satisfied.

## Exit criteria

- All backlog acceptance criteria and two-device field evidence pass.
- Commercial SQLite is demonstrably encrypted and has no plaintext fallback.
- Credentials/key material never enter SQLite, logs, diagnostics, or payloads.
- No POC identity/header/stream/table/reference is commercial authority.
- No settlement, Purchase Bill, payable, Inventory, Sales, Finance, or
  finalization behavior exists.
