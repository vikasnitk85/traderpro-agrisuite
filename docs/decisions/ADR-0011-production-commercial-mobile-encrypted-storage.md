# ADR-0011: Production Commercial mobile encrypted storage

- Status: Accepted
- Date: 2026-08-08
- Scope: TraderPro V1 Commercial Android database encryption and secure-store
  input for Task 7C2B2

## Decision

TraderPro V1 selects SQLite3MultipleCiphers 2.3.6, reached through the Dart
`sqlite3` package 3.5.0, with its SQLite 3.53.3 baseline. The one production
database engine is configured explicitly as ChaCha20-Poly1305 and keyed with a
true random 256-bit raw key. This is a technical selection; Task 7C2B2 has not
started and no production Flutter dependency or database has been introduced.

The exact frozen database configuration is:

| Property | Accepted value |
| --- | --- |
| Engine | SQLite3MultipleCiphers 2.3.6 |
| SQLite | 3.53.3 |
| Dart wrapper | `sqlite3` 3.5.0 |
| Cipher | ChaCha20-Poly1305 via `PRAGMA cipher = 'chacha20'` |
| Key interpretation | 32 true-random bytes supplied as raw-key syntax `PRAGMA key = "x'<64 lowercase hex characters>'"` |
| Page size | 4096 bytes via `PRAGMA page_size = 4096` |
| Authentication/integrity | Poly1305 authentication with `PRAGMA hmac_check = 1` |
| KDF setting | `PRAGMA kdf_iter = 64007`; retained as a verified cipher setting even though raw-key syntax bypasses passphrase derivation |
| Legacy mode | Disabled via `PRAGMA legacy = 0` |
| Legacy WAL | Disabled via `PRAGMA mc_legacy_wal = 0` |
| Plaintext header | Zero bytes via `PRAGMA plaintext_header_size = 0` |
| Temporary storage | Memory only via `PRAGMA temp_store = MEMORY` |
| Journal mode | WAL after successful keyed configuration and verification |

Every connection must apply the cipher selection and all required cipher
parameters before applying the raw key and before any schema access. Startup
then reads back and asserts every supported frozen value, confirms that the
page size is 4096, and successfully reads `sqlite_master` before enabling
foreign keys, memory-only temporary storage, and WAL. A missing optional
`memory_security` readback must be reported as unsupported, never as enabled.
Configuration drift is a startup failure, not a compatibility signal.

The implementation must not:

- interpret the 64-character hexadecimal representation as a passphrase;
- fall back to a legacy cipher, another cipher, another engine, or plaintext;
- generate a key when ciphertext already exists;
- delete, recreate, reset, or reinitialize a database automatically after a
  key or open failure;
- automatically or in-place rekey a production database in V1; or
- switch engines after production data exists without an approved migration.

## Secure-store input for Task 7C2B2

The selected secure-store candidate is `flutter_secure_storage` 10.3.1 with
Android RSA-OAEP-SHA256 and AES-GCM, using:

| Option | Accepted value |
| --- | --- |
| `migrateOnAlgorithmChange` | `true` |
| `migrateWithBackup` | `false` |
| `resetOnError` | `false` |

Migration is permitted only to initialize or align the dedicated empty
namespace. It is never a recovery mechanism for database-key loss. If an
encrypted database exists and its key is missing, malformed, or unavailable,
startup fails closed, preserves the ciphertext, and generates no replacement
key. The production namespace and alias are intentionally not frozen here;
they require Task 7C2B2 design review.

## Rationale

Both candidates passed the required host, official API-24, and physical API-35
runtime, corruption, raw-key, lifecycle, replacement, and fail-closed gates.
SQLite3MultipleCiphers' explicit ChaCha20-Poly1305 configuration rejected the
scoped corruption tests, its true raw-key path passed, and the reviewed SBOM,
SCA, and manual native review found no applicable unresolved Critical or High
advisory. It adds no OpenSSL native dependency, has the smaller native and APK
footprint recorded by B1, and had acceptable measured performance.

