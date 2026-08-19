# TPRUN-007: Production mobile identity validation

- Status: Task 7C2B3 validated through the final PR #5 corrective pass on
  2026-08-19
- Applies to: production `apps/mobile/lib/main.dart` identity composition
- Data rule: synthetic/disposable identity data only

## Safety

Never use a production database, persistent production credential, customer
Workspace, or customer Device. Never print or retain passwords, activation
codes, Device secrets, access/refresh tokens, Authorization headers, secure
store envelopes, database keys, or test base-URL credentials. Keep ADB dumps,
APKs, temporary signing material, databases, and scanner output outside Git
and delete them after evidence review.

Do not clear application data or uninstall until preservation/replacement
checks are complete. Force-stop proves process death, not power-loss atomicity.

## Host gate

From `apps/mobile`, supply a non-secret HTTPS test origin at compile time and
run:

```powershell
flutter analyze --no-pub
flutter test --no-pub
flutter build apk --release --split-per-abi `
  --target-platform android-arm,android-arm64 `
  --dart-define=TRADERPRO_API_BASE_URL=https://identity.example.test/
```

Also run the repository backend identity regression script from the root:

```powershell
pwsh -NoProfile -File .\scripts\test\test-production-identity-foundation.ps1
```

The host gate must prove the frozen OpenAPI fixture, strict codec/error
handling, origin/TLS rules, response bounds, credential journals, schema
migration, immutable binding, refresh crash/race matrix, safe UI, and
architecture ownership rules.

## Device gate

Use the existing official API-24 emulator and the authorized Android 15/API-35
physical Device. Record only model class/API/ABI, not a committed serial.
Install a debug/test build that targets a repository-approved disposable local
identity backend. Any debug HTTP use requires the explicit
`TRADERPRO_ALLOW_DEBUG_HTTP=true` define and must never appear in release.

On each Device validate:

1. first activation and Device credential persistence;
2. response loss followed by exact activation recovery;
3. login and authoritative `/auth/me` first binding;
4. force-stop/process restart and refresh restoration;
5. exact predecessor recovery inside 30 seconds;
6. explicit login after the replay window;
7. exact-match reopen and each controlled immutable-context mismatch;
8. role/display snapshot update;
9. normal logout and logout-all preserving DB/key/binding/Device;
10. reactivation and definite inactive-Device handling;
11. offline bound startup with no business operation available;
12. secure-store failure without credential/database reset;
13. same-signature `adb install -r` replacement preserving expected state;
14. task-switcher/screenshot blocking and backup/cleartext manifest posture.

Use `adb shell am force-stop <package>` for force-stop. Use `adb install -r`
for replacement; `flutter install` may uninstall first and is not preservation
evidence.

## Artifact and log inspection

Verify the release merged manifest/APK is non-debuggable, requests INTERNET,
sets cleartext and backup false, includes backup rules, and packages only the
approved ARM SQLite3MC native graph. Search accessible logs, SQLite artifacts,
and generated source/artifact text for the synthetic sentinel values used by
the test. Do not search for or echo real secrets.

Confirm:

- no secret value occurs in Drift or logs;
- no access token survives process death;
- only identity infrastructure writes Authorization;
- secure-store entries use the dedicated namespace and journal keys;
- no LAN/private fallback is embedded in release;
- production main starts no POC or Receiving service; and
- pubspec, lockfile, dependency graph, and approved B2 notices are unchanged.

## Failure handling

Stop and preserve non-secret diagnostics if the frozen API differs, a
credential can be lost after committed activation/refresh, schema migration
is destructive, a late request resurrects a logged-out session, a mismatch
shows cached business data, release accepts cleartext, or either required
Device gate fails. Do not repair by deleting ciphertext, regenerating keys,
loosening TLS, retrying a non-idempotent POST generically, or adding B4 scope.

## B3 closeout evidence

The procedure passed on 2026-08-17 on the official Android 7/API-24 x86_64
emulator and the authorized `2311DRK48I` Android 15/API-35 arm64-v8a physical
Device. Both targets passed the production secure-foundation tests, full
identity response-loss/crash/replay/binding/offline/logout/reactivation
matrix, same-signature replacement, force-stop/cold-start continuity,
`FLAG_SECURE`, encrypted-artifact scans, and package-scoped log scans.

