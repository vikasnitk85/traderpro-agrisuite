import 'dart:convert';
import 'dart:io';

import 'package:flutter_test/flutter_test.dart';
import 'package:sqlite3/sqlite3.dart' as sqlite;
import 'package:traderpro_agrisuite_mobile/core/security/commercial_database_key_material.dart';
import 'package:traderpro_agrisuite_mobile/core/security/commercial_provisioning_marker.dart';
import 'package:traderpro_agrisuite_mobile/core/security/commercial_storage_failure.dart';
import 'package:traderpro_agrisuite_mobile/infrastructure/database/commercial/commercial_database.dart';
import 'package:traderpro_agrisuite_mobile/infrastructure/database/commercial/commercial_database_opener.dart';

void main() {
  late Directory directory;
  late File databaseFile;
  late CommercialDatabaseKeyMaterial key;
  const opener = CommercialDatabaseOpener();
  final marker = CommercialProvisioningMarker(
    version: 1,
    generation: '019fad0f-2d6a-7000-8000-000000000101',
    installationId: '019fad0f-2d6a-7000-8000-000000000102',
    databaseInstanceId: '019fad0f-2d6a-7000-8000-000000000103',
    state: CommercialProvisioningMarkerState.preparing,
  );

  setUp(() async {
    directory = await Directory.systemTemp.createTemp('traderpro-b2-db-');
    databaseFile = File('${directory.path}/commercial.sqlite3');
    key = CommercialDatabaseKeyMaterial.copyFrom(
      List<int>.generate(32, (i) => i + 1),
    );
  });

  tearDown(() async {
    key.dispose();
    if (await directory.exists()) {
      await directory.delete(recursive: true);
    }
  });

  test(
    'encrypted schema 1 creates, verifies, checkpoints, and reopens',
    () async {
      var database = await opener.open(file: databaseFile, keyMaterial: key);
      await database.ensureFoundationMetadata(
        marker: marker,
        createdAtUtcMicros: 1786156200000000,
      );
      await database.verifyFoundationMetadata(marker);

      final rows = await database
          .select(database.commercialStorageMetadata)
          .get();
      expect(rows, hasLength(1));
      expect(rows.single.singletonId, 1);
      expect(rows.single.schemaContractVersion, 1);
      expect(rows.single.storageContractVersion, 1);
      expect(rows.single.installationId, marker.installationId);
      expect(rows.single.databaseInstanceId, marker.databaseInstanceId);
      expect(rows.single.keyAliasVersion, 1);
      expect(rows.single.createdAtUtcMicros, 1786156200000000);

      expect(await _pragma(database, 'cipher'), 'chacha20');
      expect((await _pragma(database, 'legacy')).toString(), '0');
      expect((await _pragma(database, 'kdf_iter')).toString(), '64007');
      expect(
        (await _pragma(database, 'plaintext_header_size')).toString(),
        '0',
      );
      expect((await _pragma(database, 'hmac_check')).toString(), '1');
      expect((await _pragma(database, 'mc_legacy_wal')).toString(), '0');
      expect((await _pragma(database, 'page_size')).toString(), '4096');
      expect(await _pragma(database, 'foreign_keys'), 1);
      expect(await _pragma(database, 'temp_store'), 2);
      expect(await _pragma(database, 'journal_mode'), 'wal');
      expect(
        (await database.customSelect('SELECT sqlite_version()').getSingle())
            .read<String>('sqlite_version()'),
        '3.53.3',
      );

      await database.checkpointWal();
      await database.close();
      database = await opener.open(file: databaseFile, keyMaterial: key);
      await database.verifyFoundationMetadata(marker);
      await database.close();
    },
  );

  test(
    'wrong and unkeyed access are refused without plaintext fallback',
    () async {
      final database = await opener.open(file: databaseFile, keyMaterial: key);
      await database.ensureFoundationMetadata(
        marker: marker,
        createdAtUtcMicros: 1786156200000000,
      );
      await database.checkpointWal();
      await database.close();
      final before = await databaseFile.readAsBytes();

      final wrongKey = CommercialDatabaseKeyMaterial.copyFrom(
        List.filled(32, 9),
      );
      try {
        await expectLater(
          opener.open(file: databaseFile, keyMaterial: wrongKey),
          throwsA(
            isA<CommercialStorageException>().having(
              (error) => error.code,
              'code',
              CommercialStorageFailureCode.databaseWrongKeyOrUnreadable,
            ),
          ),
        );
      } finally {
        wrongKey.dispose();
      }

      final unkeyed = sqlite.sqlite3.open(databaseFile.path);
      try {
        expect(
          () => unkeyed.select('SELECT count(*) FROM sqlite_master'),
          throwsA(isA<sqlite.SqliteException>()),
        );
      } finally {
        unkeyed.close();
      }
      expect(await databaseFile.readAsBytes(), orderedEquals(before));
    },
  );

  test('metadata update and delete are rejected by immutable guards', () async {
    final database = await opener.open(file: databaseFile, keyMaterial: key);
    await database.ensureFoundationMetadata(
      marker: marker,
      createdAtUtcMicros: 1786156200000000,
    );
    await expectLater(
      database.customStatement(
        'UPDATE commercial_storage_metadata SET key_alias_version = 2',
      ),
      throwsA(isNotNull),
    );
    await expectLater(
      database.customStatement('DELETE FROM commercial_storage_metadata'),
      throwsA(isNotNull),
    );
    await database.verifyFoundationMetadata(marker);
    await database.close();
  });

  test('transaction rollback leaves no partial schema artifact', () async {
    final database = await opener.open(file: databaseFile, keyMaterial: key);
    await database.ensureFoundationMetadata(
      marker: marker,
      createdAtUtcMicros: 1786156200000000,
    );
    await expectLater(
      database.transaction(() async {
        await database.customStatement(
          'CREATE TABLE rollback_probe (id INTEGER)',
        );
        throw StateError('synthetic rollback');
      }),
      throwsStateError,
    );
    final probe = await database
        .customSelect(
          "SELECT count(*) AS count FROM sqlite_master WHERE name = 'rollback_probe'",
        )
        .getSingle();
    expect(probe.read<int>('count'), 0);
    await database.close();
  });

  test(
    'main database and sidecars contain no configured plaintext markers',
    () async {
      final database = await opener.open(file: databaseFile, keyMaterial: key);
      await database.ensureFoundationMetadata(
        marker: marker,
        createdAtUtcMicros: 1786156200000000,
      );
      await database.checkpointWal();
      await database.close();

      final markers = <List<int>>[
        utf8.encode('SQLite format 3'),
        utf8.encode(marker.installationId),
        utf8.encode(marker.databaseInstanceId),
        utf8.encode('commercial_storage_metadata'),
      ];
      for (final file in <File>[
        databaseFile,
        File('${databaseFile.path}-wal'),
        File('${databaseFile.path}-shm'),
        File('${databaseFile.path}-journal'),
      ]) {
        if (!await file.exists()) {
          continue;
        }
        final bytes = await file.readAsBytes();
        for (final plaintext in markers) {
          expect(_containsBytes(bytes, plaintext), isFalse, reason: file.path);
        }
      }
    },
  );

  test(
    'controlled corruption is classified without deleting ciphertext',
    () async {
      final database = await opener.open(file: databaseFile, keyMaterial: key);
      await database.ensureFoundationMetadata(
        marker: marker,
        createdAtUtcMicros: 1786156200000000,
      );
      await database.checkpointWal();
      await database.close();
      final bytes = await databaseFile.readAsBytes();
      bytes[100] ^= 0x5a;
      await databaseFile.writeAsBytes(bytes, flush: true);
      final corrupted = await databaseFile.readAsBytes();

      await expectLater(
        opener.open(file: databaseFile, keyMaterial: key),
        throwsA(isA<CommercialStorageException>()),
      );
      expect(await databaseFile.readAsBytes(), orderedEquals(corrupted));
    },
  );
}

Future<Object?> _pragma(CommercialDatabase database, String name) async {
  final row = await database.customSelect('PRAGMA $name').getSingle();
  return row.data.values.single;
}

bool _containsBytes(List<int> haystack, List<int> needle) {
  if (needle.isEmpty || needle.length > haystack.length) {
    return false;
  }
  for (var start = 0; start <= haystack.length - needle.length; start++) {
    var matches = true;
    for (var offset = 0; offset < needle.length; offset++) {
      if (haystack[start + offset] != needle[offset]) {
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
