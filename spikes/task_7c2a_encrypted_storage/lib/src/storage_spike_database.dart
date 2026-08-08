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

enum StorageEncryptionEngine {
  sqlcipher('sqlcipher'),
  sqlite3MultipleCiphers('sqlite3mc');

  const StorageEncryptionEngine(this.evidenceName);

  final String evidenceName;
}

enum DatabaseKeyInterpretation { raw256Bit, textualHexPassphrase }

String activeEncryptionEngine() {
  final database = sqlite.sqlite3.openInMemory();
  try {
    return detectEncryptionEngine(database).evidenceName;
  } finally {
    database.close();
  }
}

StorageEncryptionEngine detectEncryptionEngine(sqlite.Database database) {
  final cipherVersion = database.select('PRAGMA cipher_version');
  if (cipherVersion.isNotEmpty &&
      cipherVersion.first.values.firstOrNull?.toString().isNotEmpty == true) {
    return StorageEncryptionEngine.sqlcipher;
  }

  final sqlite3mcCipher = database.select('PRAGMA cipher');
  if (sqlite3mcCipher.isNotEmpty &&
      sqlite3mcCipher.first.values.firstOrNull?.toString().isNotEmpty == true) {
    return StorageEncryptionEngine.sqlite3MultipleCiphers;
  }
  throw StateError('Loaded SQLite library has no encryption support.');
}

void configureEncryptedDatabase(sqlite.Database database, String keyHex) {
  configureEncryptedDatabaseForEvidence(
    database,
    keyHex,
    keyInterpretation: DatabaseKeyInterpretation.raw256Bit,
  );
}

/// Applies the frozen Task 7C2B1 candidate configuration. Production-oriented
/// opening always uses [DatabaseKeyInterpretation.raw256Bit]. The textual path
/// exists only so tests can prove that 64 hexadecimal characters are not, by
/// themselves, raw-key syntax.
void configureEncryptedDatabaseForEvidence(
  sqlite.Database database,
  String keyHex, {
  required DatabaseKeyInterpretation keyInterpretation,
}) {
  final validatedKey = validateDatabaseKey(keyHex);
  final engine = detectEncryptionEngine(database);

  switch (engine) {
    case StorageEncryptionEngine.sqlcipher:
      _applySqlCipherConfiguration(database, validatedKey, keyInterpretation);
      break;
    case StorageEncryptionEngine.sqlite3MultipleCiphers:
      _applySqlite3McConfiguration(database, validatedKey, keyInterpretation);
      break;
  }

  verifyEncryptedDatabaseConfiguration(database, engine);
  database.select('SELECT count(*) FROM sqlite_master');
  database.execute('PRAGMA foreign_keys = ON');
  database.execute('PRAGMA temp_store = MEMORY');
  database.execute('PRAGMA journal_mode = WAL');
}

void _applySqlCipherConfiguration(
  sqlite.Database database,
  String validatedKey,
  DatabaseKeyInterpretation interpretation,
) {
  _applyKey(database, _keyPragma(validatedKey, interpretation));
  // SQLCipher connection parameters are applied after keying and before the
  // first operation that reads or writes a database page.
  database.execute('PRAGMA cipher_compatibility = 4');
  database.execute('PRAGMA cipher_page_size = 4096');
  database.execute('PRAGMA kdf_iter = 256000');
  database.execute('PRAGMA cipher_kdf_algorithm = PBKDF2_HMAC_SHA512');
  database.execute('PRAGMA cipher_hmac_algorithm = HMAC_SHA512');
  database.execute('PRAGMA cipher_use_hmac = ON');
  database.execute('PRAGMA cipher_plaintext_header_size = 0');
  database.execute('PRAGMA cipher_memory_security = ON');
}

void _applySqlite3McConfiguration(
  sqlite.Database database,
  String validatedKey,
  DatabaseKeyInterpretation interpretation,
) {
  // SQLite3MC requires cipher selection and cipher parameters before keying.
  database.execute("PRAGMA cipher = 'chacha20'");
  database.execute('PRAGMA legacy = 0');
  database.execute('PRAGMA kdf_iter = 64007');
  database.execute('PRAGMA plaintext_header_size = 0');
  database.execute('PRAGMA hmac_check = 1');
  database.execute('PRAGMA mc_legacy_wal = 0');
  database.execute('PRAGMA memory_security = 1');
  database.execute('PRAGMA page_size = 4096');
  _applyKey(database, _keyPragma(validatedKey, interpretation));
}

void _applyKey(sqlite.Database database, String keyStatement) {
  try {
    database.execute(keyStatement);
  } on sqlite.SqliteException {
    throw StateError('Encrypted database keying failed.');
  }
}

String _keyPragma(
  String validatedKey,
  DatabaseKeyInterpretation interpretation,
) => switch (interpretation) {
  DatabaseKeyInterpretation.raw256Bit => '''PRAGMA key = "x'$validatedKey'"''',
  DatabaseKeyInterpretation.textualHexPassphrase =>
    "PRAGMA key = '$validatedKey'",
};

