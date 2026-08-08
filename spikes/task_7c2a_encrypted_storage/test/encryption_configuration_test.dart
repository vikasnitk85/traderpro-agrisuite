import 'dart:io';

import 'package:flutter_test/flutter_test.dart';
import 'package:sqlite3/sqlite3.dart' as sqlite;
import 'package:traderpro_7c2a_storage_spike/src/database_key.dart';
import 'package:traderpro_7c2a_storage_spike/src/storage_spike_database.dart';

void main() {
  late Directory temporaryDirectory;

  setUp(() async {
    temporaryDirectory = await Directory.systemTemp.createTemp(
      'traderpro_7c2b1_configuration_',
    );
  });

  tearDown(() async {
    if (await temporaryDirectory.exists()) {
      await temporaryDirectory.delete(recursive: true);
    }
  });

  test('frozen raw-key configuration self-verifies and reopens', () {
    final file = File('${temporaryDirectory.path}/raw-key.db');
    final key = generateDatabaseKey();
    final created = sqlite.sqlite3.open(file.path);
    late final StorageEncryptionEngine engine;
    try {
      engine = detectEncryptionEngine(created);
      configureEncryptedDatabase(created, key);
      created.execute(
        'CREATE TABLE evidence (id INTEGER PRIMARY KEY, marker TEXT NOT NULL)',
      );
      created.execute('INSERT INTO evidence(marker) VALUES (?)', [
        'TRADERPRO_7C2B1_RAW_KEY_EVIDENCE',
      ]);
      final configuration = readEncryptionConfiguration(created, engine);
      expect(configuration['engine'], engine.evidenceName);
      expect(configuration['pageSize'].toString(), '4096');
      expect(configuration['compileOptions'], isNotEmpty);
      switch (engine) {
        case StorageEncryptionEngine.sqlcipher:
          expect(configuration['cipherStatus'].toString(), '1');
          expect(configuration['cipherPageSize'].toString(), '4096');
          expect(configuration['kdfIterations'].toString(), '256000');
          expect(configuration['kdfAlgorithm'], 'PBKDF2_HMAC_SHA512');
          expect(configuration['hmacAlgorithm'], 'HMAC_SHA512');
          expect(configuration['hmacEnabled'].toString(), '1');
          expect(configuration['plaintextHeaderSize'].toString(), '0');
          expect(configuration['memorySecurity'].toString(), '1');
          break;
        case StorageEncryptionEngine.sqlite3MultipleCiphers:
          expect(configuration['cipher'], 'chacha20');
          expect(configuration['legacy'].toString(), '0');
          expect(configuration['kdfIterations'].toString(), '64007');
          expect(configuration['plaintextHeaderSize'].toString(), '0');
          expect(configuration['hmacCheck'].toString(), '1');
          expect(configuration['legacyWal'].toString(), '0');
          expect(configuration['memorySecurity'], isNull);
          expect(configuration['memorySecuritySupported'], isFalse);
          break;
      }
    } finally {
      created.close();
    }

    final reopened = sqlite.sqlite3.open(file.path);
    try {
      configureEncryptedDatabase(reopened, key);
      expect(
        reopened.select('SELECT marker FROM evidence').single['marker'],
        'TRADERPRO_7C2B1_RAW_KEY_EVIDENCE',
      );
    } finally {
      reopened.close();
    }
  });

  test('raw 256-bit syntax is not a textual hexadecimal passphrase', () {
    final key = generateDatabaseKey();
    final rawFile = File('${temporaryDirectory.path}/raw.db');
    final rawDatabase = sqlite.sqlite3.open(rawFile.path);
    try {
      configureEncryptedDatabaseForEvidence(
        rawDatabase,
        key,
        keyInterpretation: DatabaseKeyInterpretation.raw256Bit,
      );
      rawDatabase.execute('CREATE TABLE evidence (marker TEXT NOT NULL)');
      rawDatabase.execute('INSERT INTO evidence VALUES (?)', ['raw']);
    } finally {
      rawDatabase.close();
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

    final textualFile = File('${temporaryDirectory.path}/textual.db');
    final textualDatabase = sqlite.sqlite3.open(textualFile.path);
    try {
      configureEncryptedDatabaseForEvidence(
        textualDatabase,
        key,
        keyInterpretation: DatabaseKeyInterpretation.textualHexPassphrase,
      );
      textualDatabase.execute('CREATE TABLE evidence (marker TEXT NOT NULL)');
      textualDatabase.execute('INSERT INTO evidence VALUES (?)', ['textual']);
    } finally {
      textualDatabase.close();
    }

    final rawAttempt = sqlite.sqlite3.open(textualFile.path);
    try {
      expect(
        () => configureEncryptedDatabaseForEvidence(
          rawAttempt,
          key,
          keyInterpretation: DatabaseKeyInterpretation.raw256Bit,
        ),
        throwsA(anything),
      );
    } finally {
      rawAttempt.close();
    }
  });

  test('malformed raw key is rejected before an engine operation', () {
    final database = sqlite.sqlite3.openInMemory();
    try {
      expect(
        () => configureEncryptedDatabase(database, 'not-a-raw-key'),
        throwsA(isA<InvalidDatabaseKeyException>()),
      );
    } finally {
      database.close();
    }
  });

  test('native keying failures do not expose synthetic key material', () {
    final database = sqlite.sqlite3.openInMemory();
    final key = generateDatabaseKey();
    try {
      if (detectEncryptionEngine(database) !=
          StorageEncryptionEngine.sqlite3MultipleCiphers) {
        return;
      }
      Object? failure;
      try {
        configureEncryptedDatabase(database, key);
      } catch (error) {
        failure = error;
      }
      expect(failure, isA<StateError>());
      expect(failure.toString(), isNot(contains(key)));
    } finally {
      database.close();
    }
  });
}