The API-24 matrix exposed a PostgreSQL timestamp-precision mismatch in exact
activation replay. Normalize activation time to PostgreSQL microsecond
precision before persistence/comparison; the focused precision regression,
existing committed-response-loss regression, and both Device matrices pass.
Do not work around this case with generic retry or Device rotation.

Inducing destructive OEM secure-store invalidation was intentionally excluded
from this authorized Device run. Use the instrumented native failure matrix to
prove fail-closed ciphertext preservation during engineering validation and
schedule real OEM invalidation only as a separately controlled pilot gate.

## PR #5 corrective revalidation — 2026-08-18

For corrective review, run the focused identity matrix for the following seven
behaviors before repeating Device evidence:

1. Device labels: trimmed, 200 accepted, 201/blank rejected before storage or
   transport, with a correctable form.
2. Revoked Device: same-Device reactivation is available and local encrypted
   state remains intact.
3. Logout-all: refreshes an expired access token exactly once, shares a
   concurrent refresh, prevents late publication, and distinguishes remotely
   confirmed logout from explicit local-only fallback.
4. Startup: a durable pending activation/reactivation wins over an older
   committed Device credential and exact replay remains available.
5. Activation recovery: TLS and ambiguous network failures retain their
   activation-specific exact-recovery UI and durable attempt.
6. Logout storage failure: exits progress, clears memory authorization, locks
   fail-closed, and preserves Device/binding/database ciphertext/key material.
7. Login correction: empty/invalid values issue no request and allow a valid
   resubmission.

The 2026-08-18 corrective gate passed 44 focused identity tests, 98 combined
identity/security/architecture tests, the full 266-test Flutter suite, analyzer
validation, and 13 architecture tests. The existing API-24 corrective matrix
passed. The authorized `2311DRK48I` Android 15/API-35 arm64-v8a physical Device
then passed the same 44/44 targeted tests.

The physical continuity probe must use isolated synthetic namespaces with the
real production secure-store adapter, secure database-key store, encrypted
Drift database, immutable binding repository, and identity controller. Record
the isolated `prepared` phase, verify the process is absent after explicit
`adb shell am force-stop <package>`, relaunch without reinstalling, and require
the isolated `passed` phase. That pass verifies exact pending-reactivation
recovery, Device secret-version advancement, pending-attempt consumption, and
unchanged immutable binding across process death.

While the probe is foregrounded, verify a screenshot yields a black protected
application surface and scan package logs for only the synthetic sentinels.
The 2026-08-18 physical run produced a black `FLAG_SECURE` capture and zero log
matches for all five sentinels. Remove temporary entrypoints, Device artifacts,
and the disposable synthetic installation afterward. Do not repeat already-
passed host/API-24 gates unless physical validation exposes a production code
defect; do not begin B4 as part of corrective validation.

## PR #5 second corrective revalidation — 2026-08-19

The second corrective pass covers two later static-review findings only:

1. A binding or snapshot storage failure must translate to the typed
   `IDENTITY_BINDING_STORAGE_FAILED` state, retire persisted refresh authority,
   publish no access token, exit authentication/revalidation progress, and
   preserve the encrypted database, database key, Device credential, and any
   existing immutable binding.
2. Release-origin validation must recognize `::ffff:0:0/96` IPv4-mapped IPv6
   addresses and apply the existing IPv4 local/private policy to the embedded
   address without rejecting mapped public addresses.

Focused host coverage passed 60/60 across the identity flow, binding repository,
and API-origin suites. The full Flutter suite passed 271/271, analyzer validation
reported no issues, and the architecture gate passed all 13 core checks plus
three protected POC checks. The retained Android integration probe then passed
1/1 on the official API-24 emulator and 1/1 on the authorized `2311DRK48I`
Android 15/API-35 arm64-v8a Device. Already-passed host and API-24 gates were not
repeated during the physical closeout.

The Android probe uses isolated entries in the real secure-store namespaces, an
isolated encrypted Commercial database and key, the production binding
repository, identity service, refresh coordinator, and controller. SQLite abort
triggers inject both first-binding snapshot insertion failure and existing-
binding snapshot update failure. On API 35, both paths reached the typed locked
fail-closed state with no published memory access token; the committed refresh
credential was replaced by its durable cleared state; Device ID, database key,
and immutable binding remained unchanged; and normal login/binding recovery
succeeded after each trigger was removed. Probe cleanup removed its isolated
database, secure-store entries, screenshot, and temporary debug installation.

