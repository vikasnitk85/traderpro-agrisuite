# Task 7C2A encrypted-storage compatibility spike

Date: 2026-08-03

Branch: `task/7c2a-contract-security-spike`

Decision: **No production candidate selected. Both candidates passed the physical API-35 corrective matrix, but API-24 runtime, destructive lifecycle/actual Keystore invalidation, restore/transfer, crash-safe rekey, repeated performance, and supply-chain gates remain open.**

## Scope and safety boundary

The evidence harness is isolated at `spikes/task_7c2a_encrypted_storage`. It has its own manifest, lock file, entry point, Android application ID, schema, tests, and synthetic files. It is not imported by `apps/mobile`, is not reachable from production navigation, and never opens `traderpro-local.sqlite`. Android backup is disabled. No production credential, customer datum, token, capability, lease ID, authorization header, or persisted database key was used.

The spike exercises two database-engine builds through the same pinned `sqlite3` package hook and one Android OS-backed secure-store package. This document does not approve an engine or change ADR-0009's status.

## Environment

| Component | Executed version |
|---|---|
| Host | Windows 11 25H2, x64 |
| Flutter | 3.44.4 stable, framework `ad70ec4617` |
| Dart | 3.12.2 |
| Android SDK | 36.1; platform/build tools 36.1 used by Flutter |
| Java | Android Studio JBR OpenJDK 21.0.10; source/target compatibility 17 |
| Spike Android levels | min 24, compile 36, target 36 |
| Corrective physical device | Xiaomi `2311DRK48I`, Android 15 / API 35, arm64; unique serial omitted from commit evidence |
| Device build | HyperOS `OS2.0.208.0.VNLINXM` |
| Drift | 2.34.3 |
| sqlite3 Dart bindings | 3.5.0 |
| Secure store | flutter_secure_storage 10.3.1 |

The exact hosted-package versions and SHA-256 package hashes are in `spikes/task_7c2a_encrypted_storage/pubspec.lock`. Relevant transitive versions are `hooks 2.0.2`, `native_toolchain_c 0.19.2`, `jni 1.0.3`, `jni_flutter 1.0.2`, `jni_util 1.0.0`, `flutter_secure_storage_darwin 0.3.2`, `flutter_secure_storage_linux 3.0.1`, `flutter_secure_storage_platform_interface 2.0.2`, `flutter_secure_storage_web 2.1.1`, and `flutter_secure_storage_windows 4.2.2`.

## Candidates tested

### SQLite3MultipleCiphers via sqlite3 3.5.0

