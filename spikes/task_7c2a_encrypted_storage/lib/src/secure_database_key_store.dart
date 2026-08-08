import 'package:flutter_secure_storage/flutter_secure_storage.dart';

import 'database_key.dart';

final class FlutterSecureDatabaseKeyStore implements DatabaseKeyStore {
  FlutterSecureDatabaseKeyStore({
    FlutterSecureStorage? storage,
    this.options = androidOptions,
  }) : _storage = storage ?? const FlutterSecureStorage();

  // Public only so the isolated device test can inject a malformed persisted
  // value through the real plugin and prove that normal loading fails closed.
  static const entryName = 'encrypted_database_key_v1';
  static const androidOptions = AndroidOptions(
    storageNamespace: 'traderpro_7c2a_storage_spike',
    resetOnError: false,
    migrateOnAlgorithmChange: true,
    migrateWithBackup: false,
    enforceBiometrics: false,
    keyCipherAlgorithm:
        KeyCipherAlgorithm.RSA_ECB_OAEPwithSHA_256andMGF1Padding,
    storageCipherAlgorithm: StorageCipherAlgorithm.AES_GCM_NoPadding,
  );

  // Evidence-only comparison for the pinned plugin's backup migration path.
  // This namespace is intentionally separate so both options start without
  // values or algorithm markers after one clean application install.
  static const backupMigrationAndroidOptions = AndroidOptions(
    storageNamespace: 'traderpro_7c2b1_storage_spike_backup_migration',
    resetOnError: false,
    migrateOnAlgorithmChange: true,
    migrateWithBackup: true,
    enforceBiometrics: false,
    keyCipherAlgorithm:
        KeyCipherAlgorithm.RSA_ECB_OAEPwithSHA_256andMGF1Padding,
    storageCipherAlgorithm: StorageCipherAlgorithm.AES_GCM_NoPadding,
  );

  final FlutterSecureStorage _storage;
  final AndroidOptions options;

  @override
  Future<String?> read() => _storage.read(key: entryName, aOptions: options);

  @override
  Future<void> write(String keyHex) => _storage.write(
    key: entryName,
    value: validateDatabaseKey(keyHex),
    aOptions: options,
  );

  @override
  Future<void> delete() => _storage.delete(key: entryName, aOptions: options);
}
