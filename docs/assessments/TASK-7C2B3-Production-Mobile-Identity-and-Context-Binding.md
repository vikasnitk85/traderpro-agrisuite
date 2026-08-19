# Task 7C2B3 production mobile identity and context binding assessment

- Status: Complete through the final PR #5 corrective pass; host,
  release-posture, API-24, and physical API-35 gates pass
- Evidence date: 2026-08-19
- Branch: `task/7c2b3-mobile-identity-context`
- Base: `91a0da7ac2eaf688e351077d5699638b93abb29e`
- ADR: ADR-0014 Accepted

## Outcome

The production Flutter entry now composes the B2 encrypted foundation with
the frozen production identity API. It supports crash-safe Device activation,
workspace login, journaled refresh rotation, authoritative `/auth/me`
resolution, permanent encrypted context binding, safe offline revalidation,
reactivation, and local logout/logout-all. Access tokens remain memory-only.

No production Receiving, master, outbox, cursor, Inventory, Finance, or other
B4 behavior was added. The protected POC, legacy local database, Receiving,
and isolated storage-spike sources remain unchanged.

## Implemented boundaries

| Area | Result |
| --- | --- |
| Contract | Exact frozen activation/login/refresh/logout/logout-all/me routes, methods, camelCase codecs, strict known fields, additive unknown fields, safe backend error mapping |
| Environment | Required normalized origin; release HTTPS; debug HTTP only by explicit define; no local/private/credential-bearing origin |
| Transport | One runtime-owned bounded `dart:io` client; TLS intact; no redirects/bad-certificate callback; safe headers; UUID correlation |
| Activation | At least 192 random idempotency bits; exact attempt durable before send; response-loss recovery; Device commit/readback before attempt clear |
| Credentials | Dedicated namespace; versioned A/B Device and refresh journals; readback/head verification; Device retirement; durable refresh tombstone |
| Refresh | One single-flight future; 60-second skew; durable predecessor marker; one replay inside 30 seconds; epoch-protected logout/reactivation |
| Context | `/auth/me` authoritative; immutable API/Workspace/Company/Branch/User/Device tuple; mutable safe snapshot; mismatch fail-closed |
| Database | Explicit transactional schema `1 -> 2`; B2 metadata preserved; immutable binding triggers; transactional first bind/snapshot |
| Startup | Extends B2 fail-closed open; classifies credential journals; restores/revalidates; bound offline state is non-authorizing |
| Android/UI | Production INTERNET, cleartext false, backup false, global FLAG_SECURE; identity-only UI with obscured secret inputs |
| Isolation | Production main has no POC import; B3 has no legacy/spike/Receiving import; runtime is the sole transport/coordinator construction owner |

## Automated evidence

- Flutter analyzer: pass, no issues.
- Full Flutter unit/widget/database/contract/architecture suite: 242 passed.
- Domain/environment/transport contract tests: pass.
- Secure Device/refresh/activation journal tests: pass.
- Fresh schema 2, encrypted schema `1 -> 2`, rollback, metadata, binding,
  trigger, mismatch, and snapshot tests: pass.
- Activation response-loss, persistence, definitive failure, and login
  no-retry tests: pass.
- Refresh crash matrix A-F, exact replay boundary, concurrency, logout epoch,
  reactivation race, and mismatch family-revocation tests: pass.
- Activation/login/ready/offline/mismatch/revoked/logout widget states: pass.
- Android security and identity architecture rules: pass.
- Backend production identity regression: 17 focused unit, 44 PostgreSQL/API
  identity, and 25 architecture tests passed; Task 5 and Task 6A regression
  groups also passed.
- Unsigned release build: pass for ARMv7 and ARM64. Merged manifest requests
  INTERNET, has no debuggable flag, sets backup/cleartext false, and retains
  both exclusion resources. Each APK contains only app/Flutter/JNI plus one
  `libsqlite3mc.so` for its ABI.
