import 'dart:math';

abstract interface class DatabaseKeyStore {
  Future<String?> read();

  Future<void> write(String keyHex);

  Future<void> delete();
}

final class MissingDatabaseKeyException implements Exception {
  const MissingDatabaseKeyException();

  @override
  String toString() =>
      'MissingDatabaseKeyException: encrypted database key is unavailable';
}

final class InvalidDatabaseKeyException implements Exception {
  const InvalidDatabaseKeyException();

  @override
  String toString() =>
      'InvalidDatabaseKeyException: database key must be 32-byte hexadecimal';
}

final class DatabaseKeyController {
  DatabaseKeyController(this._store);

  final DatabaseKeyStore _store;

  /// Normal opening is fail-closed. A missing/reset/unavailable secure-store
  /// value is never replaced and the encrypted database is never deleted.
  Future<String> loadExisting() async {
    final value = await _store.read();
    if (value == null) {
      throw const MissingDatabaseKeyException();
    }
    return validateDatabaseKey(value);
  }

  /// Explicit provisioning path for a first-run database only.
  Future<String> provisionNewDatabase() async {
    final existing = await _store.read();
    if (existing != null) {
      return validateDatabaseKey(existing);
    }

    final key = generateDatabaseKey();
    await _store.write(key);
    return key;
  }
}

String validateDatabaseKey(String value) {
  if (!RegExp(r'^[0-9a-fA-F]{64}$').hasMatch(value)) {
    throw const InvalidDatabaseKeyException();
  }
  return value.toLowerCase();
}

String generateDatabaseKey() {
  final random = Random.secure();
  return List<int>.generate(
    32,
    (_) => random.nextInt(256),
  ).map((byte) => byte.toRadixString(16).padLeft(2, '0')).join();
}
