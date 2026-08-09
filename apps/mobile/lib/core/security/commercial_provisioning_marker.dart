import 'dart:convert';

import 'commercial_storage_failure.dart';

enum CommercialProvisioningMarkerState { preparing, complete }

final class CommercialProvisioningMarker {
  const CommercialProvisioningMarker({
    required this.version,
    required this.generation,
    required this.installationId,
    required this.databaseInstanceId,
    required this.state,
  });

  static final RegExp _uuidPattern = RegExp(
    r'^[0-9a-f]{8}-[0-9a-f]{4}-[1-8][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$',
  );

  final int version;
  final String generation;
  final String installationId;
  final String databaseInstanceId;
  final CommercialProvisioningMarkerState state;

  CommercialProvisioningMarker complete() => CommercialProvisioningMarker(
    version: version,
    generation: generation,
    installationId: installationId,
    databaseInstanceId: databaseInstanceId,
    state: CommercialProvisioningMarkerState.complete,
  );

  Map<String, Object> toJson() => <String, Object>{
    'version': version,
    'generation': generation,
    'installationId': installationId,
    'databaseInstanceId': databaseInstanceId,
    'state': state.name,
  };

  String encode() => jsonEncode(toJson());

  static CommercialProvisioningMarker decode(String source) {
    try {
      final decoded = jsonDecode(source);
      if (decoded is! Map<String, Object?> ||
          decoded.length != 5 ||
          decoded['version'] != 1 ||
          !_validUuid(decoded['generation']) ||
          !_validUuid(decoded['installationId']) ||
          !_validUuid(decoded['databaseInstanceId'])) {
        throw const FormatException();
      }
      final state = switch (decoded['state']) {
        'preparing' => CommercialProvisioningMarkerState.preparing,
        'complete' => CommercialProvisioningMarkerState.complete,
        _ => throw const FormatException(),
      };
      return CommercialProvisioningMarker(
        version: 1,
        generation: decoded['generation']! as String,
        installationId: decoded['installationId']! as String,
        databaseInstanceId: decoded['databaseInstanceId']! as String,
        state: state,
      );
    } on FormatException {
      throw const CommercialStorageException(
        CommercialStorageFailureCode.databaseInitializationIncomplete,
        phase: 'provisioning-marker-decode',
      );
    }
  }

  static bool _validUuid(Object? value) =>
      value is String && _uuidPattern.hasMatch(value);
}
