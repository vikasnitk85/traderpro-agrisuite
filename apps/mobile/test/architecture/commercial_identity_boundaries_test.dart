import 'dart:io';

import 'package:flutter_test/flutter_test.dart';

void main() {
  final dartFiles = _dartFilesUnder(Directory('lib'));

  test('identity widgets cannot depend on storage, HTTP, or token types', () {
    final widgets = dartFiles.where((file) {
      final source = file.readAsStringSync();
      return source.contains('extends StatelessWidget') ||
          source.contains('extends StatefulWidget') ||
          source.contains('extends State<');
    });
    for (final file in widgets) {
      final source = file.readAsStringSync();
      for (final forbidden in <String>[
        'package:drift/',
        'flutter_secure_storage',
        "import 'dart:io'",
        'HttpClient',
        'AccessTokenLease',
        'RefreshCredential',
        'DeviceCredential',
        'commercial_identity_models.dart',
      ]) {
        expect(source, isNot(contains(forbidden)), reason: _path(file));
      }
    }
  });

  test('identity network and coordinator construction have one owner', () {
    expect(
      _filesContainingExceptDefinition(
        dartFiles,
        'CommercialIdentityHttpClient(',
        'lib/infrastructure/network/commercial_identity_http_client.dart',
      ),
      <String>['lib/infrastructure/runtime/commercial_secure_runtime.dart'],
    );
    expect(
      _filesContainingExceptDefinition(
        dartFiles,
        'CommercialRefreshCoordinator(',
        'lib/infrastructure/identity/commercial_refresh_coordinator.dart',
      ),
      <String>['lib/infrastructure/runtime/commercial_secure_runtime.dart'],
    );
  });

  test('only identity transport writes Authorization', () {
    expect(
      _filesContaining(dartFiles, 'HttpHeaders.authorizationHeader'),
      <String>[
        'lib/infrastructure/network/commercial_identity_http_client.dart',
      ],
    );
    expect(_filesContaining(dartFiles, "headers.set('Authorization'"), isEmpty);
  });

  test('Commercial Drift declarations contain no identity secrets', () {
    final source = File(
      'lib/infrastructure/database/commercial/commercial_database.dart',
    ).readAsStringSync();
    for (final forbidden in <String>[
      'password',
      'activationCode',
      'deviceSecret',
      'accessToken',
      'refreshToken',
      'credentialValue',
      'Authorization',
    ]) {
      expect(source, isNot(contains(forbidden)), reason: forbidden);
    }
  });

  test('B3 identity paths do not import legacy, spike, POC, or Receiving', () {
    final b3Files = dartFiles.where((file) {
      final path = _path(file);
      return path.startsWith('lib/core/identity/') ||
          path.startsWith('lib/infrastructure/identity/') ||
          path.startsWith('lib/infrastructure/network/') ||
          path == 'lib/app/commercial_identity_controller.dart' ||
          path == 'lib/app/commercial_identity_screens.dart' ||
          path == 'lib/infrastructure/runtime/commercial_secure_runtime.dart';
    });
    for (final file in b3Files) {
      final source = file.readAsStringSync();
      for (final forbidden in <String>[
        'trader_pro_local_database',
        'procurement_poc',
        '/spike',
        'spikes/',
        'features/receiving',
        'LocalReceivingStore',
      ]) {
        expect(source, isNot(contains(forbidden)), reason: _path(file));
      }
    }
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

List<String> _filesContainingExceptDefinition(
  List<File> files,
  String value,
  String definition,
) => _filesContaining(
  files.where((file) => _path(file) != definition).toList(growable: false),
  value,
);

String _path(File file) => file.path.replaceAll('\\', '/');
