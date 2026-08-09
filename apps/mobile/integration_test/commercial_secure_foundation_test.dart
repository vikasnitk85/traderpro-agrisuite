import 'dart:convert';
import 'dart:io';
import 'dart:typed_data';

import 'package:flutter_test/flutter_test.dart';
import 'package:integration_test/integration_test.dart';
import 'package:path_provider/path_provider.dart';
import 'package:sqlite3/sqlite3.dart' as sqlite;
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:traderpro_agrisuite_mobile/app/commercial_secure_startup_state.dart';
import 'package:traderpro_agrisuite_mobile/core/security/commercial_database_key_material.dart';
import 'package:traderpro_agrisuite_mobile/core/security/commercial_database_key_store.dart';
import 'package:traderpro_agrisuite_mobile/core/security/commercial_provisioning_marker.dart';
import 'package:traderpro_agrisuite_mobile/core/security/commercial_storage_failure.dart';
import 'package:traderpro_agrisuite_mobile/core/security/commercial_storage_state_machine.dart';
import 'package:traderpro_agrisuite_mobile/infrastructure/database/commercial/commercial_database.dart';
import 'package:traderpro_agrisuite_mobile/infrastructure/database/commercial/commercial_database_initializer.dart';
import 'package:traderpro_agrisuite_mobile/infrastructure/database/commercial/commercial_database_opener.dart';
import 'package:traderpro_agrisuite_mobile/infrastructure/database/commercial/commercial_database_paths.dart';
import 'package:traderpro_agrisuite_mobile/infrastructure/runtime/commercial_secure_runtime.dart';
import 'package:traderpro_agrisuite_mobile/infrastructure/security/flutter_secure_commercial_database_key_store.dart';
import 'package:traderpro_agrisuite_mobile/infrastructure/storage/commercial_provisioning_marker_store.dart';