void verifyEncryptedDatabaseConfiguration(
  sqlite.Database database,
  StorageEncryptionEngine engine,
) {
  switch (engine) {
    case StorageEncryptionEngine.sqlcipher:
      _expectPragma(database, 'cipher_status', '1');
      _expectPragma(database, 'cipher_page_size', '4096');
      _expectPragma(database, 'kdf_iter', '256000');
      _expectPragma(database, 'cipher_kdf_algorithm', 'PBKDF2_HMAC_SHA512');
      _expectPragma(database, 'cipher_hmac_algorithm', 'HMAC_SHA512');
      _expectPragma(database, 'cipher_use_hmac', '1');
      _expectPragma(database, 'cipher_plaintext_header_size', '0');
      _expectPragma(database, 'cipher_memory_security', '1');
      break;
    case StorageEncryptionEngine.sqlite3MultipleCiphers:
      _expectPragma(database, 'cipher', 'chacha20');
      _expectPragma(database, 'legacy', '0');
      _expectPragma(database, 'kdf_iter', '64007');
      _expectPragma(database, 'plaintext_header_size', '0');
      _expectPragma(database, 'hmac_check', '1');
      _expectPragma(database, 'mc_legacy_wal', '0');
      final memorySecurity = _optionalPragmaValue(database, 'memory_security');
      if (memorySecurity != null && memorySecurity.toString() != '1') {
        throw StateError(
          'Encrypted database configuration verification failed for '
          'memory_security.',
        );
      }
      _expectPragma(database, 'page_size', '4096');
      break;
  }
}

Map<String, Object?> readEncryptionConfiguration(
  sqlite.Database database,
  StorageEncryptionEngine engine,
) {
  final common = <String, Object?>{
    'engine': engine.evidenceName,
    'sqliteVersion': database
        .select('SELECT sqlite_version()')
        .single
        .values
        .first,
    'sqliteSourceId': database
        .select('SELECT sqlite_source_id()')
        .single
        .values
        .first,
    'pageSize': _pragmaValue(database, 'page_size'),
    'compileOptions': database
        .select('PRAGMA compile_options')
        .map((row) => row.values.single.toString())
        .toList(),
  };
  switch (engine) {
    case StorageEncryptionEngine.sqlcipher:
      return {
        ...common,
        'cipherVersion': _pragmaValue(database, 'cipher_version'),
        'cipherStatus': _pragmaValue(database, 'cipher_status'),
        'cipherPageSize': _pragmaValue(database, 'cipher_page_size'),
        'kdfIterations': _pragmaValue(database, 'kdf_iter'),
        'kdfAlgorithm': _pragmaValue(database, 'cipher_kdf_algorithm'),
        'hmacAlgorithm': _pragmaValue(database, 'cipher_hmac_algorithm'),
        'hmacEnabled': _pragmaValue(database, 'cipher_use_hmac'),
        'plaintextHeaderSize': _pragmaValue(
          database,
          'cipher_plaintext_header_size',
        ),
        'memorySecurity': _pragmaValue(database, 'cipher_memory_security'),
        'cryptoProvider': _pragmaValue(database, 'cipher_provider'),
        'cryptoProviderVersion': _pragmaValue(
          database,
          'cipher_provider_version',
        ),
      };
    case StorageEncryptionEngine.sqlite3MultipleCiphers:
      return {
        ...common,
        'cipher': _pragmaValue(database, 'cipher'),
        'legacy': _pragmaValue(database, 'legacy'),
        'kdfIterations': _pragmaValue(database, 'kdf_iter'),
        'plaintextHeaderSize': _pragmaValue(database, 'plaintext_header_size'),
        'hmacCheck': _pragmaValue(database, 'hmac_check'),
        'legacyWal': _pragmaValue(database, 'mc_legacy_wal'),
        'memorySecurity': _optionalPragmaValue(database, 'memory_security'),
        'memorySecuritySupported':
            _optionalPragmaValue(database, 'memory_security') != null,
      };
  }
}

void _expectPragma(sqlite.Database database, String pragma, String expected) {
  final actual = _pragmaValue(database, pragma).toString();
  if (actual.toUpperCase() != expected.toUpperCase()) {
    throw StateError(
      'Encrypted database configuration verification failed for $pragma.',
    );
  }
}

Object? _pragmaValue(sqlite.Database database, String pragma) {
  final rows = database.select('PRAGMA $pragma');
  if (rows.isEmpty || rows.first.values.isEmpty) {
    throw StateError(
      'Encrypted database configuration verification failed for $pragma.',
    );
  }
  return rows.first.values.first;
}

Object? _optionalPragmaValue(sqlite.Database database, String pragma) {
  final rows = database.select('PRAGMA $pragma');
  if (rows.isEmpty || rows.first.values.isEmpty) {
    return null;
  }
  return rows.first.values.first;
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
