import 'dart:io';

import 'package:flutter/foundation.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:integration_test/integration_test.dart';
import 'package:path/path.dart' as p;
import 'package:path_provider/path_provider.dart';
import 'package:traderpro_7c2a_storage_spike/src/database_key.dart';
import 'package:traderpro_7c2a_storage_spike/src/secure_database_key_store.dart';
import 'package:traderpro_7c2a_storage_spike/src/storage_spike_database.dart';

const _expectFresh = bool.fromEnvironment(
  'TRADERPRO_EXPECT_FRESH_SECURE_STORE',
  defaultValue: true,
);
const _markerPrefix = 'TRADERPRO_7C2B1_SECURE_STORE_BOOTSTRAP';

void main() {
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();

  for (final strategy
      in <({String name, AndroidOptions options, bool expectedFreshSuccess})>[
        (
          name: 'A',
          options: FlutterSecureDatabaseKeyStore.androidOptions,
          expectedFreshSuccess: true,
        ),
        (
          name: 'B',
          options: FlutterSecureDatabaseKeyStore.backupMigrationAndroidOptions,
          expectedFreshSuccess: true,
        ),
      ]) {
    testWidgets(
      'strategy ${strategy.name} exercises secure bootstrap and fail-closed states',
      (_) async {
        final supportDirectory = await getApplicationSupportDirectory();
        final file = File(
          p.join(
            supportDirectory.path,
            'secure-store-strategy-${strategy.name.toLowerCase()}.db',
          ),
        );
        final store = FlutterSecureDatabaseKeyStore(options: strategy.options);
        final controller = DatabaseKeyController(store);

        if (!_expectFresh) {
          if (!strategy.expectedFreshSuccess) {
            expect(await file.exists(), isFalse);
            return;
          }
          expect(await file.exists(), isTrue);
          final key = await controller.loadExisting();
          final reopened = StorageSpikeDatabase.open(file: file, keyHex: key);
          expect(
            (await reopened.readMarkers()).single.marker,
            '$_markerPrefix:${strategy.name}',
          );
          await reopened.close();
          // The process ID is evidence only and must not be copied into the
          // committed assessment.
          debugPrint(
            'TASK7C2B1_SECURE_STORE_REOPEN='
            '${strategy.name}:pid=$pid:success=true',
          );
          return;
        }

        expect(await file.exists(), isFalse);
        if (!strategy.expectedFreshSuccess) {
          await expectLater(store.read(), throwsA(anything));
          expect(await file.exists(), isFalse);
          debugPrint(
            'TASK7C2B1_SECURE_STORE_FRESH='
            '${strategy.name}:success=false',
          );
          return;
        }

        // A true first install has no value, no algorithm markers, and no DB.
        expect(await store.read(), isNull);
        final key = await controller.provisionNewDatabase(
          databaseExists: await file.exists(),
        );
        final created = StorageSpikeDatabase.open(file: file, keyHex: key);
        await created.insertMarker('$_markerPrefix:${strategy.name}');
        await created.close();
        final ciphertext = await file.readAsBytes();

        // Normal reopen and an existing correctly configured value.
        final reopenedKey = await FlutterSecureDatabaseKeyStore(
          options: strategy.options,
        ).read();
        expect(reopenedKey, key);
        expect(await controller.loadExisting(), key);

        // A missing value with ciphertext must not provision a replacement.
        await store.delete();
        await expectLater(
          controller.loadExisting(),
          throwsA(isA<MissingDatabaseKeyException>()),
        );
        await expectLater(
          controller.provisionNewDatabase(databaseExists: await file.exists()),
          throwsA(isA<MissingDatabaseKeyException>()),
        );
        expect(await file.readAsBytes(), ciphertext);

        // A malformed value with ciphertext must also fail closed.
        const rawStorage = FlutterSecureStorage();
        await rawStorage.write(
          key: FlutterSecureDatabaseKeyStore.entryName,
          value: 'malformed',
          aOptions: strategy.options,
        );
        await expectLater(
          controller.loadExisting(),
          throwsA(isA<InvalidDatabaseKeyException>()),
        );
        expect(await file.readAsBytes(), ciphertext);

        // Restore the known valid value without regenerating it, then verify it.
        await store.write(key);
        expect(await controller.loadExisting(), key);

        // A secure-store exception propagates without touching ciphertext.
        await expectLater(
          DatabaseKeyController(_UnavailableKeyStore()).loadExisting(),
          throwsStateError,
        );
        expect(await file.readAsBytes(), ciphertext);
        debugPrint(
          'TASK7C2B1_SECURE_STORE_FRESH='
          '${strategy.name}:success=true:engine=${activeEncryptionEngine()}',
        );
      },
      timeout: const Timeout(Duration(minutes: 3)),
    );
  }
}

final class _UnavailableKeyStore implements DatabaseKeyStore {
  @override
  Future<void> delete() => throw UnsupportedError('delete is not expected');

  @override
  Future<String?> read() => throw StateError('synthetic secure-store outage');

  @override
  Future<void> write(String keyHex) =>
      throw UnsupportedError('write is not expected');
}
