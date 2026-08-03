import 'dart:convert';
import 'dart:io';

import 'package:flutter/foundation.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:integration_test/integration_test.dart';
import 'package:path/path.dart' as p;
import 'package:path_provider/path_provider.dart';
import 'package:sqlite3/sqlite3.dart' as sqlite;
import 'package:traderpro_7c2a_storage_spike/src/database_key.dart';
import 'package:traderpro_7c2a_storage_spike/src/secure_database_key_store.dart';
import 'package:traderpro_7c2a_storage_spike/src/storage_spike_database.dart';

const _artifactMarker = 'TRADERPRO_7C2A_DEVICE_ARTIFACT_MARKER';
const _rollbackMarker = 'TRADERPRO_7C2A_DEVICE_ROLLBACK_MARKER';
const _restartMarker = 'TRADERPRO_7C2A_DEVICE_REOPEN_MARKER';
const _testTimeout = Timeout(Duration(minutes: 3));

void main() {
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();

  late String engine;
  late Directory supportDirectory;
  late Directory evidenceDirectory;

  setUpAll(() async {
    engine = activeEncryptionEngine();
    supportDirectory = await getApplicationSupportDirectory();
    final externalDirectory = await getExternalStorageDirectory();
    if (externalDirectory == null) {
      throw StateError('Android external app storage is unavailable.');
    }
    evidenceDirectory = Directory(
      p.join(externalDirectory.path, 'task7c2a', engine),
    );
    if (await evidenceDirectory.exists()) {
      await evidenceDirectory.delete(recursive: true);
    }
    await evidenceDirectory.create(recursive: true);
    debugPrint('TASK7C2A_DEVICE_ENGINE=$engine');
  });

  testWidgets(
    'real secure store handles correct, missing, malformed, and deleted values',
    (_) async {
      final store = FlutterSecureDatabaseKeyStore();
      final controller = DatabaseKeyController(store);
      const rawStorage = FlutterSecureStorage();
      await store.delete();

      await expectLater(
        controller.loadExisting(),
        throwsA(isA<MissingDatabaseKeyException>()),
      );

      final key = generateDatabaseKey();
      await store.write(key);
      expect(await store.read(), key);
      expect(await controller.loadExisting(), key);

      await rawStorage.write(
        key: FlutterSecureDatabaseKeyStore.entryName,
        value: 'not-a-valid-database-key',
        aOptions: FlutterSecureDatabaseKeyStore.androidOptions,
      );
      await expectLater(
        controller.loadExisting(),
        throwsA(isA<InvalidDatabaseKeyException>()),
      );

      await store.delete();
      expect(await store.read(), isNull);
      await expectLater(
        controller.loadExisting(),
        throwsA(isA<MissingDatabaseKeyException>()),
      );
    },
    timeout: _testTimeout,
  );

  testWidgets(
    'database and secure-store failure paths retain ciphertext and fail closed',
    (_) async {
      final file = File(p.join(supportDirectory.path, 'key-matrix-$engine.db'));
      await _deleteDatabaseFiles(file);
      final store = FlutterSecureDatabaseKeyStore();
      final controller = DatabaseKeyController(store);
      const rawStorage = FlutterSecureStorage();
      await store.delete();

      final correctKey = await controller.provisionNewDatabase();
      final created = StorageSpikeDatabase.open(file: file, keyHex: correctKey);
      await created.insertMarker(_restartMarker);
      await created.close();
      expect(await file.exists(), isTrue);

      final reopened = StorageSpikeDatabase.open(
        file: file,
        keyHex: correctKey,
      );
      expect((await reopened.readMarkers()).single.marker, _restartMarker);
      await reopened.close();

      final wrongKeyDatabase = StorageSpikeDatabase.open(
        file: file,
        keyHex: generateDatabaseKey(),
      );
      await expectLater(wrongKeyDatabase.readMarkers(), throwsA(anything));
      await _closeAfterExpectedOpenFailure(wrongKeyDatabase);
      expect(await file.exists(), isTrue);

      await store.delete();
      await expectLater(
        controller.loadExisting(),
        throwsA(isA<MissingDatabaseKeyException>()),
      );
      expect(await file.exists(), isTrue);

      await rawStorage.write(
        key: FlutterSecureDatabaseKeyStore.entryName,
        value: 'malformed',
        aOptions: FlutterSecureDatabaseKeyStore.androidOptions,
      );
      await expectLater(
        controller.loadExisting(),
        throwsA(isA<InvalidDatabaseKeyException>()),
      );
      expect(await file.exists(), isTrue);

      await expectLater(
        DatabaseKeyController(_UnavailableKeyStore()).loadExisting(),
        throwsA(isA<StateError>()),
      );
      expect(await file.exists(), isTrue);

      final unkeyed = sqlite.sqlite3.open(file.path);
      try {
        expect(
          () => unkeyed.select('SELECT count(*) FROM sqlite_master'),
          throwsA(isA<sqlite.SqliteException>()),
        );
      } finally {
        unkeyed.close();
      }

      await store.write(correctKey);
      final recovered = StorageSpikeDatabase.open(
        file: file,
        keyHex: await controller.loadExisting(),
      );
      expect((await recovered.readMarkers()).single.marker, _restartMarker);
      await recovered.close();
      await store.delete();
    },
    timeout: _testTimeout,
  );

  testWidgets(
    'Drift migrates, rolls back, and opens through its background isolate',
    (_) async {
      final migrationFile = File(
        p.join(supportDirectory.path, 'migration-$engine.db'),
      );
      await _deleteDatabaseFiles(migrationFile);
      final migrationKey = generateDatabaseKey();
      createEncryptedVersionOneDatabase(
        file: migrationFile,
        keyHex: migrationKey,
      );

      final migrated = StorageSpikeDatabase.open(
        file: migrationFile,
        keyHex: migrationKey,
      );
      await migrated.customSelect('SELECT * FROM migration_evidence').get();
      final version = await migrated
          .customSelect('PRAGMA user_version')
          .getSingle();
      expect(version.read<int>('user_version'), 2);
      await migrated.close();

      final rollbackFile = File(
        p.join(supportDirectory.path, 'transaction-$engine.db'),
      );
      await _deleteDatabaseFiles(rollbackFile);
      final rollbackDatabase = StorageSpikeDatabase.open(
        file: rollbackFile,
        keyHex: generateDatabaseKey(),
      );
      await expectLater(
        rollbackDatabase.transaction(() async {
          await rollbackDatabase.insertMarker(_rollbackMarker);
          throw StateError('synthetic rollback trigger');
        }),
        throwsStateError,
      );
      expect(await rollbackDatabase.readMarkers(), isEmpty);
      await rollbackDatabase.close();
    },
    timeout: _testTimeout,
  );

  testWidgets(
    'WAL checkpoint preserves encrypted data across close and reopen',
    (_) async {
      final file = File(p.join(supportDirectory.path, 'wal-reopen-$engine.db'));
      await _deleteDatabaseFiles(file);
      final key = generateDatabaseKey();
      final database = StorageSpikeDatabase.open(file: file, keyHex: key);
      await database.insertMarker(_restartMarker);
      final checkpoint = await database
          .customSelect('PRAGMA wal_checkpoint(FULL)')
          .getSingle();
      expect(checkpoint.read<int>('busy'), 0);
      await database.close();

      final reopened = StorageSpikeDatabase.open(file: file, keyHex: key);
      expect((await reopened.readMarkers()).single.marker, _restartMarker);
      await reopened.close();
    },
    timeout: _testTimeout,
  );

  testWidgets(
    'available Android database artifacts contain no known plaintext',
    (_) async {
      final workDirectory = Directory(
        p.join(supportDirectory.path, 'artifact-work-$engine'),
      );
      if (await workDirectory.exists()) {
        await workDirectory.delete(recursive: true);
      }
      await workDirectory.create(recursive: true);
      final key = generateDatabaseKey();

      final walFile = File(p.join(workDirectory.path, 'wal.db'));
      final walDatabase = StorageSpikeDatabase.open(file: walFile, keyHex: key);
      await walDatabase.insertMarker(_artifactMarker);
      await walDatabase.customSelect('PRAGMA wal_checkpoint(PASSIVE)').get();
      final capturedFiles = <File>[
        await walFile.copy(p.join(evidenceDirectory.path, 'wal-main.db')),
        await _copyRequired(
          File('${walFile.path}-wal'),
          File(p.join(evidenceDirectory.path, 'wal.db-wal')),
        ),
        await _copyRequired(
          File('${walFile.path}-shm'),
          File(p.join(evidenceDirectory.path, 'wal.db-shm')),
        ),
      ];
      await walDatabase.close();

      final rollbackFile = File(p.join(workDirectory.path, 'rollback.db'));
      final rawDatabase = sqlite.sqlite3.open(rollbackFile.path);
      try {
        configureEncryptedDatabase(rawDatabase, key);
        final journalMode = rawDatabase
            .select('PRAGMA journal_mode = DELETE')
            .single
            .values
            .single
            .toString();
        expect(journalMode, 'delete');
        rawDatabase.execute(
          'CREATE TABLE evidence (id INTEGER PRIMARY KEY, marker TEXT NOT NULL)',
        );
        final committedPayload =
            '$_artifactMarker:${List.filled(1024, 'A').join()}';
        rawDatabase.execute('BEGIN IMMEDIATE');
        for (var index = 0; index < 128; index++) {
          rawDatabase.execute(
            'INSERT INTO evidence (id, marker) VALUES (?, ?)',
            [index + 1, '$committedPayload:$index'],
          );
        }
        rawDatabase.execute('COMMIT');
        rawDatabase.execute('BEGIN IMMEDIATE');
        rawDatabase.execute('UPDATE evidence SET marker = ?', [
          '$_rollbackMarker:${List.filled(1024, 'B').join()}',
        ]);
        capturedFiles.add(
          await rollbackFile.copy(
            p.join(evidenceDirectory.path, 'rollback-main.db'),
          ),
        );
        final rollbackJournal = File('${rollbackFile.path}-journal');
        if (await rollbackJournal.exists()) {
          capturedFiles.add(
            await rollbackJournal.copy(
              p.join(evidenceDirectory.path, 'rollback.db-journal'),
            ),
          );
        } else {
          debugPrint(
            'TASK7C2A_ROLLBACK_JOURNAL=not-produced '
            'engine=$engine mode=$journalMode',
          );
        }
        rawDatabase.execute('ROLLBACK');
      } finally {
        rawDatabase.close();
      }

      final needles = <String>[
        'SQLite format 3',
        _artifactMarker,
        _rollbackMarker,
        'synthetic_markers',
        'migration_evidence',
      ];
      final scan = <String, Object?>{
        'engine': engine,
        'rollbackJournalCaptured': capturedFiles.any(
          (file) => p.basename(file.path) == 'rollback.db-journal',
        ),
        'files': <Map<String, Object?>>[],
        'plaintextMatches': 0,
      };
      var matchCount = 0;
      for (final file in capturedFiles) {
        final bytes = await file.readAsBytes();
        final matches = needles
            .where((needle) => _containsBytes(bytes, utf8.encode(needle)))
            .toList();
        matchCount += matches.length;
        (scan['files']! as List<Map<String, Object?>>).add({
          'name': p.basename(file.path),
          'bytes': bytes.length,
          'matches': matches,
        });
      }
      scan['plaintextMatches'] = matchCount;
      await File(
        p.join(evidenceDirectory.path, 'scan-results.json'),
      ).writeAsString(
        const JsonEncoder.withIndent('  ').convert(scan),
        flush: true,
      );
      debugPrint('TASK7C2A_ARTIFACT_SCAN_JSON=${jsonEncode(scan)}');
      expect(matchCount, 0);
      expect(
        await workDirectory
            .list()
            .where((entry) => p.basename(entry.path).contains('-journal'))
            .isEmpty,
        isTrue,
      );
    },
    timeout: _testTimeout,
  );

  testWidgets(
    'records a comparable encrypted create, write, checkpoint, and reopen workload',
    (_) async {
      const rows = 1000;
      final file = File(p.join(supportDirectory.path, 'benchmark-$engine.db'));
      await _deleteDatabaseFiles(file);
      final key = generateDatabaseKey();
      final totalWatch = Stopwatch()..start();

      final openWatch = Stopwatch()..start();
      final database = StorageSpikeDatabase.open(file: file, keyHex: key);
      await database.customSelect('SELECT 1').getSingle();
      openWatch.stop();

      final writeWatch = Stopwatch()..start();
      await database.transaction(() async {
        for (var index = 0; index < rows; index++) {
          await database.insertMarker('SYNTHETIC_BENCHMARK_$index');
        }
      });
      writeWatch.stop();

      final checkpointWatch = Stopwatch()..start();
      final checkpoint = await database
          .customSelect('PRAGMA wal_checkpoint(FULL)')
          .getSingle();
      checkpointWatch.stop();
      expect(checkpoint.read<int>('busy'), 0);
      await database.close();

      final reopenWatch = Stopwatch()..start();
      final reopened = StorageSpikeDatabase.open(file: file, keyHex: key);
      final countRow = await reopened
          .customSelect('SELECT count(*) AS row_count FROM synthetic_markers')
          .getSingle();
      reopenWatch.stop();
      expect(countRow.read<int>('row_count'), rows);
      await reopened.close();
      totalWatch.stop();

      final result = <String, Object>{
        'engine': engine,
        'rows': rows,
        'openMs': openWatch.elapsedMilliseconds,
        'writeTransactionMs': writeWatch.elapsedMilliseconds,
        'walCheckpointMs': checkpointWatch.elapsedMilliseconds,
        'reopenAndCountMs': reopenWatch.elapsedMilliseconds,
        'totalMs': totalWatch.elapsedMilliseconds,
        'databaseBytes': await file.length(),
      };
      final resultJson = jsonEncode(result);
      debugPrint('TASK7C2A_PERF_JSON=$resultJson');
      await File(
        p.join(evidenceDirectory.path, 'performance.json'),
      ).writeAsString(
        const JsonEncoder.withIndent('  ').convert(result),
        flush: true,
      );
    },
    timeout: _testTimeout,
  );
}

