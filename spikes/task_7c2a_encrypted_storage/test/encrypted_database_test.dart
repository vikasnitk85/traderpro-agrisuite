import 'dart:io';

import 'package:flutter_test/flutter_test.dart';
import 'package:sqlite3/sqlite3.dart';
import 'package:traderpro_7c2a_storage_spike/src/database_key.dart';
import 'package:traderpro_7c2a_storage_spike/src/storage_spike_database.dart';

void main() {
  late Directory temporaryDirectory;

  setUp(() async {
    temporaryDirectory = await Directory.systemTemp.createTemp(
      'traderpro_7c2a_',
    );
  });

  tearDown(() async {
    if (await temporaryDirectory.exists()) {
      await temporaryDirectory.delete(recursive: true);
    }
  });

  test(
    'encrypted Drift survives close and background-isolate reopen',
    () async {
      final file = File('${temporaryDirectory.path}/restart.db');
      final key = generateDatabaseKey();
      final first = StorageSpikeDatabase.open(file: file, keyHex: key);
      await first.insertMarker('TRADERPRO_7C2A_SYNTHETIC_RESTART_MARKER');
      await first.close();

      final reopened = StorageSpikeDatabase.open(file: file, keyHex: key);
      final rows = await reopened.readMarkers();
      expect(rows.single.marker, 'TRADERPRO_7C2A_SYNTHETIC_RESTART_MARKER');
      final tempStore = await reopened
          .customSelect('PRAGMA temp_store')
          .getSingle();
      expect(tempStore.read<int>('temp_store'), 2);
      await reopened.close();
    },
  );

  test('transaction rollback does not retain the synthetic fact', () async {
    final database = StorageSpikeDatabase.open(
      file: File('${temporaryDirectory.path}/rollback.db'),
      keyHex: generateDatabaseKey(),
    );

    await expectLater(
      database.transaction(() async {
        await database.insertMarker('TRADERPRO_7C2A_SYNTHETIC_ROLLBACK_MARKER');
        throw StateError('force rollback');
      }),
      throwsStateError,
    );
    expect(await database.readMarkers(), isEmpty);
    await database.close();
  });

  test('version-one encrypted database migrates to schema two', () async {
    final file = File('${temporaryDirectory.path}/migration.db');
    final key = generateDatabaseKey();
    createEncryptedVersionOneDatabase(file: file, keyHex: key);

    final migrated = StorageSpikeDatabase.open(file: file, keyHex: key);
    await migrated.customSelect('SELECT * FROM migration_evidence').get();
    final version = await migrated
        .customSelect('PRAGMA user_version')
        .getSingle();
    expect(version.read<int>('user_version'), 2);
    await migrated.close();
  });

  test('wrong key and missing key both fail closed', () async {
    final file = File('${temporaryDirectory.path}/wrong-key.db');
    final correctKey = generateDatabaseKey();
    final database = StorageSpikeDatabase.open(file: file, keyHex: correctKey);
    await database.insertMarker('TRADERPRO_7C2A_SYNTHETIC_WRONG_KEY_MARKER');
    await database.close();

    final wrong = StorageSpikeDatabase.open(
      file: file,
      keyHex: generateDatabaseKey(),
    );
    await expectLater(wrong.readMarkers(), throwsA(anything));
    await wrong.close();

    expect(
      () => StorageSpikeDatabase.open(file: file, keyHex: ''),
      throwsA(isA<InvalidDatabaseKeyException>()),
    );
  });

  test('unkeyed encrypted engine cannot read the encrypted database', () async {
    final file = File('${temporaryDirectory.path}/unkeyed-read.db');
    final database = StorageSpikeDatabase.open(
      file: file,
      keyHex: generateDatabaseKey(),
    );
    await database.insertMarker('TRADERPRO_7C2A_SYNTHETIC_UNKEYED_MARKER');
    await database.close();

    final unkeyedAttempt = sqlite3.open(file.path);
    try {
      expect(
        () => unkeyedAttempt.select('SELECT * FROM sqlite_master'),
        throwsA(isA<SqliteException>()),
      );
    } finally {
      unkeyedAttempt.close();
    }
  });
}