- Native dependency continuity: both SQLite3MC ELF SHA-256 values exactly match
  the approved B2 provenance; pubspec and lockfile have no diff.
- Release sentinel scan: no local/private fallback or synthetic credential
  sentinel was found in either compiled `libapp.so`; B3 sources contain no
  print/debug/developer logging call.

## Live Device evidence

On 2026-08-17 the complete identity matrix passed on both required targets:

| Target | Platform | Result |
| --- | --- | --- |
| Official `TraderPro_API24` emulator | Android 7 / API 24 / x86_64 | Pass |
| Authorized `2311DRK48I` physical Device | Android 15 / API 35 / arm64-v8a | Pass |

Each target used the production Android secure-store plugin, encrypted
Commercial database, schema 2, real HTTP identity client, disposable local
PostgreSQL/API, and synthetic in-memory credentials. The matrix passed first
activation with committed-response loss and exact replay; credential commit
ordering; login and authoritative `/auth/me` first binding; reopen and
process restoration; concurrent refresh single flight; committed refresh
response loss across a process boundary; replay-window expiry; immutable
origin mismatch; bound-offline revalidation and online recovery; logout and
logout-all preservation; definite inactive-Device handling; same-Device
reactivation; and database/key/binding/snapshot continuity.

After the matrix, a separately installed preparation entrypoint established a
valid encrypted state. Replacing it with the normal production entrypoint by
same-signature `adb install -r` preserved the expected Device, database,
binding, and identity state on both targets. A subsequent force-stop and cold
launch again reached `Identity confirmed`. The active windows carried
`FLAG_SECURE`: API 35 returned a black protected capture and API 24 blocked the
capture. Package-scoped log scans and encrypted database/WAL/SHM/journal/temp
scans found none of the generated credentials, tokens, Authorization values,
or idempotency keys.

The API-24 response-loss case initially exposed a backend defect: the
protected activation replay record stored PostgreSQL microsecond precision,
while exact comparison used the original 100-nanosecond clock value. The
activation timestamp is now normalized before persistence/comparison. The new
`Initial_activation_replay_normalizes_PostgreSql_timestamp_precision` test
and the existing committed-response-loss regression both pass, and the full
API-24 and API-35 Device matrices passed against the corrected backend.

The retained release evidence was re-inspected without rebuilding: the ARMv7
and ARM64 release APKs and merged manifest remain non-debuggable by default,
request `INTERNET`, disable cleartext and backup, retain both exclusion
resources, and preserve the approved native graph. Host gates that had already
passed were not repeated except for the two focused backend regressions needed
for the Device-exposed defect.

## PR #5 corrective evidence — 2026-08-18

All seven PR #5 findings were corrected within the B3 mobile identity scope:

1. Device-label validation now trims, accepts 200 characters, rejects 201 or
   blank input before persistence/HTTP, and leaves the form correctable.
2. A revoked Device now exposes same-Device reactivation without clearing the
   encrypted database, key, immutable binding, or safe snapshot.
3. Logout-all obtains one valid access token before the remote request, shares
   an in-flight refresh, blocks late publication, and presents retry plus an
   explicit local-only fallback when the remote outcome is unconfirmed.
4. A durable pending activation/reactivation attempt takes startup precedence
   over an older committed Device credential and is recovered with the exact
   stored idempotency key and body.
5. First activation and reactivation keep TLS and ambiguous network outcomes
   in the exact-recovery flow instead of degrading to a generic failure.
6. Secure-store failures during logout exit the spinner into a deterministic
   fail-closed state while preserving Device, binding, database ciphertext,
   and database-key state.
7. Empty or invalid login input remains correctable and can be resubmitted
   without an intervening restart or unintended HTTP request.

Corrective host evidence passed before Device validation: 44 focused identity
tests, 98 combined identity/security/architecture tests, the complete 266-test
Flutter suite, `flutter analyze --no-pub`, and 13 architecture tests. The
already-passed host and API-24 gates were not repeated during the final
physical run.

