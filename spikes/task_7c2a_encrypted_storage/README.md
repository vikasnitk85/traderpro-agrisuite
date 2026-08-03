# Task 7C2A encrypted-storage spike

This is an isolated, non-production Flutter/Android evidence harness. It is not imported by `apps/mobile`, has no TraderPro business schema or real data, and is not connected to production navigation, repositories, credentials, or databases.

The evidence harness uses Drift `2.34.3` over `sqlite3` `3.5.0`, a randomly generated 32-byte hexadecimal database key, and `flutter_secure_storage` `10.3.1`. The hook is alternated between `source: sqlite3mc` with explicit ChaCha20 and `source: sqlcipher`; it is left at `sqlcipher` after the final corrective run. This is test configuration, not a production engine selection. Both candidates use the same current-toolchain matrix and their own captured artifact directory.

Evidence paths:

- `test/encrypted_database_test.dart`: encrypted create/write/read, close/reopen through a background isolate, transaction rollback, schema migration, wrong/missing-key failure, and an unkeyed-open rejection.
- `test/database_key_test.dart`: explicit first-run provisioning plus missing, reset, malformed, and unavailable secure-store failure behavior. A missing key never triggers silent replacement or database deletion.
- `integration_test/secure_store_test.dart`: small real Android secure-storage smoke test.
- `integration_test/device_storage_test.dart`: corrective real-device key-failure, encrypted reopen, migration, rollback, background-isolate, WAL, artifact-scan, and comparable performance matrix.
- `lib/device_restart_harness.dart`: alternate synthetic entry point used to prove a secure-store-backed encrypted reopen after Android force-stop and a new process ID.
- `tool/generate_encrypted_artifacts.dart`: creates synthetic encrypted database, WAL/SHM, and rollback-journal samples under `artifacts/<engine>/` for an external raw-byte scan without printing or persisting the key.

The Android application disables backup and has minimum SDK 24. Physical runtime tests use debug builds; unsigned release-mode packaging is evidence only and is not a production release. No key, password, token, activation code, lease capability, or real customer data may be added to captured evidence.