void main() {
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();

  testWidgets('production secure store provisions and reopens ciphertext', (
    tester,
  ) async {
    final support = await getApplicationSupportDirectory();
    final paths = CommercialDatabasePaths.fromApplicationSupport(support);
    final markerStore = CommercialProvisioningMarkerStore(
      paths.provisioningMarker,
    );
    final keyStore = FlutterSecureCommercialDatabaseKeyStore();

    final first = CommercialSecureRuntime.production(
      applicationSupportDirectory: support,
    );
    expect((await first.open()).status, CommercialSecureStartupStatus.ready);
    await first.close();

    final firstMarker = await markerStore.read();
    expect(firstMarker, isNotNull);
    expect(firstMarker!.state, CommercialProvisioningMarkerState.complete);
    final firstKeyState = await keyStore.read(keyExpected: true);
    expect(firstKeyState, isA<CommercialDatabaseKeyStoreValid>());
    final firstKey =
        (firstKeyState as CommercialDatabaseKeyStoreValid).keyMaterial;
    final firstKeyBytes = firstKey.useBytes(Uint8List.fromList);
    final database = await const CommercialDatabaseOpener().open(
      file: paths.finalDatabase,
      keyMaterial: firstKey,
    );
    try {
      final metadataRows = await database
          .select(database.commercialStorageMetadata)
          .get();
      expect(metadataRows, hasLength(1));
      final metadata = metadataRows.single;
      expect(metadata.singletonId, 1);
      expect(metadata.schemaContractVersion, 1);
      expect(metadata.storageContractVersion, 1);
      expect(metadata.keyAliasVersion, 1);
      expect(metadata.installationId, firstMarker.installationId);
      expect(metadata.databaseInstanceId, firstMarker.databaseInstanceId);
      await _expectPragma(database, 'cipher', 'chacha20');
      await _expectPragma(database, 'legacy', '0');
      await _expectPragma(database, 'kdf_iter', '64007');
      await _expectPragma(database, 'plaintext_header_size', '0');
      await _expectPragma(database, 'hmac_check', '1');
      await _expectPragma(database, 'mc_legacy_wal', '0');
      await _expectPragma(database, 'page_size', '4096');
      await _expectPragma(database, 'foreign_keys', '1');
      await _expectPragma(database, 'temp_store', '2');
      await _expectPragma(database, 'journal_mode', 'wal');
      await _expectPragma(database, 'user_version', '1');
    } finally {
      await database.close();
      firstKey.dispose();
    }

    final second = CommercialSecureRuntime.production(
      applicationSupportDirectory: support,
    );
    expect((await second.open()).status, CommercialSecureStartupStatus.ready);
    await second.close();

    final secondMarker = await markerStore.read();
    expect(secondMarker, isNotNull);
    expect(secondMarker!.generation, firstMarker.generation);
    expect(secondMarker.installationId, firstMarker.installationId);
    expect(secondMarker.databaseInstanceId, firstMarker.databaseInstanceId);
    expect(secondMarker.state, CommercialProvisioningMarkerState.complete);
    final secondKeyState = await keyStore.read(keyExpected: true);
    expect(secondKeyState, isA<CommercialDatabaseKeyStoreValid>());
    final secondKey =
        (secondKeyState as CommercialDatabaseKeyStoreValid).keyMaterial;
    try {
      expect(
        secondKey.useBytes(Uint8List.fromList),
        orderedEquals(firstKeyBytes),
      );
    } finally {
      secondKey.dispose();
    }

    final ciphertext = await paths.finalDatabase.readAsBytes();
    try {
      expect(ciphertext, isNotEmpty);
      expect(_contains(ciphertext, utf8.encode('SQLite format 3')), isFalse);
      expect(
        _contains(ciphertext, utf8.encode('commercial_storage_metadata')),
        isFalse,
      );
      expect(
        _contains(ciphertext, utf8.encode(firstMarker.installationId)),
        isFalse,
      );
      expect(
        _contains(ciphertext, utf8.encode(firstMarker.databaseInstanceId)),
        isFalse,
      );
      expect(_contains(ciphertext, firstKeyBytes), isFalse);
      expect(
        _contains(ciphertext, utf8.encode(_lowercaseHex(firstKeyBytes))),
        isFalse,
      );
    } finally {
      firstKeyBytes.fillRange(0, firstKeyBytes.length, 0);
    }

    final pocDatabaseExists = await support
        .list(recursive: true, followLinks: false)
        .any(
          (entity) =>
              entity is File &&
              entity.path.endsWith(
                '${Platform.pathSeparator}traderpro-local.sqlite',
              ),
        );
    expect(pocDatabaseExists, isFalse);

    final unkeyed = sqlite.sqlite3.open(paths.finalDatabase.path);
    try {
      expect(
        () => unkeyed.select('SELECT count(*) FROM sqlite_master'),
        throwsA(isA<sqlite.SqliteException>()),
      );
    } finally {
      unkeyed.close();
    }
  });

  testWidgets('real Android key failures preserve production ciphertext', (
    tester,
  ) async {
    final support = await getApplicationSupportDirectory();
    final paths = CommercialDatabasePaths.fromApplicationSupport(support);
    final markerStore = CommercialProvisioningMarkerStore(
      paths.provisioningMarker,
    );
    const secureStorage = FlutterSecureStorage(
      aOptions: FlutterSecureStringStore.androidOptions,
    );
    const entry = FlutterSecureCommercialDatabaseKeyStore.databaseKeyEntry;
    final originalSerialized = await secureStorage.read(key: entry);
    expect(originalSerialized, isNotNull);
    final ciphertextBefore = await paths.finalDatabase.readAsBytes();
    expect(
      _contains(ciphertextBefore, utf8.encode(originalSerialized!)),
      isFalse,
    );

    await secureStorage.delete(key: entry);
    try {
      await _expectInitializerFailure(
        paths: paths,
        keyStore: FlutterSecureCommercialDatabaseKeyStore(),
        markerStore: markerStore,
        expected: CommercialStorageFailureCode.secureStoreMissing,
      );
      expect(await secureStorage.read(key: entry), isNull);
      expect(
        await paths.finalDatabase.readAsBytes(),
        orderedEquals(ciphertextBefore),
      );
    } finally {
      await secureStorage.write(key: entry, value: originalSerialized);
    }

    const malformed = 'v1:malformed-secure-store-test-value';
    await secureStorage.write(key: entry, value: malformed);
    try {
      await _expectInitializerFailure(
        paths: paths,
        keyStore: FlutterSecureCommercialDatabaseKeyStore(),
        markerStore: markerStore,
        expected: CommercialStorageFailureCode.secureStoreMalformed,
      );
      expect(await secureStorage.read(key: entry), malformed);
      expect(
        await paths.finalDatabase.readAsBytes(),
        orderedEquals(ciphertextBefore),
      );
    } finally {
      await secureStorage.write(key: entry, value: originalSerialized);
    }

    final wrongKey = CommercialDatabaseKeyMaterial.generate(
      SecureRandomDatabaseKeyGenerator(),
    );
    final wrongKeyBytes = wrongKey.useBytes(Uint8List.fromList);
    final wrongSerialized = const CommercialDatabaseKeyCodec().encode(wrongKey);
    await secureStorage.write(key: entry, value: wrongSerialized);
    try {
      final failure = await _expectInitializerFailure(
        paths: paths,
        keyStore: FlutterSecureCommercialDatabaseKeyStore(),
        markerStore: markerStore,
        expected: CommercialStorageFailureCode.databaseWrongKeyOrUnreadable,
      );
      expect(failure.toString(), isNot(contains('SqliteException')));
      expect(await secureStorage.read(key: entry), wrongSerialized);
      final unchanged = await paths.finalDatabase.readAsBytes();
      expect(unchanged, orderedEquals(ciphertextBefore));
      expect(_contains(unchanged, wrongKeyBytes), isFalse);
      expect(
        _contains(unchanged, utf8.encode(_lowercaseHex(wrongKeyBytes))),
        isFalse,
      );
      expect(_contains(unchanged, utf8.encode(wrongSerialized)), isFalse);
    } finally {
      await secureStorage.write(key: entry, value: originalSerialized);
      wrongKey.dispose();
      wrongKeyBytes.fillRange(0, wrongKeyBytes.length, 0);
    }

    final restored = CommercialSecureRuntime.production(
      applicationSupportDirectory: support,
    );
    expect((await restored.open()).status, CommercialSecureStartupStatus.ready);
    await restored.close();
  });

  testWidgets('Android native matrix fails closed and resumes interruption', (
    tester,
  ) async {
    final support = await getApplicationSupportDirectory();
    final root = Directory(
      '${support.path}${Platform.pathSeparator}b2-device-matrix',
    );
    if (await root.exists()) {
      await root.delete(recursive: true);
    }
    await root.create(recursive: true);

    final paths = CommercialDatabasePaths.fromApplicationSupport(root);
    final keyStore = _MemoryKeyStore();
    final markerStore = CommercialProvisioningMarkerStore(
      paths.provisioningMarker,
    );
    final interrupted = CommercialDatabaseInitializer(
      paths: paths,
      keyStore: keyStore,
      markerStore: markerStore,
      opener: _FailFirstOpenCommercialDatabaseOpener(),
    );
    await expectLater(
      interrupted.initialize(
        context: CommercialProvisioningContext.explicitFirstInitialization,
      ),
      throwsA(
        isA<CommercialStorageException>().having(
          (error) => error.code,
          'code',
          CommercialStorageFailureCode.storageUnavailable,
        ),
      ),
    );
    expect(keyStore.hasKey, isTrue);
    expect(await paths.finalDatabase.exists(), isFalse);
    expect(await paths.provisioningMarker.exists(), isTrue);

    final resumed = CommercialDatabaseInitializer(
      paths: paths,
      keyStore: keyStore,
      markerStore: markerStore,
    );
    await resumed.initialize(
      context: CommercialProvisioningContext.explicitFirstInitialization,
    );
    await resumed.close();
    expect(keyStore.writeCount, 1);

    final completeMarker = await markerStore.read();
    expect(completeMarker, isNotNull);
    expect(completeMarker!.state, CommercialProvisioningMarkerState.complete);
    await markerStore.write(
      CommercialProvisioningMarker(
        version: completeMarker.version,
        generation: completeMarker.generation,
        installationId: completeMarker.installationId,
        databaseInstanceId: completeMarker.databaseInstanceId,
        state: CommercialProvisioningMarkerState.preparing,
      ),
    );
    final promotedResume = CommercialDatabaseInitializer(
      paths: paths,
      keyStore: keyStore,
      markerStore: markerStore,
    );
    await promotedResume.initialize(
      context: CommercialProvisioningContext.explicitFirstInitialization,
    );
    await promotedResume.close();
    expect(keyStore.writeCount, 1);
    final completedAfterPromotion = await markerStore.read();
    expect(
      completedAfterPromotion!.state,
      CommercialProvisioningMarkerState.complete,
    );
    expect(completedAfterPromotion.generation, completeMarker.generation);

    final before = await paths.finalDatabase.readAsBytes();
    for (final scenario
        in <
          ({
            CommercialDatabaseKeyStore keyStore,
            CommercialStorageFailureCode expected,
          })
        >[
          (
            keyStore: const _FixedKeyStore.missing(),
            expected: CommercialStorageFailureCode.secureStoreMissing,
          ),
          (
            keyStore: const _FixedKeyStore.malformed(),
            expected: CommercialStorageFailureCode.secureStoreMalformed,
          ),
          (
            keyStore: const _FixedKeyStore.unavailable(),
            expected: CommercialStorageFailureCode.secureStoreUnavailable,
          ),
          (
            keyStore: const _FixedKeyStore.invalidated(),
            expected: CommercialStorageFailureCode.secureStoreInvalidated,
          ),
        ]) {
      final initializer = CommercialDatabaseInitializer(
        paths: paths,
        keyStore: scenario.keyStore,
        markerStore: markerStore,
      );
      await expectLater(
        initializer.initialize(
          context: CommercialProvisioningContext.explicitFirstInitialization,
        ),
        throwsA(
          isA<CommercialStorageException>().having(
            (error) => error.code,
            'code',
            scenario.expected,
          ),
        ),
      );
      expect(await paths.finalDatabase.readAsBytes(), orderedEquals(before));
    }

    final unreadableRoot = Directory(
      '${support.path}${Platform.pathSeparator}b2-device-unreadable',
    );
    if (await unreadableRoot.exists()) {
      await unreadableRoot.delete(recursive: true);
    }
    await unreadableRoot.create(recursive: true);
    final unreadablePaths = CommercialDatabasePaths.fromApplicationSupport(
      unreadableRoot,
    );
    final unreadableMarkerStore = CommercialProvisioningMarkerStore(
      unreadablePaths.provisioningMarker,
    );
    await unreadableMarkerStore.write(completedAfterPromotion);
    await unreadablePaths.directory.create(recursive: true);
    await unreadablePaths.finalDatabase.writeAsBytes(
      const <int>[],
      flush: true,
    );
    await _expectInitializerFailure(
      paths: unreadablePaths,
      keyStore: keyStore,
      markerStore: unreadableMarkerStore,
      expected: CommercialStorageFailureCode.databaseWrongKeyOrUnreadable,
    );
    expect(await unreadablePaths.finalDatabase.exists(), isTrue);
    expect(await unreadablePaths.finalDatabase.length(), 0);

    expect(_contains(before, utf8.encode('SQLite format 3')), isFalse);
    expect(
      _contains(before, utf8.encode('commercial_storage_metadata')),
      isFalse,
    );
  });
}

