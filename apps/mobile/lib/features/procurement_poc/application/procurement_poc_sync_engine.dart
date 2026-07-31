import '../../../core/sync/local_outbox_operation.dart';

// ignore_for_file: prefer_initializing_formals

import 'procurement_poc_feature.dart';
import 'procurement_poc_ports.dart';
import '../domain/procurement_poc_models.dart';

final class SyncRunResult {
  const SyncRunResult({
    required this.sent,
    required this.completed,
    required this.needsAttention,
    required this.rejected,
    required this.networkAmbiguous,
    required this.message,
  });

  const SyncRunResult.idle(String message)
    : this(
        sent: 0,
        completed: 0,
        needsAttention: 0,
        rejected: 0,
        networkAmbiguous: false,
        message: message,
      );

  final int sent;
  final int completed;
  final int needsAttention;
  final int rejected;
  final bool networkAmbiguous;
  final String message;
}

final class ProcurementPocSyncEngine {
  ProcurementPocSyncEngine({
    required ProcurementPocFeatureConfiguration feature,
    required ProcurementPocProfileStore profileStore,
    required ProcurementPocSyncStore syncStore,
    required ProcurementPocApiFactory apiFactory,
    ProcurementPocClock clock = const SystemProcurementPocClock(),
  }) : _feature = feature,
       _profileStore = profileStore,
       _syncStore = syncStore,
       _apiFactory = apiFactory,
       _clock = clock;

  static const maximumBatchSize = 50;

  final ProcurementPocFeatureConfiguration _feature;
  final ProcurementPocProfileStore _profileStore;
  final ProcurementPocSyncStore _syncStore;
  final ProcurementPocApiFactory _apiFactory;
  final ProcurementPocClock _clock;
  var _isRunning = false;

