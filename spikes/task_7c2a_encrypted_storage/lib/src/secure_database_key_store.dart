import 'package:flutter_secure_storage/flutter_secure_storage.dart';

import 'database_key.dart';

final class FlutterSecureDatabaseKeyStore implements DatabaseKeyStore {
  FlutterSecureDatabaseKeyStore({FlutterSecureStorage? storage})
    : _storage = storage ?? const FlutterSecureStorage();

  // Public only so the isolated device test can inject a malformed persisted
  // value through the real plugin and prove that normal loading fails closed.
  static const entryName = 'encrypted_database_key_v1';
  static const androidOptions = AndroidOptions(
    storageNamespace: 'traderpro_7c2a_storage_spike',
    resetOnError: false,
  );

  final FlutterSecureStorage _storage;

  @override
  Future<String?> read() =>
      _storage.read(key: entryName, aOptions: androidOptions);

  @override
  Future<void> write(String keyHex) => _storage.write(
    key: entryName,
    value: validateDatabaseKey(keyHex),
    aOptions: androidOptions,
  );

  @override
  Future<void> delete() =>
      _storage.delete(key: entryName, aOptions: androidOptions);
}
