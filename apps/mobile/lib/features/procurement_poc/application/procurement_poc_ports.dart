import '../../../core/sync/local_outbox_operation.dart';
import '../../receiving/domain/local_receiving_records.dart';
import '../domain/procurement_poc_models.dart';

final class ProcurementPocApiException implements Exception {
  const ProcurementPocApiException({
    required this.code,
    required this.message,
    required this.retryable,
    required this.responseAmbiguous,
    this.statusCode,
    this.correlationId,
  });

  final String code;
  final String message;
  final bool retryable;
  final bool responseAmbiguous;
  final int? statusCode;
  final String? correlationId;

  @override
  String toString() => '$code: $message';
}

final class ProcurementPocLocalException implements Exception {
  const ProcurementPocLocalException(this.code, this.message);

  static const profileChangeBlocked = 'POC_PROFILE_CHANGE_BLOCKED';
  static const localStateConflict = 'POC_LOCAL_STATE_CONFLICT';
  static const eventInvalid = 'POC_EVENT_INVALID';
  static const cloudContractInvalid = 'POC_CLOUD_CONTRACT_INVALID';
  static const captureTimestampInvalid = 'POC_CAPTURE_TIMESTAMP_INVALID';

  final String code;
  final String message;

  @override
  String toString() => '$code: $message';
}

abstract interface class ProcurementPocApi {
  Future<MobileSyncBatchResult> sendOperations(
    List<MobileSyncOperationEnvelope> operations,
  );

  Future<MobileSyncEventPage> readEvents({
    required int after,
    required int limit,
  });

  Future<ReceivingSessionListPage> listReceivingSessions({
    String? status,
    int? after,
    int limit = 50,
  });

  Future<ReceivingSessionLiveView> getReceivingSessionLiveView(
    String sessionId,
  );

  Future<ProcurementPocCommandResult> heartbeat({
    required String sessionId,
    required String leaseId,
    required String idempotencyKey,
  });

  Future<ProcurementPocCommandResult> approve({
    required String sessionId,
    required int expectedVersion,
    required String idempotencyKey,
  });

  Future<ProcurementPocCommandResult> finalize({
    required String sessionId,
    required int expectedVersion,
    required String idempotencyKey,
  });

  Future<ProcurementPocBootstrapResult> bootstrap();

  void dispose();
}

typedef ProcurementPocApiFactory =
    ProcurementPocApi Function(PocDeviceProfile profile);

typedef ProcurementPocBootstrapApiFactory =
    ProcurementPocApi Function(String backendBaseUrl);

abstract interface class ProcurementPocProfileStore {
  Future<PocDeviceProfile?> loadActiveProfile();

  Future<PocDeviceProfile> saveActiveProfile(PocDeviceProfile profile);

  Future<PocDeviceProfile> setAutomaticSyncPaused(bool paused, DateTime nowUtc);
}

abstract interface class ProcurementPocSyncStore {
  Future<List<LocalOutboxOperation>> loadSyncCandidates();

  Future<ReceivingSessionCloudState?> getCloudState(
    PocDeviceProfile profile,
    String localSessionId,
  );

  Future<void> recoverAmbiguousMobileOperations(DateTime nowUtc);

  Future<void> markMobileOperationsSending(
    List<String> operationIds,
    DateTime nowUtc,
  );

  Future<void> recordMobileTransportFailure({
    required List<String> operationIds,
    required String errorCode,
    required DateTime nowUtc,
  });

  Future<void> applyMobileSyncResults({
    required PocDeviceProfile profile,
    required List<MobileSyncOperationResult> results,
    required DateTime nowUtc,
  });

  Future<void> markCloudStateNeedsAttention({
    required PocDeviceProfile profile,
    required String localSessionId,
    required String errorCode,
    required String message,
    required DateTime nowUtc,
  });
}

abstract interface class ProcurementPocEventStore {
  Future<int> loadEventCursor(PocDeviceProfile profile);

