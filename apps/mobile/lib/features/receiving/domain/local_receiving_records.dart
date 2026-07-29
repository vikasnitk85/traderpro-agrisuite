import 'local_receiving_status.dart';
import 'weight_source.dart';

final class LocalReceivingSessionRecord {
  const LocalReceivingSessionRecord({
    required this.id,
    required this.cloudId,
    required this.temporaryReference,
    required this.localStatus,
    required this.cloudStatus,
    required this.localVersion,
    required this.cloudVersion,
    required this.nextLocalSequence,
    required this.activeEntryCount,
    required this.processedTotalWeightKg,
    required this.createdAtDeviceUtc,
    required this.updatedAtDeviceUtc,
    required this.lastCloudSyncAtUtc,
  });

  final String id;
  final String? cloudId;
  final String temporaryReference;
  final LocalReceivingStatus localStatus;
  final String? cloudStatus;
  final int localVersion;
  final int? cloudVersion;
  final int nextLocalSequence;
  final int activeEntryCount;
  final String processedTotalWeightKg;
  final DateTime createdAtDeviceUtc;
  final DateTime updatedAtDeviceUtc;
  final DateTime? lastCloudSyncAtUtc;
}

final class LocalReceivingEntryRecord {
  const LocalReceivingEntryRecord({
    required this.id,
    required this.receivingSessionId,
    required this.operationId,
    required this.localSequence,
    required this.productReference,
    required this.bagTypeReference,
    required this.bagCount,
    required this.rawWeightKg,
    required this.processedWeightKg,
    required this.displayWeightKg,
    required this.decimalPlaces,
    required this.processingMethod,
    required this.weightSource,
    required this.entryStatus,
    required this.reversalOfEntryId,
    required this.capturedAtDeviceUtc,
    required this.createdAtDeviceUtc,
  });

  final String id;
  final String receivingSessionId;
  final String operationId;
  final int localSequence;
  final String productReference;
  final String bagTypeReference;
  final int bagCount;
  final String rawWeightKg;
  final String processedWeightKg;
  final String displayWeightKg;
  final int decimalPlaces;
  final String processingMethod;
  final WeightSource weightSource;
  final LocalReceivingEntryStatus entryStatus;
  final String? reversalOfEntryId;
  final DateTime capturedAtDeviceUtc;
  final DateTime createdAtDeviceUtc;
}

final class ReceivingSessionProjection {
  const ReceivingSessionProjection({
    required this.activeEntryCount,
    required this.processedTotalWeightKg,
  });

  final int activeEntryCount;
  final String processedTotalWeightKg;

  bool matches(ReceivingSessionProjection other) {
    return activeEntryCount == other.activeEntryCount &&
        processedTotalWeightKg == other.processedTotalWeightKg;
  }
}

final class ReceivingSessionProjectionComparison {
  const ReceivingSessionProjectionComparison({
    required this.cached,
    required this.derived,
  });

  final ReceivingSessionProjection cached;
  final ReceivingSessionProjection derived;

  bool get matches => cached.matches(derived);
}
