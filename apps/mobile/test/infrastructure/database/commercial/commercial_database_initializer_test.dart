import 'dart:io';
import 'dart:typed_data';

import 'package:crypto/crypto.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:traderpro_agrisuite_mobile/core/security/commercial_database_key_material.dart';
import 'package:traderpro_agrisuite_mobile/core/security/commercial_database_key_store.dart';
import 'package:traderpro_agrisuite_mobile/core/security/commercial_provisioning_marker.dart';
import 'package:traderpro_agrisuite_mobile/core/security/commercial_storage_failure.dart';
import 'package:traderpro_agrisuite_mobile/core/security/commercial_storage_state_machine.dart';
import 'package:traderpro_agrisuite_mobile/infrastructure/database/commercial/commercial_database.dart';
import 'package:traderpro_agrisuite_mobile/infrastructure/database/commercial/commercial_database_initializer.dart';
import 'package:traderpro_agrisuite_mobile/infrastructure/database/commercial/commercial_database_opener.dart';
import 'package:traderpro_agrisuite_mobile/infrastructure/database/commercial/commercial_database_paths.dart';
import 'package:traderpro_agrisuite_mobile/infrastructure/storage/commercial_provisioning_marker_store.dart';

void main() {
  late Directory applicationSupport;
  late CommercialDatabasePaths paths;
  late CommercialProvisioningMarkerStore markerStore;
  late _MemoryKeyStore keyStore;
  late _SequenceUuidGenerator uuids;

  setUp(() async {
    applicationSupport = await Directory.systemTemp.createTemp(
      'traderpro-b2-initializer-',
    );
    paths = CommercialDatabasePaths.fromApplicationSupport(applicationSupport);
    markerStore = CommercialProvisioningMarkerStore(paths.provisioningMarker);
    keyStore = _MemoryKeyStore();
    uuids = _SequenceUuidGenerator();
  });

  tearDown(() async {
    keyStore.dispose();
    if (await applicationSupport.exists()) {
      await applicationSupport.delete(recursive: true);
    }
  });

  test('fresh explicit initialization provisions exactly once', () async {
    final initializer = _initializer(
      paths: paths,
      markerStore: markerStore,
      keyStore: keyStore,
      uuids: uuids,
    );
    final database = await initializer.initialize(
      context: CommercialProvisioningContext.explicitFirstInitialization,
    );

    expect(keyStore.writeCount, 1);
    expect(await paths.finalDatabase.exists(), isTrue);
    expect(await paths.initializingDatabase.exists(), isFalse);
    expect(
      (await markerStore.read())?.state,
      CommercialProvisioningMarkerState.complete,
    );
    expect(
      await database.select(database.commercialStorageMetadata).get(),
      hasLength(1),
    );
    await initializer.close();

    final reopen = _initializer(
      paths: paths,
      markerStore: markerStore,
      keyStore: keyStore,
      uuids: uuids,
    );
    await reopen.initialize(
      context: CommercialProvisioningContext.normalStartup,
    );
    expect(keyStore.writeCount, 1);
    await reopen.close();
  });

  test('concurrent callers share one database initialization', () async {
    final initializer = _initializer(
      paths: paths,
      markerStore: markerStore,
      keyStore: keyStore,
      uuids: uuids,
    );
    final first = initializer.initialize(
      context: CommercialProvisioningContext.explicitFirstInitialization,
    );
    final second = initializer.initialize(
      context: CommercialProvisioningContext.explicitFirstInitialization,
    );
    final databases = await Future.wait(<Future<CommercialDatabase>>[
      first,
      second,
    ]);
    expect(identical(databases[0], databases[1]), isTrue);
    expect(keyStore.writeCount, 1);
    await initializer.close();
  });

  test(
    'stored key plus failed DB creation resumes deterministically',
    () async {
      final failing = _initializer(
        paths: paths,
        markerStore: markerStore,
        keyStore: keyStore,
        uuids: uuids,
        opener: const _FailingOpener(),
      );
      await expectLater(
        failing.initialize(
          context: CommercialProvisioningContext.explicitFirstInitialization,
        ),
        throwsA(isA<CommercialStorageException>()),
      );
      expect(keyStore.writeCount, 1);
      expect(
        (await markerStore.read())?.state,
        CommercialProvisioningMarkerState.preparing,
      );
      expect(await paths.finalDatabase.exists(), isFalse);

      final resumed = _initializer(
        paths: paths,
        markerStore: markerStore,
        keyStore: keyStore,
        uuids: uuids,
      );
      await resumed.initialize(
        context: CommercialProvisioningContext.normalStartup,
      );
      expect(keyStore.writeCount, 1);
      expect(await paths.finalDatabase.exists(), isTrue);
      await resumed.close();
    },
  );

  test(
    'missing key with final ciphertext fails closed byte-for-byte',
    () async {
      final initializer = _initializer(
        paths: paths,
        markerStore: markerStore,
        keyStore: keyStore,
        uuids: uuids,
      );
      await initializer.initialize(
        context: CommercialProvisioningContext.explicitFirstInitialization,
      );
      await initializer.close();
      final before = sha256.convert(await paths.finalDatabase.readAsBytes());
      keyStore.dispose();

      final reopen = _initializer(
        paths: paths,
        markerStore: markerStore,
        keyStore: keyStore,
        uuids: uuids,
      );
      await expectLater(
        reopen.initialize(context: CommercialProvisioningContext.normalStartup),
        throwsA(
          isA<CommercialStorageException>().having(
            (error) => error.code,
            'code',
            CommercialStorageFailureCode.secureStoreMissing,
          ),
        ),
      );
      expect(sha256.convert(await paths.finalDatabase.readAsBytes()), before);
    },
  );

  test('valid key without a preparing marker never creates a DB', () async {
    keyStore.setBytes(List.filled(32, 8));
    final initializer = _initializer(
      paths: paths,
      markerStore: markerStore,
      keyStore: keyStore,
      uuids: uuids,
    );
    await expectLater(
      initializer.initialize(
        context: CommercialProvisioningContext.explicitFirstInitialization,
      ),
      throwsA(
        isA<CommercialStorageException>().having(
          (error) => error.code,
          'code',
          CommercialStorageFailureCode.databaseInitializationIncomplete,
        ),
      ),
    );
    expect(await paths.finalDatabase.exists(), isFalse);
  });

  test('valid generation-matching temp DB resumes and promotes', () async {
    final marker = _marker(CommercialProvisioningMarkerState.preparing);
    await markerStore.write(marker);
    keyStore.setBytes(List<int>.generate(32, (i) => i + 1));
    final key = keyStore.copyKey();
    final partial = await const CommercialDatabaseOpener().open(
      file: paths.initializingDatabase,
      keyMaterial: key,
    );
    await partial.ensureFoundationMetadata(
      marker: marker,
      createdAtUtcMicros: 1786156200000000,
    );
    await partial.checkpointWal();
    await partial.close();
    key.dispose();

    final initializer = _initializer(
      paths: paths,
      markerStore: markerStore,
      keyStore: keyStore,
      uuids: uuids,
    );
    await initializer.initialize(
      context: CommercialProvisioningContext.normalStartup,
    );
    expect(await paths.initializingDatabase.exists(), isFalse);
    expect(await paths.finalDatabase.exists(), isTrue);
    expect(
      (await markerStore.read())?.state,
      CommercialProvisioningMarkerState.complete,
    );
    await initializer.close();
  });

  test('zero-length final artifact is preserved and blocks startup', () async {
    await paths.directory.create(recursive: true);
    await paths.finalDatabase.writeAsBytes(<int>[], flush: true);
    await markerStore.write(
      _marker(CommercialProvisioningMarkerState.complete),
    );
    keyStore.setBytes(List.filled(32, 3));

    final initializer = _initializer(
      paths: paths,
      markerStore: markerStore,
      keyStore: keyStore,
      uuids: uuids,
    );
    await expectLater(
      initializer.initialize(
        context: CommercialProvisioningContext.normalStartup,
      ),
      throwsA(isA<CommercialStorageException>()),
    );
    expect(await paths.finalDatabase.exists(), isTrue);
    expect(await paths.finalDatabase.length(), 0);
  });

  test(
    'promotion-before-marker-completion is verified then completed',
    () async {
      final marker = _marker(CommercialProvisioningMarkerState.preparing);
      await markerStore.write(marker);
      keyStore.setBytes(List<int>.generate(32, (i) => 200 - i));
      final key = keyStore.copyKey();
      final database = await const CommercialDatabaseOpener().open(
        file: paths.finalDatabase,
        keyMaterial: key,
      );
      await database.ensureFoundationMetadata(
        marker: marker,
        createdAtUtcMicros: 1786156200000000,
      );
      await database.checkpointWal();
      await database.close();
      key.dispose();

      final initializer = _initializer(
        paths: paths,
        markerStore: markerStore,
        keyStore: keyStore,
        uuids: uuids,
      );
      await initializer.initialize(
        context: CommercialProvisioningContext.normalStartup,
      );
      expect(
        (await markerStore.read())?.state,
        CommercialProvisioningMarkerState.complete,
      );
      await initializer.close();
    },
  );
}

