import 'dart:io';
import 'dart:typed_data';

import 'package:drift/native.dart';
import 'package:sqlite3/sqlite3.dart' as sqlite;

import '../../../core/security/commercial_database_key_material.dart';
import '../../../core/security/commercial_storage_failure.dart';
import 'commercial_database.dart';

abstract interface class CommercialDatabaseConnectionOpener {
  Future<CommercialDatabase> open({
    required File file,
    required CommercialDatabaseKeyMaterial keyMaterial,
  });
}

final class CommercialDatabaseOpener
    implements CommercialDatabaseConnectionOpener {
  const CommercialDatabaseOpener();

  @override
  Future<CommercialDatabase> open({
    required File file,
    required CommercialDatabaseKeyMaterial keyMaterial,
  }) async {
    late Uint8List transferBuffer;
    keyMaterial.useBytes((bytes) {
      transferBuffer = Uint8List.fromList(bytes);
    });

    final database = CommercialDatabase(
      NativeDatabase.createInBackground(
        file,
        logStatements: false,
        readPool: 0,
        setup: (nativeDatabase) {
          try {
            configureCommercialSqlite3Mc(nativeDatabase, transferBuffer);
          } finally {
            transferBuffer.fillRange(0, transferBuffer.length, 0);
          }
        },
      ),
    );
    try {
      await database.customSelect('SELECT 1').getSingle();
      return database;
    } on CommercialStorageException {
      await database.close();
      rethrow;
    } on Object {
      await database.close();
      throw const CommercialStorageException(
        CommercialStorageFailureCode.databaseWrongKeyOrUnreadable,
        phase: 'database-open',
      );
    } finally {
      transferBuffer.fillRange(0, transferBuffer.length, 0);
    }
  }
}

void configureCommercialSqlite3Mc(
  sqlite.Database database,
  Uint8List rawKeyBytes,
) {
  if (rawKeyBytes.length != CommercialDatabaseKeyMaterial.byteLength) {
    throw const CommercialStorageException(
      CommercialStorageFailureCode.databaseKeyUnavailable,
      phase: 'database-key-length',
    );
  }

  database.execute("PRAGMA cipher = 'chacha20'");
  database.execute('PRAGMA legacy = 0');
  database.execute('PRAGMA kdf_iter = 64007');
  database.execute('PRAGMA plaintext_header_size = 0');
  database.execute('PRAGMA hmac_check = 1');
  database.execute('PRAGMA mc_legacy_wal = 0');
  database.execute('PRAGMA page_size = 4096');

  _expectPragma(database, 'cipher', 'chacha20');
  // Read the linked library version without querying the database. Any SQL
  // against an existing encrypted file must happen only after the key is set.
  _expectValue(
    sqlite.sqlite3.version.libVersion,
    '3.53.3',
    phase: 'sqlite-version',
  );
  if (_optionalPragma(database, 'cipher_version') != null) {
    throw const CommercialStorageException(
      CommercialStorageFailureCode.databaseConfigurationMismatch,
      phase: 'sqlcipher-prohibited',
    );
  }

  String? keyHex;
  String? keyStatement;
  try {
    keyHex = _lowercaseHex(rawKeyBytes);
    keyStatement = '''PRAGMA key = "x'$keyHex'"''';
    try {
      database.execute(keyStatement);
    } on sqlite.SqliteException {
      throw const CommercialStorageException(
        CommercialStorageFailureCode.databaseWrongKeyOrUnreadable,
        phase: 'database-key-apply',
      );
    }
  } finally {
    keyStatement = null;
    keyHex = null;
  }

  _expectPragma(database, 'cipher', 'chacha20');
  _expectPragma(database, 'legacy', '0');
  _expectPragma(database, 'kdf_iter', '64007');
  _expectPragma(database, 'plaintext_header_size', '0');
  _expectPragma(database, 'hmac_check', '1');
  _expectPragma(database, 'mc_legacy_wal', '0');
  _expectPragma(database, 'page_size', '4096');
  final memorySecurity = _optionalPragma(database, 'memory_security');
  if (memorySecurity != null && memorySecurity.toString() != '1') {
    throw const CommercialStorageException(
      CommercialStorageFailureCode.databaseConfigurationMismatch,
      phase: 'memory-security-readback',
    );
  }

  try {
    database.select('SELECT count(*) FROM sqlite_master');
  } on sqlite.SqliteException {
    throw const CommercialStorageException(
      CommercialStorageFailureCode.databaseWrongKeyOrUnreadable,
      phase: 'cryptographic-schema-verification',
    );
  }

  database.execute('PRAGMA foreign_keys = ON');
  database.execute('PRAGMA temp_store = MEMORY');
  database.execute('PRAGMA journal_mode = WAL');
  _expectPragma(database, 'foreign_keys', '1');
  _expectPragma(database, 'temp_store', '2');
  _expectPragma(database, 'journal_mode', 'wal');
}

String _lowercaseHex(Uint8List bytes) {
  final buffer = StringBuffer();
  for (final byte in bytes) {
    buffer.write(byte.toRadixString(16).padLeft(2, '0'));
  }
  return buffer.toString();
}

void _expectPragma(sqlite.Database database, String pragma, String expected) {
  final rows = database.select('PRAGMA $pragma');
  if (rows.isEmpty) {
    throw CommercialStorageException(
      CommercialStorageFailureCode.databaseConfigurationMismatch,
      phase: 'pragma-$pragma',
    );
  }
  _expectValue(rows.single.values.single, expected, phase: 'pragma-$pragma');
}

Object? _optionalPragma(sqlite.Database database, String pragma) {
  final rows = database.select('PRAGMA $pragma');
  return rows.isEmpty ? null : rows.single.values.single;
}

void _expectValue(Object? actual, String expected, {required String phase}) {
  if (actual.toString().toLowerCase() != expected.toLowerCase()) {
    throw CommercialStorageException(
      CommercialStorageFailureCode.databaseConfigurationMismatch,
      phase: phase,
    );
  }
}
