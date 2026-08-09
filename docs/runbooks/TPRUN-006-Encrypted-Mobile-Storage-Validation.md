# TPRUN-006: Encrypted mobile-storage validation

- Status: Task 7C2B1 selection complete; `TASK 7C2B2 = COMPLETE`; Task 7C2B3 not started
- Applies to: isolated B1 spike and the reviewed `apps/mobile` B2 foundation
- Prohibited target: production/customer data and any unreviewed business schema

## Purpose and safety

Use the B1 sections to reproduce candidate selection and the B2 section below
to validate the production secure foundation. Use synthetic data only. Never
print, persist in reports, or pass a database key on a command line. Do not
capture device serials in committed evidence. Do not keep APKs,
databases, WAL/SHM/journal files, secure-store dumps, extracted ELFs, Gradle
caches, scanner databases, or emulator snapshots in Git.

Process termination is not power loss. Label the evidence accurately.

## Task 7C2B2 production-foundation procedure

From `apps/mobile`, keep `source: sqlite3mc` and run:

```powershell
flutter pub get
dart run build_runner build
flutter analyze
flutter test
flutter test integration_test/commercial_secure_foundation_test.dart `
  -d <authorized-api-24-or-api-35-device>
flutter build apk --debug
flutter build apk --release --split-per-abi `
  --target-platform android-arm,android-arm64
dart run tool/generate_b2_production_sbom.dart
```

The integration test uses the real production secure-store composition for
first provisioning/reopen and real missing/malformed/wrong stored-key cases.
It uses separate synthetic app-support subtrees for unavailable/invalidated,
interruption, and unreadable-final-file cases. It must restore the real test
key after each controlled failure and must never delete or reset production
ciphertext. Force-stop and replacement evidence must launch `lib/main.dart`,
use `adb install -r` for a true data-preserving replacement, and confirm only
the safe ready state. Flutter's `flutter install` may uninstall first and is
not valid replacement-preservation evidence.

For release evidence, accept an unsigned APK only when repository signing is
absent. Verify the merged release manifest is non-debuggable, has no INTERNET,
sets backup/cleartext false, and references both exclusion XML files. Package
only ARM/ARM64 for production; classify x86_64 as emulator/test-only.

Generate B2 supply-chain files with distinct B2 names. Scan the new SBOM, not
the comparative B1 SBOM, and keep raw scanner JSON/cache outside the repository.
The Project/Legal Owner approved the frozen B2 exact-text bundle on 2026-08-08.
The future product surface is About / Legal / Third-Party Notices and includes
the SQLite public-domain dedication/provenance; implementing that UI is a later
distribution/release requirement, not B2 scope. Any graph change requires
notice/SBOM regeneration and Owner review again.

### Recorded B2 physical API-35 result

On 2026-08-08, model 2311DRK48I running Android 15/API 35 on arm64-v8a passed
the three-test production B2 integration matrix. Frozen SQLite3MC settings,
metadata/key/identity continuity, real and simulated fail-closed states,
unkeyed/wrong-key refusal, zero-length preservation, and both approved
interruption resumes passed. A force-stop cold reopen and true `adb install -r`
replacement retained identical database and marker hashes and returned only
the safe ready state. No POC database or route appeared.

An ARM64 release copy signed only with an external two-day evidence key also
cold-launched successfully. Its installed flags excluded `DEBUGGABLE` and
`ALLOW_BACKUP`, its requested permissions excluded INTERNET, and its merged
manifest retained cleartext false and both backup resource references. The
temporary signing key/APK, installed package/data, UI dumps, and rebuilt APKs
were deleted after review. Rebuilt ARM native-library hashes matched the frozen
B2 provenance exactly, so the production SBOM/SCA/native inventory did not
require regeneration.

## Preconditions

1. Confirm a clean reviewed branch and the expected repository baseline.
2. Confirm `apps/mobile/pubspec.yaml` and `apps/mobile/pubspec.lock` have no
   diff.
3. Confirm the spike pins Drift 2.34.3, `sqlite3` 3.5.0, and
   `flutter_secure_storage` 10.3.1.
4. Record Flutter/Dart, Java, AGP, Kotlin, NDK, Android platform-tools, device
   OS/API, ABI, model class, free storage, battery, and thermal status.
5. Disable screen recording, crash uploads, and verbose logs that could collect
   synthetic secrets or machine/device identifiers.
6. Obtain user approval before downloading an SDK image or installing any SCA
   tool.

## Candidate switch