CommercialDatabaseInitializer _initializer({
  required CommercialDatabasePaths paths,
  required CommercialProvisioningMarkerStore markerStore,
  required _MemoryKeyStore keyStore,
  required _SequenceUuidGenerator uuids,
  CommercialDatabaseConnectionOpener opener = const CommercialDatabaseOpener(),
}) => CommercialDatabaseInitializer(
  paths: paths,
  keyStore: keyStore,
  markerStore: markerStore,
  opener: opener,
  keyGenerator: _FixedKeyGenerator(),
  uuidGenerator: uuids,
  clock: () => DateTime.utc(2026, 8, 8, 12),
  coarsePlatform: 'test',
);

CommercialProvisioningMarker _marker(CommercialProvisioningMarkerState state) =>
    CommercialProvisioningMarker(
      version: 1,
      generation: '019fad0f-2d6a-7000-8000-000000000201',
      installationId: '019fad0f-2d6a-7000-8000-000000000202',
      databaseInstanceId: '019fad0f-2d6a-7000-8000-000000000203',
      state: state,
    );

final class _MemoryKeyStore implements CommercialDatabaseKeyStore {
  Uint8List? _bytes;
  int writeCount = 0;

  void setBytes(List<int> bytes) {
    dispose();
    _bytes = Uint8List.fromList(bytes);
  }

