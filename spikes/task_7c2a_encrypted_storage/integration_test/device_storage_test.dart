import 'dart:convert';
import 'dart:io';
import 'dart:math' as math;

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

      final correctKey = await controller.provisionNewDatabase(
        databaseExists: await file.exists(),
      );
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
    'frozen raw-key configuration self-verifies and rejects textual keying',
    (_) async {
      final rawFile = File(
        p.join(supportDirectory.path, 'raw-configuration-$engine.db'),
      );
      await _deleteDatabaseFiles(rawFile);
      final key = generateDatabaseKey();
      final created = sqlite.sqlite3.open(rawFile.path);
      late final StorageEncryptionEngine detectedEngine;
      try {
        detectedEngine = detectEncryptionEngine(created);
        configureEncryptedDatabase(created, key);
        created.execute(
          'CREATE TABLE configuration_evidence '
          '(id INTEGER PRIMARY KEY, marker TEXT NOT NULL)',
        );
        created.execute(
          'INSERT INTO configuration_evidence(marker) VALUES (?)',
          ['TRADERPRO_7C2B1_RAW_KEY_EVIDENCE'],
        );
        final configuration = readEncryptionConfiguration(
          created,
          detectedEngine,
        );
        debugPrint(
          'TASK7C2B1_DEVICE_CONFIGURATION_JSON=${jsonEncode(configuration)}',
        );
        expect(configuration['engine'], engine);
        expect(configuration['pageSize'].toString(), '4096');
      } finally {
        created.close();
      }

      final reopened = sqlite.sqlite3.open(rawFile.path);
      try {
        configureEncryptedDatabase(reopened, key);
        expect(
          reopened
              .select('SELECT marker FROM configuration_evidence')
              .single['marker'],
          'TRADERPRO_7C2B1_RAW_KEY_EVIDENCE',
        );
      } finally {
        reopened.close();
      }

      final textualAttempt = sqlite.sqlite3.open(rawFile.path);
      try {
        expect(
          () => configureEncryptedDatabaseForEvidence(
            textualAttempt,
            key,
            keyInterpretation: DatabaseKeyInterpretation.textualHexPassphrase,
          ),
          throwsA(anything),
        );
      } finally {
        textualAttempt.close();
      }
    },
    timeout: _testTimeout,
  );

  testWidgets(
    'controlled encrypted-page tamper is detected and original is retained',
    (_) async {
      final original = File(
        p.join(supportDirectory.path, 'tamper-original-$engine.db'),
      );
      final tampered = File(
        p.join(supportDirectory.path, 'tamper-copy-$engine.db'),
      );
      await _deleteDatabaseFiles(original);
      await _deleteDatabaseFiles(tampered);
      final key = generateDatabaseKey();
      final created = sqlite.sqlite3.open(original.path);
      late final StorageEncryptionEngine detectedEngine;
      late final int pageSize;
      late final int targetPage;
      try {
        detectedEngine = detectEncryptionEngine(created);
        configureEncryptedDatabase(created, key);
        created.execute(
          'CREATE TABLE tamper_evidence '
          '(id INTEGER PRIMARY KEY, marker TEXT NOT NULL)',
        );
        created.execute('BEGIN IMMEDIATE');
        final payload = List.filled(768, 'X').join();
        for (var index = 0; index < 512; index++) {
          created.execute('INSERT INTO tamper_evidence(marker) VALUES (?)', [
            'TRADERPRO_7C2B1_TAMPER_ROW_$index:$payload',
          ]);
        }
        created.execute('COMMIT');
        created.select('PRAGMA wal_checkpoint(TRUNCATE)');
        pageSize = _asInt(
          created.select('PRAGMA page_size').single.values.single,
        );
        targetPage = _asInt(
          created
              .select(
                "SELECT pageno FROM dbstat "
                "WHERE name = 'tamper_evidence' AND pagetype = 'leaf' "
                'ORDER BY pageno DESC LIMIT 1',
              )
              .single['pageno'],
        );
        expect(targetPage, greaterThan(1));
      } finally {
        created.close();
      }

      await original.copy(tampered.path);
      final originalBytes = await original.readAsBytes();
      final tamperedBytes = Uint8List.fromList(await tampered.readAsBytes());
      final flipOffset = ((targetPage - 1) * pageSize) + 128;
      expect(flipOffset, lessThan(tamperedBytes.length));
      tamperedBytes[flipOffset] ^= 0x01;
      await tampered.writeAsBytes(tamperedBytes, flush: true);
      expect(await tampered.readAsBytes(), isNot(equals(originalBytes)));

      final corrupted = sqlite.sqlite3.open(tampered.path);
      try {
        configureEncryptedDatabase(corrupted, key);
        if (detectedEngine == StorageEncryptionEngine.sqlcipher) {
          expect(corrupted.select('PRAGMA cipher_integrity_check'), isNotEmpty);
        }
        expect(
          () => corrupted.select(
            'SELECT sum(length(marker)) FROM tamper_evidence',
          ),
          throwsA(isA<sqlite.SqliteException>()),
        );
      } finally {
        corrupted.close();
      }

      final retainedOriginal = sqlite.sqlite3.open(original.path);
      try {
        configureEncryptedDatabase(retainedOriginal, key);
        expect(
          retainedOriginal
              .select('SELECT count(*) FROM tamper_evidence')
              .single
              .values
              .single,
          512,
        );
      } finally {
        retainedOriginal.close();
      }
      debugPrint(
        'TASK7C2B1_DEVICE_TAMPER_JSON='
        '${jsonEncode(<String, Object>{'engine': engine, 'detected': true, 'originalRetained': true, 'page': targetPage, 'offset': flipOffset})}',
      );
    },
    timeout: _testTimeout,
  );

  testWidgets(
    'records a comparable encrypted create, write, checkpoint, and reopen workload',
    (_) async {
      const samples = 10;
      final coldOpenMs = <int>[];
      for (var sample = 0; sample < samples; sample++) {
        final file = File(
          p.join(supportDirectory.path, 'benchmark-cold-$engine-$sample.db'),
        );
        await _deleteDatabaseFiles(file);
        final watch = Stopwatch()..start();
        final database = StorageSpikeDatabase.open(
          file: file,
          keyHex: generateDatabaseKey(),
        );
        await database.customSelect('SELECT 1').getSingle();
        watch.stop();
        coldOpenMs.add(watch.elapsedMilliseconds);
        await database.close();
        await _deleteDatabaseFiles(file);
      }

      final warmFile = File(
        p.join(supportDirectory.path, 'benchmark-warm-$engine.db'),
      );
      await _deleteDatabaseFiles(warmFile);
      final warmKey = generateDatabaseKey();
      final warmCreated = StorageSpikeDatabase.open(
        file: warmFile,
        keyHex: warmKey,
      );
      await warmCreated.insertMarker('SYNTHETIC_WARM_REOPEN');
      await warmCreated.close();
      final warmReopenMs = <int>[];
      for (var sample = 0; sample < samples; sample++) {
        final watch = Stopwatch()..start();
        final reopened = StorageSpikeDatabase.open(
          file: warmFile,
          keyHex: warmKey,
        );
        expect((await reopened.readMarkers()).length, 1);
        watch.stop();
        warmReopenMs.add(watch.elapsedMilliseconds);
        await reopened.close();
      }

      final transactionResults = <String, Object>{};
      for (final rows in <int>[1000, 10000]) {
        final file = File(
          p.join(supportDirectory.path, 'benchmark-$rows-$engine.db'),
        );
        await _deleteDatabaseFiles(file);
        final database = StorageSpikeDatabase.open(
          file: file,
          keyHex: generateDatabaseKey(),
        );
        await database.customSelect('SELECT 1').getSingle();
        final watch = Stopwatch()..start();
        await database.transaction(() async {
          for (var index = 0; index < rows; index++) {
            await database.insertMarker('SYNTHETIC_BENCHMARK_${rows}_$index');
          }
        });
        watch.stop();
        expect((await database.readMarkers()).length, rows);
        await database.close();
        transactionResults['rows$rows'] = <String, Object>{
          'elapsedMs': watch.elapsedMilliseconds,
          'databaseBytes': await file.length(),
        };
      }

      final queueFile = File(
        p.join(supportDirectory.path, 'benchmark-queue-$engine.db'),
      );
      await _deleteDatabaseFiles(queueFile);
      final queueKey = generateDatabaseKey();
      final queueDatabase = StorageSpikeDatabase.open(
        file: queueFile,
        keyHex: queueKey,
      );
      await queueDatabase.customSelect('SELECT 1').getSingle();
      final queueWatch = Stopwatch()..start();
      for (var index = 0; index < 50; index++) {
        await queueDatabase.insertMarker('SYNTHETIC_QUEUE_$index');
      }
      queueWatch.stop();
      final checkpointWatch = Stopwatch()..start();
      final checkpoint = await queueDatabase
          .customSelect('PRAGMA wal_checkpoint(FULL)')
          .getSingle();
      checkpointWatch.stop();
      expect(checkpoint.read<int>('busy'), 0);
      await queueDatabase.close();

      final backgroundReopenWatch = Stopwatch()..start();
      final backgroundReopened = StorageSpikeDatabase.open(
        file: queueFile,
        keyHex: queueKey,
      );
      expect((await backgroundReopened.readMarkers()).length, 50);
      backgroundReopenWatch.stop();
      await backgroundReopened.close();

      final result = <String, Object>{
        'engine': engine,
        'mode': 'debug-integration-test',
        'samples': samples,
        'coldOpenMs': coldOpenMs,
        'coldOpenSummary': _summarize(coldOpenMs),
        'warmReopenMs': warmReopenMs,
        'warmReopenSummary': _summarize(warmReopenMs),
        'transactions': transactionResults,
        'queuedWrites50Ms': queueWatch.elapsedMilliseconds,
        'walCheckpointMs': checkpointWatch.elapsedMilliseconds,
        'backgroundIsolateReopenMs': backgroundReopenWatch.elapsedMilliseconds,
        'queueDatabaseBytes': await queueFile.length(),
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

Map<String, num> _summarize(List<int> values) {
  final sorted = [...values]..sort();
  final middle = sorted.length ~/ 2;
  final median = sorted.length.isOdd
      ? sorted[middle].toDouble()
      : (sorted[middle - 1] + sorted[middle]) / 2;
  final p95Index = math.max(0, (sorted.length * 0.95).ceil() - 1);
  return <String, num>{
    'min': sorted.first,
    'median': median,
    'max': sorted.last,
    'p95': sorted[p95Index],
  };
}

int _asInt(Object? value) => switch (value) {
  int parsed => parsed,
  String text => int.parse(text),
  _ => throw StateError('Expected an integer SQLite evidence value.'),
};

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