The targeted corrective matrix passed on the official API-24 emulator and then
passed 44/44 on the authorized `2311DRK48I` Android 15/API-35 arm64-v8a
physical Device. The physical force-stop probe used the production secure-store
adapter, secure database-key store, encrypted Drift database, immutable
binding repository, and refresh/activation controller. It reached `prepared`,
the process was absent after explicit `adb shell am force-stop`, and the cold
relaunch reached `passed`. Recovery retained the same Device ID and immutable
binding, advanced the Device secret version, consumed the pending activation,
and reused the exact idempotency key without a startup HTTP request.

The physical active window retained `FLAG_SECURE`; its captured application
surface was black. Package logs contained zero matches for the five synthetic
Device-secret, activation, idempotency, and Device-ID sentinels. No backend,
dependency, lockfile, protected POC, legacy database, Receiving, or storage-
spike source changed. Temporary corrective entrypoints, Device artifacts, and
the disposable synthetic installation were removed after evidence capture.
ADR-0014 remains Accepted, and B4 was not started.

## PR #5 second corrective evidence — 2026-08-19

The later complete-PR review identified two additional defects. First,
unexpected Drift/SQLite exceptions during authoritative binding could escape
the typed identity boundary after refresh persistence and access-token
assignment. The correction adds a narrow binding-storage failure taxonomy,
translates repository and binding-port exceptions, keeps access provisional
until `/auth/me` and transactional binding succeed, durably clears refresh
authority on binding-storage failure, and moves login/refresh UI to locked
fail-closed without rereading the failing database. Device credential,
encrypted database, database key, and immutable binding are preserved.

Second, release origin validation now detects canonical IPv4-mapped IPv6
addresses under `::ffff:0:0/96`, extracts the final IPv4 octets, and applies the
same unspecified, loopback, carrier-grade NAT, link-local, and private-range
policy used for ordinary IPv4. Host boundaries reject mapped `0/8`, `10/8`,
`127/8`, `169.254/16`, `172.16/12`, and `192.168/16` examples while accepting
mapped public `8.8.8.8`, `172.15.255.255`, and `172.32.0.0`.

Corrective host evidence passed 60/60 focused identity/binding/origin tests, the
complete 271-test Flutter suite, analyzer validation with no issues, and 16/16
architecture checks (13 core plus three protected POC checks). The retained
Android binding-failure probe passed 1/1 on the official API-24 emulator and
1/1 on the authorized `2311DRK48I` Android 15/API-35 arm64-v8a Device. No
already-passed host or API-24 gate was repeated during physical closeout.

The physical probe used real Android secure-store journals, an isolated
SQLite3MC-encrypted Commercial database and real stored database key, the
production binding repository, and the production identity controller flow.
SQLite abort triggers covered first-binding snapshot insertion and existing-
binding snapshot update during session restoration. Both failures exposed
`IDENTITY_BINDING_STORAGE_FAILED`, left no memory access token or identity-ready
session, retired the persisted refresh credential, preserved Device/key/data and
the immutable binding, and recovered normally after fault removal. The
production Android path has no alternate origin parser, so the deterministic
mapped-origin host cases remain authoritative; no platform divergence was
observed.

The physical `MainActivity` window retained `FLAG_SECURE`, and the captured app
region exposed no application content. All five scoped synthetic secret/token/
identity log scans returned zero matches. The isolated secure-store entries,
database, screenshot, and temporary debug installation were removed. No
dependency, lockfile, backend, protected POC, legacy database, Receiving, or
storage-spike source changed. ADR-0014 remains Accepted, and B4 was not started.

## PR #5 final corrective evidence — 2026-08-19

