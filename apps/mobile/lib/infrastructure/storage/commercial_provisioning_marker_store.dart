import 'dart:io';

import '../../core/security/commercial_provisioning_marker.dart';
import '../../core/security/commercial_storage_failure.dart';

final class CommercialProvisioningMarkerStore {
  const CommercialProvisioningMarkerStore(this.markerFile);

  final File markerFile;

  File get _nextFile => File('${markerFile.path}.next');
  File get _previousFile => File('${markerFile.path}.previous');

  Future<CommercialProvisioningMarker?> read() async {
    final candidates = <({File file, CommercialProvisioningMarker marker})>[];
    var artifactExists = false;
    for (final file in <File>[markerFile, _nextFile, _previousFile]) {
      if (!await file.exists()) {
        continue;
      }
      artifactExists = true;
      try {
        candidates.add((
          file: file,
          marker: CommercialProvisioningMarker.decode(
            await file.readAsString(),
          ),
        ));
      } on Object {
        // Another valid sidecar may provide deterministic crash recovery.
      }
    }
    if (candidates.isEmpty) {
      if (!artifactExists) {
        return null;
      }
      throw const CommercialStorageException(
        CommercialStorageFailureCode.databaseInitializationIncomplete,
        phase: 'provisioning-marker-unreadable',
      );
    }
    final generations = candidates
        .map((candidate) => candidate.marker.generation)
        .toSet();
    if (generations.length != 1) {
      throw const CommercialStorageException(
        CommercialStorageFailureCode.databaseInitializationIncomplete,
        phase: 'provisioning-marker-conflict',
      );
    }
    for (final candidate in candidates) {
      if (candidate.marker.state ==
          CommercialProvisioningMarkerState.complete) {
        return candidate.marker;
      }
    }
    return candidates
        .firstWhere(
          (candidate) => candidate.file.path == markerFile.path,
          orElse: () => candidates.first,
        )
        .marker;
  }

  Future<void> write(CommercialProvisioningMarker marker) async {
    try {
      await markerFile.parent.create(recursive: true);
      final next = _nextFile;
      await next.writeAsString(marker.encode(), flush: true);

      final previous = _previousFile;
      if (await markerFile.exists()) {
        if (await previous.exists()) {
          await previous.delete();
        }
        await markerFile.rename(previous.path);
      }
      await next.rename(markerFile.path);
      if (await previous.exists()) {
        await previous.delete();
      }
    } on CommercialStorageException {
      rethrow;
    } on FileSystemException {
      throw const CommercialStorageException(
        CommercialStorageFailureCode.storageUnavailable,
        phase: 'provisioning-marker-write',
      );
    }
  }
}