Future<void> _deleteDatabaseFiles(File file) async {
  for (final path in [file.path, '${file.path}-wal', '${file.path}-shm']) {
    final candidate = File(path);
    if (await candidate.exists()) {
      await candidate.delete();
    }
  }
}

Future<File> _copyRequired(File source, File destination) async {
  if (!await source.exists()) {
    throw StateError('Expected SQLite evidence file was not created: $source');
  }
  return source.copy(destination.path);
}

bool _containsBytes(Uint8List haystack, List<int> needle) {
  if (needle.isEmpty || needle.length > haystack.length) {
    return false;
  }
  for (var start = 0; start <= haystack.length - needle.length; start++) {
    var matched = true;
    for (var offset = 0; offset < needle.length; offset++) {
      if (haystack[start + offset] != needle[offset]) {
        matched = false;
        break;
      }
    }
    if (matched) {
      return true;
    }
  }
  return false;
}

Future<void> _closeAfterExpectedOpenFailure(
  StorageSpikeDatabase database,
) async {
  try {
    await database.close();
  } on Object {
    // Initialization failed as expected, so there may be no isolate to close.
  }
}

final class _UnavailableKeyStore implements DatabaseKeyStore {
  @override
  Future<String?> read() => throw StateError('synthetic secure-store outage');

  @override
  Future<void> write(String keyHex) =>
      throw UnsupportedError('write is not expected');

  @override
  Future<void> delete() => throw UnsupportedError('delete is not expected');
}
