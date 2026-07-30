import 'dart:convert';

import 'package:crypto/crypto.dart';

enum PocDisplayRole {
  operator('Operator'),
  owner('Owner');

  const PocDisplayRole(this.storageValue);

  final String storageValue;

  static PocDisplayRole fromStorage(String value) {
    return switch (value) {
      'Operator' => PocDisplayRole.operator,
      'Owner' => PocDisplayRole.owner,
      _ => throw FormatException('Unsupported POC display role: $value'),
    };
  }
}

enum PocControlCommandType {
  heartbeat('Heartbeat'),
  approve('Approve'),
  finalize('Finalize');

  const PocControlCommandType(this.storageValue);

  final String storageValue;

  static PocControlCommandType fromStorage(String value) {
    return switch (value) {
      'Heartbeat' => PocControlCommandType.heartbeat,
      'Approve' => PocControlCommandType.approve,
      'Finalize' => PocControlCommandType.finalize,
      _ => throw FormatException('Unsupported POC command type: $value'),
    };
  }
}

enum PocControlCommandStatus {
  pending('Pending'),
  sending('Sending'),
  completed('Completed'),
  needsAttention('NeedsAttention');

  const PocControlCommandStatus(this.storageValue);

  final String storageValue;

  static PocControlCommandStatus fromStorage(String value) {
    return switch (value) {
      'Pending' => PocControlCommandStatus.pending,
      'Sending' => PocControlCommandStatus.sending,
      'Completed' => PocControlCommandStatus.completed,
      'NeedsAttention' => PocControlCommandStatus.needsAttention,
      _ => throw FormatException('Unsupported POC command status: $value'),
    };
  }
}

final class PocDeviceProfile {
  PocDeviceProfile({
    required String backendBaseUrl,
    required String workspaceId,
    required String deviceId,
    required this.displayRole,
    this.displayLabel,
    this.automaticSyncPaused = false,
    required this.createdAtUtc,
    required this.updatedAtUtc,
  }) : backendBaseUrl = normalizeBackendBaseUrl(backendBaseUrl),
       workspaceId = canonicalUuid(workspaceId, 'workspaceId'),
       deviceId = canonicalUuid(deviceId, 'deviceId') {
    requireUtc(createdAtUtc, 'createdAtUtc');
    requireUtc(updatedAtUtc, 'updatedAtUtc');
  }

  final String backendBaseUrl;
  final String workspaceId;
  final String deviceId;
  final PocDisplayRole displayRole;
  final String? displayLabel;
  final bool automaticSyncPaused;
  final DateTime createdAtUtc;
  final DateTime updatedAtUtc;

  String get sourceKey => PocEventSourceScope.keyFor(
    backendBaseUrl: backendBaseUrl,
    workspaceId: workspaceId,
  );

  PocDeviceProfile copyWith({
    PocDisplayRole? displayRole,
    String? displayLabel,
    bool? automaticSyncPaused,
    DateTime? updatedAtUtc,
  }) {
    return PocDeviceProfile(
      backendBaseUrl: backendBaseUrl,
      workspaceId: workspaceId,
      deviceId: deviceId,
      displayRole: displayRole ?? this.displayRole,
      displayLabel: displayLabel ?? this.displayLabel,
      automaticSyncPaused: automaticSyncPaused ?? this.automaticSyncPaused,
      createdAtUtc: createdAtUtc,
      updatedAtUtc: updatedAtUtc ?? this.updatedAtUtc,
    );
  }
}

abstract final class PocEventSourceScope {
  static String keyFor({
    required String backendBaseUrl,
    required String workspaceId,
  }) {
    final normalizedUrl = normalizeBackendBaseUrl(backendBaseUrl);
    final normalizedWorkspace = canonicalUuid(workspaceId, 'workspaceId');
    return sha256
        .convert(utf8.encode('$normalizedUrl\n$normalizedWorkspace'))
        .toString();
  }
}

final class ReceivingSessionCloudState {
  const ReceivingSessionCloudState({
    required this.sourceKey,
    required this.boundDeviceId,
    required this.localSessionId,
    required this.cloudSessionId,
    required this.cloudReference,
    required this.cloudStatus,
    required this.cloudVersion,
    required this.leaseId,
    required this.leaseExpiresAtUtc,
    required this.editorDeviceId,
    required this.lastSuccessfulSyncAtUtc,
    required this.lastCloudUpdateAtUtc,
    required this.lastErrorCode,
    required this.lastErrorMessage,
  });

  final String sourceKey;
  final String boundDeviceId;
  final String localSessionId;
  final String cloudSessionId;
  final String cloudReference;
  final String cloudStatus;
  final int cloudVersion;
  final String? leaseId;
  final DateTime? leaseExpiresAtUtc;
  final String? editorDeviceId;
  final DateTime lastSuccessfulSyncAtUtc;
  final DateTime lastCloudUpdateAtUtc;
  final String? lastErrorCode;
  final String? lastErrorMessage;

