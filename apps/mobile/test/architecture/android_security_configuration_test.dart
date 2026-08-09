import 'dart:io';

import 'package:flutter_test/flutter_test.dart';

void main() {
  test('main Android manifest freezes release security controls', () {
    final manifest = File(
      'android/app/src/main/AndroidManifest.xml',
    ).readAsStringSync();
    expect(manifest, contains('android:allowBackup="false"'));
    expect(manifest, contains('android:fullBackupContent="@xml/backup_rules"'));
    expect(
      manifest,
      contains('android:dataExtractionRules="@xml/data_extraction_rules"'),
    );
    expect(manifest, contains('android:usesCleartextTraffic="false"'));
    expect(manifest, isNot(contains('android.permission.INTERNET')));
    expect(manifest, isNot(contains('android:debuggable')));
  });

  test('backup and transfer rules exclude every protected storage domain', () {
    final legacy = File(
      'android/app/src/main/res/xml/backup_rules.xml',
    ).readAsStringSync();
    for (final domain in <String>[
      'root',
      'file',
      'database',
      'sharedpref',
      'external',
    ]) {
      expect(
        legacy,
        contains('<exclude domain="$domain" path="." />'),
        reason: domain,
      );
    }

    final extraction = File(
      'android/app/src/main/res/xml/data_extraction_rules.xml',
    ).readAsStringSync();
    expect(extraction, contains('<cloud-backup '));
    expect(extraction, contains('<device-transfer>'));
    for (final domain in <String>[
      'root',
      'file',
      'database',
      'sharedpref',
      'external',
      'device_root',
      'device_file',
      'device_database',
      'device_sharedpref',
    ]) {
      expect(
        extraction.split('<exclude domain="$domain" path="." />').length,
        3,
        reason: domain,
      );
    }
  });

  test(
    'debug cleartext override is explicit and release signing is absent',
    () {
      final debugManifest = File(
        'android/app/src/debug/AndroidManifest.xml',
      ).readAsStringSync();
      expect(debugManifest, contains('xmlns:tools='));
      expect(debugManifest, contains('android:usesCleartextTraffic="true"'));
      expect(
        debugManifest,
        contains('tools:replace="android:usesCleartextTraffic"'),
      );

      final build = File('android/app/build.gradle.kts').readAsStringSync();
      expect(build, isNot(contains('signingConfigs.getByName("debug")')));
      expect(build, isNot(contains('signingConfig =')));
    },
  );
}
