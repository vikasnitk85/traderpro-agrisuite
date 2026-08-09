import 'package:flutter/services.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:traderpro_agrisuite_mobile/core/security/commercial_database_key_material.dart';
import 'package:traderpro_agrisuite_mobile/core/security/commercial_database_key_store.dart';
import 'package:traderpro_agrisuite_mobile/core/security/commercial_storage_failure.dart';
import 'package:traderpro_agrisuite_mobile/infrastructure/security/flutter_secure_commercial_database_key_store.dart';

void main() {
  test('approved Android options and identifiers remain frozen', () {
    final params = FlutterSecureStringStore.androidOptions.params;
    expect(params['resetOnError'], 'false');
    expect(params['migrateOnAlgorithmChange'], 'true');
    expect(params['migrateWithBackup'], 'false');
    expect(
      params['keyCipherAlgorithm'],
      'RSA_ECB_OAEPwithSHA_256andMGF1Padding',
    );
    expect(params['storageCipherAlgorithm'], 'AES_GCM_NoPadding');
    expect(
      params['storageNamespace'],
      FlutterSecureCommercialDatabaseKeyStore.storageNamespace,
    );
    expect(
      FlutterSecureCommercialDatabaseKeyStore.databaseKeyEntry,
      'commercial_database_key_v1',
    );
  });

  test(
    'empty store distinguishes uninitialized from expected missing',
    () async {
      final store = FlutterSecureCommercialDatabaseKeyStore(
        secureStringStore: _MemorySecureStringStore(),
      );
      expect(
        (await store.read(keyExpected: false)).kind,
        CommercialDatabaseKeyStoreStateKind.uninitialized,
      );
      expect(
        (await store.read(keyExpected: true)).kind,
        CommercialDatabaseKeyStoreStateKind.missing,
      );
    },
  );

  test('initial write validates readback and returns raw bytes', () async {
    final memory = _MemorySecureStringStore();
    final store = FlutterSecureCommercialDatabaseKeyStore(
      secureStringStore: memory,
    );
    final key = CommercialDatabaseKeyMaterial.copyFrom(
      List<int>.generate(32, (i) => i),
    );

    await store.writeInitial(key);
    expect(memory.value, matches(RegExp(r'^v1:[A-Za-z0-9_-]{43}$')));
    final state = await store.read(keyExpected: true);
    expect(state, isA<CommercialDatabaseKeyStoreValid>());
    final valid = state as CommercialDatabaseKeyStoreValid;
    valid.keyMaterial.useBytes(
      (bytes) => expect(bytes, orderedEquals(List<int>.generate(32, (i) => i))),
    );
    valid.keyMaterial.dispose();
    key.dispose();
  });

  test('malformed value is classified without exposing it', () async {
    final store = FlutterSecureCommercialDatabaseKeyStore(
      secureStringStore: _MemorySecureStringStore()..value = 'not-a-key',
    );
    expect(
      (await store.read(keyExpected: true)).kind,
      CommercialDatabaseKeyStoreStateKind.malformed,
    );
  });

  test('writeInitial never overwrites an existing value', () async {
    final memory = _MemorySecureStringStore()..value = 'existing';
    final store = FlutterSecureCommercialDatabaseKeyStore(
      secureStringStore: memory,
    );
    final key = CommercialDatabaseKeyMaterial.copyFrom(List.filled(32, 1));
    expect(
      () => store.writeInitial(key),
      throwsA(
        isA<CommercialStorageException>().having(
          (error) => error.code,
          'code',
          CommercialStorageFailureCode.databaseInitializationIncomplete,
        ),
      ),
    );
    key.dispose();
  });

  test('platform errors map to unavailable or reliably invalidated', () async {
    final unavailable = FlutterSecureCommercialDatabaseKeyStore(
      secureStringStore: _ThrowingSecureStringStore('channel_error'),
    );
    final invalidated = FlutterSecureCommercialDatabaseKeyStore(
      secureStringStore: _ThrowingSecureStringStore(
        'key_permanently_invalidated',
      ),
    );
    expect(
      (await unavailable.read(keyExpected: true)).kind,
      CommercialDatabaseKeyStoreStateKind.unavailable,
    );
    expect(
      (await invalidated.read(keyExpected: true)).kind,
      CommercialDatabaseKeyStoreStateKind.invalidated,
    );
  });
}

final class _MemorySecureStringStore implements SecureStringStore {
  String? value;

  @override
  Future<String?> read(String key) async => value;

  @override
  Future<void> write(String key, String value) async {
    this.value = value;
  }
}

final class _ThrowingSecureStringStore implements SecureStringStore {
  _ThrowingSecureStringStore(this.code);

  final String code;

  @override
  Future<String?> read(String key) => throw PlatformException(code: code);

  @override
  Future<void> write(String key, String value) =>
      throw PlatformException(code: code);
}
