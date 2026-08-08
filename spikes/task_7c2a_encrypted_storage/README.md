# Task 7C2A encrypted-storage spike

This is an isolated, non-production Flutter/Android evidence harness. It is not imported by `apps/mobile`, has no TraderPro business schema or real data, and is not connected to production navigation, repositories, credentials, or databases.

The evidence harness uses Drift `2.34.3` over `sqlite3` `3.5.0`, a randomly generated 32-byte hexadecimal database key, and `flutter_secure_storage` `10.3.1`. The hook is alternated between `source: sqlite3mc` with explicit ChaCha20-Poly1305 and `source: sqlcipher`; after the final two-engine regression it is retained at `sqlite3mc` to match ADR-0011. This remains isolated test configuration, not a production Flutter implementation. Both candidates use the same current-toolchain matrix and their own captured artifact directory.

Task 7C2B1 freezes true raw-key syntax (`x'<64 hex>'`) rather than silently treating the hexadecimal representation as a text passphrase. Both openers apply and read back explicit compatibility, page, authentication, plaintext-header, KDF-compatibility, and WAL settings before schema access. SQLCipher enables its memory-security pragma; the resolved SQLite3MultipleCiphers artifact does not expose its optional memory-security pragma, so the harness reports that control as unsupported instead of claiming it is enabled. KDF settings are retained as explicit format/compatibility values but are bypassed by the selected raw-key syntax.

Evidence paths:

- `test/encrypted_database_test.dart`: encrypted create/write/read, close/reopen through a background isolate, transaction rollback, schema migration, wrong/missing-key failure, and an unkeyed-open rejection.
- `test/database_key_test.dart`: explicit first-run provisioning plus missing, reset, malformed, and unavailable secure-store failure behavior. A missing key never triggers silent replacement or database deletion.
- `test/encryption_configuration_test.dart`: runtime configuration readback, raw-key-versus-text-passphrase incompatibility, malformed-key rejection, and key-free native failure messages.
- `test/ciphertext_integrity_test.dart`: controlled copy-only encrypted-page bit flips, targeted corruption reads, and wrong-key versus later-page corruption behavior.
- `integration_test/secure_store_test.dart`: small real Android secure-storage smoke test.
- `integration_test/device_storage_test.dart`: corrective real-device key-failure, encrypted reopen, migration, rollback, background-isolate, WAL, artifact-scan, and comparable performance matrix.
- `lib/device_restart_harness.dart`: alternate synthetic entry point used to prove a secure-store-backed encrypted reopen after Android force-stop and a new process ID.
- `tool/generate_encrypted_artifacts.dart`: creates synthetic encrypted database, WAL/SHM, and rollback-journal samples under `artifacts/<engine>/` for an external raw-byte scan without printing or persisting the key.
- `tool/run_interruption_matrix.dart`: kills isolated child processes during active write, pre-commit, WAL-checkpoint, and transactional migration phases, then verifies restart consistency and ciphertext preservation. It is process-termination evidence, not power-loss simulation.
- `tool/print_storage_configuration.dart`: emits sanitized engine/version/pragma evidence without key material.
- `tool/generate_sbom.dart`: deterministically regenerates the reviewed CycloneDX inventory from the lockfile plus the recorded Gradle/native graph.

Task 7C2B1's corrected pinned-10.3.1 Strategy A passes the API-24 and physical API-35 clean-install, fail-closed, force-stop/new-process, and same-data replacement matrix with both encrypted engines. The frozen Grype scan and manual native advisory review are complete. ADR-0011 selects SQLite3MultipleCiphers for V1 and B1 is complete subject to commit review; production Task 7C2B2 has not started. The physical API-35 engine/performance matrix is recorded, while a designated lower-end physical matrix remains a before-pilot gate. See `docs/assessments/TASK-7C2B1-Production-Storage-Selection.md`, `docs/supply-chain/TASK-7C2B1-Mobile-SCA-Review.md`, and `docs/runbooks/TPRUN-006-Encrypted-Mobile-Storage-Validation.md`.

The Android application disables backup and has minimum SDK 24. Physical runtime tests use debug builds; unsigned release-mode packaging is evidence only and is not a production release. No key, password, token, activation code, lease capability, or real customer data may be added to captured evidence.
