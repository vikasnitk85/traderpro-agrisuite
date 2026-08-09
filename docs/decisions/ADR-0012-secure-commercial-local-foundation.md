# ADR-0012: Secure Commercial local foundation

- Status: Accepted
- Date: 2026-08-08
- Acceptance date: 2026-08-08
- Scope: Task 7C2B2 production Android/Flutter encrypted local foundation
- Supersedes: none
- Extends: ADR-0011

## Context

ADR-0011 selected SQLite3MultipleCiphers 2.3.6 through `sqlite3` 3.5.0,
SQLite 3.53.3, and ChaCha20-Poly1305 for the V1 Commercial database. Task
7C2B2 must turn that selection into a production foundation without adding
authentication, account binding, Receiving, master, outbox, cursor, Inventory,
Finance, or other B3/business schema.

Cloud-confirmed data remains authoritative. Local initialization must preserve
offline physical facts and all suspicious ciphertext or interruption artifacts
for explicit reconciliation. A missing key is not permission to replace a
database.

## Decision

ADR-0011 remains the authoritative encrypted-storage selection. This ADR
accepts only the B2 secure Commercial local foundation that implements that
selection. Authentication and Commercial context binding remain B3, and B2
introduces no Commercial Receiving business workflow.

### Storage identity and separation

- One Commercial database exists per app installation.
- Its app-support path is
  `commercial/traderpro-commercial-v1.sqlite3`.
- Provisioning uses the deterministic sibling
  `commercial/traderpro-commercial-v1.initializing.sqlite3`.
- The non-secret crash marker is
  `commercial/traderpro-commercial-v1.provisioning.json`.
- The legacy/POC `traderpro-local.sqlite` database, schema, openers, and stores
  are not migrated, imported, or accessed by production Commercial code.
- Production uses `lib/main.dart`; the POC is reachable only through the
  separate `lib/main_poc.dart` entrypoint.

### Database key and secure store

- The database key is exactly 32 bytes from a cryptographically secure random
  generator and is held in owned mutable buffers with best-effort wiping.
- Secure-store serialization is exactly
  `v1:<base64url-without-padding>`.
- The dedicated namespace is `traderpro_commercial_storage_v1`; the entry is
  `commercial_database_key_v1`; both contracts are version 1.
- Android uses RSA OAEP SHA-256 for key wrapping and AES-GCM for stored values,
  with `resetOnError=false`, `migrateOnAlgorithmChange=true`,
  `migrateWithBackup=false`, and biometrics disabled for this key.
- Writes are create-only and must be read back and decoded before provisioning
  proceeds. Production has no secure-store reset or key deletion recovery.
- A 64-character lowercase hexadecimal representation exists only briefly at
  the SQLite3MC boundary to form the ADR-0011 raw-key pragma.

### SQLite3MC connection contract

Every production Commercial connection is created in a background isolate.
Before Drift schema access it applies, in order:

1. cipher `chacha20`;
2. legacy mode `0`;
3. KDF iteration setting `64007`;
4. plaintext header size `0`;
5. HMAC check `1`;
6. legacy WAL `0`;
7. page size `4096`;
8. linked SQLite version attestation without querying an encrypted file;
9. the temporary raw-key pragma;
10. exact configuration readbacks and keyed `sqlite_master` access;
11. foreign keys on, temporary storage in memory, and WAL mode; and
12. exact post-key readbacks before handing the executor to Drift.

`cipher_version` is prohibited because it would indicate a SQLCipher engine.
`memory_security` is capability evidence only and is not claimed where the
selected artifact does not expose reliable enabled readback. Native errors,
SQL, keys, paths, and stacks do not cross the safe error boundary.

### Provisioning and schema

Schema version 1 has one application table only:
`commercial_storage_metadata`. Its singleton binds schema/storage contract
version 1, installation UUID, database-instance UUID, key-alias version 1, and
creation UTC microseconds. Database triggers reject ordinary update and delete.