The final complete-PR review identified three additional storage-boundary
defects. First, the production activation path read the installation reference
from Drift while constructing the activation transaction, before the service's
typed storage boundary; a raw local exception could therefore escape while the
controller remained `activating`. The service now translates that read to
`IDENTITY_BINDING_STORAGE_FAILED`, and activation/recovery controller boundaries
also map an unexpected implementation exception to a deterministic locked
fail-closed state. A failure before the durable attempt write sends no HTTP and
creates no ambiguous journal record; failures after a durable attempt retain
the accepted ADR-0013 exact-replay semantics and never regenerate its key.

Second, the binding and safe-snapshot reads used to classify a failed
revalidation were themselves fallible. A secondary read failure could escape
while the controller remained `refreshing`. Revalidation now clears the
memory-only access token before classification, translates binding/snapshot
read failures to the typed fail-closed storage state, preserves the durable
refresh predecessor journal and immutable local state, and republishes
authority only after the fault is removed, exact refresh recovery succeeds,
and `/auth/me` confirms the context. The adjacent startup audit found the same
exception-boundary class around initial binding, snapshot, activation-journal,
Device-journal, and refresh-journal reads. Startup now catches both typed and
unexpected failures, clears memory authority, and exits loading fail-closed.

Third, local Device retirement after `DEVICE_NOT_ACTIVE` could throw while the
controller was applying the remote failure, bypassing its terminal state. The
coordinator now translates raw retirement/storage exceptions to
`IDENTITY_CREDENTIAL_PERSISTENCE_FAILED`; the controller catches typed and raw
retirement failures, clears memory authority, and locks fail-closed. If
retirement cannot be confirmed, the possibly revoked Device is not used for
authorization and is not silently deleted or replaced. While the secure-store
fault persists, restart remains fail-closed. After recovery, the remote
inactive result is applied again, durable retirement completes, and
same-Device reactivation rotates the Device secret while preserving encrypted
data, its key, immutable binding, and safe snapshot.

Corrective host evidence passed 53/53 focused identity lifecycle tests, 78/78
combined identity/binding/secure-journal/origin tests, the complete 278-test
Flutter suite, analyzer validation with no issues, and all 13 architecture
checks. Dart formatting verification passed for the five corrective Dart files;
the existing protected POC remained untouched. No Drift schema or generated
file changed.

The retained Android production-adapter probe passed 1/1 on the official
`TraderPro_API24` emulator and 1/1 on the authorized `2311DRK48I` Android
15/API-35 arm64-v8a physical Device. It used isolated entries in the real
Android secure store, the real stored database key, an isolated
SQLite3MC-encrypted Drift database, the production binding repository, and the
production identity service/coordinator/controller. It injected the activation
metadata read failure, binding and snapshot reads during network-failed
revalidation, secure-store unavailable and persistence-verification retirement
failures, and an unexpected platform retirement exception. Every path exited
its transient state, published no access token, preserved the policy-required
Device/DB/key/binding/snapshot/journal state, stayed fail-closed while its fault
persisted, and recovered after fault removal. Same-Device reactivation retained
the Device ID and advanced its secret version.

Both current Device runs retained `FLAG_SECURE`; the API-35 `MainActivity`
window explicitly reported `SECURE`, and captured API-24/API-35 application
regions were blank/protected. The scoped scan for six synthetic Device-secret,
access-token, refresh-token, password, Device-ID, and activation-code sentinels
returned zero log matches on each target. The isolated test state and
disposable physical debug installation were removed. Dependencies, lockfiles,
backend, contracts, protected POC, legacy database, Receiving, and B2 storage-
spike sources remain unchanged. ADR-0014 remains Accepted, and B4 remains not
started.

## Deferred and residual

- B4 master/Receiving/sync/business authorization is not started.
- Offline identity status does not authorize a Commercial business operation.
- Rooted/live-process compromise, MFA, password reset, SSO, hardware
  attestation, and RLS are not solved here.
- Destructive OEM secure-store invalidation was not induced on the authorized
  physical Device. The instrumented native failure matrix passed fail-closed
  ciphertext preservation; OEM invalidation remains a controlled pilot gate.
- No production credential, production database, or customer data is used for
  validation.
