# TPRUN-007: Production mobile identity validation

- Status: Task 7C2B3 validated on 2026-08-17
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