Provisioning is single-flight and follows preparing marker, one-time key
creation and readback, encrypted temporary database creation, metadata
verification, full WAL checkpoint, close/reopen verification, same-filesystem
atomic promotion, final reopen, and marker completion. A generation-matching
preparing marker is required to resume a stored-key/missing-final state.

Zero-length final databases, unreadable temporary databases, sidecar
conflicts, marker conflicts, wrong keys, missing/malformed/unavailable/
invalidated secure-store state, and configuration drift all fail closed. No
suspicious artifact is automatically deleted and no replacement key is made
when ciphertext exists.

### Runtime and diagnostics

`CommercialSecureRuntime` is the only production composition and lifecycle
owner for the secure-store adapter, initializer, database, startup state, and
close operation. Concurrent callers share one asynchronous open; one database
is owned; close is idempotent. Widgets receive only `ready`, `unavailable` with
a stable safe code, or `closed`. Widgets do not open Drift or read secure
storage, and no DI package is introduced.

Diagnostics are no-op by default and allow only stable code, phase,
database-exists flag, safe store state, contract versions, coarse platform,
UTC timestamp, and ephemeral in-memory correlation UUID. Installation and
database IDs, device serial, user/business identity, payloads, credentials,
keys, SQL, paths, native errors, and stacks are prohibited.

### Android and release policy

- Main/release sets `allowBackup=false`, broad cloud-backup and device-transfer
  exclusions, and `usesCleartextTraffic=false`.
- Main/release adds no INTERNET permission. The debug overlay keeps only its
  explicit development INTERNET/cleartext behavior.
- The hardcoded release use of debug signing is removed. Production signing
  material is not stored in this repository; reviewed release evidence is
  unsigned unless an external temporary evidence key is separately approved.
- Production distribution ABIs are `armeabi-v7a` and `arm64-v8a`.
  `x86_64` is emulator/test-only.
- Screenshot/task-switcher protection remains deferred to B3.

### Notice approval and distribution

- On 2026-08-08, the Project/Legal Owner approved the complete B2 exact-text
  third-party notice bundle for the frozen production dependency and ARM
  binary graph.
- The approved future product surface is About / Legal / Third-Party Notices.
- The SQLite public-domain dedication/provenance must be included.
- B2 records this later distribution/release requirement but does not
  implement the presentation UI.
- Any dependency, version, ABI policy, storage engine, Flutter engine revision,
  or production binary-graph change reopens the notice/SBOM review gate.
- The approval is Project/Legal Owner approval, not external legal counsel
  advice.

## Consequences

- B2 provides encrypted foundation metadata and startup state only; B3 is not
  started by this decision.
- Lost or invalidated keys can make unsynced ciphertext unrecoverable. The app
  blocks instead of silently discarding it.
- Rooted devices, debugger/instrumentation access, and compromised live-process
  memory remain outside the at-rest guarantee.
- Key rotation, discard/recovery UX, OEM restore/transfer, future OS/device
  regression, and lower-end pilot evidence remain separately gated.
- The exact production notice bundle is Owner-approved for the frozen graph.
  A graph change invalidates that approval until regenerated evidence is
  reviewed again.

## Evidence

- `docs/assessments/TASK-7C2B2-Secure-Commercial-Local-Foundation.md`
- `docs/supply-chain/TASK-7C2B2-Mobile-Production-Native-Provenance.md`
- `docs/supply-chain/TASK-7C2B2-Mobile-Production-SBOM.cdx.json`
- `docs/supply-chain/TASK-7C2B2-Mobile-Production-SCA-Review.md`
- `docs/supply-chain/TASK-7C2B2-Third-Party-Notices.md`
- `docs/supply-chain/TASK-7C2B2-Third-Party-Notices-Exact.md`
- `docs/runbooks/TPRUN-006-Encrypted-Mobile-Storage-Validation.md`