- Hook setting: `sqlite3.source: sqlite3mc`.
- Bundled engine: SQLite3MultipleCiphers 2.3.6 over SQLite 3.53.3, as recorded by the pinned package changelog.
- Spike cipher: explicit `chacha20`, the engine's recommended ChaCha20-Poly1305 authenticated mode. The key is set before schema access, and `sqlite_master` is queried immediately to force verification.
- Provenance: prebuilt `libsqlite3mc.so` native assets from the `sqlite3-3.5.0` release. The package embeds expected SHA-256 hashes; Android hashes exist for arm, arm64, ia32, and x64. The built Flutter APK contained arm64-v8a, armeabi-v7a, and x86_64.
- License/notices: the Dart wrapper and SQLite3MultipleCiphers are MIT; upstream SQLite is public domain. A production distribution must retain the applicable MIT copyright/license notices and recorded package/native-asset provenance. [SQLite3MultipleCiphers license](https://raw.githubusercontent.com/utelle/SQLite3MultipleCiphers/main/LICENSE).
- Maintenance: 2.3.6 is recent in the pinned package; upstream release history remains active. Cipher agility is useful for compatibility but increases the risk of an accidental weak/legacy configuration. This spike hard-codes ChaCha20 and never permits an empty key.
- Rekey: `PRAGMA rekey` / `hexrekey` exists, but in-place rekey and crash recovery were not executed. Upstream warns that cipher configuration pragmas are not transactional and rekey has WAL/page-layout constraints. [SQLite3MultipleCiphers pragma documentation](https://utelle.github.io/SQLite3MultipleCiphers/docs/configuration/config_sql_pragmas/).

### SQLCipher Community via sqlite3 3.5.0

- Hook setting during its matrix: `sqlite3.source: sqlcipher`.
- Bundled engine: SQLCipher 4.17.0, as recorded by the pinned package changelog.
- Spike cipher: SQLCipher defaults, with key-before-schema access and forced verification through `sqlite_master`.
- Provenance: prebuilt `libsqlcipher.so` native assets from the same checksummed `sqlite3-3.5.0` release. The SQLCipher Android build links OpenSSL. The built APK contained arm64-v8a, armeabi-v7a, and x86_64.
- License/notices: SQLCipher Community is BSD-3-Clause and the Dart wrapper is MIT. A production distribution must retain their copyright/license notices plus the linked OpenSSL license/notices. The exact bundled OpenSSL version still requires extraction and must appear in the SBOM/native-binary provenance. [SQLCipher license](https://raw.githubusercontent.com/sqlcipher/sqlcipher/master/LICENSE.md).
- Maintenance: SQLCipher has an active maintained repository, published changelog, tamper-detecting page HMAC, and documented migration/rekey behavior. The release APK was 8,894,860 bytes larger than the SQLite3MultipleCiphers release APK in this minimal harness.
- Rekey: `PRAGMA rekey` is supported after opening with the old key, but the spike did not establish a crash-safe two-phase production procedure. [SQLCipher API](https://www.zetetic.net/sqlcipher/sqlcipher-api/).

### Android secure-store candidate

- `flutter_secure_storage 10.3.1`, BSD-3-Clause, pinned with hosted-package SHA-256; a production distribution must retain its copyright/license notice.
- Android implementation requires API 23; the spike requires API 24. The package compiles against API 36 and Java 17.
- Default Android protection is RSA-OAEP with SHA-256 key wrapping plus AES-GCM storage encryption. `storageNamespace` isolates this spike's aliases and preferences. `resetOnError` is explicitly `false` because its default `true` can permanently erase the stored database key on an error. [Package documentation](https://pub.dev/packages/flutter_secure_storage).
- The generated 32-byte value is stored only through the secure-store abstraction. Normal database opening calls `loadExisting` and fails closed on null, malformed data, or a platform exception. Only an explicit first-run provisioning path may create a key.
- Host unit simulations passed for missing, reset, malformed, and unavailable storage. On the API-35 device, real write/read/delete, correct-value reload, malformed persisted value, deleted value, and missing value all behaved as required. Simulated platform unavailability propagated without deleting the ciphertext. Uninstall/reinstall, restore, and actual Android Keystore invalidation remain unexecuted.

## Compatibility matrix

| Gate | SQLite3MultipleCiphers | SQLCipher Community | Evidence |
|---|---|---|---|
| Flutter 3.44.4 / Dart 3.12.2 | Pass | Pass | Resolution, analysis/tests, APK builds |
| Drift 2.34.3 generated schema/queries | Pass | Pass | Same generated database and nine-test matrix |
| Correct-key create/write/read/reopen | Pass | Pass | Host and physical API-35 execution |
| Wrong, missing, malformed, deleted, and unavailable key fail closed | Pass | Pass | Physical plugin values plus simulated unavailable-store exception; ciphertext retained |
| No plaintext fallback | Pass | Pass | Unkeyed schema read failed; device and host header/marker/schema scans clean |
| Schema 1 to 2 migration | Pass | Pass | Encrypted v1 file migrated through Drift on API 35 |
| Transaction rollback | Pass | Pass | Synthetic insert absent after forced rollback on API 35 |
| Background isolate | Pass | Pass | `NativeDatabase.createInBackground` opened and queried both engines on API 35 |
| WAL checkpoint and reopen | Pass | Pass | `wal_checkpoint(FULL)`, close, background-isolate reopen, exact marker recovery |
| Force-stop / new-process reopen | Pass | Pass | Process absent after force-stop; each cold launch received a different PID and reopened the encrypted database |
| Encrypted main/WAL/SHM device artifacts | Pass | Pass | Four files / 266,464 bytes per engine; zero known plaintext matches |
| Rollback-journal artifact | Host pass; device not produced | Host pass; device not produced | Host captured/scanned journals; API-35 VFS produced no journal in delete mode even for the expanded committed-page fixture |
| Android API 24 build | Pass | Pass | `aapt2`: minSdk 24 |
| Android 15 / API 35 | Pass | Pass | Physical `2311DRK48I`, arm64, API 35 |
| Debug APK | Pass | Pass | `assembleDebug` |
| Unsigned release-mode APK | Pass | Pass | `assembleRelease`; no production signing |
| APK ABIs | Pass | Pass | arm64-v8a, armeabi-v7a, x86_64 |
| OS secure-store runtime | Pass | Pass | `flutter_secure_storage` 10.3.1 on physical API 35 |
| Rekey / crash recovery | Not established | Not established | API support assessed only |
| Uninstall/reinstall/cross-device restore | Not executed | Not executed | Must fail closed in pilot |

## Functional test matrix

The finalized host test suite passed 9/9 under each engine:

1. explicit first-run key provisioning creates and persists one generated key;
2. missing/reset secure-store key fails without silent replacement;
3. secure-store exception propagates without mutation;
4. malformed key is rejected before open;
5. encrypted Drift write, close, background-isolate reopen, read, and `temp_store=MEMORY`;
6. transaction rollback;
7. explicit schema v1-to-v2 migration;
8. wrong key and missing key refusal;
9. unkeyed engine refusal.

The corrective physical-device suite then passed 6/6 under each engine. It covered real secure-store correct/missing/malformed/deleted values, unavailable-store fail-closed behavior, correct/wrong/unkeyed database opening, ciphertext retention, Drift migration, rollback, background-isolate opening, WAL checkpoint/reopen, available-artifact plaintext scans, and the identical 1,000-row measurement workload.

The separate force-stop harness used an explicit first-database provisioning path and normal fail-closed loading thereafter. For each engine, the process was force-stopped until `pidof` returned no process; the next cold launch received a different PID and reopened the existing encrypted database. Both second launches reported `databaseExistedBeforeLaunch: true`, `phase: reopened`, `markerCount: 1`, `success: true`, and a 16,384-byte encrypted database.

## Artifact and plaintext inspection

The generator creates a random test key in memory, writes only the synthetic marker `TRADERPRO_7C2A_SYNTHETIC_ARTIFACT_MARKER`, captures files while transactions are active, and never prints or persists the key. It forces `temp_store=MEMORY`; each engine run reported zero unexpected temporary spill files.

Executed inspection pattern:

```powershell
dart --suppress-analytics run tool/generate_encrypted_artifacts.dart
Get-ChildItem artifacts -Recurse -File | Get-FileHash -Algorithm SHA256
Get-ChildItem artifacts -Recurse -File | Select-String -SimpleMatch 'TRADERPRO_7C2A_SYNTHETIC_ARTIFACT_MARKER'
Get-ChildItem artifacts -Recurse -File | Select-String -SimpleMatch 'SQLite format 3'
tar -tf build\app\outputs\flutter-apk\app-release.apk
```

The original host scans returned zero marker matches and zero ordinary `SQLite format 3` header matches across all ten captured files. The corrective SQLCipher host rerun additionally searched `synthetic_markers` and `migration_evidence`; all four patterns had zero matches, and no unexpected temporary spill file was produced.

| Engine | File | Bytes | SHA-256 |
|---|---:|---:|---|
| SQLite3MultipleCiphers | rollback-main.db | 8,192 | `997EDB48F00402F027311E76222A186602D23C6081D81C80BED8E94B450358FA` |
| SQLite3MultipleCiphers | rollback.db-journal | 4,616 | `CB8AA5B47D89577A77788A5D7E22E5E611430A28225D80924E8C20D3C8DA7DCF` |
| SQLite3MultipleCiphers | wal-main.db | 16,384 | `A6279E1414DE63C4684E10E04FD1C8145A4F231DAB9761F6CEB06C7F4946A4C4` |
| SQLite3MultipleCiphers | wal.db-shm | 32,768 | `A9F1234BAE5A068FD7D0FC8181954C44BC0B2B7F52F845F353AB7E95DAC09EC8` |
| SQLite3MultipleCiphers | wal.db-wal | 32,992 | `8DE71084F38B9F5CFEDEE15E7727F08DE2A75B8C0F6D4D94FDFAC797A1123841` |
| SQLCipher | rollback-main.db | 8,192 | `8FD89F64766A008CC75426B2AABE6593098EAAEB7E949B10FF0DA63BC8D7D0D6` |
| SQLCipher | rollback.db-journal | 4,616 | `8B7261BDD7BD85B99F72EBA3571A6B7D4C72C066A3ACC28E3520F2FFE6520E2A` |
| SQLCipher | wal-main.db | 16,384 | `5D6BA573FD6DA3D8FE0AE724B5B6929FFF40F5F743A4B2701567ECBC4888A821` |
| SQLCipher | wal.db-shm | 32,768 | `190F6E566DA5E3B94750073D4C6260834A0C0A40DEA713A3760597D345F0AF4D` |
| SQLCipher | wal.db-wal | 32,992 | `BC227B8E68914B853367889FF759064914ACF48ED94C6C8B23EA1BD95D1501A0` |

The SHM file is WAL-index metadata and is not claimed to be an encrypted user-payload container; the relevant finding is that the known payload marker was absent. These small synthetic scans do not replace forensic review under memory pressure, large sorts, interrupted checkpoints, or OEM filesystems.

On the physical API-35 device, each engine captured the same four available files: `wal-main.db` (16,384 bytes), `wal.db-wal` (32,992), `wal.db-shm` (32,768), and `rollback-main.db` (184,320), totaling 266,464 bytes. Raw-byte searches for the synthetic artifact marker, rollback marker, ordinary `SQLite format 3` header, `synthetic_markers`, and `migration_evidence` returned zero matches for both engines. No `rollback.db-journal` was emitted on this device even after selecting `journal_mode=delete`, committing 128 rows with 1 KiB payloads, and updating the committed pages inside an active transaction. The report therefore records `rollbackJournalCaptured: false` rather than claiming a nonexistent device artifact was scanned; both host engines did produce and pass scans of rollback journals.

Extracted APK scans found zero marker matches. No test key appeared in console output. The SQLCipher wrong-key test logged only expected HMAC/page-decryption errors. Fixtures use explicit placeholders rather than real tokens/capabilities, and backend drift tests reject a UUID-shaped frozen lease/refresh/activation/access capability.

## Android and release evidence

`aapt2 dump badging` reported min SDK 24, target SDK 36, compile SDK 36, and native code for `arm64-v8a`, `armeabi-v7a`, and `x86_64`. `sqlite3` publishes Android assets for armv7a, aarch64, x86, and x64, but the Flutter APK did not package 32-bit x86. [sqlite3 platform and hook documentation](https://pub.dev/packages/sqlite3).

| Engine | Debug APK | Release-mode APK | Release SHA-256 |
|---|---:|---:|---|
| SQLite3MultipleCiphers | 164,064,483 bytes, pass | 49,873,080 bytes, pass | `BA43CA5F2CC27B3ADE58B42B5EB2935A0B246B8516C4BC6054E8DB94E21850CF` |
| SQLCipher | 177,055,339 bytes, pass | 58,767,940 bytes, pass | `BB4BD64F04714A47D578B51BE2A5E04B6F49EE328C70E7ABED272E2EC86ADBEA` |

Release mode was compiled and packaged without production signing. API-35 startup, background-isolate database access, OS-backed secure storage, and force-stop/new-process recovery were subsequently exercised on the physical arm64 device. No AAB, Play install, API-24 runtime, or hardware-backed-key attestation was performed. The corrective SQLCipher restart APK was 185,665,389 bytes with SHA-256 `726A2592589056DFCFF14E5C35615BF726E40BB9C7432BD40E24200E26C3B505` and contained `libsqlcipher.so` for arm64-v8a, armeabi-v7a, and x86_64.

## Performance observations

The corrective comparison used the identical debug-mode workload on the same API-35 device and internal app storage: encrypted background-isolate open, one transaction inserting 1,000 synthetic rows, a full WAL checkpoint, close, encrypted background-isolate reopen, and row count. Build, installation, and test-framework time were excluded.

| Engine | Open | 1,000-row transaction | WAL checkpoint | Reopen + count | Total | Final DB |
|---|---:|---:|---:|---:|---:|---:|
| SQLite3MultipleCiphers | 104 ms | 967 ms | 4 ms | 55 ms | 1,139 ms | 77,824 bytes |
| SQLCipher Community | 179 ms | 1,075 ms | 3 ms | 127 ms | 1,393 ms | 77,824 bytes |

In this single synthetic run SQLCipher was 72% slower to open, 11% slower for the write transaction, 131% slower to reopen/count, and 22% slower overall; the 3-4 ms checkpoint difference is noise-scale. These measurements are comparable evidence, not a production benchmark or selection basis. Production-like volumes, repeated samples, thermal controls, lower-end hardware, low-storage behavior, and percentile reporting remain necessary.

## Rekey, recovery, backup, and restore assessment

- Both engines expose online rekey, but neither was proven crash-safe. An in-place `PRAGMA rekey` alone is not an accepted two-phase recovery design.
- Task 7C2 V1 will not implement automatic rekey. Rekey requires a separately approved, fault-injected recovery design.
- A production design needs an explicit state machine that retains old and new wrapped keys until a verified reopen/checkpoint completes, or uses a separately encrypted copy plus verified atomic promotion. WAL mode and free-space/power-loss cases require device testing.
- If Android secure storage loses or invalidates the key, ciphertext must be retained and access must stop. The app must not silently generate a replacement key, open plaintext, delete the database, or rewrite offline physical facts.
- Reauthentication can recover access only if a separately reviewed, authenticated cloud-assisted key recovery design exists. No such recovery is approved here.
- Cross-device restore is unsupported in V1 and must fail closed because Android Keystore material is device-bound. `allowBackup=false` and `fullBackupContent=false` reduce accidental backup in this spike, but API-31 data-extraction/OEM device-transfer behavior still needs an on-device pilot and explicit production rules.
- Database discard/reinitialization is permissible only through a separate, explicitly approved recovery flow that first classifies/preserves unsynchronized physical facts and retains ciphertext until authorization. That destructive UX is outside Task 7C2A.

## Security, maintenance, and license findings

- Both engines rejected wrong/unkeyed opens and exposed no known plaintext in inspected device main/WAL/SHM samples or host main/WAL/SHM/journal samples.
- SQLite3MultipleCiphers has the smaller artifacts and newer bundled SQLite base, but its multiple legacy cipher modes make exact configuration and drift tests mandatory.
- SQLCipher has a narrower, mature encrypted-SQLite contract and per-page HMAC diagnostics, but adds OpenSSL provenance/notices and a larger binary.
- `flutter_secure_storage` is maintained and current, but its destructive `resetOnError=true` default conflicts with TraderPro's preservation rule. The spike overrides it to false and disables backup.
- No project-specific published advisory was found in the checked official project/package security pages on 2026-08-02. This is not an SCA result. A pilot must run locked-SBOM vulnerability/license scanning over the Dart graph and extracted native libraries and must define patch SLAs for SQLite, cipher engine, OpenSSL, and secure storage.

## Commands and results

| Command | Result |
|---|---|
| `flutter pub get` in spike | Pass; lock file produced |
| `flutter analyze` in spike | Pass; no issues |
| `flutter test` with `source: sqlite3mc` | Pass, 9/9 |
| `flutter test` with `source: sqlcipher` | Pass, 9/9 |
| `flutter build apk --debug` for each engine | Pass |
| `flutter build apk --release` for each engine | Pass, unsigned evidence only |
| artifact generator and PowerShell raw scans | Pass; zero headers/markers/spills |
| `adb devices -l` through SDK ADB | Pass; one authorized physical `2311DRK48I`; unique serial omitted from commit evidence |
| `flutter devices` | Pass; Android 15/API 35, arm64 |
| API-35 `device_storage_test.dart`, `source: sqlite3mc` | Pass, 6/6 |
| API-35 `device_storage_test.dart`, `source: sqlcipher` | Pass, 6/6 |
| Force-stop/new-process harness | Pass for both engines; PID absent after stop and changed after cold launch |
| Device available-artifact raw scans | Pass; zero matches across 266,464 bytes per engine; rollback journal not produced |
| Comparable 1,000-row device workload | Pass; SQLCipher 1,393 ms vs SQLite3MC 1,139 ms total |
| Focused disposable PostgreSQL Testcontainers integration | Pass, 22/22 `CommercialReceivingApiTests` in 2m36s after starting the local Docker engine |
| uninstall/reinstall/restore/actual Keystore invalidation | Not run |

## Unresolved mandatory gates

1. Run the same matrix on a physical API-24 device; API-35 coverage alone does not prove the minimum-SDK path.
2. Add low-storage, low-memory, abrupt kill during write/checkpoint/migration, and power-loss fault injection on Android.
3. Exercise uninstall/reinstall, app upgrade, auto-backup, API-31 data extraction, OEM device transfer, screen-lock change, and actual Keystore invalidation; prove fail-closed ciphertext retention.
4. Design and fault-inject a crash-safe rekey/recovery state machine before approving either engine.
5. Benchmark production-like volumes with repeated samples on representative low-end hardware.
6. Complete SBOM, native-binary/OpenSSL notice, vulnerability, and supply-chain review; mirror/checksum approved assets if required.
7. Decide whether production will standardize SQLCipher's narrower contract or SQLite3MultipleCiphers' smaller/faster observed build. Keep the build hook and cipher choice immutable after production data exists unless a migration is explicitly designed.

## Recommendation

**Needs corrective work before production selection.** Both encrypted database candidates passed the executable host, physical API-35, secure-store, force-stop, Drift, WAL, available-artifact, and focused disposable-backend gates. SQLCipher provides the narrower mature encrypted-SQLite contract, while SQLite3MultipleCiphers was smaller in the prior release build and 22% faster in this single device workload; neither observation is sufficient to select a production engine. No accepted ADR is justified until the API-24, destructive lifecycle/Keystore invalidation, crash-safe rekey, repeated performance, and supply-chain gates are complete. Full Task 7C2 implementation must not begin from this assessment alone.
