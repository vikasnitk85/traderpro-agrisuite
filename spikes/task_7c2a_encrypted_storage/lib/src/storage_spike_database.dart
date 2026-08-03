import 'dart:io';

import 'package:drift/drift.dart';
import 'package:drift/native.dart';
import 'package:sqlite3/sqlite3.dart' as sqlite;

import 'database_key.dart';

part 'storage_spike_database.g.dart';

class SyntheticMarkers extends Table {
  IntColumn get id => integer().autoIncrement()();

  TextColumn get marker => text()();

  TextColumn get createdAtUtc => text()();
}

class MigrationEvidence extends Table {
  IntColumn get id => integer().autoIncrement()();

  TextColumn get evidence => text()();
}

@DriftDatabase(tables: [SyntheticMarkers, MigrationEvidence])
class StorageSpikeDatabase extends _$StorageSpikeDatabase {
  StorageSpikeDatabase(super.executor);

  factory StorageSpikeDatabase.open({
    required File file,
    required String keyHex,
  }) {
    final validatedKey = validateDatabaseKey(keyHex);
    return StorageSpikeDatabase(
      NativeDatabase.createInBackground(
        file,
        logStatements: false,
        setup: (database) => configureEncryptedDatabase(database, validatedKey),
      ),
    );
  }

  @override
  int get schemaVersion => 2;

  @override
  MigrationStrategy get migration => MigrationStrategy(
    onCreate: (migrator) => migrator.createAll(),
    onUpgrade: (migrator, from, to) async {
      if (from < 2) {
        await migrator.createTable(migrationEvidence);
      }
    },
  );

  Future<int> insertMarker(String value) => into(syntheticMarkers).insert(
    SyntheticMarkersCompanion.insert(
      marker: value,
      createdAtUtc: DateTime.now().toUtc().toIso8601String(),
    ),
  );

  Future<List<SyntheticMarker>> readMarkers() => select(syntheticMarkers).get();
}

String activeEncryptionEngine() {
  final database = sqlite.sqlite3.openInMemory();
  try {
    final cipherVersion = database.select('PRAGMA cipher_version');
    if (cipherVersion.isNotEmpty &&
        cipherVersion.first.values.firstOrNull?.toString().isNotEmpty == true) {
      return 'sqlcipher';
    }

    final sqlite3mcCipher = database.select('PRAGMA cipher');
    if (sqlite3mcCipher.isNotEmpty &&
        sqlite3mcCipher.first.values.firstOrNull?.toString().isNotEmpty ==
            true) {
      return 'sqlite3mc';
    }
    throw StateError('Loaded SQLite library has no encryption support.');
  } finally {
    database.close();
  }
}

void configureEncryptedDatabase(sqlite.Database database, String keyHex) {
  final validatedKey = validateDatabaseKey(keyHex);
  final cipherVersion = database.select('PRAGMA cipher_version');
  if (cipherVersion.isEmpty) {
    final sqlite3mcCipher = database.select('PRAGMA cipher');
    if (sqlite3mcCipher.isEmpty) {
      throw StateError('Loaded SQLite library has no encryption support.');
    }
    database.execute("PRAGMA cipher = 'chacha20'");
  }

  database.execute("PRAGMA key = '$validatedKey'");
  // Force key verification before Drift can run or migrate any schema.
  database.select('SELECT count(*) FROM sqlite_master');
  database.execute('PRAGMA foreign_keys = ON');
  database.execute('PRAGMA temp_store = MEMORY');
  database.execute('PRAGMA journal_mode = WAL');
}

void createEncryptedVersionOneDatabase({
  required File file,
  required String keyHex,
}) {
  final database = sqlite.sqlite3.open(file.path);
  try {
    configureEncryptedDatabase(database, keyHex);
    database.execute('''
      CREATE TABLE synthetic_markers (
        id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
        marker TEXT NOT NULL,
        created_at_utc TEXT NOT NULL
      )
    ''');
    database.execute('PRAGMA user_version = 1');
  } finally {
    database.close();
  }
}
