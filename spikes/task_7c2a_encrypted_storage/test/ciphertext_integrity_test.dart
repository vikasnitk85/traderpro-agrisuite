import 'dart:io';
import 'dart:typed_data';

import 'package:flutter_test/flutter_test.dart';
import 'package:sqlite3/sqlite3.dart' as sqlite;
import 'package:traderpro_7c2a_storage_spike/src/database_key.dart';
import 'package:traderpro_7c2a_storage_spike/src/storage_spike_database.dart';

void main() {
  late Directory temporaryDirectory;

  setUp(() async {
    temporaryDirectory = await Directory.systemTemp.createTemp(
      'traderpro_7c2b1_tamper_',
    );
  });

  tearDown(() async {
    if (await temporaryDirectory.exists()) {
      await temporaryDirectory.delete(recursive: true);
    }
  });

  test('controlled encrypted-page bit flip is detected on targeted read', () {
    final original = File('${temporaryDirectory.path}/original.db');
    final tampered = File('${temporaryDirectory.path}/tampered.db');
    final key = generateDatabaseKey();
    final created = sqlite.sqlite3.open(original.path);
    late final StorageEncryptionEngine engine;
    late final int pageSize;
    late final int targetPage;
    try {
      engine = detectEncryptionEngine(created);
      configureEncryptedDatabase(created, key);
      created.execute(
        'CREATE TABLE evidence (id INTEGER PRIMARY KEY, marker TEXT NOT NULL)',
      );
      created.execute('BEGIN IMMEDIATE');
      final payload = List.filled(768, 'X').join();
      for (var index = 0; index < 512; index++) {
        created.execute('INSERT INTO evidence(marker) VALUES (?)', [
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
              "WHERE name = 'evidence' AND pagetype = 'leaf' "
              'ORDER BY pageno DESC LIMIT 1',
            )
            .single['pageno'],
      );
      expect(targetPage, greaterThan(1));
    } finally {
      created.close();
    }

    original.copySync(tampered.path);
    final originalHashInput = original.readAsBytesSync();
    final tamperedBytes = Uint8List.fromList(tampered.readAsBytesSync());
    final flipOffset = ((targetPage - 1) * pageSize) + 128;
    expect(flipOffset, lessThan(tamperedBytes.length));
    tamperedBytes[flipOffset] ^= 0x01;
    tampered.writeAsBytesSync(tamperedBytes, flush: true);
    expect(tampered.readAsBytesSync(), isNot(equals(originalHashInput)));

    final corrupted = sqlite.sqlite3.open(tampered.path);
    try {
      configureEncryptedDatabase(corrupted, key);
      if (engine == StorageEncryptionEngine.sqlcipher) {
        final integrityErrors = corrupted.select(
          'PRAGMA cipher_integrity_check',
        );
        expect(integrityErrors, isNotEmpty);
      }
      expect(
        () => corrupted.select('SELECT sum(length(marker)) FROM evidence'),
        throwsA(isA<sqlite.SqliteException>()),
      );
      expect(tampered.lengthSync(), original.lengthSync());
    } finally {
      corrupted.close();
    }

    final retainedOriginal = sqlite.sqlite3.open(original.path);
    try {
      configureEncryptedDatabase(retainedOriginal, key);
      expect(
        retainedOriginal
            .select('SELECT count(*) FROM evidence')
            .single
            .values
            .first,
        512,
      );
    } finally {
      retainedOriginal.close();
    }
  });

  test(
    'wrong key fails during startup while later-page corruption opens then fails',
    () {
      final original = File('${temporaryDirectory.path}/classification.db');
      final corrupt = File(
        '${temporaryDirectory.path}/classification-corrupt.db',
      );
      final key = generateDatabaseKey();
      final created = sqlite.sqlite3.open(original.path);
      late final int pageSize;
      late final int targetPage;
      try {
        configureEncryptedDatabase(created, key);
        created.execute(
          'CREATE TABLE evidence (id INTEGER PRIMARY KEY, marker TEXT NOT NULL)',
        );
        created.execute('BEGIN IMMEDIATE');
        final payload = List.filled(768, 'Y').join();
        for (var index = 0; index < 256; index++) {
          created.execute('INSERT INTO evidence(marker) VALUES (?)', [
            'CLASSIFICATION_$index:$payload',
          ]);
        }
        created.execute('COMMIT');
        created.select('PRAGMA wal_checkpoint(TRUNCATE)');
        pageSize = _asInt(
          created.select('PRAGMA page_size').single.values.first,
        );
        targetPage = _asInt(
          created
              .select(
                "SELECT pageno FROM dbstat "
                "WHERE name = 'evidence' AND pagetype = 'leaf' "
                'ORDER BY pageno DESC LIMIT 1',
              )
              .single['pageno'],
        );
      } finally {
        created.close();
      }

      final wrongKeyDatabase = sqlite.sqlite3.open(original.path);
      try {
        expect(
          () => configureEncryptedDatabase(
            wrongKeyDatabase,
            generateDatabaseKey(),
          ),
          throwsA(anything),
        );
      } finally {
        wrongKeyDatabase.close();
      }

      original.copySync(corrupt.path);
      final bytes = Uint8List.fromList(corrupt.readAsBytesSync());
      final offset = ((targetPage - 1) * pageSize) + 128;
      bytes[offset] ^= 0x01;
      corrupt.writeAsBytesSync(bytes, flush: true);
      final corrupted = sqlite.sqlite3.open(corrupt.path);
      try {
        configureEncryptedDatabase(corrupted, key);
        expect(
          () => corrupted.select('SELECT sum(length(marker)) FROM evidence'),
          throwsA(isA<sqlite.SqliteException>()),
        );
      } finally {
        corrupted.close();
      }
    },
  );
}

int _asInt(Object? value) => switch (value) {
  int parsed => parsed,
  String text => int.parse(text),
  _ => throw StateError('Expected an integer SQLite evidence value.'),
};
