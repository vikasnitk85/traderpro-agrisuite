import 'dart:io';

import 'package:flutter_test/flutter_test.dart';

void main() {
  test('diagnostic contract contains only allowlisted fields', () {
    final source = File(
      'lib/core/security/storage_diagnostics.dart',
    ).readAsStringSync();
    for (final prohibited in <String>[
      'installationId',
      'deviceSerial',
      'businessPayload',
      'rawKey',
      'keyHex',
      'pragmaKey',
      'nativeException',
      'stackTrace',
    ]) {
      expect(source, isNot(contains(prohibited)));
    }
  });

  test('keying SQL exists only in approved database infrastructure', () {
    final roots = <Directory>[Directory('lib')];
    final matches = <String>[];
    for (final root in roots) {
      for (final entity in root.listSync(recursive: true)) {
        if (entity is! File || !entity.path.endsWith('.dart')) {
          continue;
        }
        final source = entity.readAsStringSync();
        if (source.contains('PRAGMA key')) {
          matches.add(entity.path.replaceAll('\\', '/'));
        }
      }
    }
    expect(matches, <String>[
      'lib/infrastructure/database/commercial/commercial_database_opener.dart',
    ]);
  });
}
