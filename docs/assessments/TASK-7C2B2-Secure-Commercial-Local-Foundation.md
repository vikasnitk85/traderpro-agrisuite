# Task 7C2B2 secure Commercial local foundation assessment

- Status: `TASK 7C2B2 = COMPLETE`; ADR-0012 accepted; Task 7C2B3 not started
- Evidence date: 2026-08-08
- Branch: `task/7c2b2-secure-commercial-foundation`
- Base: `57be8bfdc3bec17c70fa33cd0bfb670b5fbdd0d0`
- Scope: secure Commercial local foundation only

## Outcome

The production mobile entrypoint now initializes a separate encrypted
Commercial schema-1 database through SQLite3MultipleCiphers and an Android
OS-backed key. It fails closed on storage/key mismatches, resumes only approved
interrupted-provisioning states, exposes only a redacted foundation startup
state, and keeps the POC entrypoint and all POC/Receiving storage unchanged.

No authentication, account/profile binding, credentials, Receiving tables,
master cache, outbox, cursor, business UI, backend code, or database migration
was added.

## Closeout status

| Gate | Status |
| --- | --- |
| Technical implementation | COMPLETE |
| API-24 | PASSED |
| Physical Android 15/API-35 | PASSED |
| Production SBOM/SCA/native provenance | PASSED |
| Exact notice coverage | PASSED |
| Owner notice | APPROVED - 2026-08-08 |
| ADR-0012 | ACCEPTED |
| Task 7C2B3 | NOT STARTED |

## Owner notice approval record

On 2026-08-08, the Project/Legal Owner approved the complete exact-text notice
bundle for the frozen B2 production dependency and ARM binary graph. The
approved future distribution surface is About / Legal / Third-Party Notices,
and the SQLite public-domain dedication/provenance must be included. B2 records
that later distribution/release requirement but does not implement the UI.

- Pre-approval exact-bundle SHA-256:
  `1583ED65450E0B6722CA9A1990FDA63D2890748FBB31EF0A1183F2885DCDC33D`;
- approved exact-bundle SHA-256:
  `8C2DCA5A6479E72A01DA0E9737AF75AC3689AFAD92DF928426CC30EF5D2DB77E`;
- approval state: `LEGAL_NOTICE_APPROVAL = APPROVED`.

This is Project/Legal Owner approval, not external legal counsel advice. Any
dependency, version, ABI policy, storage engine, Flutter engine revision, or
production binary-graph change requires notice/SBOM regeneration and Owner
review again.

## Implemented contract

| Area | Result |
| --- | --- |
| Dependency | Drift/sqlite pins retained; `flutter_secure_storage` 10.3.1 and Android SQLite3MC hook selected |
| Key | Secure random 32-byte mutable owned key; `v1:` base64url serialization; temporary lowercase raw-key hex only in opener |
| Secure store | Dedicated namespace/entry, RSA-OAEP-SHA256 + AES-GCM, create/readback, no reset/backup/destructive recovery |
| Files | Exact final, initializing, and provisioning-marker names under app support `commercial/` |
| Provisioning | Preparing marker, key write/readback, encrypted temp, verify/checkpoint/reopen, atomic promote, final verify, complete marker |
| Database | Drift schema 1 with only immutable `commercial_storage_metadata` singleton |
| Runtime | One `CommercialSecureRuntime`, shared open future, one initializer/database, idempotent close, lifecycle detach close |
| UI | Minimal safe ready/unavailable/closed foundation state only |
| Android | Backup/transfer exclusions, cleartext false in main/release, no release INTERNET, no debug release signing |
| Isolation | Separate production and POC entrypoints; architecture tests prohibit cross-imports and widget storage access |

## Startup and interruption evidence

The automated state-machine test covers all 30 combinations of final database
state, key-store state, and explicit/normal context. Additional tests prove:

- fresh explicit provisioning exactly once;
- concurrent callers share the same initialization;
- key-written/database-create-failed resumes with the stored key;
- a generation-matching valid temporary database resumes and promotes;
- promotion-before-marker-completion reopens and completes the marker;
- final ciphertext plus missing/malformed/unavailable/invalidated key blocks
  without changing bytes;
- zero-length final artifacts are preserved and block before SQLite can mutate
  them;
- wrong/unkeyed access, controlled corruption, conflicting sidecars, and
  metadata mutation fail closed; and
- no configured plaintext markers appear in database or sidecar bytes.

## Validation matrix

