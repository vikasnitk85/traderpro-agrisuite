# Task 7C2B1 mobile native provenance

- Status: Reviewed comparative B1 evidence; SQLite3MultipleCiphers selected by ADR-0011
- Evidence date: 2026-08-08
- Scope: universal debug and unsigned release APKs built from the isolated
  `spikes/task_7c2a_encrypted_storage` project

No APK, AAB, extracted ELF, local SDK path, device serial, or generated build
directory is retained by this report.

## Resolution chain

1. The spike locks Dart `sqlite3` 3.5.0 in `pubspec.lock`.
2. The spike `pubspec.yaml` hook alternates `source: sqlcipher` and
   `source: sqlite3mc`; after the final two-engine regression the repository
   retains `sqlite3mc` to reflect ADR-0011. Both inventories below remain B1
   comparative evidence.
3. `sqlite3` 3.5.0's build hook retrieves release assets compiled in the
   package's upstream automation and verifies its hard-coded SHA-256 allowlist.
4. Flutter/Gradle packages the selected asset once per supported Android ABI.
5. Android packaging strips the full symbol table; `.dynsym` remains. Therefore
   upstream source-asset hashes and packaged-ELF hashes intentionally differ.

Upstream package source:
[sqlite3.dart](https://github.com/simolus3/sqlite3.dart),
[SQLCipher](https://github.com/sqlcipher/sqlcipher), and
[SQLite3MultipleCiphers](https://github.com/utelle/SQLite3MultipleCiphers).

## Upstream hook allowlist

These hashes are copied from the resolved `sqlite3` 3.5.0
`lib/src/hook/asset_hashes.dart`. They identify the downloaded assets before
Android's stripping/packaging transform.

| Candidate | ABI | Upstream asset | SHA-256 |
| --- | --- | --- | --- |
| SQLCipher | armeabi-v7a | `libsqlcipher.arm.android.so` | `92db9f415dc91ad19620089f5d57ab8feb3f6e26def18aa90de699d9691a6b66` |
| SQLCipher | arm64-v8a | `libsqlcipher.arm64.android.so` | `aab3ca747d6fd64ab20d6cea8de09009e2117d2943b5f0d577d8a371fa9f264b` |
| SQLCipher | x86_64 | `libsqlcipher.x64.android.so` | `3b604415a5c4c358139c0e58dc60ca62549351921f5d01225ec77c74cc549710` |
| SQLite3MC | armeabi-v7a | `libsqlite3mc.arm.android.so` | `7cc7f11847f7df1541995ea2a6f6014ef2301b9fda89311d4eca080bd4b95d72` |
| SQLite3MC | arm64-v8a | `libsqlite3mc.arm64.android.so` | `ede484177891da6a999be0fd74923e800b357d9ec3fff6bf5911cbccd4d55cab` |
| SQLite3MC | x86_64 | `libsqlite3mc.x64.android.so` | `2a252f4956e01a2b5bb8c66871f255696aec641a0e4408b19eae84391ca191af` |

The cached SQLite3MC asset hashes were independently recalculated and matched
all three allowlist values.

## SQLCipher packaged inventory

Runtime identity is SQLite 3.53.3, SQLCipher 4.17.0 Community, provider
`openssl`, provider version `OpenSSL 3.6.2 7 Apr 2026`. `PRAGMA compile_options`
includes `HAS_CODEC`, `EXTRA_INIT=sqlcipher_extra_init`,
`EXTRA_SHUTDOWN=sqlcipher_extra_shutdown`, and
`ENABLE_MEMORY_MANAGEMENT`.

| ABI | Packaged ELF | Bytes | SHA-256 | ELF class / machine |
| --- | --- | ---: | --- | --- |
| arm64-v8a | `libsqlcipher.so` | 5,080,752 | `434b5862737328d913964bd5eefac7db8748cd979ac863b3dfc09952e89f9cbd` | ELF64 / AArch64 |
| armeabi-v7a | `libsqlcipher.so` | 4,094,016 | `e82f4ccf192bdd8fa5c1f996618fe70362bdeb7a4cec4f3e98d10da51f49a27d` | ELF32 / ARM |
| x86_64 | `libsqlcipher.so` | 5,939,008 | `1bca6e2a4db6e39004f1933bc10b5b6e95ee371c70c1d16a920c787ee57afaed` | ELF64 / AMD x86-64 |

All three ELFs declare only Android system dependencies: `libm.so`,
`liblog.so`, `libdl.so`, and `libc.so`. No `libcrypto.so` or `libssl.so` is
packaged or dynamically required. OpenSSL version strings and exported `EVP_*`
symbols occur inside `libsqlcipher.so`, which is evidence that OpenSSL is
statically linked and its license/notice remains a distribution obligation.

The packaged files have `.dynsym` but no `.symtab` or debug sections. They are
stripped of the full symbol table while retaining required dynamic symbols.

## SQLite3MultipleCiphers packaged inventory

Runtime identity is SQLite 3.53.3 and SQLite3MultipleCiphers 2.3.6. The runtime
configuration explicitly selects ChaCha20-Poly1305. The build does not expose
the optional `memory_security` pragma.

| ABI | Packaged ELF | Bytes | SHA-256 | ELF class / machine |
| --- | --- | ---: | --- | --- |
| arm64-v8a | `libsqlite3mc.so` | 2,043,872 | `3500da95ebda57b2a749cfaf74258d9bb7b94702f14e435505d302942d869403` | ELF64 / AArch64 |
| armeabi-v7a | `libsqlite3mc.so` | 2,037,668 | `7e90f4263ae214925f945054a5ec514f5182ee3dedf2576c7bec1142f67040a6` | ELF32 / ARM |
| x86_64 | `libsqlite3mc.so` | 2,139,576 | `88be4bd4ceb1e65e84d04916489f33d8b08b566950a5e1311ac94a47ced90d28` | ELF64 / AMD x86-64 |

All three declare only `libm.so`, `libdl.so`, and `libc.so`. There is no
additional packaged or dynamically required crypto library. Dynamic symbols
include `sqlite3_key`, `sqlite3_key_v2`, and `sqlite3mc_version`. As with
SQLCipher, `.dynsym` remains but `.symtab` and debug sections are absent.

## APK packaging evidence

| Candidate | Variant | Bytes | SHA-256 |
| --- | --- | ---: | --- |
| SQLCipher | debug universal APK | 170,809,358 | `bd30f48d089b8ce2031463a2ef5c0a92b7d8170709a188c1dc55b88c635ef0e4` |
| SQLCipher | unsigned release universal APK | 58,767,812 | `c709a0573c78a7dff333afd889e243b8c33141a98f9b0d19c220f3dd83263ea7` |
| SQLite3MC | debug universal APK | 161,914,498 | `882ea2d529f0737a137807e0c896371cf337e0568fa362de37d5757ae8258a54` |
| SQLite3MC | unsigned release universal APK | 49,872,956 | `bc961004ac3d0bd555539cd81815a1c6dfb85f1b5d2c1ef265e98b3a7919c571` |

Debug and release contain exactly the selected engine library for
`arm64-v8a`, `armeabi-v7a`, and `x86_64`; neither candidate APK includes both
engines. Release also contains ABI-specific Flutter `libapp.so` and
`libflutter.so`. These universal APK sizes are not Play split or AAB download
sizes.

## Android secure-store dependency evidence

The resolved `flutter_secure_storage` 10.3.1 Android project has min SDK 23,
Java 17, and a direct runtime dependency on
`com.google.crypto.tink:tink-android:1.21.0`. The Gradle offline
`releaseRuntimeClasspath` report resolved Tink with:

- `androidx.annotation:annotation-jvm:1.8.2`;
- `com.google.code.findbugs:jsr305:3.0.2`;
- `com.google.code.gson:gson:2.13.2`;
- `com.google.errorprone:error_prone_annotations:2.41.0`.

The complete resolved Dart and Android component inventory is in
`TASK-7C2B1-Mobile-SBOM.cdx.json`.

## Reproduction commands

Run only from the isolated spike and never retain the outputs:

```powershell
flutter clean
flutter pub get
flutter build apk --debug --no-pub
flutter build apk --release --no-pub
Get-FileHash -Algorithm SHA256 build\app\outputs\flutter-apk\app-release.apk
tar -tf build\app\outputs\flutter-apk\app-release.apk
llvm-readelf -h -d <extracted-library>
llvm-readelf -S --dyn-syms <extracted-library>
llvm-strings <extracted-library>
```

With Flutter 3.44.4, the dev-only `integration_test` plugin was generated into
the release registrant while absent from the release classpath. The evidence
release builds therefore temporarily excluded that dev dependency, then
restored `pubspec.yaml` and `pubspec.lock` byte-for-byte. This workaround must
not be copied into production configuration without independent review.

## Limitations

- Package hash verification plus ELF inspection establishes a reproducible
  chain from the pinned wrapper to the packaged file; it is not independent
  source-reproducible-build verification.
- No code-signing provenance or release signature is claimed; builds are
  unsigned evidence artifacts.
- The inventory alone is not a vulnerability result. The frozen comparative
  SBOM was subsequently scanned with Anchore Grype 0.116.1/database v6.1.9;
  unmatched native components received manual review in
  `TASK-7C2B1-Mobile-SCA-Review.md`.
- Legal approval of the selected SQLite3MultipleCiphers/SQLite/`sqlite3` and
  shared secure-store/transitive notice set remains outstanding. SQLCipher and
  OpenSSL are retained candidate-evidence notice inputs only.
- The B1 SBOM intentionally remains comparative. Task 7C2B2 must generate a
  production SBOM from the actual production graph after the selected
  dependency is introduced.
