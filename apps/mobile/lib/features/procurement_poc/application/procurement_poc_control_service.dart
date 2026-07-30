import '../../../core/database/local_id_generator.dart';

// ignore_for_file: prefer_initializing_formals

import 'procurement_poc_feature.dart';
import 'procurement_poc_ports.dart';
import '../domain/procurement_poc_models.dart';

final class ControlRunResult {
  const ControlRunResult({
    required this.completed,
    required this.pending,
    required this.needsAttention,
    required this.message,
  });

  final int completed;
  final int pending;
  final int needsAttention;
  final String message;
}

final class ProcurementPocControlService {
  ProcurementPocControlService({
    required ProcurementPocFeatureConfiguration feature,
    required ProcurementPocProfileStore profileStore,
    required ProcurementPocSyncStore syncStore,
    required ProcurementPocControlStore controlStore,
    required ProcurementPocApiFactory apiFactory,
    ProcurementPocClock clock = const SystemProcurementPocClock(),
    LocalIdGenerator? idGenerator,
    this.heartbeatLeadTime = const Duration(minutes: 2),
  }) : _feature = feature,
       _profileStore = profileStore,
       _syncStore = syncStore,
       _controlStore = controlStore,
       _apiFactory = apiFactory,
       _clock = clock,
       _idGenerator = idGenerator ?? UuidV7LocalIdGenerator();

  final ProcurementPocFeatureConfiguration _feature;
  final ProcurementPocProfileStore _profileStore;
  final ProcurementPocSyncStore _syncStore;
  final ProcurementPocControlStore _controlStore;
  final ProcurementPocApiFactory _apiFactory;
  final ProcurementPocClock _clock;
  final LocalIdGenerator _idGenerator;
  final Duration heartbeatLeadTime;
  var _isRunning = false;
  var _heartbeatScheduling = false;

  Future<PocControlCommand> queueApproval({
    required String sessionId,
    required int expectedVersion,
  }) async {
    return _queueVersioned(
      type: PocControlCommandType.approve,
      sessionId: sessionId,
      expectedVersion: expectedVersion,
    );
  }

  Future<PocControlCommand> queueFinalization({
    required String sessionId,
    required int expectedVersion,
  }) async {
    return _queueVersioned(
      type: PocControlCommandType.finalize,
      sessionId: sessionId,
      expectedVersion: expectedVersion,
    );
  }

  Future<PocControlCommand> _queueVersioned({
    required PocControlCommandType type,
    required String sessionId,
    required int expectedVersion,
  }) async {
    final profile = await _requireProfile();
    return _controlStore.enqueueControlCommand(
      profile: profile,
      commandType: type,
      commandId: _idGenerator.newUuidV7(),
      sessionId: sessionId,
      expectedCloudVersion: expectedVersion,
      leaseId: null,
      nowUtc: _clock.nowUtc(),
    );
  }

  Future<int> queueDueHeartbeats() async {
    if (_heartbeatScheduling) {
      return 0;
    }
    _heartbeatScheduling = true;
    try {
      final profile = await _requireProfile();
      final now = _clock.nowUtc();
      var queued = 0;
      for (final session in await _controlStore.listLeasedOperatorSessions(
        profile,
      )) {
        final expiry = session.leaseExpiresAtUtc;
        if (expiry == null || !expiry.isAfter(now)) {
          await _syncStore.markCloudStateNeedsAttention(
            profile: profile,
            localSessionId: session.localSessionId,
            errorCode: 'RECEIVING_POC_LEASE_EXPIRED',
            message:
                'The server lease expired. Forced recovery is not available '
                'in this development POC.',
            nowUtc: now,
          );
          continue;
        }
        if (expiry.difference(now) > heartbeatLeadTime) {
          continue;
        }
        final existing = await _controlStore.findOutstandingControlCommand(
          profile: profile,
          commandType: PocControlCommandType.heartbeat,
          sessionId: session.localSessionId,
        );
        if (existing != null) {
          continue;
        }
        await _controlStore.enqueueControlCommand(
          profile: profile,
          commandType: PocControlCommandType.heartbeat,
          commandId: _idGenerator.newUuidV7(),
          sessionId: session.localSessionId,
          expectedCloudVersion: null,
          leaseId: session.leaseId,
          nowUtc: now,
        );
        queued++;
      }
      return queued;
    } finally {
      _heartbeatScheduling = false;
    }
  }

  Future<PocControlCommand> retrySameFinalization(String sessionId) async {
    final profile = await _requireProfile();
    final completed = await _controlStore.findLatestCompletedFinalization(
      profile,
      sessionId,
    );
    if (completed == null) {
      throw StateError(
        'No completed finalization command is available for replay.',
      );
    }
    return _controlStore.requeueCompletedControlCommand(
      profile: profile,
      commandId: completed.commandId,
      nowUtc: _clock.nowUtc(),
    );
  }

