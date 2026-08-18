# Task 7C2B3 production mobile identity and context binding assessment

- Status: Complete; host, release-posture, API-24, and physical API-35 gates pass
- Evidence date: 2026-08-18
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
