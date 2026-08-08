import 'dart:async';
import 'dart:convert';
import 'dart:io';

import 'package:sqlite3/sqlite3.dart' as sqlite;
import 'package:traderpro_7c2a_storage_spike/src/database_key.dart';
import 'package:traderpro_7c2a_storage_spike/src/storage_spike_database.dart';

const _keyEnvironmentName = 'TRADERPRO_7C2B1_SYNTHETIC_DATABASE_KEY';
const _activeWriteMarker = 'TRADERPRO_7C2B1_INTERRUPTED_ACTIVE_WRITE';
const _beforeCommitMarker = 'TRADERPRO_7C2B1_INTERRUPTED_BEFORE_COMMIT';
const _checkpointMarkerPrefix = 'TRADERPRO_7C2B1_CHECKPOINT_ROW_';
const _baselineMarker = 'TRADERPRO_7C2B1_INTERRUPTION_BASELINE';

Future<void> main(List<String> arguments) async {
  if (arguments.isNotEmpty && arguments.first == '--worker') {
    await _runWorker(arguments.skip(1).toList());
    return;
  }
  await _runParent();
}

Future<void> _runParent() async {
  final project = Directory.current.absolute;
  final manifest = File('${project.path}/pubspec.yaml');
  if (!manifest.existsSync() ||
      !manifest.readAsStringSync().contains('traderpro_7c2a_storage_spike')) {
    throw StateError('Run this tool from the isolated Task 7C2A spike root.');
  }

  final root = await Directory.systemTemp.createTemp(
    'traderpro_7c2b1_interrupt_',
  );
  final engine = activeEncryptionEngine();
  final key = generateDatabaseKey();
  final results = <Map<String, Object?>>[];
  try {
    final workerProject = await _prepareWorkerProject(project, root);
    for (final scenario in [
      'active-write',
      'before-commit',
      'wal-checkpoint',
      'schema-migration',
    ]) {
      final databaseFile = File('${root.path}/$scenario.db');
      _createBaseline(databaseFile, key);
      final readyFile = File('${root.path}/$scenario.ready');
      final goFile = File('${root.path}/$scenario.go');
      final process = await Process.start(
        Platform.resolvedExecutable,
        [
          'run',
          'tool/run_interruption_matrix.dart',
          '--worker',
          scenario,
          databaseFile.path,
          readyFile.path,
          goFile.path,
        ],
        workingDirectory: workerProject.path,
        environment: {...Platform.environment, _keyEnvironmentName: key},
      );
      final stdoutFuture = process.stdout.transform(utf8.decoder).join();
      final stderrFuture = process.stderr.transform(utf8.decoder).join();
      await _waitForFile(readyFile, process);
      if (scenario == 'wal-checkpoint') {
        await goFile.writeAsString('checkpoint', flush: true);
        await Future<void>.delayed(const Duration(milliseconds: 1));
      }
      final terminated = process.kill();
      if (!terminated) {
        throw StateError('Failed to terminate the synthetic worker process.');
      }
      final exitCode = await process.exitCode.timeout(
        const Duration(seconds: 15),
      );
      final childOutput = await stdoutFuture;
      final childError = await stderrFuture;
      if (exitCode == 0) {
        throw StateError(
          'Synthetic worker exited normally instead of being terminated.',
        );
      }
      if (childOutput.contains(key) || childError.contains(key)) {
        throw StateError('Synthetic key material appeared in worker output.');
      }

      final verification = _verifyAfterRestart(databaseFile, key, scenario);
      results.add({
        'scenario': scenario,
        'workerTerminated': true,
        ...verification,
      });
    }

    stdout.writeln(
      const JsonEncoder.withIndent('  ').convert({
        'engine': engine,
        'powerLossSimulated': false,
        'processTerminationOnly': true,
        'results': results,
      }),
    );
  } finally {
    if (await root.exists()) {
      await root.delete(recursive: true);
    }
  }
}