  Future<SyncRunResult> synchronize() async {
    if (!_feature.isEnabled) {
      return const SyncRunResult.idle(
        'The development Procurement POC is disabled.',
      );
    }
    if (_isRunning) {
      return const SyncRunResult.idle('Synchronization is already running.');
    }
    _isRunning = true;
    try {
      final profile = await _profileStore.loadActiveProfile();
      if (profile == null) {
        return const SyncRunResult.idle(
          'Configure a development device profile first.',
        );
      }
      await _syncStore.recoverAmbiguousMobileOperations(_clock.nowUtc());

      var sent = 0;
      var completed = 0;
      var attention = 0;
      var rejected = 0;
      for (var pass = 0; pass < 100; pass++) {
        final candidates = await _syncStore.loadSyncCandidates();
        if (candidates.isEmpty) {
          return SyncRunResult(
            sent: sent,
            completed: completed,
            needsAttention: attention,
            rejected: rejected,
            networkAmbiguous: false,
            message: sent == 0
                ? 'No queued operation is ready.'
                : 'Foreground synchronization completed.',
          );
        }

        final start = candidates
            .where(
              (operation) => operation.operationType == 'StartReceivingSession',
            )
            .firstOrNull;
        final prepared = start == null
            ? await _prepareDependentBatch(profile, candidates)
            : <_PreparedOperation>[
                _PreparedOperation(
                  operation: start,
                  envelope: _envelope(start, leaseId: null),
                ),
              ];
        if (prepared.isEmpty) {
          return SyncRunResult(
            sent: sent,
            completed: completed,
            needsAttention: attention,
            rejected: rejected,
            networkAmbiguous: false,
            message:
                'Queued work is blocked by missing or expired lease state.',
          );
        }

        final ids = prepared
            .map((item) => item.operation.operationId)
            .toList(growable: false);
        final now = _clock.nowUtc();
        await _syncStore.markMobileOperationsSending(ids, now);
        ProcurementPocApi? api;
        try {
          api = _apiFactory(profile);
          final response = await api.sendOperations(
            prepared.map((item) => item.envelope).toList(growable: false),
          );
          sent += prepared.length;
          if (!_responseMatches(prepared, response.operations) ||
              !await _successfulResponsesAreValid(
                profile,
                prepared,
                response.operations,
              )) {
            await _syncStore.recordMobileTransportFailure(
              operationIds: ids,
              errorCode: 'POC_RESPONSE_INVALID',
              nowUtc: _clock.nowUtc(),
            );
            return SyncRunResult(
              sent: sent,
              completed: completed,
              needsAttention: attention,
              rejected: rejected,
              networkAmbiguous: true,
              message:
                  'The response did not match the durable operations or '
                  'required cloud state. The same operations remain queued.',
            );
          }
          if (response.operations.any(_isRetryableTransportResult)) {
            await _syncStore.recordMobileTransportFailure(
              operationIds: ids,
              errorCode: 'POC_NETWORK_AMBIGUOUS',
              nowUtc: _clock.nowUtc(),
            );
            return SyncRunResult(
              sent: sent,
              completed: completed,
              needsAttention: attention,
              rejected: rejected,
              networkAmbiguous: true,
              message:
                  'The server reported an unavailable or ambiguous transport '
                  'outcome. The same operations remain queued.',
            );
          }
          try {
            await _syncStore.applyMobileSyncResults(
              profile: profile,
              results: response.operations,
              nowUtc: _clock.nowUtc(),
            );
          } on ProcurementPocLocalException catch (error) {
            await _syncStore.recordMobileTransportFailure(
              operationIds: ids,
              errorCode: error.code,
              nowUtc: _clock.nowUtc(),
            );
            return SyncRunResult(
              sent: sent,
              completed: completed,
              needsAttention: attention,
              rejected: rejected,
              networkAmbiguous: true,
              message: error.message,
            );
          }
          for (final result in response.operations) {
            if (result.isAccepted) {
              completed++;
            } else if (result.resultStatus == 'NeedsAttention') {
              attention++;
            } else if (result.resultStatus == 'Rejected') {
              rejected++;
            }
          }
        } on ProcurementPocApiException catch (error) {
          if (error.retryable || error.responseAmbiguous) {
            await _syncStore.recordMobileTransportFailure(
              operationIds: ids,
              errorCode: error.code,
              nowUtc: _clock.nowUtc(),
            );
            return SyncRunResult(
              sent: sent + prepared.length,
              completed: completed,
              needsAttention: attention,
              rejected: rejected,
              networkAmbiguous: true,
              message: error.message,
            );
          }
          final results = prepared
              .map(
                (item) => MobileSyncOperationResult(
                  operationId: item.operation.operationId,
                  aggregateId: item.operation.aggregateId,
                  localSequence: item.operation.localSequence,
                  resultStatus: 'NeedsAttention',
                  cloudAggregateVersion: null,
                  cloudReference: null,
                  leaseId: null,
                  leaseExpiresAtUtc: null,
                  errorCode: error.code,
                  message: error.message,
                ),
              )
              .toList(growable: false);
          await _syncStore.applyMobileSyncResults(
            profile: profile,
            results: results,
            nowUtc: _clock.nowUtc(),
          );
          attention += results.length;
        } on Object {
          await _syncStore.recordMobileTransportFailure(
            operationIds: ids,
            errorCode: 'POC_NETWORK_AMBIGUOUS',
            nowUtc: _clock.nowUtc(),
          );
          return SyncRunResult(
            sent: sent + prepared.length,
            completed: completed,
            needsAttention: attention,
            rejected: rejected,
            networkAmbiguous: true,
            message:
                'The network outcome is unknown. The same durable operations '
                'remain queued for safe replay.',
          );
        } finally {
          api?.dispose();
        }
      }
      return SyncRunResult(
        sent: sent,
        completed: completed,
        needsAttention: attention,
        rejected: rejected,
        networkAmbiguous: false,
        message: 'Synchronization stopped at the safe pass limit.',
      );
    } finally {
      _isRunning = false;
    }
  }

  Future<List<_PreparedOperation>> _prepareDependentBatch(
    PocDeviceProfile profile,
    List<LocalOutboxOperation> candidates,
  ) async {
    final now = _clock.nowUtc();
    final prepared = <_PreparedOperation>[];
    final blockedAggregates = <String>{};
    for (final operation in candidates) {
      if (prepared.length == maximumBatchSize ||
          blockedAggregates.contains(operation.aggregateId)) {
        continue;
      }
      final cloud = await _syncStore.getCloudState(
        profile,
        operation.aggregateId,
      );
      if (cloud == null) {
        await _markLocalDependencyAttention(
          profile,
          operation,
          errorCode: 'POC_CLOUD_STATE_MISSING',
          message:
              'Start must synchronize and return its lease before dependent '
              'operations can be sent.',
        );
        blockedAggregates.add(operation.aggregateId);
        continue;
      }
      if (!cloud.hasUsableLease(now)) {
        await _markLocalDependencyAttention(
          profile,
          operation,
          errorCode: 'RECEIVING_POC_LEASE_EXPIRED',
          message:
              'The persisted cloud lease is missing or expired. Forced lease '
              'recovery is not implemented.',
        );
        await _syncStore.markCloudStateNeedsAttention(
          profile: profile,
          localSessionId: operation.aggregateId,
          errorCode: 'RECEIVING_POC_LEASE_EXPIRED',
          message: 'The persisted cloud lease is missing or expired.',
          nowUtc: now,
        );
        blockedAggregates.add(operation.aggregateId);
        continue;
      }
      prepared.add(
        _PreparedOperation(
          operation: operation,
          envelope: _envelope(operation, leaseId: cloud.leaseId),
        ),
      );
    }
    return prepared;
  }