Future<void> _expectPragma(
  CommercialDatabase database,
  String name,
  String expected,
) async {
  final row = await database.customSelect('PRAGMA $name').getSingle();
  expect(
    row.data.values.single.toString().toLowerCase(),
    expected.toLowerCase(),
  );
}

Future<CommercialStorageException> _expectInitializerFailure({
  required CommercialDatabasePaths paths,
  required CommercialDatabaseKeyStore keyStore,
  required CommercialProvisioningMarkerStore markerStore,
  required CommercialStorageFailureCode expected,
}) async {
  final initializer = CommercialDatabaseInitializer(
    paths: paths,
    keyStore: keyStore,
    markerStore: markerStore,
  );
  try {
    await initializer.initialize(
      context: CommercialProvisioningContext.explicitFirstInitialization,
    );
    fail('expected a typed Commercial storage failure');
  } on CommercialStorageException catch (error) {
    expect(error.code, expected);
    return error;
  } finally {
    await initializer.close();
  }
}

String _lowercaseHex(Uint8List bytes) {
  final output = StringBuffer();
  for (final byte in bytes) {
    output.write(byte.toRadixString(16).padLeft(2, '0'));
  }
  return output.toString();
}

bool _contains(List<int> haystack, List<int> needle) {
  if (needle.isEmpty || needle.length > haystack.length) {
    return false;
  }
  for (var offset = 0; offset <= haystack.length - needle.length; offset++) {
    var matches = true;
    for (var index = 0; index < needle.length; index++) {
      if (haystack[offset + index] != needle[index]) {
        matches = false;
        break;
      }
    }
    if (matches) {
      return true;
    }
  }
  return false;
}

