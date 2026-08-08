# Task 7C2B1: Production encrypted-storage selection

- Status: SQLite3MultipleCiphers selected; Task 7C2B1 complete subject to commit review
- Evidence date: 2026-08-08
- Repository baseline: `10230b85d3ef25a4c67eed4b218db7a1f19d07b3`
- Evidence branch: `task/7c2b1-storage-selection`
- Scope: isolated `spikes/task_7c2a_encrypted_storage` harness and reviewed
  evidence only

## Decision

Select SQLite3MultipleCiphers 2.3.6 through Dart `sqlite3` 3.5.0 for TraderPro
V1, using SQLite 3.53.3, explicit ChaCha20-Poly1305, and a true random 256-bit
raw key. The accepted configuration and prohibitions are frozen by
`docs/decisions/ADR-0011-production-commercial-mobile-encrypted-storage.md`.
Task 7C2B1 is complete subject to commit review. Task 7C2B2 is the next
milestone and has not started; production Flutter remains unchanged.

The original API-24 and physical API-35 matrices exposed a shared clean-install
`flutter_secure_storage` failure. Source inspection identified the cause. The
corrected pinned-10.3.1 Strategy A passed clean provisioning, normal reopen,
missing/malformed/unavailable-store fail-closed behavior, exact ciphertext
preservation, process force-stop/reopen, and same-data app replacement on API
24 and the physical API-35 phone. The API-35 lifecycle evidence passed with
both SQLCipher and SQLite3MultipleCiphers, proving the correction is shared and
engine-independent. Engine-specific database, configuration, tamper, artifact,
migration, rollback, WAL, isolate, restart, and repeated physical-performance
evidence remains valid for both candidates.

The approved Grype database was downloaded, hash-verified, installed outside
the repository, frozen against automatic updates, and used to scan the locked
SBOM. Grype returned zero matches. Ten native/generic components were not
reliably matchable and received explicit manual upstream review; OpenSSL 3.6.2
has published advisories, but none of their affected APIs is reachable through
SQLCipher's exact crypto-provider call surface. The dynamic B1 validation gates
are closed. Final static comparison accepted SQLite3MultipleCiphers because it
meets the authenticated at-rest and fail-closed requirements without the
additional statically linked OpenSSL provenance, advisory, patching, and notice
surface. This choice is not based merely on speed or APK size. SQLCipher is a
validated alternative, not an insecure engine.

SQLite3MultipleCiphers' broader cipher/configuration surface and lack of an
effective exposed enhanced memory-security control are accepted risks. B2 must
prevent configuration drift through startup assertions and tests and must
minimize key lifetime and diagnostic exposure. Database encryption does not
protect a compromised live process, rooted device, attached debugger or
instrumentation, or arbitrary runtime-memory inspection.

## Scope and dependency invariants

The production dependency graph was not changed. Before and after this task:

| Production file | SHA-256 |
| --- | --- |
| `apps/mobile/pubspec.yaml` | `B55E258266E008D7F2AA2F37916E9F45BDCEB82632FF958928BCE65868BED359` |
| `apps/mobile/pubspec.lock` | `108F2A378F90F7A23317D0C371D417ED18BACBD6FB2D14B1C25D321991128880` |

No production Flutter source, Android configuration, backend source, database
migration, or frozen Commercial Receiving contract was modified. This work
does not begin Task 7C2B2.

## Resolved candidate versions

| Component | Resolved version / identity | Evidence |
| --- | --- | --- |
| Dart `sqlite3` wrapper and native hook | 3.5.0 | spike `pubspec.lock` |
| Drift | 2.34.3 | spike `pubspec.lock` |
| SQLCipher Community | 4.17.0 community | runtime `PRAGMA cipher_version` |
| SQLite3MultipleCiphers | 2.3.6 | `sqlite3` 3.5.0 changelog and pinned hook release |
| Embedded SQLite | 3.53.3 | runtime `sqlite_version()` and source ID |
| SQLCipher crypto provider | OpenSSL 3.6.2, 7 Apr 2026 | runtime provider pragma and ELF strings |
| `flutter_secure_storage` | 10.3.1 | spike `pubspec.lock` |
| Android Tink transitively used by secure storage | 1.21.0 | Gradle `releaseRuntimeClasspath` |