  Future<void> _markLocalDependencyAttention(
    PocDeviceProfile profile,
    LocalOutboxOperation operation, {
    required String errorCode,
    required String message,
  }) async {
    final now = _clock.nowUtc();
    await _syncStore.markMobileOperationsSending([operation.operationId], now);
    await _syncStore.applyMobileSyncResults(
      profile: profile,
      results: [
        MobileSyncOperationResult(
          operationId: operation.operationId,
          aggregateId: operation.aggregateId,
          localSequence: operation.localSequence,
          resultStatus: 'NeedsAttention',
          cloudAggregateVersion: null,
          cloudReference: null,
          leaseId: null,
          leaseExpiresAtUtc: null,
          errorCode: errorCode,
          message: message,
        ),
      ],
      nowUtc: now,
    );
  }

  static MobileSyncOperationEnvelope _envelope(
    LocalOutboxOperation operation, {
    required String? leaseId,
  }) {
    return MobileSyncOperationEnvelope(
      operationId: operation.operationId,
      operationType: operation.operationType,
      aggregateId: operation.aggregateId,
      localSequence: operation.localSequence,
      expectedCloudVersion: operation.expectedCloudVersion,
      payloadJson: operation.payloadJson,
      payloadHash: operation.payloadHash,
      leaseId: leaseId,
    );
  }

  static bool _responseMatches(
    List<_PreparedOperation> sent,
    List<MobileSyncOperationResult> received,
  ) {
    if (sent.length != received.length) {
      return false;
    }
    for (var index = 0; index < sent.length; index++) {
      final operation = sent[index].operation;
      final result = received[index];
      if (operation.operationId != result.operationId ||
          operation.aggregateId != result.aggregateId ||
          operation.localSequence != result.localSequence) {
        return false;
      }
    }
    return true;
  }

  static bool _isRetryableTransportResult(
    MobileSyncOperationResult result,
  ) {
    return !result.isAccepted &&
        const {
          'POC_NETWORK_AMBIGUOUS',
          'POC_RESPONSE_INVALID',
          'POC_HTTP_ERROR',
          'POC_CLIENT_DISPOSED',
          'POC_RUNTIME_DISPOSED',
        }.contains(result.errorCode);
  }

  Future<bool> _successfulResponsesAreValid(
    PocDeviceProfile profile,
    List<_PreparedOperation> sent,
    List<MobileSyncOperationResult> received,
  ) async {
    final now = _clock.nowUtc();
    for (var index = 0; index < sent.length; index++) {
      final operation = sent[index].operation;
      final result = received[index];
      if (!result.isAccepted) {
        continue;
      }
      final version = result.cloudAggregateVersion;
      if (version == null ||
          version <= 0 ||
          result.cloudReference == null ||
          result.cloudReference!.isEmpty) {
        return false;
      }
      final existing = await _syncStore.getCloudState(
        profile,
        operation.aggregateId,
      );
      if (existing != null && version < existing.cloudVersion) {
        return false;
      }
      final isSubmit = operation.operationType == 'SubmitReceivingSession';
      if (isSubmit) {
        if (result.leaseId != null || result.leaseExpiresAtUtc != null) {
          return false;
        }
      } else if (result.leaseId == null ||
          result.leaseExpiresAtUtc == null ||
          !result.leaseExpiresAtUtc!.isAfter(now)) {
        return false;
      }
    }
    return true;
  }
}

final class _PreparedOperation {
  const _PreparedOperation({required this.operation, required this.envelope});

  final LocalOutboxOperation operation;
  final MobileSyncOperationEnvelope envelope;
}

extension<T> on Iterable<T> {
  T? get firstOrNull {
    final iterator = this.iterator;
    return iterator.moveNext() ? iterator.current : null;
  }
}
