import 'outbox_operation_status.dart';

final class LocalOutboxOperation {
  const LocalOutboxOperation({
    required this.operationId,
    required this.aggregateId,
    required this.aggregateType,
    required this.operationType,
    required this.localSequence,
    required this.expectedCloudVersion,
    required this.payloadJson,
    required this.payloadHash,
    required this.status,
    required this.attemptCount,
    required this.createdAtDeviceUtc,
    required this.lastAttemptAtUtc,
    required this.acceptedAtUtc,
    required this.lastErrorCode,
  });

  final String operationId;
  final String aggregateId;
  final String aggregateType;
  final String operationType;
  final int localSequence;
  final int? expectedCloudVersion;
  final String payloadJson;
  final String payloadHash;
  final OutboxOperationStatus status;
  final int attemptCount;
  final DateTime createdAtDeviceUtc;
  final DateTime? lastAttemptAtUtc;
  final DateTime? acceptedAtUtc;
  final String? lastErrorCode;
}