| Gate | Result | Evidence/limitation |
| --- | --- | --- |
| Flutter analyze | Pass | No issues |
| Full Flutter unit/widget/architecture suite | Pass | 182 tests |
| API-24 official emulator | Pass | Android 7/API 24 x86_64; two integration tests cover real secure store, encrypted create/reopen, interruption resume, key-state failures, unkeyed refusal, and plaintext scan |
| Force-stop | Pass | API-24 and physical API-35 cold launches reopened and displayed only the safe ready state; this is process death, not power loss |
| Same-package replacement | Pass | API-24 and physical API-35 `adb install -r` preserved state; each replacement cold-launched to the safe ready state |
| API-35 physical Android 15 | Pass | Model 2311DRK48I, API 35, arm64-v8a; three integration tests plus force-stop, replacement, artifact/log scans, and temporary externally signed release-posture validation |
| ARM release build | Pass | Unsigned split APKs for armeabi-v7a and arm64-v8a |
| Release posture | Pass | Merged artifact and temporary physical release install are non-debuggable, have no INTERNET/ALLOW_BACKUP, set cleartext false, and reference both backup-rule resources; temporary signing material was external and deleted |
| Native selection | Pass | Each ARM APK contains only app/Flutter/JNI plus one `libsqlite3mc.so`; no ordinary SQLite, SQLCipher, or OpenSSL shared library |
| Backup policy | Pass for declared/testable controls | Source and merged manifest verified; installed debug package lacks `ALLOW_BACKUP`; OEM restore/transfer remains pilot evidence |
| Production SBOM | Pass | New CycloneDX 1.6 graph with 112 components from current pub, Maven, and ARM native artifacts |
| SCA | Pass | Frozen Grype 0.116.1 / DB v6.1.9 accepted the new SBOM and returned zero matches; native components manually reviewed |
| Notices/legal | Pass | Exact-text coverage validated; Project/Legal Owner approval recorded 2026-08-08 for the frozen graph; final surface is About / Legal / Third-Party Notices with SQLite dedication included |

## Physical API-35 closeout evidence

The authorized Android 15/API-35 ARM64 device passed three production-stack
integration tests. They proved real OS secure-store provisioning and key
continuity, immutable installation/database identity continuity, the metadata
singleton and schema version, every frozen SQLite3MC/runtime pragma, unkeyed
refusal, real missing/malformed/wrong stored-key failures, simulated
unavailable/invalidated failures, zero-length final-file preservation, and both
approved interruption resumes. Every failure kept ciphertext bytes unchanged,
did not create a replacement key, and exposed only the typed safe failure.

A separate `lib/main.dart` evidence install contained no POC route and created
no `traderpro-local.sqlite`. Its Commercial database and marker hashes were
unchanged across a complete force-stop/cold launch and a true same-signature
`adb install -r` replacement:

- database SHA-256:
  `cbb26ee81b58c93dfde0526d8334d1d85582ff0182785c76a9a76a6bc9bea8ad`;
- marker SHA-256:
  `0e489a755e06a0d67a835739ed3ecdaf12f2300e39bb188e2b68d518ea6e791c`.

The unsigned ARM64 release was copied outside the repository, signed with a
two-day evidence-only key, installed, and cold-launched successfully. Device
package flags had neither `DEBUGGABLE` nor `ALLOW_BACKUP`; requested permissions
contained no INTERNET. `run-as` refusal independently confirmed the installed
package was non-debuggable. The signed APK and key, installed evidence package,
device UI dumps, and rebuilt APK outputs were deleted after review.

## Release and native evidence

The selected release set excludes x86_64 and contains only `armeabi-v7a` and
`arm64-v8a`. Both packaged SQLite3MC libraries report engine 2.3.6 and SQLite
3.53.3, are stripped, and depend dynamically only on Android system
`libm`, `libdl`, and `libc`. SQLite3MC includes an internal SQLCipher-compatible
cipher configuration string as part of its multi-cipher implementation; there
is no packaged `libsqlcipher.so`, SQLCipher component, OpenSSL component, or
OpenSSL dynamic dependency. Production explicitly selects `chacha20` and
rejects SQLCipher engine identification.

## Plaintext and secret evidence

The API-24 database, WAL, and SHM were scanned in place by filename-only
matching and contained none of the SQLite header, schema marker, test raw/hex
key, serialized test key, key alias, or keying-SQL markers. The marker contained
none of the prohibited key/secret/token/credential/pragma field names. Current
process logs contained no known test key, serialized key, alias, or keying SQL.

Release `libapp.so` contains expected schema/keying configuration literals but
no integration-test key or failure marker. `libsqlite3mc.so` necessarily
contains the SQLite header constant as engine code. The upstream Flutter engine
contains a generic ascending-byte test constant that collides with one
synthetic test vector; it is not present in TraderPro `libapp.so` and is not a
TraderPro key. The true production key is intentionally inaccessible, so no
claim is made that arbitrary live-memory or rooted-device extraction was
tested.

On API 35, the real production ciphertext was also checked in-process and from
the accessible debug evidence sandbox. The database/WAL/SHM contained none of
the SQLite header, metadata name, keying SQL, secure-store namespace/entry, or
`v1:` serialized-key marker. The provisioning marker contained none of the
prohibited key/secret/token/credential/pragma field names. Scoped debug and
release process logs contained zero searched schema, key, credential, token,
serialized-key, keying-SQL, or 64-hex patterns. The raw live production key was
not extracted for a host-side scan; the integration test instead compared the
in-memory value against protected artifact bytes without printing it.

## Remaining pilot gates and recommendation

The B2 technical implementation, required API-24/API-35 validation, final
static review, Owner notice approval, and ADR acceptance are complete. The
reviewed B2 change set is ready for its local closeout commit and subsequent
push/PR review. OEM restore/device-transfer, screen-lock change, low-storage,
lower-end physical device, true power-loss/fault injection, and live-process
compromise remain pilot or later gates. Task 7C2B3 has not started.
