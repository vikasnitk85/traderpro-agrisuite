import 'dart:io';

import 'package:flutter_test/flutter_test.dart';

void main() {
  test('production entrypoint and app shell do not import POC code', () {
    for (final path in <String>['lib/main.dart', 'lib/app/app.dart']) {
      final source = File(path).readAsStringSync();
      expect(source, isNot(contains('procurement_poc')));
      expect(source, isNot(contains('ProcurementPoc')));
      expect(source, isNot(contains('traderpro-local.sqlite')));
    }
  });

  test('production dependency selection is SQLite3MC only', () {
    final pubspec = File('pubspec.yaml').readAsStringSync();
    expect(pubspec, contains('source: sqlite3mc'));
    expect(pubspec, isNot(contains('source: sqlcipher')));
    expect(pubspec, isNot(contains('sqlcipher_flutter_libs')));
  });
}