Future<Directory> _prepareWorkerProject(
  Directory project,
  Directory temporaryRoot,
) async {
  final workerProject = Directory('${temporaryRoot.path}/worker_project');
  await workerProject.create();
  for (final name in ['pubspec.yaml', 'pubspec.lock']) {
    await File('${project.path}/$name').copy('${workerProject.path}/$name');
  }
  await _copyDirectory(
    Directory('${project.path}/lib'),
    Directory('${workerProject.path}/lib'),
  );
  final workerTool = Directory('${workerProject.path}/tool');
  await workerTool.create();
  await File(
    '${project.path}/tool/run_interruption_matrix.dart',
  ).copy('${workerTool.path}/run_interruption_matrix.dart');

  final dependencyResolution = await Process.run(Platform.resolvedExecutable, [
    'pub',
    'get',
    '--offline',
  ], workingDirectory: workerProject.path);
  if (dependencyResolution.exitCode != 0) {
    throw StateError(
      'Could not resolve the isolated worker from the existing package cache. '
      'Dart exited with ${dependencyResolution.exitCode}.',
    );
  }
  return workerProject;
}

Future<void> _copyDirectory(Directory source, Directory destination) async {
  await destination.create(recursive: true);
  await for (final entry in source.list(recursive: false)) {
    final name = entry.path.replaceAll('\\', '/').split('/').last;
    if (entry is Directory) {
      await _copyDirectory(entry, Directory('${destination.path}/$name'));
    } else if (entry is File) {
      await entry.copy('${destination.path}/$name');
    }
  }
}

void _createBaseline(File file, String key) {
  final database = sqlite.sqlite3.open(file.path);
  try {
    configureEncryptedDatabase(database, key);
    database.execute(
      'CREATE TABLE evidence (id INTEGER PRIMARY KEY, marker TEXT NOT NULL)',
    );
    database.execute('INSERT INTO evidence(marker) VALUES (?)', [
      _baselineMarker,
    ]);
    database.execute('PRAGMA user_version = 1');
    database.select('PRAGMA wal_checkpoint(TRUNCATE)');
  } finally {
    database.close();
  }
}

Future<void> _runWorker(List<String> arguments) async {
  if (arguments.length != 4) {
    throw ArgumentError('Worker requires scenario, database, ready, and go.');
  }
  final scenario = arguments[0];
  final databaseFile = File(arguments[1]);
  final readyFile = File(arguments[2]);
  final goFile = File(arguments[3]);
  final key = Platform.environment[_keyEnvironmentName];
  if (key == null) {
    throw StateError('Synthetic worker key is unavailable.');
  }
  final database = sqlite.sqlite3.open(databaseFile.path);
  try {
    configureEncryptedDatabase(database, key);
    switch (scenario) {
      case 'active-write':
        database.execute('BEGIN IMMEDIATE');
        database.execute('INSERT INTO evidence(marker) VALUES (?)', [
          _activeWriteMarker,
        ]);
        await readyFile.writeAsString('active', flush: true);
        await _waitIndefinitely();
        break;
      case 'before-commit':
        database.execute('BEGIN IMMEDIATE');
        database.execute('INSERT INTO evidence(marker) VALUES (?)', [
          _beforeCommitMarker,
        ]);
        await readyFile.writeAsString('before-commit', flush: true);
        await _waitIndefinitely();
        break;
      case 'wal-checkpoint':
        database.execute('BEGIN IMMEDIATE');
        final payload = List.filled(1024, 'W').join();
        for (var index = 0; index < 5000; index++) {
          database.execute('INSERT INTO evidence(marker) VALUES (?)', [
            '$_checkpointMarkerPrefix$index:$payload',
          ]);
        }
        database.execute('COMMIT');
        await readyFile.writeAsString('checkpoint-ready', flush: true);
        await _waitForFileOnly(goFile);
        database.select('PRAGMA wal_checkpoint(TRUNCATE)');
        await _waitIndefinitely();
        break;
      case 'schema-migration':
        database.execute('BEGIN IMMEDIATE');
        database.execute(
          'CREATE TABLE migration_v2 (id INTEGER PRIMARY KEY, value TEXT)',
        );
        database.execute('PRAGMA user_version = 2');
        await readyFile.writeAsString('migration-before-commit', flush: true);
        await _waitIndefinitely();
        break;
      default:
        throw ArgumentError.value(scenario, 'scenario');
    }
  } finally {
    database.close();
  }
}