  Future<void> applyMobileSyncEvent({
    required PocDeviceProfile profile,
    required MobileSyncEvent event,
    required DateTime receivedAtUtc,
  });

  Future<void> recordEventPollSuccess({
    required PocDeviceProfile profile,
    required DateTime nowUtc,
  });

  Future<void> recordEventPollFailure({
    required PocDeviceProfile profile,
    required String errorCode,
    required String message,
    required DateTime nowUtc,
  });
}

abstract interface class ProcurementPocControlStore {
  Future<PocControlCommand> enqueueControlCommand({
    required PocDeviceProfile profile,
    required PocControlCommandType commandType,
    required String commandId,
    required String sessionId,
    required int? expectedCloudVersion,
    required String? leaseId,
    required DateTime nowUtc,
  });

  Future<List<PocControlCommand>> loadPendingControlCommands(
    PocDeviceProfile profile,
    DateTime nowUtc,
  );

  Future<void> recoverAmbiguousControlCommands(
    PocDeviceProfile profile,
    DateTime nowUtc,
  );

  Future<PocControlCommand> markControlCommandSending(
    PocDeviceProfile profile,
    String commandId,
    DateTime nowUtc,
  );

  Future<void> recordControlTransportFailure({
    required PocDeviceProfile profile,
    required String commandId,
    required String errorCode,
    required String message,
    required DateTime nowUtc,
  });

  Future<void> recordControlNeedsAttention({
    required PocDeviceProfile profile,
    required String commandId,
    required String errorCode,
    required String message,
    required DateTime nowUtc,
  });

  Future<void> completeControlCommand({
    required PocDeviceProfile profile,
    required String commandId,
    required ProcurementPocCommandResult result,
    required DateTime nowUtc,
  });

  Future<PocControlCommand?> findOutstandingControlCommand({
    required PocDeviceProfile profile,
    required PocControlCommandType commandType,
    required String sessionId,
  });

  Future<PocControlCommand?> findLatestCompletedFinalization(
    PocDeviceProfile profile,
    String sessionId,
  );

  Future<PocControlCommand> requeueCompletedControlCommand({
    required PocDeviceProfile profile,
    required String commandId,
    required DateTime nowUtc,
  });

  Future<List<ReceivingSessionCloudState>> listLeasedOperatorSessions(
    PocDeviceProfile profile,
  );
}

abstract interface class ProcurementPocReadStore {
  Future<List<LocalReceivingSessionRecord>> listLocalReceivingSessions();

  Future<List<LocalReceivingEntryRecord>> listLocalReceivingEntries(
    String sessionId,
  );

  Future<List<LocalOutboxOperation>> listLocalOutboxOperations({
    String? aggregateId,
  });

  Future<List<RemoteReceivingSessionProjection>> listRemoteSessions(
    PocDeviceProfile profile,
  );

  Future<RemoteReceivingSessionProjection?> getRemoteSession(
    PocDeviceProfile profile,
    String sessionId,
  );

  Future<void> saveLiveView({
    required PocDeviceProfile profile,
    required ReceivingSessionLiveView liveView,
  });

  Future<void> saveSessionList({
    required PocDeviceProfile profile,
    required List<RemoteReceivingSessionProjection> sessions,
  });

  Future<List<PocControlCommand>> listControlCommands(
    PocDeviceProfile profile, {
    String? sessionId,
  });

  Future<PocSyncDiagnostics> loadDiagnostics(PocDeviceProfile profile);
}

abstract interface class ProcurementPocClock {
  DateTime nowUtc();
}

final class SystemProcurementPocClock implements ProcurementPocClock {
  const SystemProcurementPocClock();

  @override
  DateTime nowUtc() => DateTime.now().toUtc();
}

abstract interface class ProcurementPocScheduledTask {
  void cancel();
}

abstract interface class ProcurementPocScheduler {
  ProcurementPocScheduledTask schedulePeriodic(
    Duration interval,
    void Function() callback,
  );
}
