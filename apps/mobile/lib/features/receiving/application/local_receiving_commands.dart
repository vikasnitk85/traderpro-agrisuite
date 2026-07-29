import '../../../core/measurements/weight_processing_method.dart';
import '../domain/local_receiving_records.dart';
import '../domain/weight_source.dart';
import '../../../core/sync/local_outbox_operation.dart';

final class RecordWeightLocallyCommand {
  const RecordWeightLocallyCommand({
    this.operationId,
    required this.sessionId,
    required this.productReference,
    required this.bagTypeReference,
    required this.bagCount,
    required this.rawWeightKg,
    required this.decimalPlaces,
    required this.processingMethod,
    required this.weightSource,
    required this.capturedAtDeviceUtc,
  });

  final String? operationId;
  final String sessionId;
  final String productReference;
  final String bagTypeReference;
  final int bagCount;
  final String rawWeightKg;
  final int decimalPlaces;
  final WeightProcessingMethod processingMethod;
  final WeightSource weightSource;
  final DateTime capturedAtDeviceUtc;
}

final class CreateLocalReceivingSessionResult {
  const CreateLocalReceivingSessionResult({
    required this.session,
    required this.outboxOperation,
  });

  final LocalReceivingSessionRecord session;
  final LocalOutboxOperation outboxOperation;
}

final class AcceptedLocalReceivingEntry {
  const AcceptedLocalReceivingEntry({
    required this.entry,
    required this.outboxOperation,
    required this.wasDuplicate,
  });

  final LocalReceivingEntryRecord entry;
  final LocalOutboxOperation outboxOperation;
  final bool wasDuplicate;
}
