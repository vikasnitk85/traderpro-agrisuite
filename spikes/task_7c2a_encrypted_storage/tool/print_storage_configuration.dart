import 'dart:convert';
import 'dart:io';

import 'package:sqlite3/sqlite3.dart' as sqlite;
import 'package:traderpro_7c2a_storage_spike/src/database_key.dart';
import 'package:traderpro_7c2a_storage_spike/src/storage_spike_database.dart';

void main() {
  final directory = Directory.systemTemp.createTempSync(
    'traderpro_7c2b1_configuration_',
  );
  final database = sqlite.sqlite3.open('${directory.path}/configuration.db');
  try {
    final engine = detectEncryptionEngine(database);
    configureEncryptedDatabase(database, generateDatabaseKey());
    final configuration = readEncryptionConfiguration(database, engine);
    stdout.writeln(const JsonEncoder.withIndent('  ').convert(configuration));
  } finally {
    database.close();
    directory.deleteSync(recursive: true);
  }
}
