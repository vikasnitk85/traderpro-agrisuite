import 'dart:io';

import 'package:sqlite3/sqlite3.dart' as sqlite;
import 'package:traderpro_7c2a_storage_spike/src/database_key.dart';
import 'package:traderpro_7c2a_storage_spike/src/storage_spike_database.dart';

const marker = 'TRADERPRO_7C2A_SYNTHETIC_ARTIFACT_MARKER';

Future<void> main() async {
  final project = Directory.current.absolute;
  final manifest = File('${project.path}/pubspec.yaml');
  if (!manifest.existsSync() ||
      !manifest.readAsStringSync().contains('traderpro_7c2a_storage_spike')) {
    throw StateError('Run this tool from the isolated Task 7C2A spike root.');
  }

  final engine = _activeEngine();
  final artifactRoot = Directory('${project.path}/artifacts/$engine');
  if (artifactRoot.existsSync()) {
    artifactRoot.deleteSync(recursive: true);
  }
  artifactRoot.createSync(recursive: true);
  final working = Directory('${artifactRoot.path}/capture_work')..createSync();
  final key = generateDatabaseKey();

  final walFile = File('${working.path}/wal.db');
  final drift = StorageSpikeDatabase.open(file: walFile, keyHex: key);
  await drift.insertMarker(marker);
  await drift.customStatement('PRAGMA wal_checkpoint(PASSIVE)');
  await _copyIfPresent(walFile, File('${artifactRoot.path}/wal-main.db'));
  await _copyIfPresent(
    File('${walFile.path}-wal'),
    File('${artifactRoot.path}/wal.db-wal'),
  );
  await _copyIfPresent(
    File('${walFile.path}-shm'),
    File('${artifactRoot.path}/wal.db-shm'),
  );
  await drift.close();

  final rollbackFile = File('${working.path}/rollback.db');
  final raw = sqlite.sqlite3.open(rollbackFile.path);
  try {
    configureEncryptedDatabase(raw, key);
    raw.execute('PRAGMA journal_mode = DELETE');
    raw.execute('CREATE TABLE evidence (marker TEXT NOT NULL)');
    raw.execute('BEGIN IMMEDIATE');
    raw.execute('INSERT INTO evidence(marker) VALUES (?)', [marker]);
    await _copyIfPresent(
      rollbackFile,
      File('${artifactRoot.path}/rollback-main.db'),
    );
    await _copyIfPresent(
      File('${rollbackFile.path}-journal'),
      File('${artifactRoot.path}/rollback.db-journal'),
    );
    raw.execute('ROLLBACK');
  } finally {
    raw.close();
  }

  final unexpectedTemporaryFiles = working.listSync().whereType<File>().where((
    file,
  ) {
    final name = file.uri.pathSegments.last;
    return name != 'wal.db' && name != 'rollback.db';
  }).toList();
  if (unexpectedTemporaryFiles.isNotEmpty) {
    throw StateError(
      'Unexpected temporary artifacts: '
      '${unexpectedTemporaryFiles.map((file) => file.uri.pathSegments.last).join(', ')}',
    );
  }

  working.deleteSync(recursive: true);
  stdout.writeln(
    'Captured encrypted $engine artifacts without persisting key material; temporary spill files: 0.',
  );
}

String _activeEngine() {
  final database = sqlite.sqlite3.openInMemory();
  try {
    if (database.select('PRAGMA cipher_version').isNotEmpty) {
      return 'sqlcipher';
    }
    if (database.select('PRAGMA cipher').isNotEmpty) {
      return 'sqlite3mc';
    }
    throw StateError('Active SQLite library is not encrypted.');
  } finally {
    database.close();
  }
}

Future<void> _copyIfPresent(File source, File destination) async {
  if (await source.exists()) {
    await source.copy(destination.path);
  }
}