Change only the isolated spike hook:

```yaml
hooks:
  user_defines:
    sqlite3:
      source: sqlcipher # or sqlite3mc
```

After every switch run `flutter clean` and `flutter pub get`. Do not change
version pins. ADR-0011 is accepted, so the retained post-run spike hook should
be `source: sqlite3mc`. SQLCipher remains preserved comparative evidence, not
the selected production engine.

## Host validation

For each candidate:

```powershell
flutter clean
flutter pub get
flutter analyze --no-pub
flutter test --no-pub
dart run tool/print_storage_configuration.dart
dart run tool/run_interruption_matrix.dart
dart run tool/generate_sbom.dart
```

Required results:

- analyzer has no issues;
- all tests pass, including raw-versus-text key syntax, wrong/unkeyed/malformed
  failures, native error redaction, targeted bit-flip detection, migration,
  rollback, WAL, and background-isolate reopen;
- all four interruption scenarios retain the committed baseline, roll back
  incomplete work, pass consistency checks, reject unkeyed reads, and show no
  configured plaintext markers;
- configuration output contains no key material and every supported frozen
  value reads back exactly;
- SQLite3MC reports unsupported memory security explicitly if the resolved
  artifact still omits that pragma.

## API-24 emulator acquisition gate

First inspect without downloading:

```powershell
flutter emulators
sdkmanager --list_installed
adb devices -l
flutter devices
```

If unavailable, obtain approval for the exact packages before installation:

```text
system-images;android-24;google_apis;x86_64
platforms;android-24
```

The approver must see the current SDK-manager download estimate, installed
size, SDK write locations, and expected AVD disk reservation. Reserve several
GB and meet the Android emulator's host free-space requirement. Create an AVD
with an unambiguous evidence-only name, cold boot it, verify `sdk` is `24`, and
record emulator versus physical-device status. Do not silently substitute a
third-party image.

## Android functional matrix

Run for each candidate on API 24 and the authorized physical API-35 device:

```powershell
flutter test integration_test/secure_store_test.dart -d <authorized-device>
flutter test integration_test/device_storage_test.dart -d <authorized-device>
```

Verify:

- application startup;
- secure-store create/read/delete using the frozen Android options;
- encrypted create, correct-key reopen, wrong/unkeyed rejection;
- missing and malformed secure-store value fails closed with ciphertext kept;
- Drift queries, schema migration, rollback, background-isolate opening;
- WAL checkpoint/reopen and artifact plaintext scan;
- no plaintext fallback or silent database replacement.

Build the unsigned release variant and install it where the test environment
supports release-mode integration. Do not configure production signing.

## Force-stop and new-process evidence

Use `lib/device_restart_harness.dart` with a clean evidence-only app install:

1. Launch it once and verify phase `created`, success `true`, marker count 1.
2. Record the process ID only in temporary evidence.
3. Force-stop the app with Android tooling; do not clear data.
4. Launch again and verify phase `reopened`, a different process ID, success
   `true`, and the same one marker.
5. Remove the temporary report after recording the reviewed result.

Force-stop proves process restart, not power loss.

## Lifecycle and recovery matrix

Run each case against synthetic ciphertext and record expected versus actual
state without extracting key values:

| Case | Required result | Gate |
| --- | --- | --- |
| App upgrade over retained data | Correct-key reopen; binding unchanged | Before B2 merge |
| Uninstall/reinstall | Old app data/key not silently reused; fresh provisioning explicit | Before B2 merge |
| Secure-store value deletion | Fail closed; ciphertext retained | Before selection/B2 |
| Corrupt secure-store value | Fail closed; ciphertext retained | Before selection/B2 |
| Simulated Keystore invalidation | Safe unavailable/invalidated state; no replacement | Before B2 merge |
| Screen-lock change | Either successful reopen or classified Keystore failure; no deletion | Before pilot |
| Android backup/restore | Secrets excluded; cross-install/device restore unsupported and fails closed | Before pilot |
| OEM device transfer | No silent usable database/key transfer | Before pilot |
| App data clear | Both local database and app secure value removed by OS; no recovery claim | Before pilot |
| Low storage during write/migration/checkpoint | Atomic failure, existing ciphertext preserved | Before pilot |
| Low memory/process pressure | Restart is consistent; no plaintext fallback | Before pilot |
| Database corruption | Safe corruption state, no auto-delete/recreate | Before B2 merge |