  Future<ControlRunResult> executePending() async {
    if (!_feature.isEnabled) {
      return const ControlRunResult(
        completed: 0,
        pending: 0,
        needsAttention: 0,
        message: 'The development Procurement POC is disabled.',
      );
    }
    if (_isRunning) {
      return const ControlRunResult(
        completed: 0,
        pending: 0,
        needsAttention: 0,
        message: 'Control-command processing is already running.',
      );
    }
    _isRunning = true;
    var completed = 0;
    var pending = 0;
    var attention = 0;
    try {
      final profile = await _profileStore.loadActiveProfile();
      if (profile == null) {
        return const ControlRunResult(
          completed: 0,
          pending: 0,
          needsAttention: 0,
          message: 'Configure a development device profile first.',
        );
      }
      await _controlStore.recoverAmbiguousControlCommands(
        profile,
        _clock.nowUtc(),
      );
      final commands = await _controlStore.loadPendingControlCommands(
        profile,
        _clock.nowUtc(),
      );
      for (final queued in commands) {
        final command = await _controlStore.markControlCommandSending(
          profile,
          queued.commandId,
          _clock.nowUtc(),
        );
        final api = _apiFactory(profile);
        try {
          final result = await _execute(api, command);
          final cloud = await _syncStore.getCloudState(
            profile,
            command.sessionId,
          );
          _validateSuccessfulResult(command, result, cloud, _clock.nowUtc());
          await _controlStore.completeControlCommand(
            profile: profile,
            commandId: command.commandId,
            result: result,
            nowUtc: _clock.nowUtc(),
          );
          completed++;
        } on ProcurementPocApiException catch (error) {
          if (error.retryable || error.responseAmbiguous) {
            await _controlStore.recordControlTransportFailure(
              profile: profile,
              commandId: command.commandId,
              errorCode: error.code,
              message: error.message,
              nowUtc: _clock.nowUtc(),
            );
            pending++;
          } else {
            await _controlStore.recordControlNeedsAttention(
              profile: profile,
              commandId: command.commandId,
              errorCode: error.code,
              message: error.message,
              nowUtc: _clock.nowUtc(),
            );
            attention++;
          }
        } on ProcurementPocLocalException catch (error) {
          await _controlStore.recordControlTransportFailure(
            profile: profile,
            commandId: command.commandId,
            errorCode: error.code,
            message: error.message,
            nowUtc: _clock.nowUtc(),
          );
          pending++;
        } finally {
          api.dispose();
        }
      }
      return ControlRunResult(
        completed: completed,
        pending: pending,
        needsAttention: attention,
        message: commands.isEmpty
            ? 'No control command is ready.'
            : 'Control-command processing completed.',
      );
    } finally {
      _isRunning = false;
    }
  }

  static Future<ProcurementPocCommandResult> _execute(
    ProcurementPocApi api,
    PocControlCommand command,
  ) {
    return switch (command.commandType) {
      PocControlCommandType.heartbeat => api.heartbeat(
        sessionId: command.sessionId,
        leaseId: command.leaseId!,
        idempotencyKey: command.commandId,
      ),
      PocControlCommandType.approve => api.approve(
        sessionId: command.sessionId,
        expectedVersion: command.expectedCloudVersion!,
        idempotencyKey: command.commandId,
      ),
      PocControlCommandType.finalize => api.finalize(
        sessionId: command.sessionId,
        expectedVersion: command.expectedCloudVersion!,
        idempotencyKey: command.commandId,
      ),
    };
  }

  Future<PocDeviceProfile> _requireProfile() async {
    final profile = await _profileStore.loadActiveProfile();
    if (profile == null) {
      throw const ProcurementPocLocalException(
        ProcurementPocLocalException.localStateConflict,
        'Configure a development device profile first.',
      );
    }
    return profile;
  }

  static void _validateSuccessfulResult(
    PocControlCommand command,
    ProcurementPocCommandResult result,
    ReceivingSessionCloudState? cloud,
    DateTime nowUtc,
  ) {
    final minimumVersion = command.expectedCloudVersion ?? cloud?.cloudVersion;
    final versionRegressed =
        minimumVersion != null && result.version < minimumVersion;
    final valid = switch (command.commandType) {
      PocControlCommandType.heartbeat =>
        cloud != null &&
            result.sessionId == command.sessionId &&
            result.status == 'ReceivingInProgress' &&
            result.leaseId != null &&
            result.leaseExpiresAtUtc != null &&
            result.leaseExpiresAtUtc!.isAfter(nowUtc) &&
            result.finalizationId == null &&
            !versionRegressed,
      PocControlCommandType.approve =>
        result.sessionId == command.sessionId &&
            result.status == 'Approved' &&
            result.leaseId == null &&
            result.leaseExpiresAtUtc == null &&
            result.finalizationId == null &&
            !versionRegressed,
      PocControlCommandType.finalize =>
        result.sessionId == command.sessionId &&
            result.status == 'Finalized' &&
            result.leaseId == null &&
            result.leaseExpiresAtUtc == null &&
            result.finalizationId != null &&
            !versionRegressed,
    };
    if (!valid) {
      throw const ProcurementPocApiException(
        code: 'POC_RESPONSE_INVALID',
        message:
            'The server may have committed the control command, but the '
            'successful response did not match its required cloud state. '
            'The same idempotency key will be reused.',
        retryable: true,
        responseAmbiguous: true,
      );
    }
  }
}