final class _MemoryKeyStore implements CommercialDatabaseKeyStore {
  Uint8List? _bytes;
  int writeCount = 0;

  bool get hasKey => _bytes != null;

  @override
  Future<CommercialDatabaseKeyStoreState> read({
    required bool keyExpected,
  }) async {
    final bytes = _bytes;
    if (bytes == null) {
      return keyExpected
          ? const CommercialDatabaseKeyStoreMissing()
          : const CommercialDatabaseKeyStoreUninitialized();
    }
    return CommercialDatabaseKeyStoreValid(
      CommercialDatabaseKeyMaterial.copyFrom(bytes),
    );
  }

  @override
  Future<void> writeInitial(CommercialDatabaseKeyMaterial keyMaterial) async {
    if (_bytes != null) {
      throw StateError('test key already exists');
    }
    keyMaterial.useBytes((bytes) {
      _bytes = Uint8List.fromList(bytes);
    });
    writeCount += 1;
  }
}

enum _FixedKeyStoreState { missing, malformed, unavailable, invalidated }

final class _FixedKeyStore implements CommercialDatabaseKeyStore {
  const _FixedKeyStore.missing() : state = _FixedKeyStoreState.missing;
  const _FixedKeyStore.malformed() : state = _FixedKeyStoreState.malformed;
  const _FixedKeyStore.unavailable() : state = _FixedKeyStoreState.unavailable;
  const _FixedKeyStore.invalidated() : state = _FixedKeyStoreState.invalidated;

