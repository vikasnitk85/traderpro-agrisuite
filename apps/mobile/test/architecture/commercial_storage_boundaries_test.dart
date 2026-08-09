import 'dart:io';

import 'package:flutter_test/flutter_test.dart';

void main() {
  final dartFiles = _dartFilesUnder(Directory('lib'));

  test('presentation and widgets cannot access storage implementations', () {
    final presentationFiles = dartFiles.where(
      (file) =>
          _path(file).contains('/presentation/') ||
          _path(file) == 'lib/app/app.dart',
    );
    for (final file in presentationFiles) {
      final source = file.readAsStringSync();
      expect(source, isNot(contains('package:drift/')), reason: _path(file));
      expect(
        source,
        isNot(contains('flutter_secure_storage')),
        reason: _path(file),
      );
      expect(
        source,
        isNot(contains('infrastructure/database/commercial')),
        reason: _path(file),
      );
      expect(
        source,
        isNot(contains('infrastructure/security')),
        reason: _path(file),
      );
    }

    for (final file in dartFiles) {
      final source = file.readAsStringSync();
      final isWidget =
          source.contains('extends StatelessWidget') ||
          source.contains('extends StatefulWidget') ||
          source.contains('extends State<');
      if (!isWidget) {
        continue;
      }
      expect(
        source,
        isNot(contains('CommercialDatabase(')),
        reason: _path(file),
      );
      expect(
        source,
        isNot(contains('FlutterSecureStorage(')),
        reason: _path(file),
      );
    }
  });

  test('Commercial production and POC implementations remain isolated', () {
    final commercialProductionFiles = dartFiles.where((file) {
      final path = _path(file);
      return path.startsWith('lib/core/security/') ||
          path.startsWith('lib/infrastructure/database/commercial/') ||
          path.startsWith('lib/infrastructure/security/') ||
          path.startsWith('lib/infrastructure/storage/') ||
          path.startsWith('lib/infrastructure/runtime/') ||
          path == 'lib/main.dart' ||
          path == 'lib/app/app.dart' ||
          path == 'lib/app/commercial_secure_startup_state.dart';
    });
    for (final file in commercialProductionFiles) {
      final source = file.readAsStringSync();
      expect(source, isNot(contains('procurement_poc')), reason: _path(file));
      expect(
        source,
        isNot(contains('trader_pro_local_database')),
        reason: _path(file),
      );
      expect(source, isNot(contains('/spike')), reason: _path(file));
      expect(source, isNot(contains('spikes/')), reason: _path(file));
    }

    for (final file in dartFiles.where(
      (file) => _path(file).startsWith('lib/features/procurement_poc/'),
    )) {
      final source = file.readAsStringSync();
      expect(
        source,
        isNot(contains('infrastructure/database/commercial')),
        reason: _path(file),
      );
      expect(
        source,
        isNot(contains('commercial_secure_runtime')),
        reason: _path(file),
      );
      expect(
        source,
        isNot(contains('flutter_secure_commercial_database_key_store')),
        reason: _path(file),
      );
    }
  });

  test('one opener and runtime own Commercial storage construction', () {
    expect(
      _filesContaining(
        dartFiles
            .where((file) => _path(file).contains('/commercial/'))
            .toList(growable: false),
        'NativeDatabase.createInBackground',
      ),
      <String>[
        'lib/infrastructure/database/commercial/commercial_database_opener.dart',
      ],
    );
    expect(
      _filesContaining(dartFiles, 'FlutterSecureCommercialDatabaseKeyStore()'),
      <String>['lib/infrastructure/runtime/commercial_secure_runtime.dart'],
    );
    expect(
      _filesImporting(
        dartFiles,
        'database/commercial/commercial_database_initializer.dart',
      ),
      <String>['lib/infrastructure/runtime/commercial_secure_runtime.dart'],
    );
  });
}

List<File> _dartFilesUnder(Directory root) => root
    .listSync(recursive: true)
    .whereType<File>()
    .where((file) => file.path.endsWith('.dart'))
    .toList(growable: false);

List<String> _filesContaining(List<File> files, String value) =>
    files
        .where((file) => file.readAsStringSync().contains(value))
        .map(_path)
        .toList(growable: false)
      ..sort();

List<String> _filesImporting(List<File> files, String importSuffix) =>
    files
        .where((file) => file.readAsStringSync().contains(importSuffix))
        .map(_path)
        .toList(growable: false)
      ..sort();

String _path(File file) => file.path.replaceAll('\\', '/');
