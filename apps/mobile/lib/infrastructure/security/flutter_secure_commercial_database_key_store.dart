import 'package:flutter/services.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

import '../../core/security/commercial_database_key_material.dart';
import '../../core/security/commercial_database_key_store.dart';
import '../../core/security/commercial_storage_failure.dart';

abstract interface class SecureStringStore {
  Future<String?> read(String key);

  Future<void> write(String key, String value);
}

final class FlutterSecureStringStore implements SecureStringStore {
  FlutterSecureStringStore({FlutterSecureStorage? storage})
    : _storage = storage ?? _createStorage();

  static const AndroidOptions androidOptions = AndroidOptions(
    resetOnError: false,
    migrateOnAlgorithmChange: true,
    migrateWithBackup: false,
    enforceBiometrics: false,
    keyCipherAlgorithm:
        KeyCipherAlgorithm.RSA_ECB_OAEPwithSHA_256andMGF1Padding,
    storageCipherAlgorithm: StorageCipherAlgorithm.AES_GCM_NoPadding,
    storageNamespace: FlutterSecureCommercialDatabaseKeyStore.storageNamespace,
  );

  final FlutterSecureStorage _storage;

  static FlutterSecureStorage _createStorage() =>
      const FlutterSecureStorage(aOptions: androidOptions);

  @override
  Future<String?> read(String key) => _storage.read(key: key);

  @override
  Future<void> write(String key, String value) =>
      _storage.write(key: key, value: value);
}

final class FlutterSecureCommercialDatabaseKeyStore
    implements CommercialDatabaseKeyStore {
  FlutterSecureCommercialDatabaseKeyStore({
    SecureStringStore? secureStringStore,
  }) : _secureStringStore = secureStringStore ?? FlutterSecureStringStore(),
       _codec = const CommercialDatabaseKeyCodec();

  static const String storageNamespace = 'traderpro_commercial_storage_v1';
  static const String databaseKeyEntry = 'commercial_database_key_v1';
  static const int contractVersion = 1;

  final SecureStringStore _secureStringStore;
  final CommercialDatabaseKeyCodec _codec;

  @override
  Future<CommercialDatabaseKeyStoreState> read({
    required bool keyExpected,
  }) async {
    try {
      final serialized = await _secureStringStore.read(databaseKeyEntry);
      if (serialized == null) {
        return keyExpected
            ? const CommercialDatabaseKeyStoreMissing()
            : const CommercialDatabaseKeyStoreUninitialized();
      }
      try {
        return CommercialDatabaseKeyStoreValid(_codec.decode(serialized));
      } on CommercialStorageException {
        return const CommercialDatabaseKeyStoreMalformed();
      }
    } on PlatformException catch (error) {
      return _isReliablyInvalidated(error.code)
          ? const CommercialDatabaseKeyStoreInvalidated()
          : const CommercialDatabaseKeyStoreUnavailable();
    } on Object {
      return const CommercialDatabaseKeyStoreUnexpected();
    }
  }

  @override
  Future<void> writeInitial(CommercialDatabaseKeyMaterial keyMaterial) async {
    try {
      if (await _secureStringStore.read(databaseKeyEntry) != null) {
        throw const CommercialStorageException(
          CommercialStorageFailureCode.databaseInitializationIncomplete,
          phase: 'secure-store-write-existing',
        );
      }
      final serialized = _codec.encode(keyMaterial);
      await _secureStringStore.write(databaseKeyEntry, serialized);
      final readback = await _secureStringStore.read(databaseKeyEntry);
      if (readback != serialized) {
        throw const CommercialStorageException(
          CommercialStorageFailureCode.secureStoreUnavailable,
          phase: 'secure-store-write-readback',
        );
      }
      final verified = _codec.decode(readback!);
      verified.dispose();
    } on CommercialStorageException {
      rethrow;
    } on PlatformException catch (error) {
      throw CommercialStorageException(
        _isReliablyInvalidated(error.code)
            ? CommercialStorageFailureCode.secureStoreInvalidated
            : CommercialStorageFailureCode.secureStoreUnavailable,
        phase: 'secure-store-write',
      );
    } on Object {
      throw const CommercialStorageException(
        CommercialStorageFailureCode.unexpectedStorageFailure,
        phase: 'secure-store-write',
      );
    }
  }

  static bool _isReliablyInvalidated(String code) =>
      code == 'key_permanently_invalidated' ||
      code == 'KeyPermanentlyInvalidatedException';
}
