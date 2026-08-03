import 'dart:convert';
import 'dart:io';

import 'package:flutter/material.dart';
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
  final databaseFile = File(
    p.join(supportDirectory.path, 'process-restart-evidence.db'),
  );
  final reportFile = File(
    p.join(supportDirectory.path, 'restart-evidence.json'),
  );
  final existedBeforeLaunch = await databaseFile.exists();
  final engine = activeEncryptionEngine();
  final result = <String, Object?>{
    'engine': engine,
    'pid': pid,
    'databaseExistedBeforeLaunch': existedBeforeLaunch,
    'startedAtUtc': DateTime.now().toUtc().toIso8601String(),
  };

  try {
    final controller = DatabaseKeyController(FlutterSecureDatabaseKeyStore());
    final key = existedBeforeLaunch
        ? await controller.loadExisting()
        : await controller.provisionNewDatabase();
    final database = StorageSpikeDatabase.open(file: databaseFile, keyHex: key);
    if (!existedBeforeLaunch) {
      await database.insertMarker(_restartMarker);
    }
    final markers = await database.readMarkers();
    await database.customSelect('PRAGMA wal_checkpoint(FULL)').get();
    await database.close();
    if (markers.length != 1 || markers.single.marker != _restartMarker) {
      throw StateError(
        'Synthetic restart marker was not recovered exactly once.',
      );
    }
    result.addAll({
      'phase': existedBeforeLaunch ? 'reopened' : 'created',
      'markerCount': markers.length,
      'success': true,
      'databaseBytes': await databaseFile.length(),
    });
  } on Object catch (error) {
    result.addAll({'phase': 'failed', 'success': false, 'error': '$error'});
  }

  await reportFile.writeAsString(
    const JsonEncoder.withIndent('  ').convert(result),
    flush: true,
  );
  return result;
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