  bool hasUsableLease(DateTime nowUtc) {
    return leaseId != null &&
        leaseExpiresAtUtc != null &&
        leaseExpiresAtUtc!.isAfter(nowUtc);
  }
}

final class MobileSyncOperationEnvelope {
  const MobileSyncOperationEnvelope({
    required this.operationId,
    required this.operationType,
    required this.aggregateId,
    required this.localSequence,
    required this.expectedCloudVersion,
    required this.payloadJson,
    required this.payloadHash,
    required this.leaseId,
  });

  final String operationId;
  final String operationType;
  final String aggregateId;
  final int localSequence;
  final int? expectedCloudVersion;
  final String payloadJson;
  final String payloadHash;
  final String? leaseId;

  Map<String, Object?> toJson() {
    return <String, Object?>{
      'operationId': operationId,
      'operationType': operationType,
      'aggregateId': aggregateId,
      'localSequence': localSequence,
      'expectedCloudVersion': expectedCloudVersion,
      'payloadJson': payloadJson,
      'payloadHash': payloadHash,
      if (leaseId != null) 'leaseId': leaseId,
    };
  }
}

final class MobileSyncOperationResult {
  const MobileSyncOperationResult({
    required this.operationId,
    required this.aggregateId,
    required this.localSequence,
    required this.resultStatus,
    required this.cloudAggregateVersion,
    required this.cloudReference,
    required this.leaseId,
    required this.leaseExpiresAtUtc,
    required this.errorCode,
    required this.message,
  });

  final String operationId;
  final String aggregateId;
  final int localSequence;
  final String resultStatus;
  final int? cloudAggregateVersion;
  final String? cloudReference;
  final String? leaseId;
  final DateTime? leaseExpiresAtUtc;
  final String? errorCode;
  final String? message;

  bool get isAccepted =>
      resultStatus == 'Accepted' || resultStatus == 'PreviouslyProcessed';
}

final class MobileSyncBatchResult {
  const MobileSyncBatchResult({
    required this.operations,
    required this.rawResponseJson,
  });

  final List<MobileSyncOperationResult> operations;
  final String rawResponseJson;
}

final class MobileSyncEvent {
  const MobileSyncEvent({
    required this.sequence,
    required this.eventId,
    required this.eventType,
    required this.eventVersion,
    required this.aggregateType,
    required this.aggregateId,
    required this.aggregateVersion,
    required this.occurredAtUtc,
    required this.correlationId,
    required this.payloadJson,
  });

  final int sequence;
  final String eventId;
  final String eventType;
  final int eventVersion;
  final String aggregateType;
  final String aggregateId;
  final int aggregateVersion;
  final DateTime occurredAtUtc;
  final String correlationId;
  final String payloadJson;
}

final class MobileSyncEventPage {
  const MobileSyncEventPage({
    required this.events,
    required this.nextCursor,
    required this.hasMore,
  });

  final List<MobileSyncEvent> events;
  final int nextCursor;
  final bool hasMore;
}

final class RemoteReceivingEntrySummary {
  const RemoteReceivingEntrySummary({
    required this.entryId,
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
    required this.capturedAtDeviceUtc,
    required this.acceptedAtServerUtc,
  });

  final String entryId;
  final int localSequence;
  final String productReference;
  final String bagTypeReference;
  final int bagCount;
  final String? rawWeightKg;
  final String processedWeightKg;
  final String? displayWeightKg;
  final int? decimalPlaces;
  final String? processingMethod;
  final String? weightSource;
  final DateTime? capturedAtDeviceUtc;
  final DateTime? acceptedAtServerUtc;
}

final class RemoteReceivingSessionProjection {
  const RemoteReceivingSessionProjection({
    required this.sessionId,
    required this.cloudReference,
    required this.status,
    required this.editorDeviceId,
    required this.leaseExpiresAtUtc,
    required this.entryCount,
    required this.processedTotalWeightKg,
    required this.cloudVersion,
    required this.approvedByDeviceId,
    required this.finalizationId,
    required this.lastCloudUpdateAtUtc,
    this.recentEntries = const [],
  });

  final String sessionId;
  final String cloudReference;
  final String status;
  final String editorDeviceId;
  final DateTime? leaseExpiresAtUtc;
  final int entryCount;
  final String processedTotalWeightKg;
  final int cloudVersion;
  final String? approvedByDeviceId;
  final String? finalizationId;
  final DateTime lastCloudUpdateAtUtc;
  final List<RemoteReceivingEntrySummary> recentEntries;
}

typedef ReceivingSessionLiveView = RemoteReceivingSessionProjection;

final class ReceivingSessionListPage {
  const ReceivingSessionListPage({
    required this.sessions,
    required this.nextCursor,
    required this.hasMore,
  });