No version was upgraded. The hook retrieves upstream-built assets and checks
the SHA-256 allowlist in `sqlite3` 3.5.0 `asset_hashes.dart` before use.

## Frozen SQLCipher candidate configuration

`configureEncryptedDatabaseForEvidence` applies the following values after
keying and before the first schema read. `verifyEncryptedDatabaseConfiguration`
reads them back, then the opener executes
`SELECT count(*) FROM sqlite_master` as the startup verification query.

| Parameter | Frozen value | Classification | Notes |
| --- | --- | --- | --- |
| Key input | `PRAGMA key = "x'<64 hex>'"` | Fixed production candidate value | True raw 32-byte key syntax, not a text passphrase. |
| Salt | Engine-generated 16-byte database salt | Engine-required | Stored in the encrypted database header; no external salt is supplied. |
| Compatibility | `cipher_compatibility = 4` | Fixed | Prevents default drift across major modes. |
| Page size | 4096 | Fixed | Read back through `cipher_page_size`. |
| KDF algorithm | PBKDF2-HMAC-SHA512 | Version-dependent compatibility value | Explicitly frozen, but PBKDF2 is bypassed for the chosen raw-key input. |
| KDF iterations | 256000 | Version-dependent compatibility value | Explicitly frozen; not applied to raw input. |
| Authentication | HMAC-SHA512, enabled | Fixed | Per-page integrity; controlled corruption produced HMAC failure. |
| Plaintext header | 0 bytes | Fixed | No SQLite plaintext header. |
| Memory security | enabled | Fixed where supported | Runtime readback was `1`; does not protect against a rooted live process. |
| Provider | OpenSSL | Engine-required for this artifact | Runtime version 3.6.2; statically linked in `libsqlcipher.so`. |