Map<String, Object?> _verifyAfterRestart(
  File databaseFile,
  String key,
  String scenario,
) {
  if (!databaseFile.existsSync() || databaseFile.lengthSync() == 0) {
    throw StateError('Encrypted database was missing after interruption.');
  }
  final database = sqlite.sqlite3.open(databaseFile.path);
  try {
    configureEncryptedDatabase(database, key);
    final integrity = database
        .select('PRAGMA integrity_check')
        .single
        .values
        .first;
    if (integrity != 'ok') {
      throw StateError('SQLite consistency check failed after interruption.');
    }
    final rows = database.select('SELECT marker FROM evidence');
    final markers = rows.map((row) => row['marker'] as String).toList();
    if (!markers.contains(_baselineMarker)) {
      throw StateError('Committed baseline was lost after interruption.');
    }
    switch (scenario) {
      case 'active-write':
        if (markers.contains(_activeWriteMarker)) {
          throw StateError('Interrupted transaction was silently committed.');
        }
        break;
      case 'before-commit':
        if (markers.contains(_beforeCommitMarker)) {
          throw StateError('Pre-commit transaction was silently committed.');
        }
        break;
      case 'wal-checkpoint':
        final checkpointRows = markers
            .where((marker) => marker.startsWith(_checkpointMarkerPrefix))
            .length;
        if (checkpointRows != 5000) {
          throw StateError('Committed WAL rows were not recovered exactly.');
        }
        break;
      case 'schema-migration':
        final version = int.parse(
          database.select('PRAGMA user_version').single.values.first.toString(),
        );
        final migrationTable = database
            .select(
              "SELECT count(*) FROM sqlite_master "
              "WHERE type = 'table' AND name = 'migration_v2'",
            )
            .single
            .values
            .first;
        if (version != 1 || migrationTable.toString() != '0') {
          throw StateError('Interrupted migration was partially committed.');
        }
        break;
    }
  } finally {
    database.close();
  }

  final artifacts = <File>[
    databaseFile,
    File('${databaseFile.path}-wal'),
    File('${databaseFile.path}-shm'),
    File('${databaseFile.path}-journal'),
  ].where((file) => file.existsSync()).toList();
  const forbidden = [
    'SQLite format 3',
    _baselineMarker,
    _activeWriteMarker,
    _beforeCommitMarker,
    _checkpointMarkerPrefix,
    'migration_v2',
  ];
  for (final artifact in artifacts) {
    final bytes = artifact.readAsBytesSync();
    for (final marker in forbidden) {
      if (_contains(bytes, utf8.encode(marker))) {
        throw StateError(
          'Known plaintext appeared in an interrupted artifact.',
        );
      }
    }
  }

  final unkeyed = sqlite.sqlite3.open(databaseFile.path);
  try {
    var rejected = false;
    try {
      unkeyed.select('SELECT count(*) FROM sqlite_master');
    } on sqlite.SqliteException {
      rejected = true;
    }
    if (!rejected) {
      throw StateError('Interrupted database opened without a key.');
    }
  } finally {
    unkeyed.close();
  }

  return {
    'restartConsistent': true,
    'ciphertextRetained': true,
    'knownPlaintextMatches': 0,
    'unkeyedRejected': true,
  };
}

Future<void> _waitForFile(File file, Process process) async {
  final deadline = DateTime.now().add(const Duration(seconds: 60));
  while (!file.existsSync()) {
    if (DateTime.now().isAfter(deadline)) {
      process.kill();
      throw TimeoutException('Synthetic worker did not reach its fault point.');
    }
    await Future<void>.delayed(const Duration(milliseconds: 25));
  }
}

Future<void> _waitForFileOnly(File file) async {
  while (!file.existsSync()) {
    await Future<void>.delayed(const Duration(milliseconds: 1));
  }
}

Future<void> _waitIndefinitely() => Completer<void>().future;

bool _contains(List<int> haystack, List<int> needle) {
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