  final List<RemoteReceivingSessionProjection> sessions;
  final int? nextCursor;
  final bool hasMore;
}

final class ProcurementPocCommandResult {
  const ProcurementPocCommandResult({
    required this.sessionId,
    required this.cloudReference,
    required this.status,
    required this.editorDeviceId,
    required this.leaseId,
    required this.leaseExpiresAtUtc,
    required this.entryCount,
    required this.processedTotalWeightKg,
    required this.version,
    required this.finalizationId,
    required this.idempotencyStatus,
    required this.rawResponseJson,
  });

  final String sessionId;
  final String cloudReference;
  final String status;
  final String editorDeviceId;
  final String? leaseId;
  final DateTime? leaseExpiresAtUtc;
  final int entryCount;
  final String processedTotalWeightKg;
  final int version;
  final String? finalizationId;
  final String idempotencyStatus;
  final String rawResponseJson;
}

final class ProcurementPocBootstrapResult {
  const ProcurementPocBootstrapResult({
    required this.workspaceId,
    required this.companyId,
    required this.branchId,
    required this.operatorDeviceId,
    required this.ownerDeviceId,
    required this.setupCode,
    required this.rawResponseJson,
  });

  final String workspaceId;
  final String companyId;
  final String branchId;
  final String operatorDeviceId;
  final String ownerDeviceId;
  final String setupCode;
  final String rawResponseJson;
}

final class PocControlCommand {
  const PocControlCommand({
    required this.commandId,
    required this.sourceKey,
    required this.actingDeviceId,
    required this.commandType,
    required this.sessionId,
    required this.expectedCloudVersion,
    required this.leaseId,
    required this.status,
    required this.attemptCount,
    required this.nextAttemptAtUtc,
    required this.lastAttemptAtUtc,
    required this.lastErrorCode,
    required this.lastErrorMessage,
    required this.successfulResponseJson,
    required this.createdAtUtc,
    required this.updatedAtUtc,
  });

  final String commandId;
  final String sourceKey;
  final String actingDeviceId;
  final PocControlCommandType commandType;
  final String sessionId;
  final int? expectedCloudVersion;
  final String? leaseId;
  final PocControlCommandStatus status;
  final int attemptCount;
  final DateTime? nextAttemptAtUtc;
  final DateTime? lastAttemptAtUtc;
  final String? lastErrorCode;
  final String? lastErrorMessage;
  final String? successfulResponseJson;
  final DateTime createdAtUtc;
  final DateTime updatedAtUtc;
}

final class PocSyncDiagnostics {
  const PocSyncDiagnostics({
    required this.pending,
    required this.sending,
    required this.completed,
    required this.needsAttention,
    required this.rejected,
    required this.eventCursor,
    required this.lastPollErrorCode,
  });

  final int pending;
  final int sending;
  final int completed;
  final int needsAttention;
  final int rejected;
  final int eventCursor;
  final String? lastPollErrorCode;
}

String normalizeBackendBaseUrl(String input) {
  final value = input.trim();
  final uri = Uri.tryParse(value);
  if (uri == null ||
      !uri.hasScheme ||
      !uri.hasAuthority ||
      (uri.scheme != 'http' && uri.scheme != 'https') ||
      uri.host.isEmpty ||
      uri.userInfo.isNotEmpty ||
      uri.hasQuery ||
      uri.hasFragment) {
    throw const FormatException(
      'Backend base URL must be an absolute HTTP or HTTPS URL without '
      'credentials, query, or fragment.',
    );
  }
  var normalized = uri.toString();
  while (normalized.endsWith('/')) {
    normalized = normalized.substring(0, normalized.length - 1);
  }
  return normalized;
}

String canonicalUuid(String value, String fieldName) {
  final trimmed = value.trim();
  final pattern = RegExp(
    r'^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-'
    r'[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$',
  );
  if (!pattern.hasMatch(trimmed) ||
      trimmed == '00000000-0000-0000-0000-000000000000') {
    throw FormatException('$fieldName must be one non-empty canonical UUID.');
  }
  return trimmed.toLowerCase();
}

DateTime parseRequiredUtc(Object? value, String fieldName) {
  if (value is! String) {
    throw FormatException('$fieldName must be an ISO-8601 UTC string.');
  }
  final parsed = DateTime.tryParse(value);
  if (parsed == null || !parsed.isUtc) {
    throw FormatException('$fieldName must include a UTC offset.');
  }
  return parsed;
}

DateTime? parseNullableUtc(Object? value, String fieldName) {
  return value == null ? null : parseRequiredUtc(value, fieldName);
}

void requireUtc(DateTime value, String fieldName) {
  if (!value.isUtc) {
    throw ArgumentError.value(value, fieldName, 'must be UTC');
  }
}
