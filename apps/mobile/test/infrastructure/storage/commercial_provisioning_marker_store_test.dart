import 'dart:io';

import 'package:flutter_test/flutter_test.dart';
import 'package:traderpro_agrisuite_mobile/core/security/commercial_provisioning_marker.dart';
import 'package:traderpro_agrisuite_mobile/core/security/commercial_storage_failure.dart';
import 'package:traderpro_agrisuite_mobile/infrastructure/storage/commercial_provisioning_marker_store.dart';

void main() {
  late Directory directory;
  late File markerFile;
  late CommercialProvisioningMarkerStore store;

  setUp(() async {
    directory = await Directory.systemTemp.createTemp('traderpro-b2-marker-');
    markerFile = File('${directory.path}/commercial/provisioning.json');
    store = CommercialProvisioningMarkerStore(markerFile);
  });

  tearDown(() async {
    if (await directory.exists()) {
      await directory.delete(recursive: true);
    }
  });

  test('writes and reads the exact non-secret marker contract', () async {
    final marker = _marker(CommercialProvisioningMarkerState.preparing);
    await store.write(marker);
    final read = await store.read();
    expect(read?.version, 1);
    expect(read?.generation, marker.generation);
    expect(read?.installationId, marker.installationId);
    expect(read?.databaseInstanceId, marker.databaseInstanceId);
    expect(read?.state, CommercialProvisioningMarkerState.preparing);
    expect(await File('${markerFile.path}.next').exists(), isFalse);
    expect(await File('${markerFile.path}.previous').exists(), isFalse);
  });

  test('complete update replaces preparing marker', () async {
    final marker = _marker(CommercialProvisioningMarkerState.preparing);
    await store.write(marker);
    await store.write(marker.complete());
    expect(
      (await store.read())?.state,
      CommercialProvisioningMarkerState.complete,
    );
  });

  test('recovers a valid complete next sidecar after interruption', () async {
    final marker = _marker(CommercialProvisioningMarkerState.preparing);
    await markerFile.parent.create(recursive: true);
    await markerFile.writeAsString(marker.encode(), flush: true);
    await File(
      '${markerFile.path}.next',
    ).writeAsString(marker.complete().encode(), flush: true);
    expect(
      (await store.read())?.state,
      CommercialProvisioningMarkerState.complete,
    );
  });

  test('conflicting generations fail closed', () async {
    await markerFile.parent.create(recursive: true);
    await markerFile.writeAsString(
      _marker(CommercialProvisioningMarkerState.preparing).encode(),
      flush: true,
    );
    await File('${markerFile.path}.next').writeAsString(
      _marker(
        CommercialProvisioningMarkerState.complete,
        generation: '019fad0f-2d6a-7000-8000-000000000002',
      ).encode(),
      flush: true,
    );
    expect(
      store.read,
      throwsA(
        isA<CommercialStorageException>().having(
          (error) => error.code,
          'code',
          CommercialStorageFailureCode.databaseInitializationIncomplete,
        ),
      ),
    );
  });
}

CommercialProvisioningMarker _marker(
  CommercialProvisioningMarkerState state, {
  String generation = '019fad0f-2d6a-7000-8000-000000000001',
}) => CommercialProvisioningMarker(
  version: 1,
  generation: generation,
  installationId: '019fad0f-2d6a-7000-8000-000000000010',
  databaseInstanceId: '019fad0f-2d6a-7000-8000-000000000020',
  state: state,
);
