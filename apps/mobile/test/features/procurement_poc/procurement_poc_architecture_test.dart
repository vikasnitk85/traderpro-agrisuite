import 'dart:io';

import 'package:flutter_test/flutter_test.dart';

void main() {
  test('POC presentation has no Drift or raw HTTP access', () {
    final root = Directory.current.absolute;
    final presentation = Directory(
      '${root.path}/lib/features/procurement_poc/presentation',
    );
    final violations = <String>[];
    for (final file
        in presentation.listSync(recursive: true).whereType<File>()) {
      if (!file.path.endsWith('.dart')) {
        continue;
      }
      final source = file.readAsStringSync();
      if (source.contains("package:drift/") ||
          source.contains("dart:io") ||
          source.contains('HttpClient(') ||
          source.contains('customSelect(')) {
        violations.add(file.path);
      }
    }
    expect(violations, isEmpty);
  });

  test('API and sync layers preserve dependency direction', () {
    final root = Directory.current.absolute.path;
    final api = File(
      '$root/lib/features/procurement_poc/infrastructure/'
      'procurement_poc_api_client.dart',
    ).readAsStringSync();
    final engine = File(
      '$root/lib/features/procurement_poc/application/'
      'procurement_poc_sync_engine.dart',
    ).readAsStringSync();
    final application = Directory(
      '$root/lib/features/procurement_poc/application',
    );
    final applicationViolations = application
        .listSync(recursive: true)
        .whereType<File>()
        .where((file) => file.path.endsWith('.dart'))
        .where((file) {
          final source = file.readAsStringSync();
          return source.contains('/presentation/') ||
              source.contains('/infrastructure/') ||
              source.contains('package:flutter/');
        })
        .map((file) => file.path)
        .toList(growable: false);

    expect(api, isNot(contains('package:flutter/')));
    expect(api, isNot(contains('/presentation/')));
    expect(engine, isNot(contains('/presentation/')));
    expect(engine, isNot(contains('/infrastructure/')));
    expect(engine, isNot(contains('package:flutter/')));
    expect(applicationViolations, isEmpty);
  });

  test(
    'POC mobile source has no business posting implementation references',
    () {
      final root = Directory(
        '${Directory.current.absolute.path}/lib/features/procurement_poc',
      );
      final forbidden = RegExp(
        r'(InventoryRepository|SalesRepository|FinanceRepository|'
        r'PurchaseBill|supplier payable|journal posting)',
        caseSensitive: false,
      );
      final violations = <String>[];
      for (final file in root.listSync(recursive: true).whereType<File>()) {
        if (file.path.endsWith('.dart') &&
            forbidden.hasMatch(file.readAsStringSync())) {
          violations.add(file.path);
        }
      }
      expect(violations, isEmpty);
    },
  );
}
