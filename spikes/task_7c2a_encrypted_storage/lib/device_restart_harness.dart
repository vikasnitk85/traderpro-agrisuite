import 'dart:convert';
import 'dart:io';

import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:path/path.dart' as p;
import 'package:path_provider/path_provider.dart';

import 'src/database_key.dart';
import 'src/secure_database_key_store.dart';
import 'src/storage_spike_database.dart';

const _restartMarker = 'TRADERPRO_7C2A_PROCESS_RESTART_MARKER';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();
  final result = await _runRestartEvidence();
  runApp(RestartEvidenceApp(result: result));
}

Future<Map<String, Object?>> _runRestartEvidence() async {
  final supportDirectory = await getApplicationSupportDirectory();
  final reportFile = File(
    p.join(supportDirectory.path, 'restart-evidence.json'),
  );
  final engine = activeEncryptionEngine();
  final result = <String, Object?>{
    'engine': engine,
    'strategies': <String, Object?>{},
  };

  var allSucceeded = true;
  for (final strategy in <({String name, AndroidOptions options})>[
    (name: 'A', options: FlutterSecureDatabaseKeyStore.androidOptions),
    (
      name: 'B',
      options: FlutterSecureDatabaseKeyStore.backupMigrationAndroidOptions,
    ),
  ]) {
    final databaseFile = File(
      p.join(
        supportDirectory.path,
        'process-restart-${strategy.name.toLowerCase()}-$engine.db',
      ),
    );
    final existedBeforeLaunch = await databaseFile.exists();
    final strategyResult = <String, Object?>{
      'databaseExistedBeforeLaunch': existedBeforeLaunch,
    };
    try {
      final store = FlutterSecureDatabaseKeyStore(options: strategy.options);
      final controller = DatabaseKeyController(store);
      final key = existedBeforeLaunch
          ? await controller.loadExisting()
          : await controller.provisionNewDatabase(
              databaseExists: existedBeforeLaunch,
            );
      final database = StorageSpikeDatabase.open(
        file: databaseFile,
        keyHex: key,
      );
      if (!existedBeforeLaunch) {
        await database.insertMarker('$_restartMarker:${strategy.name}');
      }
      final markers = await database.readMarkers();
      await database.customSelect('PRAGMA wal_checkpoint(FULL)').get();
      await database.close();
      if (markers.length != 1 ||
          markers.single.marker != '$_restartMarker:${strategy.name}') {
        throw StateError(
          'Synthetic restart marker was not recovered exactly once.',
        );
      }

      if (!existedBeforeLaunch && strategy.name == 'A') {
        final ciphertext = await databaseFile.readAsBytes();
        var missingReadBlocked = false;
        var replacementProvisioningBlocked = false;
        var malformedReadBlocked = false;
        var malformedProvisioningBlocked = false;
        var unavailableReadBlocked = false;

        await store.delete();
        try {
          await controller.loadExisting();
        } on MissingDatabaseKeyException {
          missingReadBlocked = true;
        }
        try {
          await controller.provisionNewDatabase(databaseExists: true);
        } on MissingDatabaseKeyException {
          replacementProvisioningBlocked = true;
        }
        final ciphertextPreservedAfterMissing = listEquals(
          ciphertext,
          await databaseFile.readAsBytes(),
        );

        const rawStorage = FlutterSecureStorage();
        await rawStorage.write(
          key: FlutterSecureDatabaseKeyStore.entryName,
          value: 'malformed',
          aOptions: strategy.options,
        );
        try {
          await controller.loadExisting();
        } on InvalidDatabaseKeyException {
          malformedReadBlocked = true;
        }
        try {
          await controller.provisionNewDatabase(databaseExists: true);
        } on InvalidDatabaseKeyException {
          malformedProvisioningBlocked = true;
        }
        final ciphertextPreservedAfterMalformed = listEquals(
          ciphertext,
          await databaseFile.readAsBytes(),
        );

        try {
          await DatabaseKeyController(_UnavailableKeyStore()).loadExisting();
        } on StateError {
          unavailableReadBlocked = true;
        }
        final ciphertextPreservedAfterUnavailable = listEquals(
          ciphertext,
          await databaseFile.readAsBytes(),
        );

        // Restore the exact original value; never generate a replacement.
        await store.write(key);
        final existingValuePreserved = await controller.loadExisting() == key;
        final validationReopen = StorageSpikeDatabase.open(
          file: databaseFile,
          keyHex: key,
        );
        final validationMarkers = await validationReopen.readMarkers();
        await validationReopen.close();
        final encryptedReopenSucceeded = validationMarkers.length == 1 &&
            validationMarkers.single.marker ==
                '$_restartMarker:${strategy.name}';

        final failClosedSucceeded = missingReadBlocked &&
            replacementProvisioningBlocked &&
            malformedReadBlocked &&
            malformedProvisioningBlocked &&
            unavailableReadBlocked &&
            ciphertextPreservedAfterMissing &&
            ciphertextPreservedAfterMalformed &&
            ciphertextPreservedAfterUnavailable &&
            existingValuePreserved &&
            encryptedReopenSucceeded;
        strategyResult['failClosedMatrix'] = <String, Object?>{
          'missingReadBlocked': missingReadBlocked,
          'replacementProvisioningBlocked': replacementProvisioningBlocked,
          'malformedReadBlocked': malformedReadBlocked,
          'malformedProvisioningBlocked': malformedProvisioningBlocked,
          'unavailableReadBlocked': unavailableReadBlocked,
          'ciphertextPreservedAfterMissing': ciphertextPreservedAfterMissing,
          'ciphertextPreservedAfterMalformed':
              ciphertextPreservedAfterMalformed,
          'ciphertextPreservedAfterUnavailable':
              ciphertextPreservedAfterUnavailable,
          'existingValuePreserved': existingValuePreserved,
          'encryptedReopenSucceeded': encryptedReopenSucceeded,
          'plaintextFallbackAttempted': false,
          'resetOnError': false,
          'success': failClosedSucceeded,
        };
        if (!failClosedSucceeded) {
          throw StateError('Fail-closed lifecycle matrix did not pass.');
        }
      }
      strategyResult.addAll({
        'phase': existedBeforeLaunch ? 'reopened' : 'created',
        'markerCount': markers.length,
        'success': true,
        'databaseBytes': await databaseFile.length(),
      });
    } on Object catch (error) {
      allSucceeded = false;
      strategyResult.addAll({
        'phase': 'failed',
        'success': false,
        'error': '$error',
      });
    }
    (result['strategies']! as Map<String, Object?>)[strategy.name] =
        strategyResult;
  }
  result['success'] = allSucceeded;

  await reportFile.writeAsString(
    const JsonEncoder.withIndent('  ').convert(result),
    flush: true,
  );
  return result;
}

final class _UnavailableKeyStore implements DatabaseKeyStore {
  @override
  Future<void> delete() => throw UnsupportedError('delete is not expected');

  @override
  Future<String?> read() => throw StateError('synthetic secure-store outage');

  @override
  Future<void> write(String keyHex) =>
      throw UnsupportedError('write is not expected');
}

class RestartEvidenceApp extends StatelessWidget {
  const RestartEvidenceApp({required this.result, super.key});

  final Map<String, Object?> result;

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'Task 7C2A restart evidence',
      home: Scaffold(
        body: SafeArea(
          child: Padding(
            padding: const EdgeInsets.all(24),
            child: SelectableText(
              const JsonEncoder.withIndent('  ').convert(result),
            ),
          ),
        ),
      ),
    );
  }
}