The selection is not based merely on speed or APK size. The deciding V1
tradeoff is that SQLite3MultipleCiphers satisfies the required authenticated
at-rest contract with a smaller native supply-chain surface. SQLCipher
Community 4.17.0 is mature, offers a narrow encrypted-SQLite contract,
per-page HMAC, and stronger documented memory sanitization and locking
controls. The tested SQLCipher artifact, however, statically includes OpenSSL
3.6.2 while a newer upstream security-patched OpenSSL exists. That adds native
provenance, advisory triage, patching, and notice obligations that the
TraderPro V1 functional evidence does not establish a requirement to accept.
SQLCipher is not considered insecure; it is a validated alternative.

## Accepted risks and Task 7C2B2 controls

SQLite3MultipleCiphers exposes a broader cipher/configuration surface than
SQLCipher. Task 7C2B2 must prevent drift with fixed startup PRAGMAs, exact
readback assertions, and tests. The tested artifact does not expose effective
enhanced memory-security wiping or locking equivalent to SQLCipher's verified
control.

At-rest database encryption does not protect plaintext or keys from a rooted
device, debugger or instrumentation attached to the running app, arbitrary
memory inspection of a compromised live process, or another live-process
compromise. Task 7C2B2 must therefore require:

- the shortest practical raw-key lifetime in Dart memory;
- no string or log serialization of the raw key;
- best-effort wiping of mutable key buffers;
- no key material in exception messages;
- no database payloads in diagnostics;
- `temp_store=MEMORY` as frozen above;
- Android backup and data-extraction exclusions; and
- a non-debuggable release posture.

These controls reduce exposure but do not claim protection against a
compromised live process. The residual risk is accepted for V1 and remains a
documented pilot limitation.

## SQLCipher fallback policy

SQLCipher Community 4.17.0 is retained as validated but non-selected evidence.
Task 7C2B2 must implement one production engine only and must not build a
dual-engine compatibility layer. Reconsidering SQLCipher requires a new ADR
and evidence pass covering:

- the then-current native artifact and current OpenSSL security patch;
- a regenerated SBOM and reviewed SCA result;
- API-24 and API-35 regression;
- raw-key and exact-configuration tests; and
- an approved, fault-injected migration plan if production SQLite3MultipleCiphers
  data already exists.

## Supply-chain and notice consequences

The B1 CycloneDX SBOM remains comparative evidence for both tested candidates;
it must not be rewritten to imply that only the selected engine was tested.
Task 7C2B2 must generate a production SBOM from the actual production
dependency graph after the selected packages are introduced. The selected
technical notice inputs are SQLite3MultipleCiphers, `sqlite3`, SQLite,
`flutter_secure_storage`, Tink, and the relevant shipped transitive Android
and Dart dependencies. Formal legal approval and final notice packaging remain
B2/release gates. SQLCipher and OpenSSL remain candidate-evidence notices only.

## Consequences

- Task 7C2B1 is selected and complete subject to commit review.
- Task 7C2B2 is the next milestone and has not started.
- Production Flutter remains unchanged until a separately reviewed B2 change.
- No automatic rekey, engine switch, plaintext recovery, database reset, or
  cross-device restore is authorized for V1.

## Evidence

- `docs/assessments/TASK-7C2B1-Production-Storage-Selection.md`
- `docs/supply-chain/TASK-7C2B1-Mobile-Native-Provenance.md`
- `docs/supply-chain/TASK-7C2B1-Mobile-SBOM.cdx.json`
- `docs/supply-chain/TASK-7C2B1-Mobile-SCA-Review.md`
- `docs/supply-chain/TASK-7C2B1-Third-Party-Notices.md`
- `docs/runbooks/TPRUN-006-Encrypted-Mobile-Storage-Validation.md`