Official semantics: [SQLCipher API documentation](https://www.zetetic.net/sqlcipher/sqlcipher-api/).

## Frozen SQLite3MultipleCiphers candidate configuration

SQLite3MC requires cipher selection and cipher parameters before the key. The
harness explicitly selects ChaCha20, disables legacy mode, then applies the raw
key. It reads all exposed values back before the same schema verification
query.

| Parameter | Frozen value | Classification | Notes |
| --- | --- | --- | --- |
| Cipher | ChaCha20-Poly1305 (`cipher = 'chacha20'`) | Fixed | Explicit selection prohibits default/legacy fallback. |
| Key input | `PRAGMA key = "x'<64 hex>'"` | Fixed production candidate value | True raw 32-byte key syntax. |
| Compatibility | `legacy = 0` | Fixed | Legacy cipher behavior is prohibited. |
| Page size | 4096 | Fixed | Read back through `page_size`. |
| KDF | PBKDF2-HMAC-SHA256, 64007 iterations | Version-dependent compatibility value | ChaCha20 default frozen explicitly; bypassed for raw input. |
| Authentication | Poly1305; `hmac_check = 1` | Engine-required / fixed verification | Per-page authentication remains enabled. |
| Plaintext header | 0 bytes | Fixed | No SQLite plaintext header. |
| Legacy WAL | `mc_legacy_wal = 0` | Fixed | Prohibits the legacy WAL scheme. |
| Memory security | unsupported by tested prebuilt artifact | Deliberately unsupported feature | Setting it is a no-op and readback is absent. The harness reports `memorySecuritySupported: false`; it never claims enabled. |

Official semantics: [SQLite3MC SQL pragmas](https://utelle.github.io/SQLite3MultipleCiphers/docs/configuration/config_sql_pragmas/)
and [ChaCha20 cipher documentation](https://utelle.github.io/SQLite3MultipleCiphers/docs/ciphers/cipher_chacha20/).

## Key-syntax and failure decision

The candidate production interpretation is a random 256-bit key represented
as exactly 64 lowercase hexadecimal characters at the Dart boundary and sent
to either engine using its documented `x'<hex>'` raw-key syntax. The key is
generated by `Random.secure`, validated before native calls, and never printed.
A 64-character string passed as ordinary quoted text is a passphrase, not a
raw key.

The evidence-only secure store freezes
`storageNamespace: traderpro_7c2a_storage_spike`, entry
`encrypted_database_key_v1`, RSA-OAEP-SHA256/MGF1 key wrapping, AES-GCM value
storage, `resetOnError: false`, `migrateOnAlgorithmChange: true`,
`migrateWithBackup: false`, and biometric enforcement disabled. A separate
evidence namespace tests the same values with `migrateWithBackup: true`.
Those namespace/entry values are not a production decision and must never be
copied as the Commercial alias. The production filename, alias/namespace,
installation/context binding, and first-run cross-store/database transaction
remain Task 7C2B2 design inputs after engine selection.

The wrapper now requires the caller to state whether ciphertext exists when it
requests first-run provisioning. A missing secure-store value can create a new
key only when the database file is absent. Missing, malformed, or unavailable
secure storage while ciphertext exists fails closed without database deletion,
replacement-key generation, or plaintext fallback. `flutter_secure_storage`
10.3.1 also brings Tink Android 1.21.0 into the reviewed supply chain.

`encryption_configuration_test.dart` proves raw-key reopen and mutual
incompatibility between raw and textual interpretations. Existing and extended
tests prove wrong-key, unkeyed, missing, and malformed-key rejection. Native
keying exceptions are converted to a key-free failure so the native statement
cannot echo key material. Raw-key use means engine KDF changes cannot be used
as password-strengthening controls; the entropy of the generated 32 bytes and
OS-backed key protection are mandatory.

Changing raw/text interpretation, salt form, engine, cipher, page format, or
compatibility value after production data exists is a data migration, not a
configuration update. Task 7C2 V1 has no automatic rekey, engine switching,
key escrow, or cross-device restore.

## `flutter_secure_storage` 10.3.1 root cause and source evidence

The exact resolved Android source under the 10.3.1 package cache explains the
shared first-launch failure:

- `lib/options/android_options.dart` documents migration of non-default
  algorithms on first access and defines the option defaults. The evidence
  configuration continues to override `resetOnError` to `false`.
- `FlutterSecureStoragePlugin.getStorageInstance` (lines 87-101) keys plugin
  instances by namespace, while `FlutterSecureStorageConfig` (lines 172-196)
  derives namespaced data preferences, key preferences, and KeyStore aliases.
- `StorageCipherFactory` (lines 27-58) interprets absent algorithm markers as
  historical RSA-PKCS1/AES-CBC. It selects the requested current algorithms,
  writes current markers immediately only for the non-backup path, and reports
  re-encryption when saved and current algorithms differ.
- `FlutterSecureStorage.initializeStorageCipher` (lines 266-273) sends that
  mismatch to `handleKeyMismatch`. With migration disabled and
  `resetOnError: false`, `handleKeyMismatch` (lines 911-966) returns the
  observed error. Destructive `deleteAllDataAndKeys` is reached only when
  reset is enabled; TraderPro leaves it disabled.
- The non-backup `migrateData` path (lines 508-571) decrypts existing entries
  into memory, deletes old keys only after all decryptions succeed, commits
  the new markers, creates the current cipher, then rewrites the values. On an
  empty namespace it decrypts and rewrites zero values, so the operation is
  marker/key initialization rather than customer-data migration.
- The backup migration path (lines 1358-1520) creates encrypted ciphertext and
  wrapped-key backup artifacts, then cleans them up after successful rewrite.
  It does not persist plaintext; plaintext exists only in process memory. It is
  more complex than necessary for a brand-new empty namespace.

There is no package-supported explicit marker-initialization API, so strategy C
does not exist. The harness does not directly mutate plugin-owned markers.

## Clean-install strategy comparison

| Strategy | Pinned options | Empty API-24 result | Security assessment |
| --- | --- | --- | --- |
| Original | OAEP-SHA256 + AES-GCM; reset false; migration false | Repeatable first-access failure | Fail closed, but unusable for first install. |
| A | Same algorithms; reset false; migration true; backup false | Pass | Preferred B1 evidence strategy. Empty namespace initializes once. Existing data migration has an interruption window after old-key deletion and marker commit, so this is not approved as a general repair path. |
| B | Strategy A plus backup migration true in a separate namespace | Pass | Safer structure for real legacy-value migration and no persisted plaintext, but unnecessary complexity for empty bootstrap. Source contains additional hard-coded backup key-preference behavior that needs review before general production migration use. |
| C | Package-supported explicit first-install marker API | Not available | Not testable without unsupported internal-marker mutation, which is prohibited. |

Both A and B passed fresh empty read/provision, normal reopen, deletion and
malformation with ciphertext, a simulated store exception, ciphertext
byte-for-byte preservation, and an existing valid value on the API-24 runtime.
The exact state "markers removed after an existing value" cannot be induced
through the public 10.3.1 API without directly editing private preferences. It
was therefore not mutated; the source path and the original real missing-marker
failure provide the evidence, and the state remains an explicit fault-injection
case for a later controlled harness.

The first-run wrapper must distinguish `EMPTY SECURE STORE + NO DATABASE` from
`MISSING/CORRUPT/UNAVAILABLE SECURE STORE + EXISTING DATABASE`. The corrected
`DatabaseKeyController.provisionNewDatabase(databaseExists: ...)` enforces that
boundary. Migration is never used to repair database-key loss.

## Host tamper and corruption evidence

Both candidates passed 16 host tests after the provisioning guard test was
added. The
controlled integrity test creates and checkpoints a synthetic database, copies
it, locates a leaf page using `dbstat`, flips a byte away from the page header,
and reads the affected rows. Corrupted data was rejected and was never returned
as valid plaintext. SQLCipher reported a page HMAC failure; wrong-key failure
occurred on page 1, while later-page corruption allowed startup and failed on
the targeted scan. SQLite3MC likewise rejected the targeted corruption.

This is narrow bit-flip evidence. It is not a claim of complete tamper
resistance and does not distinguish every storage-controller I/O failure.

## Synthetic interruption evidence

`tool/run_interruption_matrix.dart` runs each worker in a separate temporary
offline-resolved project so the parent and killed child do not share a locked
native asset. For both candidates, all four scenarios passed:

| Scenario | Restart result |
| --- | --- |
| Forced termination with active write transaction | Committed baseline retained; interrupted row absent. |
| Forced termination immediately before commit | Pre-commit row absent. |
| Forced termination around WAL checkpoint | All 5,000 committed rows recovered exactly. |
| Forced termination during synthetic migration | Schema version and table change rolled back together. |

Every restart passed `PRAGMA integrity_check`, rejected unkeyed access,
retained ciphertext, found zero configured plaintext markers, and performed no
silent replacement. This simulates process termination only. It is not an
exact power-loss simulation and cannot prove device/filesystem flush behavior.

## Android runtime gates

### API 24

The official `system-images;android-24;google_apis;x86_64` revision 27 image was
installed from `x86_64-24_r27.zip`. SDK metadata identified the reviewed
1,119,800,108-byte archive and SHA-1
`46d09c0723b77ddba00f2281099a2c44a88ac971`; `sdkmanager` completed without a
source/checksum discrepancy. The installed image occupies 3,315,622,140 bytes.
`platforms;android-24` was not installed because the build did not require it.

AVD `TraderPro_API24` boots Android 7.0/API 24 as x86_64 with Google APIs. The
retained Nexus 5 profile uses two virtual cores, 1536 MiB RAM, a 32 MiB cache,
no SD-card image, forced cold boot, and all local/downloadable snapshot
boot/save paths disabled. The emulator rejected 2048 MiB as one MiB above its
old-image limit, and both tested phone profiles rewrote a 2047 MiB override to
their 6 GiB logical `/data` default. That data image is sparse; the empty AVD
started near 550 MiB of ordinary image content. The automatically created
1.5 GiB first-boot RAM snapshot was removed after boot verification, and no
snapshot is retained. Both `adb` and Flutter detected the API-24 runtime.

Both candidates loaded their intended native engine and passed seven database
cases: wrong/unkeyed/malformed/missing-key refusal after secure-store
initialization, encrypted create/reopen, Drift migration and background
isolate access, transaction rollback, WAL checkpoint/reopen, explicit raw-key
configuration and textual-key incompatibility, controlled later-page tamper
rejection, and scans of main/WAL/SHM/rollback-journal artifacts with zero known
plaintext matches. Configuration readback was:

- SQLCipher 4.17.0 community, SQLite 3.53.3, 4096-byte pages,
  PBKDF2-HMAC-SHA512 compatibility values at 256000 iterations, HMAC-SHA512,
  zero-byte plaintext header, memory security enabled, and OpenSSL 3.6.2.
- SQLite3MC with SQLite 3.53.3, ChaCha20-Poly1305, `legacy = 0`, 4096-byte
  pages, 64007 KDF iterations, HMAC check enabled, zero-byte plaintext header,
  legacy WAL disabled, and no exposed memory-security pragma.

The corrected shared secure-store configuration was then tested on a clean app
install. Strategies A and B each started with no values, no markers, and no
database; each initialized once, created one encrypted database, and reopened
the expected marker. Both then failed closed when the secure-store value was
deleted, malformed, or synthetically unavailable while ciphertext existed.
Provisioning refused to generate a replacement key, and the database bytes
remained identical. An existing correctly configured value reopened normally.

The normal application harness also passed process force-stop/reopen without
reinstallation for A and B, then passed a replacement `adb install -r -t` and
reopen while retaining the same application data. Each database was 16,384
bytes and recovered exactly one marker. The Flutter integration-test runner
itself clears application data between invocations, so it was not used as the
upgrade/restart oracle; the ordinary application harness supplied that
evidence. These are emulator lifecycle tests, not exact power-loss evidence.

### API 35

The authorized Android 15/API-35 arm64 phone reconnected after the API-24 runs.
Both candidates then passed the same seven engine-specific database,
configuration, raw-key, tamper, migration, rollback, WAL, isolate, artifact,
and repeated-workload cases. The physical filesystem did not leave a rollback
journal available at the evidence-copy point for either candidate, but the
transaction rollback assertion passed and the captured main/WAL/SHM files had
zero known plaintext matches.

Both candidates originally reproduced the shared clean-install secure-store
failure before the database engine was involved. After that original failure,
separate processes created and force-stop/reopened one encrypted marker.
SQLCipher's forced reopen report was ready in 1,666 ms; SQLite3MC's was ready
in 1,769 ms, each with a distinct process ID and exactly one recovered marker.
This remains valid engine/restart evidence, not power-loss proof.

Corrected Strategy A then passed on true clean API-35 installations with both
engines. SQLCipher passed the secure-store integration matrix, followed by an
ordinary-app lifecycle run. SQLite3MC passed the same first-launch matrix in
the ordinary-app harness. For each engine:

- database-absent/key-absent provisioning succeeded exactly once;
- missing and malformed values with ciphertext failed closed;
- replacement provisioning with ciphertext was rejected;
- a simulated unavailable store propagated its exception;
- the ciphertext bytes remained identical after every failure state;
- restoring the exact original value reopened the one encrypted marker;
- `resetOnError` remained false and no plaintext fallback was attempted;
- force-stop removed the original process, and a different process reopened
  the same 16,384-byte database with marker count one; and
- same-data app replacement stopped the process and reopened the unchanged
  database and marker from a new process.

The private algorithm-marker-after-existing-value state remains source-reviewed
only because 10.3.1 exposes no supported API for removing that metadata. No
internal plugin preferences were edited. These results make pinned
`flutter_secure_storage` 10.3.1 Strategy A acceptable for the B1 evidence
boundary; they do not select a production engine or define the B2 alias.

## Performance and APK evidence

The following debug-mode results are from the authorized API-35 arm64 phone.
Each cold and warm series contains ten samples; p95 uses nearest rank and
therefore is the maximum for a ten-sample series. SQLCipher ran first, then
SQLite3MC. Battery moved from 85% to 82%, battery temperature from 36.6 C to
37.5 C, Android thermal status remained 0, and free `/data` space stayed above
230 GB.

| Physical API-35 workload | SQLCipher | SQLite3MC |
| --- | ---: | ---: |
| Cold open, min / median / max / p95 | 40 / 48.5 / 80 / 80 ms | 29 / 46.5 / 53 / 53 ms |
| Warm reopen, min / median / max / p95 | 8 / 9.5 / 14 / 14 ms | 7 / 9 / 10 / 10 ms |
| 1,000-row transaction | 999 ms | 867 ms |
| 10,000-row transaction | 6,086 ms | 5,405 ms |
| 50 sequential queued-operation-style writes | 60 ms | 49 ms |
| Full WAL checkpoint | 0 ms | 1 ms |
| Background-isolate reopen | 12 ms | 6 ms |
| Force-stop/new-process reopen-to-report | 1,666 ms | 1,769 ms |

The equivalent API-24 emulator evidence is retained for compatibility context,
not as a substitute for physical-device performance:

| Workload | SQLCipher | SQLite3MC |
| --- | ---: | ---: |
| Cold open, min / median / max / p95 | 243 / 343 / 383 / 383 ms | 89 / 138.5 / 214 / 214 ms |
| Warm reopen, min / median / max / p95 | 161 / 175 / 202 / 202 ms | 31 / 47.5 / 104 / 104 ms |
| 1,000-row transaction | 7,116 ms | 6,570 ms |
| 10,000-row transaction | 53,519 ms | 47,089 ms |
| 50 sequential queued-operation-style writes | 782 ms | 218 ms |
| Full WAL checkpoint | 13 ms | 4 ms |
| Background-isolate reopen | 171 ms | 19 ms |

The API-35 phone is not a designated lower-end representative device. The SCA
review is now complete, but timing alone cannot select an engine.

Universal unsigned release packaging (three ABIs) is evidence only:

| Candidate | Unsigned release APK | Release SHA-256 | Native engine inventory |
| --- | ---: | --- | --- |
| SQLCipher | 62,945,664 bytes | `6596E9523289DD879AE7AD417CF97EA6688793081E3D997BCDF7A02AB1F0144C` | only `libsqlcipher.so` for arm64-v8a, armeabi-v7a, x86_64 |
| SQLite3MC | 54,050,808 bytes | `159E65D08C43E4BCDC744861A2054AA8563A61B3D6EC5E48C303FB7114F92CA2` | only `libsqlite3mc.so` for arm64-v8a, armeabi-v7a, x86_64 |

Flutter 3.44.4 generated a release registrant for the dev-only
`integration_test` plugin but excluded that plugin from the release classpath.
For each release evidence build only, the dev dependency was temporarily
removed, dependency resolution and the build were run, then the manifest and
lock were restored byte-for-byte. `apksigner` reported both artifacts unsigned
(`Missing META-INF/MANIFEST.MF`), so release installation/start was unsupported
without adding signing configuration and was not attempted. No release package
is retained or proposed for distribution.

## Candidate comparison

| Criterion | SQLCipher Community | SQLite3MultipleCiphers |
| --- | --- | --- |
| Encrypted contract | AES-256 SQLCipher v4, HMAC-SHA512 | Explicit ChaCha20-Poly1305, legacy 0 |
| Raw-key correctness | Passed | Passed |
| Host/Drift/isolate/migration/WAL | Passed | Passed |
| Controlled corruption | HMAC rejection | Authentication/read rejection |
| Process-termination matrix | Passed | Passed |
| Memory-security pragma | Enabled and verified | Unsupported by artifact |
| Native crypto burden | Statically linked OpenSSL 3.6.2 | No additional shared crypto library |
| Universal release size | 62,945,664 bytes | 54,050,808 bytes |
| Configuration-drift surface | Narrower | Broader multi-cipher surface; guarded by explicit checks |
| API-24 / frozen API-35 | Database and corrected Strategy A secure-store/lifecycle pass on both | Database and corrected Strategy A secure-store/lifecycle pass on both |
| SCA | Frozen scan complete; OpenSSL manual review recorded | Frozen scan and manual review complete |
| Current standing | Validated alternative/fallback; not selected | Selected for TraderPro V1 by ADR-0011 |

SQLite3MultipleCiphers passed all required API-24 and API-35 runtime gates,
true-raw-key and controlled-corruption tests, has no applicable unresolved
Critical/High advisory in the reviewed evidence, has no OpenSSL native
dependency, has the smaller recorded supply-chain/native and APK footprints,
and delivered acceptable measured performance. Size and timing support the
comparison but are not the selection criteria. The deciding V1 tradeoff is the
smaller native supply-chain surface while meeting the required security and
runtime contract.

SQLCipher retains important advantages: a mature narrow encrypted-SQLite
contract, per-page HMAC, and stronger documented and tested memory
sanitization/locking controls. It was not selected because the tested artifact
statically includes OpenSSL 3.6.2 while a newer upstream security-patched
OpenSSL exists. The added provenance, advisory, patching, and notice burden is
not outweighed by a TraderPro V1 requirement in the functional/runtime
evidence. This conclusion does not classify SQLCipher as insecure.

## Selection gate status

| Gate | Result | Required milestone |
| --- | --- | --- |
| Host regression | Pass for both | B1 complete |
| Explicit configuration/readback | Pass, with SQLite3MC memory control explicitly unsupported | B1 complete; accepted risk in ADR-0011 |
| Key syntax and failure behavior | Pass for both | B1 complete |
| Tamper/corruption | Pass for scoped test | B1 complete |
| Migration/WAL/isolate/restart | Host/process pass | B1 complete |
| Physical API-35 on frozen configuration | Pass for both engines: corrected Strategy A clean provision, fail-closed states, encrypted reopen, force-stop/new-process, and same-data replacement | B1 complete |
| API-24 runtime | Pass: database submatrix plus corrected secure-store A/B clean-install, fail-closed, force-stop, and upgrade simulations | B1 complete |
| Repeated representative performance | API-24 emulator and API-35 phone samples recorded; lower-end representative device remains before pilot | B1 complete; lower-end gate before pilot |
| Native provenance | Complete for three packaged ABIs | B1 complete |
| License/notice candidate review | Technically complete; legal approval outstanding | B1 complete; approval before foundation merge/release as governed |
| Locked comparative SBOM | Generated and JSON-validated | B1 complete; production SBOM in B2 |
| SCA/advisory scan | Pass for frozen evidence: valid Grype v6.1.9 DB, zero matches, ten unmatched native/generic components manually reviewed; no applicable unresolved Critical/High | B1 complete |
| Exact power loss | Not available | Before pilot on representative device/lab method |
| App upgrade / uninstall / Keystore invalidation / screen-lock change | Same-data debug app replacement passed for both engines; uninstall/key invalidation/screen-lock cases remain later lifecycle gates | Before secure-foundation merge or pilot as sequenced in TPRUN-006 |
| OEM transfer / backup extraction | Not run | Before pilot |
| Low storage / low memory | Not run | Before pilot |
| Lower-end physical performance | Not run | Before pilot |
| Rekey / engine switch / cross-device restore | Explicitly deferred from V1 | Separate reviewed design |

The 2026-08-08 static closeout reran the isolated host suite under both frozen
hooks: SQLite3MultipleCiphers passed 16/16, SQLCipher passed 16/16, and the
final retained SQLite3MultipleCiphers state passed 16/16 again. Under the
selected hook, `flutter analyze --no-pub` reported no issues. No
performance or device matrix was rerun, no production Flutter file changed,
and the retained spike hook is `source: sqlite3mc`.

## SCA result

The official Anchore Grype v0.116.1 Windows amd64 portable executable is
installed under `%LOCALAPPDATA%\TraderProTools\grype\0.116.1` without a PATH
change. The official ZIP was 30,634,272 bytes. Its SHA-256
`a8ad864f2f06c9afcc90d417f46f5587adeaba4e1e46cc4e32a6a076dae2fda7`
matched Anchore's checksum manifest, and OpenSSL verified the Sigstore release
signature whose workflow identity is Anchore's Grype release workflow on
`refs/heads/main`. The executable reports Grype 0.116.1, commit
`30394f177175da63ae36b35cdd809c248eb4de7f`, Windows amd64, Syft 1.50.0,
and supported database schema 6.

Automatic database and application updates were disabled. Exactly the approved
v6.1.9 archive was downloaded from `https://grype.anchore.io/databases/v6/`.
Its 139,968,909-byte size and SHA-256
`c58fe02aaaaae4c10bcce9a77c3d8affd6cfd66ea4ab0a1510039594da17c914`
matched before import. The active cache consists of a 1,987,256,320-byte
database plus 97 bytes of import metadata. Grype reports schema v6.1.9, built
2026-08-06T07:04:15Z, source `manual import`, and `valid: true`.

The frozen CycloneDX scan exited 0 with zero matches at every severity. Of 166
components, 156 Pub/Maven components had supported ecosystem coordinates and
ten native/generic components lacked reliable matcher metadata. Those ten and
the security-sensitive transitive packages were manually reviewed. OpenSSL
3.6.2 has one High, five Moderate, and twelve Low official advisories affecting
the shipped library version. Exact SQLCipher 4.17.0 provider-source inspection
classified all affected APIs as not reachable; none is an applicable
unresolved Critical or High in the candidate call surface. OpenSSL 3.6.4 is
still the minimum version resolving all official issues known on the evidence
date and is a tracked native-update consideration. Full reviewed evidence is in
`docs/supply-chain/TASK-7C2B1-Mobile-SCA-Review.md`.

## Patch and update policy proposal

The following roles are proposals and require user/organization approval:

- Mobile Platform Maintainer owns Flutter, Drift, `sqlite3`,
  `flutter_secure_storage`, AGP, Kotlin, and NDK intake.
- Security Owner owns cipher, SQLite, OpenSSL, Tink, and advisory triage.
- Legal Owner approves notices and distribution obligations.
- Release Owner verifies the locked graph, ABI inventory, and release evidence.

Monitor official security advisories monthly and review the complete pinned
stack quarterly. Proposed response targets are: critical issue triage within
72 hours and an approved mitigation/release plan within 7 days; high issue
triage within 7 days and an approved update plan within 30 days. These are
targets requiring owner approval, not repository-enforced promises.

Every update must review official release notes and source, pin exact versions,
review lockfile and Gradle changes, regenerate the SBOM/notices/hashes, run SCA,
run both host suites, API-24 and representative-device matrices, corruption and
interruption tests, release packaging, plaintext/log scans, and backup checks.
Automatic encryption-engine upgrades are prohibited. Engine, cipher, raw-key
syntax, page format, or provider changes after production data require a
separate approved and fault-injected migration.

## Remaining actions and recommendation

1. Begin Task 7C2B2 only through a separate reviewed change. Introduce one
   production engine, the exact ADR-0011 startup assertions, fail-closed
   opener, secure-store namespace/alias, Android backup exclusions, release
   non-debuggable posture, key-memory handling, and production dependency
   graph. Do not build dual-engine compatibility.
2. Run the repeated performance matrix on a designated lower-end physical
   device before pilot. Release installation remains unsupported until an
   evidence-only signing approach is separately approved.
3. Obtain Legal Owner approval and generate the final production notice bundle
   from the actual B2/release dependency graph.
4. Complete uninstall/Keystore-invalidation/screen-lock, OEM backup/transfer,
   low-storage/low-memory, and representative-device pilot gates at their
   sequenced milestones.
5. If SQLCipher is reconsidered, require a new ADR and evidence pass using the
   current native artifact and OpenSSL security patch, regenerated SBOM/SCA,
   API-24/API-35 regression, raw-key/configuration tests, and a migration plan
   if SQLite3MultipleCiphers production data exists.

The B1 CycloneDX document remains comparative evidence for both candidates. It
must not be rewritten to imply only SQLite3MultipleCiphers was tested. A B2
production SBOM must be generated from the actual production dependency graph
after the selected package is introduced.

Final recommendation: **Task 7C2B1 selected/complete subject to commit review**.
Task 7C2B2 is next and not started.