  final _FixedKeyStoreState state;

  @override
  Future<CommercialDatabaseKeyStoreState> read({
    required bool keyExpected,
  }) async => switch (state) {
    _FixedKeyStoreState.missing => const CommercialDatabaseKeyStoreMissing(),
    _FixedKeyStoreState.malformed =>
      const CommercialDatabaseKeyStoreMalformed(),
    _FixedKeyStoreState.unavailable =>
      const CommercialDatabaseKeyStoreUnavailable(),
    _FixedKeyStoreState.invalidated =>
      const CommercialDatabaseKeyStoreInvalidated(),
  };

  @override
  Future<void> writeInitial(CommercialDatabaseKeyMaterial keyMaterial) =>
      throw StateError('test fixed key store cannot write');
}

final class _FailFirstOpenCommercialDatabaseOpener
    implements CommercialDatabaseConnectionOpener {
  bool _failed = false;
  final CommercialDatabaseOpener _delegate = const CommercialDatabaseOpener();

  @override
  Future<CommercialDatabase> open({
    required File file,
    required CommercialDatabaseKeyMaterial keyMaterial,
  }) {
    if (!_failed) {
      _failed = true;
      throw const CommercialStorageException(
        CommercialStorageFailureCode.storageUnavailable,
        phase: 'integration-test-interruption',
      );
    }
    return _delegate.open(file: file, keyMaterial: keyMaterial);
  }
}