Where Android or OEM behavior cannot be automated, record a witnessed manual
procedure and the exact device/build. Destructive discard of unrecoverable
ciphertext is out of this runbook and requires a separate authorized recovery
workflow.

## Repeated performance matrix

Warm each candidate before measurement and use identical data/workloads. Run
on the authorized API-35 device, then on a representative lower-end physical
device before pilot. Capture at least:

- 10 cold opens;
- 10 warm reopens;
- 1,000-row and 10,000-row transactions;
- 50 queued-operation-style writes;
- full WAL checkpoint;
- background-isolate reopen;
- restart-to-usable duration.

Record every sample and calculate minimum, median, maximum, and p95 where
meaningful. Record database bytes, debug/release mode, battery, free storage,
and observed thermal status. Alternate candidate order or allow cool-down to
reduce thermal/order bias. Do not choose an engine solely on time or APK size.

## Interruption and power-loss evidence

The host runner covers process kills during an active write, immediately before
commit, around WAL checkpoint, and during a transactional migration. Repeat
equivalent Android process-kill phases with external `adb shell am force-stop`
coordination before selection if an instrumentation fixture is approved.

A true power-loss gate needs a lab method capable of cutting/emulating storage
power or an approved filesystem fault-injection equivalent. Record cache/flush
limitations. Never relabel force-stop, `kill`, emulator close, or host-process
termination as power loss.

## Debug and unsigned release packaging

For each candidate:

```powershell
flutter build apk --debug --no-pub
flutter build apk --release --no-pub
```

If the installed Flutter release registrant again includes the dev-only
`integration_test` plugin without its release classpath, use a reviewed
temporary evidence-only manifest change, restore `pubspec.yaml` and
`pubspec.lock` byte-for-byte immediately afterward, and document the
workaround. Never add test instrumentation to the production release graph.

For every APK:

1. calculate APK SHA-256 and bytes;
2. list native entries and prove only the selected engine is present;
3. extract each ABI library to a temporary directory;
4. calculate each packaged ELF SHA-256 and size;
5. use NDK `llvm-readelf` to record class, machine, `NEEDED` libraries,
   `.dynsym`, `.symtab`, and debug-section status;
6. use `llvm-strings` plus runtime pragmas to record SQLite, engine, and crypto
   provider versions;
7. compare downloaded-asset hashes with the resolved `sqlite3` hook allowlist;
8. delete APKs and extracted libraries after reviewed metadata is saved.

## SBOM, notices, and SCA

Regenerate the CycloneDX document from the locked spike and validate its JSON.
Compare all Dart lock entries, Gradle `releaseRuntimeClasspath`, native
libraries, ABIs, SQLite, engine, OpenSSL where applicable, and Tink.

Before installing a scanner, present to the user:

- official tool name and exact version;
- official download/source URL and signature/checksum mechanism;
- download and advisory-database size;
- every write location outside the repository;
- proposed command and target scope.

After approval, record tool version, command, advisory database timestamp,
findings, severity, affected component/path, false-positive disposition, and
unscannable native components. Manual review is not SCA. Do not record “no
vulnerabilities” unless the approved scan completed successfully and its scope
supports that statement.

The Project/Legal Owner approved the third-party-notice document on 2026-08-08
against the exact frozen release SBOM and ARM binary graph. This records Owner
approval, not external legal counsel advice.

## Completion checklist

- [x] API-24 official image/emulator pass for both candidates
- [x] physical API-35 frozen-configuration pass for both candidates
- [x] repeated performance matrix and lower-end device plan
- [x] Android lifecycle/recovery matrix required by B1
- [x] debug and unsigned release native provenance
- [x] locked, JSON-valid comparative SBOM
- [x] approved SCA run and reviewed findings
- [x] Project/Legal Owner notice approval for frozen B2 graph (2026-08-08; not claimed by B1)
- [x] no plaintext/key/log leak in retained evidence
- [x] production dependency graph unchanged
- [x] no binary, database, cache, serial, key, local path, or raw scanner output retained

ADR-0011 accepted SQLite3MultipleCiphers after all B1 selection gates passed.
Task 7C2B1 is complete subject to commit review; its historical selection text
above is retained. For current B2 status use
`docs/assessments/TASK-7C2B2-Secure-Commercial-Local-Foundation.md`: API-24,
physical API-35, host, unsigned ARM release, backup declarations, SBOM, and SCA
pass; Project/Legal Owner notice approval for the frozen graph is recorded,
ADR-0012 is accepted, and the final static closeout review passes. Task 7C2B3
has not started.