  CommercialDatabaseKeyMaterial copyKey() =>
      CommercialDatabaseKeyMaterial.copyFrom(_bytes!);

  void dispose() {
    _bytes?.fillRange(0, _bytes!.length, 0);
    _bytes = null;
  }

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
      throw const CommercialStorageException(
        CommercialStorageFailureCode.databaseInitializationIncomplete,
        phase: 'test-key-overwrite',
      );
    }
    keyMaterial.useBytes((bytes) {
      _bytes = Uint8List.fromList(bytes);
    });
    writeCount++;
  }
}

final class _FixedKeyGenerator implements DatabaseKeyGenerator {
  @override
  Uint8List generate32Bytes() =>
      Uint8List.fromList(List<int>.generate(32, (i) => i + 1));
}

final class _SequenceUuidGenerator implements CommercialStorageUuidGenerator {
  int _next = 300;

  @override
  String generate() {
    final suffix = (_next++).toString().padLeft(12, '0');
    return '019fad0f-2d6a-7000-8000-$suffix';
  }
}

final class _FailingOpener implements CommercialDatabaseConnectionOpener {
  const _FailingOpener();

  @override
  Future<CommercialDatabase> open({
    required File file,
    required CommercialDatabaseKeyMaterial keyMaterial,
  }) => throw const CommercialStorageException(
    CommercialStorageFailureCode.storageUnavailable,
    phase: 'synthetic-open-failure',
  );
}