IPv4-mapped parsing remains deterministic Dart `Uri`/`InternetAddress` logic
with no Android-specific production override. Host boundary tests are therefore
authoritative: mapped unspecified, loopback, link-local, and private addresses
are rejected, while mapped public `8.8.8.8`, `172.15.255.255`, and `172.32.0.0`
remain accepted. No Android divergence was observed.

The API-35 TraderPro `MainActivity` window record contained the `SECURE` flag,
and its captured application region was blank/protected with no app content
visible. Device logs contained zero matches for all five synthetic Device,
access, refresh, password, and identity sentinels. Pubspec, lockfile, backend,
protected POC, legacy database, Receiving, and storage-spike sources remain
unchanged. ADR-0014 semantics remain unchanged, and B4 was not started.

## PR #5 final corrective revalidation — 2026-08-19

The final corrective pass covers three storage/error-boundary findings and one
adjacent same-class startup boundary:

1. Activation installation-reference reads must translate Drift/SQLite and
   other implementation errors before they reach the controller. A failure
   before the durable attempt write sends no HTTP and creates no attempt; an
   existing durable attempt must retain its exact ADR-0013 key/body for replay.
2. Binding or safe-snapshot reads used after a network-failed revalidation are
   fallible. A secondary read failure must clear memory access, exit
   `refreshing`, expose `IDENTITY_BINDING_STORAGE_FAILED`, preserve immutable
   local state and the durable refresh predecessor journal, and never classify
   cached data as authorized offline state. After fault removal, exact refresh
   recovery and `/auth/me` must precede access-token publication.
3. A local Device-retirement failure after `DEVICE_NOT_ACTIVE` must translate
   to a typed secure-store/persistence failure, exit authentication progress,
   and lock fail-closed. The Device, database, key, binding, and safe snapshot
   remain intact when retirement is unconfirmed. While the secure-store fault
   persists, restart must remain fail-closed; after recovery, retirement and
   same-Device reactivation must complete deterministically.
4. Initial binding, snapshot, activation-attempt, Device, refresh, and session-
   restoration reads use the same typed/unexpected startup boundary so no raw
   implementation exception can leave the controller loading indefinitely.

Host regression evidence for this pass is 53/53 focused identity lifecycle
tests, 78/78 combined identity/binding/secure-journal/origin tests, 278/278 for
the complete Flutter suite, no analyzer issues, and 13/13 architecture checks.
Formatting verification passed for all five corrective Dart files. No Drift
schema/generation, dependency, or lockfile change was required.

Run the retained Android integration probe on both required targets. It must
use isolated production secure-store namespaces, a real stored database key,
an isolated SQLite3MC-encrypted Commercial database, the production binding
repository, and the production identity service/coordinator/controller. Inject:

- the production activation-metadata Drift read failure before journal/HTTP;
- binding and snapshot read failures during network-failed revalidation;
- Device-retirement secure-store unavailable and persistence-verification
  failures; and
- one unexpected platform retirement exception.

For each injection, require a terminal typed fail-closed state, no memory access
token, policy-correct journal state, unchanged encrypted DB/key and immutable
identity state, persistent-fault restart behavior, and normal recovery after
removing the fault. The retirement cases must then complete remote-inactive
handling and same-Device reactivation with an advanced secret version.

On 2026-08-19 the retained probe passed 1/1 on the official Android 7/API-24
x86_64 emulator and 1/1 on the authorized `2311DRK48I` Android 15/API-35
arm64-v8a physical Device. Every injected path met the required transient-exit,
authorization, durability, continuity, restart, and recovery assertions. The
API-35 `MainActivity` window record explicitly contained `SECURE`; API-24 and
API-35 captures exposed blank/protected app regions. The scoped log scan for six
synthetic Device-secret, access-token, refresh-token, password, Device-ID, and
activation-code sentinels returned zero matches on both targets. Remove the
isolated state, captures, and disposable debug installation after review.
ADR-0014 remains Accepted, and B4 remains not started.
