import 'dart:convert';

// ignore_for_file: prefer_initializing_formals

import 'package:drift/drift.dart';

import '../../../core/database/trader_pro_local_database.dart';
import '../../../core/sync/local_outbox_operation.dart';
import '../../../core/sync/outbox_operation_status.dart';
import '../../receiving/domain/exact_weight.dart';
import '../../receiving/domain/local_receiving_records.dart';
import '../../receiving/infrastructure/local_receiving_store.dart';
import '../application/procurement_poc_ports.dart';
import '../domain/procurement_poc_models.dart';

final class ProcurementPocLocalRepository
    implements
        ProcurementPocProfileStore,
        ProcurementPocSyncStore,
        ProcurementPocEventStore,
        ProcurementPocControlStore,
        ProcurementPocReadStore {
  ProcurementPocLocalRepository({
    required TraderProLocalDatabase database,
    required LocalReceivingStore localReceivingStore,
  }) : _database = database,
       _localReceivingStore = localReceivingStore;

  static const _profileKey = 'active';
  static const _accepted = 'Accepted';
  static const _pending = 'Pending';
  static const _sending = 'Sending';
  static const _needsAttention = 'NeedsAttention';
  static const _rejected = 'Rejected';
  static const _receivingAggregateType = 'ReceivingSession';
  static const _receivingOperationTypes = <String>[
    'StartReceivingSession',
    'RecordReceivingEntry',
    'SubmitReceivingSession',
  ];

  final TraderProLocalDatabase _database;
  final LocalReceivingStore _localReceivingStore;

  @override
  Future<PocDeviceProfile?> loadActiveProfile() async {
    final row =
        await (_database.select(_database.pocDeviceProfiles)
              ..where((table) => table.profileKey.equals(_profileKey)))
            .getSingleOrNull();
    return row == null ? null : _mapProfile(row);
  }

  @override
  Future<PocDeviceProfile> saveActiveProfile(PocDeviceProfile profile) async {
    return _database.transaction(() async {
      final existing =
          await (_database.select(_database.pocDeviceProfiles)
                ..where((table) => table.profileKey.equals(_profileKey)))
              .getSingleOrNull();
      if (existing != null &&
          (existing.backendBaseUrl != profile.backendBaseUrl ||
              existing.workspaceId != profile.workspaceId ||
              existing.deviceId != profile.deviceId)) {
        await _requireProfileIdentityCanChange();
      }

      final createdAt = existing?.createdAtUtc ?? _utc(profile.createdAtUtc);
      await _database
          .into(_database.pocDeviceProfiles)
          .insertOnConflictUpdate(
            PocDeviceProfilesCompanion.insert(
              profileKey: _profileKey,
              backendBaseUrl: profile.backendBaseUrl,
              workspaceId: profile.workspaceId,
              deviceId: profile.deviceId,
              displayRole: profile.displayRole.storageValue,
              displayLabel: Value(_trimToNull(profile.displayLabel)),
              automaticSyncPaused: Value(profile.automaticSyncPaused),
              createdAtUtc: createdAt,
              updatedAtUtc: _utc(profile.updatedAtUtc),
            ),
          );
      await _ensureSource(profile, profile.updatedAtUtc);
      return (await loadActiveProfile())!;
    });
  }

  Future<void> _requireProfileIdentityCanChange() async {
    final unresolvedOperation =
        await (_database.select(_database.localOutboxOperations)
              ..where(
                (table) =>
                    table.aggregateType.equals(_receivingAggregateType) &
                    table.operationType.isIn(_receivingOperationTypes) &
                    table.status.isIn([
                      _pending,
                      _sending,
                      _needsAttention,
                      _rejected,
                    ]),
              )
              ..limit(1))
            .getSingleOrNull();
    final openSession =
        await (_database.select(_database.localReceivingSessions)
              ..where((table) => table.localStatus.equals('Open'))
              ..limit(1))
            .getSingleOrNull();
    final activeCloudSession =
        await (_database.select(_database.receivingSessionCloudStates)
              ..where(
                (table) => table.cloudStatus.equals('ReceivingInProgress'),
              )
              ..limit(1))
            .getSingleOrNull();
    final pendingCommand =
        await (_database.select(_database.pocControlCommands)
              ..where((table) => table.status.isNotValue('Completed'))
              ..limit(1))
            .getSingleOrNull();
    if (unresolvedOperation != null ||
        openSession != null ||
        activeCloudSession != null ||
        pendingCommand != null) {
      throw const ProcurementPocLocalException(
        ProcurementPocLocalException.profileChangeBlocked,
        'Development identity cannot change while local sessions, sync '
        'operations, leases, or control commands still require attention.',
      );
    }
  }

  @override
  Future<PocDeviceProfile> setAutomaticSyncPaused(
    bool paused,
    DateTime nowUtc,
  ) async {
    final current = await loadActiveProfile();
    if (current == null) {
      throw const ProcurementPocLocalException(
        ProcurementPocLocalException.localStateConflict,
        'A development device profile is required.',
      );
    }
    await (_database.update(
      _database.pocDeviceProfiles,
    )..where((table) => table.profileKey.equals(_profileKey))).write(
      PocDeviceProfilesCompanion(
        automaticSyncPaused: Value(paused),
        updatedAtUtc: Value(_utc(nowUtc)),
      ),
    );
    return (await loadActiveProfile())!;
  }

  @override
  Future<List<LocalOutboxOperation>> loadSyncCandidates() async {
    final rows =
        await (_database.select(_database.localOutboxOperations)
              ..where(
                (table) =>
                    table.aggregateType.equals(_receivingAggregateType) &
                    table.operationType.isIn(_receivingOperationTypes),
              )
              ..orderBy([
                (table) => OrderingTerm.asc(table.aggregateId),
                (table) => OrderingTerm.asc(table.localSequence),
              ]))
            .get();
    final candidates = <LocalOutboxOperation>[];
    String? aggregateId;
    var blocked = false;
    for (final row in rows) {
      if (aggregateId != row.aggregateId) {
        aggregateId = row.aggregateId;
        blocked = false;
      }
      if (blocked || row.status == _accepted) {
        continue;
      }
      if (row.status == _pending) {
        candidates.add(_mapOutbox(row));
        continue;
      }
      blocked = true;
    }
    return candidates;
  }

  @override
  Future<ReceivingSessionCloudState?> getCloudState(
    PocDeviceProfile profile,
    String localSessionId,
  ) async {
    final row =
        await (_database.select(_database.receivingSessionCloudStates)..where(
              (table) =>
                  table.localSessionId.equals(localSessionId) &
                  table.sourceKey.equals(profile.sourceKey) &
                  table.boundDeviceId.equals(profile.deviceId),
            ))
            .getSingleOrNull();
    return row == null ? null : _mapCloudState(row);
  }

  @override
  Future<void> recoverAmbiguousMobileOperations(DateTime nowUtc) async {
    requireUtc(nowUtc, 'nowUtc');
    await _database.customStatement(
      'UPDATE local_outbox_operations '
      "SET status = 'Pending', attempt_count = attempt_count + 1, "
      'last_attempt_at_utc = ?, '
      "last_error_code = 'POC_NETWORK_AMBIGUOUS' "
      "WHERE status = 'Sending' "
      "AND aggregate_type = 'ReceivingSession' "
      "AND operation_type IN ('StartReceivingSession', "
      "'RecordReceivingEntry', 'SubmitReceivingSession')",
      [_utc(nowUtc)],
    );
  }

  @override
  Future<void> markMobileOperationsSending(
    List<String> operationIds,
    DateTime nowUtc,
  ) async {
    requireUtc(nowUtc, 'nowUtc');
    await _database.transaction(() async {
      for (final operationId in operationIds) {
        final row = await _requiredOutbox(operationId);
        if (row.status != _pending) {
          throw ProcurementPocLocalException(
            ProcurementPocLocalException.localStateConflict,
            'Operation $operationId is ${row.status}; Pending is required.',
          );
        }
        await (_database.update(
          _database.localOutboxOperations,
        )..where((table) => table.operationId.equals(operationId))).write(
          LocalOutboxOperationsCompanion(
            status: const Value(_sending),
            lastAttemptAtUtc: Value(_utc(nowUtc)),
            lastErrorCode: const Value(null),
          ),
        );
      }
    });
  }

  @override
  Future<void> recordMobileTransportFailure({
    required List<String> operationIds,
    required String errorCode,
    required DateTime nowUtc,
  }) async {
    await _database.transaction(() async {
      for (final operationId in operationIds) {
        final row = await _requiredOutbox(operationId);
        if (row.status != _sending) {
          continue;
        }
        await (_database.update(
          _database.localOutboxOperations,
        )..where((table) => table.operationId.equals(operationId))).write(
          LocalOutboxOperationsCompanion(
            status: const Value(_pending),
            attemptCount: Value(row.attemptCount + 1),
            lastAttemptAtUtc: Value(_utc(nowUtc)),
            lastErrorCode: Value(errorCode),
          ),
        );
      }
    });
  }

  @override
  Future<void> applyMobileSyncResults({
    required PocDeviceProfile profile,
    required List<MobileSyncOperationResult> results,
    required DateTime nowUtc,
  }) async {
    requireUtc(nowUtc, 'nowUtc');
    await _database.transaction(() async {
      final activeProfile = await loadActiveProfile();
      if (activeProfile == null ||
          activeProfile.sourceKey != profile.sourceKey ||
          activeProfile.deviceId != profile.deviceId) {
        throw const ProcurementPocLocalException(
          ProcurementPocLocalException.localStateConflict,
          'The active execution context changed during synchronization.',
        );
      }
      for (final result in results) {
        final operation = await _requiredOutbox(result.operationId);
        if (operation.status != _sending ||
            operation.aggregateId != result.aggregateId ||
            operation.localSequence != result.localSequence) {
          throw const ProcurementPocLocalException(
            ProcurementPocLocalException.localStateConflict,
            'The mobile sync response did not match the durable operation.',
          );
        }
        if (result.isAccepted) {
          await _applyAcceptedOperation(operation, result, profile, nowUtc);
        } else if (result.resultStatus == _needsAttention ||
            result.resultStatus == _rejected) {
          await (_database.update(_database.localOutboxOperations)..where(
                (table) => table.operationId.equals(operation.operationId),
              ))
              .write(
                LocalOutboxOperationsCompanion(
                  status: Value(result.resultStatus),
                  attemptCount: Value(operation.attemptCount + 1),
                  lastAttemptAtUtc: Value(_utc(nowUtc)),
                  lastErrorCode: Value(
                    result.errorCode ?? 'POC_OPERATION_FAILED',
                  ),
                ),
              );
          await _setCloudErrorIfPresent(
            profile,
            operation.aggregateId,
            result.errorCode ?? 'POC_OPERATION_FAILED',
            result.message ?? 'The cloud operation requires attention.',
            nowUtc,
          );
        } else {
          throw const ProcurementPocLocalException(
            ProcurementPocLocalException.localStateConflict,
            'The backend returned an unsupported operation result status.',
          );
        }
      }
    });
  }

  Future<void> _applyAcceptedOperation(
    LocalOutboxOperationRow operation,
    MobileSyncOperationResult result,
    PocDeviceProfile profile,
    DateTime nowUtc,
  ) async {
    final version = result.cloudAggregateVersion;
    final reference = result.cloudReference;
    if (version == null || version <= 0 || reference == null) {
      throw const ProcurementPocLocalException(
        ProcurementPocLocalException.cloudContractInvalid,
        'An accepted operation omitted cloud version or reference.',
      );
    }
    final existing =
        await (_database.select(_database.receivingSessionCloudStates)..where(
              (table) => table.localSessionId.equals(operation.aggregateId),
            ))
            .getSingleOrNull();
    if (existing != null &&
        (existing.cloudSessionId != operation.aggregateId ||
            existing.sourceKey != profile.sourceKey ||
            existing.boundDeviceId != profile.deviceId)) {
      throw const ProcurementPocLocalException(
        ProcurementPocLocalException.cloudContractInvalid,
        'Cloud state does not match the active local session, source, and '
        'device context.',
      );
    }
    if (existing != null && version < existing.cloudVersion) {
      throw const ProcurementPocLocalException(
        ProcurementPocLocalException.cloudContractInvalid,
        'The cloud response version moved backward.',
      );
    }

    final isStart = operation.operationType == 'StartReceivingSession';
    final isSubmit = operation.operationType == 'SubmitReceivingSession';
    final hasValidLease =
        result.leaseId != null &&
        result.leaseExpiresAtUtc != null &&
        result.leaseExpiresAtUtc!.isAfter(nowUtc);
    if (!isSubmit && !hasValidLease) {
      throw const ProcurementPocLocalException(
        ProcurementPocLocalException.cloudContractInvalid,
        'A successful receiving operation did not return a valid active '
        'cloud lease.',
      );
    }
    if (isSubmit &&
        (result.leaseId != null || result.leaseExpiresAtUtc != null)) {
      throw const ProcurementPocLocalException(
        ProcurementPocLocalException.cloudContractInvalid,
        'Submission success retained lease state.',
      );
    }
    if (isStart && existing != null && existing.cloudVersion > version) {
      throw const ProcurementPocLocalException(
        ProcurementPocLocalException.cloudContractInvalid,
        'Start synchronization returned a regressive cloud version.',
      );
    }
    final cloudStatus = isSubmit ? 'SubmittedForReview' : 'ReceivingInProgress';
    if (existing == null) {
      await _database
          .into(_database.receivingSessionCloudStates)
          .insert(
            ReceivingSessionCloudStatesCompanion.insert(
              sourceKey: profile.sourceKey,
              boundDeviceId: profile.deviceId,
              localSessionId: operation.aggregateId,
              cloudSessionId: operation.aggregateId,
              cloudReference: reference,
              cloudStatus: cloudStatus,
              cloudVersion: version,
              leaseId: Value(isSubmit ? null : result.leaseId),
              leaseExpiresAtUtc: Value(
                isSubmit ? null : _nullableUtc(result.leaseExpiresAtUtc),
              ),
              editorDeviceId: Value(
                existing?.editorDeviceId ?? profile.deviceId,
              ),
              lastSuccessfulSyncAtUtc: _utc(nowUtc),
              lastCloudUpdateAtUtc: _utc(nowUtc),
              lastErrorCode: const Value(null),
              lastErrorMessage: const Value(null),
            ),
          );
    } else {
      await (_database.update(_database.receivingSessionCloudStates)..where(
            (table) =>
                table.localSessionId.equals(operation.aggregateId) &
                table.sourceKey.equals(profile.sourceKey) &
                table.boundDeviceId.equals(profile.deviceId),
          ))
          .write(
            ReceivingSessionCloudStatesCompanion(
              cloudReference: Value(reference),
              cloudStatus: Value(cloudStatus),
              cloudVersion: Value(version),
              leaseId: Value(isSubmit ? null : result.leaseId),
              leaseExpiresAtUtc: Value(
                isSubmit ? null : _nullableUtc(result.leaseExpiresAtUtc),
              ),
              editorDeviceId: Value(
                existing.editorDeviceId ?? profile.deviceId,
              ),
              lastSuccessfulSyncAtUtc: Value(_utc(nowUtc)),
              lastCloudUpdateAtUtc: Value(_utc(nowUtc)),
              lastErrorCode: const Value(null),
              lastErrorMessage: const Value(null),
            ),
          );
    }
    await (_database.update(
      _database.localOutboxOperations,
    )..where((table) => table.operationId.equals(operation.operationId))).write(
      LocalOutboxOperationsCompanion(
        status: const Value(_accepted),
        acceptedAtUtc: Value(_utc(nowUtc)),
        lastErrorCode: const Value(null),
      ),
    );
  }

  @override
  Future<void> markCloudStateNeedsAttention({
    required PocDeviceProfile profile,
    required String localSessionId,
    required String errorCode,
    required String message,
    required DateTime nowUtc,
  }) async {
    await _setCloudErrorIfPresent(
      profile,
      localSessionId,
      errorCode,
      message,
      nowUtc,
    );
  }

  Future<void> _setCloudErrorIfPresent(
    PocDeviceProfile profile,
    String localSessionId,
    String errorCode,
    String message,
    DateTime nowUtc,
  ) async {
    await (_database.update(_database.receivingSessionCloudStates)..where(
          (table) =>
              table.localSessionId.equals(localSessionId) &
              table.sourceKey.equals(profile.sourceKey) &
              table.boundDeviceId.equals(profile.deviceId),
        ))
        .write(
          ReceivingSessionCloudStatesCompanion(
            lastErrorCode: Value(errorCode),
            lastErrorMessage: Value(message),
            lastCloudUpdateAtUtc: Value(_utc(nowUtc)),
          ),
        );
  }

  @override
  Future<int> loadEventCursor(PocDeviceProfile profile) async {
    await _ensureSource(profile, profile.updatedAtUtc);
    final row = await (_database.select(
      _database.pocSyncSources,
    )..where((table) => table.sourceKey.equals(profile.sourceKey))).getSingle();
    return row.eventCursor;
  }

  @override
  Future<void> applyMobileSyncEvent({
    required PocDeviceProfile profile,
    required MobileSyncEvent event,
    required DateTime receivedAtUtc,
  }) async {
    requireUtc(receivedAtUtc, 'receivedAtUtc');
    await _database.transaction(() async {
      await _ensureSource(profile, receivedAtUtc);
      final source =
          await (_database.select(_database.pocSyncSources)
                ..where((table) => table.sourceKey.equals(profile.sourceKey)))
              .getSingle();
      if (event.sequence <= source.eventCursor) {
        final existing =
            await (_database.select(_database.mobileSyncEventInbox)..where(
                  (table) =>
                      table.sourceKey.equals(profile.sourceKey) &
                      table.eventSequence.equals(event.sequence),
                ))
                .getSingleOrNull();
        if (existing != null &&
            existing.eventId == event.eventId &&
            existing.payloadJson == event.payloadJson) {
          return;
        }
        throw const ProcurementPocLocalException(
          ProcurementPocLocalException.eventInvalid,
          'An already advanced event sequence did not match the inbox.',
        );
      }

      await _database
          .into(_database.mobileSyncEventInbox)
          .insert(
            MobileSyncEventInboxCompanion.insert(
              sourceKey: profile.sourceKey,
              eventSequence: event.sequence,
              eventId: event.eventId,
              eventType: event.eventType,
              eventVersion: event.eventVersion,
              aggregateType: event.aggregateType,
              aggregateId: event.aggregateId,
              aggregateVersion: event.aggregateVersion,
              occurredAtUtc: _utc(event.occurredAtUtc),
              correlationId: event.correlationId,
              payloadJson: event.payloadJson,
              receivedAtUtc: _utc(receivedAtUtc),
              applyStatus: _pending,
            ),
          );

      final known = _knownEventTypes.contains(event.eventType);
      if (known) {
        await _applyKnownEvent(profile, event);
      }
      await (_database.update(_database.mobileSyncEventInbox)..where(
            (table) =>
                table.sourceKey.equals(profile.sourceKey) &
                table.eventSequence.equals(event.sequence),
          ))
          .write(
            MobileSyncEventInboxCompanion(
              appliedAtUtc: Value(_utc(receivedAtUtc)),
              applyStatus: Value(known ? 'Applied' : 'SkippedUnknown'),
            ),
          );
      await (_database.update(
        _database.pocSyncSources,
      )..where((table) => table.sourceKey.equals(profile.sourceKey))).write(
        PocSyncSourcesCompanion(
          eventCursor: Value(event.sequence),
          updatedAtUtc: Value(_utc(receivedAtUtc)),
          lastErrorCode: const Value(null),
          lastErrorMessage: const Value(null),
        ),
      );
    });
  }

  static const _knownEventTypes = <String>{
    'ReceivingSessionPocStarted',
    'ReceivingEntryPocAccepted',
    'ReceivingSessionPocSubmitted',
    'ReceivingSessionPocApproved',
    'ReceivingSessionPocFinalized',
  };

  Future<void> _applyKnownEvent(
    PocDeviceProfile profile,
    MobileSyncEvent event,
  ) async {
    Map<String, Object?> payload;
    try {
      final decoded = jsonDecode(event.payloadJson);
      if (decoded is! Map) {
        throw const FormatException('Payload must be an object.');
      }
      payload = Map<String, Object?>.from(decoded);
      final sessionId = canonicalUuid(
        _requiredString(payload, 'sessionId'),
        'sessionId',
      );
      if (sessionId != event.aggregateId) {
        throw const FormatException('Aggregate identity mismatch.');
      }
      final version = _requiredPositiveInt(payload, 'version');
      if (version != event.aggregateVersion) {
        throw const FormatException('Aggregate version mismatch.');
      }
    } on Object {
      throw ProcurementPocLocalException(
        ProcurementPocLocalException.eventInvalid,
        'Malformed ${event.eventType} payload at sequence ${event.sequence}.',
      );
    }

    final existing =
        await (_database.select(_database.remoteReceivingSessionProjections)
              ..where(
                (table) =>
                    table.sourceKey.equals(profile.sourceKey) &
                    table.sessionId.equals(event.aggregateId),
              ))
            .getSingleOrNull();
    if (existing != null && event.aggregateVersion <= existing.cloudVersion) {
      return;
    }

    switch (event.eventType) {
      case 'ReceivingSessionPocStarted':
        if (payload.containsKey('leaseId')) {
          throw const ProcurementPocLocalException(
            ProcurementPocLocalException.eventInvalid,
            'A Started event must never expose a lease ID.',
          );
        }
        await _upsertProjection(
          profile,
          RemoteReceivingSessionProjection(
            sessionId: event.aggregateId,
            cloudReference: _requiredString(payload, 'cloudReference'),
            status: _requiredString(payload, 'status'),
            editorDeviceId: canonicalUuid(
              _requiredString(payload, 'editorDeviceId'),
              'editorDeviceId',
            ),
            leaseExpiresAtUtc: parseNullableUtc(
              payload['leaseExpiresAtUtc'],
              'leaseExpiresAtUtc',
            ),
            entryCount: 0,
            processedTotalWeightKg: ExactWeight.zero,
            cloudVersion: event.aggregateVersion,
            approvedByDeviceId: null,
            finalizationId: null,
            lastCloudUpdateAtUtc: event.occurredAtUtc,
          ),
        );
      case 'ReceivingEntryPocAccepted':
        final current = _requireProjection(existing, event);
        final total = _canonicalWeight(
          _requiredString(payload, 'processedTotalWeightKg'),
        );
        final processed = _canonicalWeight(
          _requiredString(payload, 'processedWeightKg'),
        );
        await _upsertProjection(
          profile,
          RemoteReceivingSessionProjection(
            sessionId: current.sessionId,
            cloudReference: current.cloudReference,
            status: current.status,
            editorDeviceId: current.editorDeviceId,
            leaseExpiresAtUtc: current.leaseExpiresAtUtc,
            entryCount: _requiredNonNegativeInt(payload, 'entryCount'),
            processedTotalWeightKg: total,
            cloudVersion: event.aggregateVersion,
            approvedByDeviceId: current.approvedByDeviceId,
            finalizationId: current.finalizationId,
            lastCloudUpdateAtUtc: event.occurredAtUtc,
          ),
        );
        await _database
            .into(_database.remoteReceivingEntrySummaries)
            .insertOnConflictUpdate(
              RemoteReceivingEntrySummariesCompanion.insert(
                sourceKey: profile.sourceKey,
                sessionId: event.aggregateId,
                entryId: canonicalUuid(
                  _requiredString(payload, 'entryId'),
                  'entryId',
                ),
                eventSequence: Value(event.sequence),
                localSequence: _requiredPositiveInt(payload, 'localSequence'),
                productReference: _requiredString(payload, 'productReference'),
                bagTypeReference: _requiredString(payload, 'bagTypeReference'),
                bagCount: _requiredPositiveInt(payload, 'bagCount'),
                processedWeightKg: processed,
              ),
            );
        await _trimRecentEntries(profile.sourceKey, event.aggregateId);
      case 'ReceivingSessionPocSubmitted':
        final current = _requireProjection(existing, event);
        await _upsertProjection(
          profile,
          RemoteReceivingSessionProjection(
            sessionId: current.sessionId,
            cloudReference: current.cloudReference,
            status: _requiredString(payload, 'status'),
            editorDeviceId: current.editorDeviceId,
            leaseExpiresAtUtc: null,
            entryCount: _requiredNonNegativeInt(payload, 'entryCount'),
            processedTotalWeightKg: _canonicalWeight(
              _requiredString(payload, 'processedTotalWeightKg'),
            ),
            cloudVersion: event.aggregateVersion,
            approvedByDeviceId: current.approvedByDeviceId,
            finalizationId: current.finalizationId,
            lastCloudUpdateAtUtc: event.occurredAtUtc,
          ),
        );
      case 'ReceivingSessionPocApproved':
        final current = _requireProjection(existing, event);
        await _upsertProjection(
          profile,
          RemoteReceivingSessionProjection(
            sessionId: current.sessionId,
            cloudReference: current.cloudReference,
            status: _requiredString(payload, 'status'),
            editorDeviceId: current.editorDeviceId,
            leaseExpiresAtUtc: null,
            entryCount: current.entryCount,
            processedTotalWeightKg: current.processedTotalWeightKg,
            cloudVersion: event.aggregateVersion,
            approvedByDeviceId: canonicalUuid(
              _requiredString(payload, 'approvedByDeviceId'),
              'approvedByDeviceId',
            ),
            finalizationId: current.finalizationId,
            lastCloudUpdateAtUtc: event.occurredAtUtc,
          ),
        );
      case 'ReceivingSessionPocFinalized':
        final current = _requireProjection(existing, event);
        await _upsertProjection(
          profile,
          RemoteReceivingSessionProjection(
            sessionId: current.sessionId,
            cloudReference: current.cloudReference,
            status: _requiredString(payload, 'status'),
            editorDeviceId: current.editorDeviceId,
            leaseExpiresAtUtc: null,
            entryCount: _requiredNonNegativeInt(payload, 'finalEntryCount'),
            processedTotalWeightKg: _canonicalWeight(
              _requiredString(payload, 'finalProcessedTotalWeightKg'),
            ),
            cloudVersion: event.aggregateVersion,
            approvedByDeviceId: current.approvedByDeviceId,
            finalizationId: canonicalUuid(
              _requiredString(payload, 'finalizationId'),
              'finalizationId',
            ),
            lastCloudUpdateAtUtc: event.occurredAtUtc,
          ),
        );
    }
  }

  RemoteReceivingSessionProjection _requireProjection(
    RemoteReceivingSessionProjectionRow? row,
    MobileSyncEvent event,
  ) {
    if (row == null) {
      throw ProcurementPocLocalException(
        ProcurementPocLocalException.eventInvalid,
        '${event.eventType} arrived before its Started projection.',
      );
    }
    return _mapProjection(row, const []);
  }

  @override
  Future<void> recordEventPollSuccess({
    required PocDeviceProfile profile,
    required DateTime nowUtc,
  }) async {
    await _ensureSource(profile, nowUtc);
    await (_database.update(
      _database.pocSyncSources,
    )..where((table) => table.sourceKey.equals(profile.sourceKey))).write(
      PocSyncSourcesCompanion(
        lastSuccessfulPollAtUtc: Value(_utc(nowUtc)),
        lastErrorCode: const Value(null),
        lastErrorMessage: const Value(null),
        updatedAtUtc: Value(_utc(nowUtc)),
      ),
    );
  }

  @override
  Future<void> recordEventPollFailure({
    required PocDeviceProfile profile,
    required String errorCode,
    required String message,
    required DateTime nowUtc,
  }) async {
    await _ensureSource(profile, nowUtc);
    await (_database.update(
      _database.pocSyncSources,
    )..where((table) => table.sourceKey.equals(profile.sourceKey))).write(
      PocSyncSourcesCompanion(
        lastErrorCode: Value(errorCode),
        lastErrorMessage: Value(message),
        updatedAtUtc: Value(_utc(nowUtc)),
      ),
    );
  }

  @override
  Future<PocControlCommand> enqueueControlCommand({
    required PocDeviceProfile profile,
    required PocControlCommandType commandType,
    required String commandId,
    required String sessionId,
    required int? expectedCloudVersion,
    required String? leaseId,
    required DateTime nowUtc,
  }) async {
    await _ensureSource(profile, nowUtc);
    final existing = await findOutstandingControlCommand(
      profile: profile,
      commandType: commandType,
      sessionId: sessionId,
    );
    if (existing != null) {
      final explicitlyChangedVersion =
          existing.status == PocControlCommandStatus.needsAttention &&
          commandType != PocControlCommandType.heartbeat &&
          expectedCloudVersion != existing.expectedCloudVersion;
      if (!explicitlyChangedVersion) {
        return existing;
      }
    }
    if (commandType == PocControlCommandType.heartbeat) {
      if (leaseId == null || expectedCloudVersion != null) {
        throw const ProcurementPocLocalException(
          ProcurementPocLocalException.localStateConflict,
          'Heartbeat requires one lease and no expected version.',
        );
      }
    } else if (expectedCloudVersion == null ||
        expectedCloudVersion <= 0 ||
        leaseId != null) {
      throw const ProcurementPocLocalException(
        ProcurementPocLocalException.localStateConflict,
        'Approve and Finalize require a positive expected version and no lease.',
      );
    }
    await _database
        .into(_database.pocControlCommands)
        .insert(
          PocControlCommandsCompanion.insert(
            commandId: canonicalUuid(commandId, 'commandId'),
            sourceKey: profile.sourceKey,
            actingDeviceId: profile.deviceId,
            commandType: commandType.storageValue,
            sessionId: canonicalUuid(sessionId, 'sessionId'),
            expectedCloudVersion: Value(expectedCloudVersion),
            leaseId: Value(leaseId),
            status: PocControlCommandStatus.pending.storageValue,
            attemptCount: 0,
            createdAtUtc: _utc(nowUtc),
            updatedAtUtc: _utc(nowUtc),
          ),
        );
    return (await _findControl(commandId))!;
  }

  @override
  Future<List<PocControlCommand>> loadPendingControlCommands(
    PocDeviceProfile profile,
    DateTime nowUtc,
  ) async {
    final rows =
        await (_database.select(_database.pocControlCommands)
              ..where(
                (table) =>
                    table.sourceKey.equals(profile.sourceKey) &
                    table.actingDeviceId.equals(profile.deviceId) &
                    table.status.equals(_pending) &
                    (table.nextAttemptAtUtc.isNull() |
                        table.nextAttemptAtUtc.isSmallerOrEqualValue(
                          _utc(nowUtc),
                        )),
              )
              ..orderBy([(table) => OrderingTerm.asc(table.createdAtUtc)]))
            .get();
    return rows.map(_mapControl).toList(growable: false);
  }

  @override
  Future<void> recoverAmbiguousControlCommands(
    PocDeviceProfile profile,
    DateTime nowUtc,
  ) async {
    await _database.customStatement(
      'UPDATE poc_control_commands '
      "SET status = 'Pending', attempt_count = attempt_count + 1, "
      'last_attempt_at_utc = ?, updated_at_utc = ?, '
      "last_error_code = 'POC_NETWORK_AMBIGUOUS', "
      "last_error_message = 'Response not received; retrying the same key.' "
      "WHERE status = 'Sending' AND source_key = ? AND acting_device_id = ?",
      [_utc(nowUtc), _utc(nowUtc), profile.sourceKey, profile.deviceId],
    );
  }

  @override
  Future<PocControlCommand> markControlCommandSending(
    PocDeviceProfile profile,
    String commandId,
    DateTime nowUtc,
  ) async {
    final row = await _requiredControlForProfile(profile, commandId);
    if (row.status != _pending) {
      throw const ProcurementPocLocalException(
        ProcurementPocLocalException.localStateConflict,
        'A Pending control command is required.',
      );
    }
    await (_database.update(
      _database.pocControlCommands,
    )..where((table) => table.commandId.equals(commandId))).write(
      PocControlCommandsCompanion(
        status: const Value(_sending),
        lastAttemptAtUtc: Value(_utc(nowUtc)),
        lastErrorCode: const Value(null),
        lastErrorMessage: const Value(null),
        updatedAtUtc: Value(_utc(nowUtc)),
      ),
    );
    return (await _findControl(commandId))!;
  }

  @override
  Future<void> recordControlTransportFailure({
    required PocDeviceProfile profile,
    required String commandId,
    required String errorCode,
    required String message,
    required DateTime nowUtc,
  }) async {
    final row = await _requiredControlForProfile(profile, commandId);
    await (_database.update(
      _database.pocControlCommands,
    )..where((table) => table.commandId.equals(commandId))).write(
      PocControlCommandsCompanion(
        status: const Value(_pending),
        attemptCount: Value(row.attemptCount + 1),
        lastAttemptAtUtc: Value(_utc(nowUtc)),
        lastErrorCode: Value(errorCode),
        lastErrorMessage: Value(message),
        updatedAtUtc: Value(_utc(nowUtc)),
      ),
    );
  }

  @override
  Future<void> recordControlNeedsAttention({
    required PocDeviceProfile profile,
    required String commandId,
    required String errorCode,
    required String message,
    required DateTime nowUtc,
  }) async {
    final row = await _requiredControlForProfile(profile, commandId);
    await (_database.update(
      _database.pocControlCommands,
    )..where((table) => table.commandId.equals(commandId))).write(
      PocControlCommandsCompanion(
        status: const Value(_needsAttention),
        attemptCount: Value(row.attemptCount + 1),
        lastAttemptAtUtc: Value(_utc(nowUtc)),
        lastErrorCode: Value(errorCode),
        lastErrorMessage: Value(message),
        updatedAtUtc: Value(_utc(nowUtc)),
      ),
    );
  }

  @override
  Future<void> completeControlCommand({
    required PocDeviceProfile profile,
    required String commandId,
    required ProcurementPocCommandResult result,
    required DateTime nowUtc,
  }) async {
    await _database.transaction(() async {
      final command = await _requiredControlForProfile(profile, commandId);
      if (command.status != _sending || command.sessionId != result.sessionId) {
        throw const ProcurementPocLocalException(
          ProcurementPocLocalException.localStateConflict,
          'The control response did not match the durable command.',
        );
      }
      await (_database.update(
        _database.pocControlCommands,
      )..where((table) => table.commandId.equals(commandId))).write(
        PocControlCommandsCompanion(
          status: const Value('Completed'),
          successfulResponseJson: Value(result.rawResponseJson),
          lastErrorCode: const Value(null),
          lastErrorMessage: const Value(null),
          updatedAtUtc: Value(_utc(nowUtc)),
        ),
      );

      final cloud =
          await (_database.select(_database.receivingSessionCloudStates)..where(
                (table) =>
                    table.localSessionId.equals(result.sessionId) &
                    table.sourceKey.equals(profile.sourceKey) &
                    table.boundDeviceId.equals(profile.deviceId),
              ))
              .getSingleOrNull();
      _validateControlResult(command, result, cloud, nowUtc);
      if (cloud != null) {
        await (_database.update(_database.receivingSessionCloudStates)..where(
              (table) =>
                  table.localSessionId.equals(result.sessionId) &
                  table.sourceKey.equals(profile.sourceKey) &
                  table.boundDeviceId.equals(profile.deviceId),
            ))
            .write(
              ReceivingSessionCloudStatesCompanion(
                cloudReference: Value(result.cloudReference),
                cloudStatus: Value(result.status),
                cloudVersion: Value(result.version),
                leaseId: Value(result.leaseId),
                leaseExpiresAtUtc: Value(
                  _nullableUtc(result.leaseExpiresAtUtc),
                ),
                lastSuccessfulSyncAtUtc: Value(_utc(nowUtc)),
                lastCloudUpdateAtUtc: Value(_utc(nowUtc)),
                lastErrorCode: const Value(null),
                lastErrorMessage: const Value(null),
              ),
            );
      }

      final current = await getRemoteSession(profile, result.sessionId);
      await _upsertProjection(
        profile,
        RemoteReceivingSessionProjection(
          sessionId: result.sessionId,
          cloudReference: result.cloudReference,
          status: result.status,
          editorDeviceId: result.editorDeviceId,
          leaseExpiresAtUtc: result.leaseExpiresAtUtc,
          entryCount: result.entryCount,
          processedTotalWeightKg: _canonicalWeight(
            result.processedTotalWeightKg,
          ),
          cloudVersion: result.version,
          approvedByDeviceId: command.commandType == 'Approve'
              ? profile.deviceId
              : current?.approvedByDeviceId,
          finalizationId: result.finalizationId ?? current?.finalizationId,
          lastCloudUpdateAtUtc: nowUtc,
          recentEntries: current?.recentEntries ?? const [],
        ),
      );
    });
  }

  static void _validateControlResult(
    PocControlCommandRow command,
    ProcurementPocCommandResult result,
    ReceivingSessionCloudStateRow? cloud,
    DateTime nowUtc,
  ) {
    final minimumVersion = command.expectedCloudVersion ?? cloud?.cloudVersion;
    if (minimumVersion != null && result.version < minimumVersion) {
      throw const ProcurementPocLocalException(
        ProcurementPocLocalException.cloudContractInvalid,
        'The control response version moved backward.',
      );
    }
    switch (command.commandType) {
      case 'Heartbeat':
        if (cloud == null ||
            result.status != 'ReceivingInProgress' ||
            result.leaseId == null ||
            result.leaseExpiresAtUtc == null ||
            !result.leaseExpiresAtUtc!.isAfter(nowUtc) ||
            result.finalizationId != null) {
          throw const ProcurementPocLocalException(
            ProcurementPocLocalException.cloudContractInvalid,
            'Heartbeat success did not return the expected active lease state.',
          );
        }
        return;
      case 'Approve':
        if (result.status != 'Approved' ||
            result.leaseId != null ||
            result.leaseExpiresAtUtc != null ||
            result.finalizationId != null) {
          throw const ProcurementPocLocalException(
            ProcurementPocLocalException.cloudContractInvalid,
            'Approval success did not return the Approved state.',
          );
        }
        return;
      case 'Finalize':
        if (result.status != 'Finalized' ||
            result.leaseId != null ||
            result.leaseExpiresAtUtc != null ||
            result.finalizationId == null) {
          throw const ProcurementPocLocalException(
            ProcurementPocLocalException.cloudContractInvalid,
            'Finalization success did not return one finalization identity.',
          );
        }
        return;
      default:
        throw const ProcurementPocLocalException(
          ProcurementPocLocalException.cloudContractInvalid,
          'The durable control command type is unsupported.',
        );
    }
  }

  @override
  Future<PocControlCommand?> findOutstandingControlCommand({
    required PocDeviceProfile profile,
    required PocControlCommandType commandType,
    required String sessionId,
  }) async {
    final row =
        await (_database.select(_database.pocControlCommands)
              ..where(
                (table) =>
                    table.sourceKey.equals(profile.sourceKey) &
                    table.actingDeviceId.equals(profile.deviceId) &
                    table.commandType.equals(commandType.storageValue) &
                    table.sessionId.equals(sessionId) &
                    table.status.isIn([_pending, _sending, _needsAttention]),
              )
              ..orderBy([(table) => OrderingTerm.desc(table.createdAtUtc)])
              ..limit(1))
            .getSingleOrNull();
    return row == null ? null : _mapControl(row);
  }

  @override
  Future<PocControlCommand?> findLatestCompletedFinalization(
    PocDeviceProfile profile,
    String sessionId,
  ) async {
    final row =
        await (_database.select(_database.pocControlCommands)
              ..where(
                (table) =>
                    table.sourceKey.equals(profile.sourceKey) &
                    table.actingDeviceId.equals(profile.deviceId) &
                    table.commandType.equals('Finalize') &
                    table.sessionId.equals(sessionId) &
                    table.status.equals('Completed'),
              )
              ..orderBy([(table) => OrderingTerm.desc(table.createdAtUtc)])
              ..limit(1))
            .getSingleOrNull();
    return row == null ? null : _mapControl(row);
  }

  @override
  Future<PocControlCommand> requeueCompletedControlCommand({
    required PocDeviceProfile profile,
    required String commandId,
    required DateTime nowUtc,
  }) async {
    final row = await _requiredControlForProfile(profile, commandId);
    if (row.commandType != 'Finalize' || row.status != 'Completed') {
      throw const ProcurementPocLocalException(
        ProcurementPocLocalException.localStateConflict,
        'Only a completed Finalize command can be replayed explicitly.',
      );
    }
    await (_database.update(
      _database.pocControlCommands,
    )..where((table) => table.commandId.equals(commandId))).write(
      PocControlCommandsCompanion(
        status: const Value(_pending),
        updatedAtUtc: Value(_utc(nowUtc)),
        lastErrorCode: const Value(null),
        lastErrorMessage: const Value(null),
      ),
    );
    return (await _findControl(commandId))!;
  }

  @override
  Future<List<ReceivingSessionCloudState>> listLeasedOperatorSessions(
    PocDeviceProfile profile,
  ) async {
    final rows =
        await (_database.select(_database.receivingSessionCloudStates)..where(
              (table) =>
                  table.sourceKey.equals(profile.sourceKey) &
                  table.boundDeviceId.equals(profile.deviceId) &
                  table.cloudStatus.equals('ReceivingInProgress') &
                  table.leaseId.isNotNull() &
                  table.leaseExpiresAtUtc.isNotNull(),
            ))
            .get();
    return rows.map(_mapCloudState).toList(growable: false);
  }

  @override
  Future<List<LocalReceivingSessionRecord>> listLocalReceivingSessions() =>
      _localReceivingStore.listSessions();

  @override
  Future<List<LocalReceivingEntryRecord>> listLocalReceivingEntries(
    String sessionId,
  ) => _localReceivingStore.listEntries(sessionId);

  @override
  Future<List<LocalOutboxOperation>> listLocalOutboxOperations({
    String? aggregateId,
  }) async {
    final query = _database.select(_database.localOutboxOperations);
    query.where(
      (table) =>
          table.aggregateType.equals(_receivingAggregateType) &
          table.operationType.isIn(_receivingOperationTypes) &
          (aggregateId == null
              ? const Constant(true)
              : table.aggregateId.equals(aggregateId)),
    );
    query.orderBy([
      (table) => OrderingTerm.asc(table.aggregateId),
      (table) => OrderingTerm.asc(table.localSequence),
    ]);
    final rows = await query.get();
    return rows.map(_mapOutbox).toList(growable: false);
  }

  @override
  Future<List<RemoteReceivingSessionProjection>> listRemoteSessions(
    PocDeviceProfile profile,
  ) async {
    final rows =
        await (_database.select(_database.remoteReceivingSessionProjections)
              ..where((table) => table.sourceKey.equals(profile.sourceKey))
              ..orderBy([
                (table) => OrderingTerm.desc(table.lastCloudUpdateAtUtc),
              ]))
            .get();
    final results = <RemoteReceivingSessionProjection>[];
    for (final row in rows) {
      results.add(
        _mapProjection(
          row,
          await _loadRecentEntries(row.sourceKey, row.sessionId),
        ),
      );
    }
    return results;
  }

  @override
  Future<RemoteReceivingSessionProjection?> getRemoteSession(
    PocDeviceProfile profile,
    String sessionId,
  ) async {
    final row =
        await (_database.select(_database.remoteReceivingSessionProjections)
              ..where(
                (table) =>
                    table.sourceKey.equals(profile.sourceKey) &
                    table.sessionId.equals(sessionId),
              ))
            .getSingleOrNull();
    if (row == null) {
      return null;
    }
    return _mapProjection(
      row,
      await _loadRecentEntries(row.sourceKey, row.sessionId),
    );
  }

  @override
  Future<void> saveLiveView({
    required PocDeviceProfile profile,
    required ReceivingSessionLiveView liveView,
  }) async {
    await _database.transaction(() async {
      await _ensureSource(profile, liveView.lastCloudUpdateAtUtc);
      final current = await getRemoteSession(profile, liveView.sessionId);
      await _upsertProjection(
        profile,
        RemoteReceivingSessionProjection(
          sessionId: liveView.sessionId,
          cloudReference: liveView.cloudReference,
          status: liveView.status,
          editorDeviceId: liveView.editorDeviceId,
          leaseExpiresAtUtc: liveView.leaseExpiresAtUtc,
          entryCount: liveView.entryCount,
          processedTotalWeightKg: _canonicalWeight(
            liveView.processedTotalWeightKg,
          ),
          cloudVersion: liveView.cloudVersion,
          approvedByDeviceId:
              liveView.approvedByDeviceId ?? current?.approvedByDeviceId,
          finalizationId: liveView.finalizationId ?? current?.finalizationId,
          lastCloudUpdateAtUtc: liveView.lastCloudUpdateAtUtc,
        ),
      );
      await (_database.delete(_database.remoteReceivingEntrySummaries)..where(
            (table) =>
                table.sourceKey.equals(profile.sourceKey) &
                table.sessionId.equals(liveView.sessionId),
          ))
          .go();
      for (final entry in liveView.recentEntries.take(5)) {
        await _database
            .into(_database.remoteReceivingEntrySummaries)
            .insert(
              RemoteReceivingEntrySummariesCompanion.insert(
                sourceKey: profile.sourceKey,
                sessionId: liveView.sessionId,
                entryId: entry.entryId,
                localSequence: entry.localSequence,
                productReference: entry.productReference,
                bagTypeReference: entry.bagTypeReference,
                bagCount: entry.bagCount,
                rawWeightKg: Value(entry.rawWeightKg),
                processedWeightKg: entry.processedWeightKg,
                displayWeightKg: Value(entry.displayWeightKg),
                decimalPlaces: Value(entry.decimalPlaces),
                processingMethod: Value(entry.processingMethod),
                weightSource: Value(entry.weightSource),
                capturedAtDeviceUtc: Value(
                  _nullableUtc(entry.capturedAtDeviceUtc),
                ),
                acceptedAtServerUtc: Value(
                  _nullableUtc(entry.acceptedAtServerUtc),
                ),
              ),
            );
      }
    });
  }

  @override
  Future<void> saveSessionList({
    required PocDeviceProfile profile,
    required List<RemoteReceivingSessionProjection> sessions,
  }) async {
    await _database.transaction(() async {
      await _ensureSource(profile, profile.updatedAtUtc);
      for (final item in sessions) {
        final current = await getRemoteSession(profile, item.sessionId);
        await _upsertProjection(
          profile,
          RemoteReceivingSessionProjection(
            sessionId: item.sessionId,
            cloudReference: item.cloudReference,
            status: item.status,
            editorDeviceId: item.editorDeviceId,
            leaseExpiresAtUtc: item.leaseExpiresAtUtc,
            entryCount: item.entryCount,
            processedTotalWeightKg: _canonicalWeight(
              item.processedTotalWeightKg,
            ),
            cloudVersion: item.cloudVersion,
            approvedByDeviceId: current?.approvedByDeviceId,
            finalizationId: current?.finalizationId,
            lastCloudUpdateAtUtc: item.lastCloudUpdateAtUtc,
            recentEntries: current?.recentEntries ?? const [],
          ),
        );
      }
    });
  }

  @override
  Future<List<PocControlCommand>> listControlCommands(
    PocDeviceProfile profile, {
    String? sessionId,
  }) async {
    final query = _database.select(_database.pocControlCommands);
    query.where(
      (table) =>
          table.sourceKey.equals(profile.sourceKey) &
          table.actingDeviceId.equals(profile.deviceId) &
          (sessionId == null
              ? const Constant(true)
              : table.sessionId.equals(sessionId)),
    );
    query.orderBy([(table) => OrderingTerm.desc(table.createdAtUtc)]);
    return (await query.get()).map(_mapControl).toList(growable: false);
  }

  @override
  Future<PocSyncDiagnostics> loadDiagnostics(PocDeviceProfile profile) async {
    final operations = await listLocalOutboxOperations();
    final commands = await listControlCommands(profile);
    final source =
        await (_database.select(_database.pocSyncSources)
              ..where((table) => table.sourceKey.equals(profile.sourceKey)))
            .getSingleOrNull();
    int count(OutboxOperationStatus status) =>
        operations.where((item) => item.status == status).length;
    String? lastErrorCode = source?.lastErrorCode;
    DateTime? lastErrorAt = source?.lastErrorCode == null
        ? null
        : _parseUtc(source!.updatedAtUtc, 'updatedAtUtc');
    void considerRetryableError(
      String? errorCode,
      DateTime? attemptedAt,
      bool retryable,
    ) {
      if (!retryable ||
          errorCode == null ||
          attemptedAt == null ||
          (lastErrorAt != null && !attemptedAt.isAfter(lastErrorAt!))) {
        return;
      }
      lastErrorCode = errorCode;
      lastErrorAt = attemptedAt;
    }

    for (final operation in operations) {
      considerRetryableError(
        operation.lastErrorCode,
        operation.lastAttemptAtUtc,
        operation.status == OutboxOperationStatus.pending ||
            operation.status == OutboxOperationStatus.sending,
      );
    }
    for (final command in commands) {
      considerRetryableError(
        command.lastErrorCode,
        command.lastAttemptAtUtc,
        command.status == PocControlCommandStatus.pending ||
            command.status == PocControlCommandStatus.sending,
      );
    }
    return PocSyncDiagnostics(
      pending: count(OutboxOperationStatus.pending),
      sending: count(OutboxOperationStatus.sending),
      completed: count(OutboxOperationStatus.accepted),
      needsAttention: count(OutboxOperationStatus.needsAttention),
      rejected: count(OutboxOperationStatus.rejected),
      eventCursor: source?.eventCursor ?? 0,
      lastPollErrorCode: lastErrorCode,
    );
  }

  Future<void> _ensureSource(PocDeviceProfile profile, DateTime nowUtc) async {
    await _database
        .into(_database.pocSyncSources)
        .insert(
          PocSyncSourcesCompanion.insert(
            sourceKey: profile.sourceKey,
            backendBaseUrl: profile.backendBaseUrl,
            workspaceId: profile.workspaceId,
            updatedAtUtc: _utc(nowUtc),
          ),
          mode: InsertMode.insertOrIgnore,
        );
  }

  Future<void> _upsertProjection(
    PocDeviceProfile profile,
    RemoteReceivingSessionProjection value,
  ) async {
    _canonicalWeight(value.processedTotalWeightKg);
    final current =
        await (_database.select(_database.remoteReceivingSessionProjections)
              ..where(
                (table) =>
                    table.sourceKey.equals(profile.sourceKey) &
                    table.sessionId.equals(value.sessionId),
              ))
            .getSingleOrNull();
    if (current != null && current.cloudVersion > value.cloudVersion) {
      return;
    }
    await _database
        .into(_database.remoteReceivingSessionProjections)
        .insertOnConflictUpdate(
          RemoteReceivingSessionProjectionsCompanion.insert(
            sourceKey: profile.sourceKey,
            sessionId: value.sessionId,
            cloudReference: value.cloudReference,
            status: value.status,
            editorDeviceId: value.editorDeviceId,
            leaseExpiresAtUtc: Value(_nullableUtc(value.leaseExpiresAtUtc)),
            entryCount: value.entryCount,
            processedTotalWeightKg: value.processedTotalWeightKg,
            cloudVersion: value.cloudVersion,
            approvedByDeviceId: Value(value.approvedByDeviceId),
            finalizationId: Value(value.finalizationId),
            lastCloudUpdateAtUtc: _utc(value.lastCloudUpdateAtUtc),
          ),
        );
  }

  Future<void> _trimRecentEntries(String sourceKey, String sessionId) async {
    final rows =
        await (_database.select(_database.remoteReceivingEntrySummaries)
              ..where(
                (table) =>
                    table.sourceKey.equals(sourceKey) &
                    table.sessionId.equals(sessionId),
              )
              ..orderBy([(table) => OrderingTerm.desc(table.localSequence)]))
            .get();
    for (final old in rows.skip(5)) {
      await (_database.delete(_database.remoteReceivingEntrySummaries)..where(
            (table) =>
                table.sourceKey.equals(sourceKey) &
                table.sessionId.equals(sessionId) &
                table.entryId.equals(old.entryId),
          ))
          .go();
    }
  }

  Future<List<RemoteReceivingEntrySummary>> _loadRecentEntries(
    String sourceKey,
    String sessionId,
  ) async {
    final rows =
        await (_database.select(_database.remoteReceivingEntrySummaries)
              ..where(
                (table) =>
                    table.sourceKey.equals(sourceKey) &
                    table.sessionId.equals(sessionId),
              )
              ..orderBy([(table) => OrderingTerm.desc(table.localSequence)])
              ..limit(5))
            .get();
    return rows.map(_mapEntrySummary).toList(growable: false);
  }

  Future<LocalOutboxOperationRow> _requiredOutbox(String operationId) async {
    final row =
        await (_database.select(_database.localOutboxOperations)
              ..where((table) => table.operationId.equals(operationId)))
            .getSingleOrNull();
    if (row == null) {
      throw const ProcurementPocLocalException(
        ProcurementPocLocalException.localStateConflict,
        'The local outbox operation does not exist.',
      );
    }
    if (row.aggregateType != _receivingAggregateType ||
        !_receivingOperationTypes.contains(row.operationType)) {
      throw const ProcurementPocLocalException(
        ProcurementPocLocalException.localStateConflict,
        'The operation does not belong to the Procurement receiving POC.',
      );
    }
    return row;
  }

  Future<PocControlCommandRow> _requiredControl(String commandId) async {
    final row = await (_database.select(
      _database.pocControlCommands,
    )..where((table) => table.commandId.equals(commandId))).getSingleOrNull();
    if (row == null) {
      throw const ProcurementPocLocalException(
        ProcurementPocLocalException.localStateConflict,
        'The local control command does not exist.',
      );
    }
    return row;
  }

  Future<PocControlCommandRow> _requiredControlForProfile(
    PocDeviceProfile profile,
    String commandId,
  ) async {
    final row = await _requiredControl(commandId);
    if (row.sourceKey != profile.sourceKey ||
        row.actingDeviceId != profile.deviceId) {
      throw const ProcurementPocLocalException(
        ProcurementPocLocalException.localStateConflict,
        'The control command belongs to another source or device.',
      );
    }
    return row;
  }

  Future<PocControlCommand?> _findControl(String commandId) async {
    final row = await (_database.select(
      _database.pocControlCommands,
    )..where((table) => table.commandId.equals(commandId))).getSingleOrNull();
    return row == null ? null : _mapControl(row);
  }

  static PocDeviceProfile _mapProfile(PocDeviceProfileRow row) {
    return PocDeviceProfile(
      backendBaseUrl: row.backendBaseUrl,
      workspaceId: row.workspaceId,
      deviceId: row.deviceId,
      displayRole: PocDisplayRole.fromStorage(row.displayRole),
      displayLabel: row.displayLabel,
      automaticSyncPaused: row.automaticSyncPaused,
      createdAtUtc: _parseUtc(row.createdAtUtc, 'createdAtUtc'),
      updatedAtUtc: _parseUtc(row.updatedAtUtc, 'updatedAtUtc'),
    );
  }

  static ReceivingSessionCloudState _mapCloudState(
    ReceivingSessionCloudStateRow row,
  ) {
    return ReceivingSessionCloudState(
      sourceKey: row.sourceKey,
      boundDeviceId: row.boundDeviceId,
      localSessionId: row.localSessionId,
      cloudSessionId: row.cloudSessionId,
      cloudReference: row.cloudReference,
      cloudStatus: row.cloudStatus,
      cloudVersion: row.cloudVersion,
      leaseId: row.leaseId,
      leaseExpiresAtUtc: _parseNullableUtc(
        row.leaseExpiresAtUtc,
        'leaseExpiresAtUtc',
      ),
      editorDeviceId: row.editorDeviceId,
      lastSuccessfulSyncAtUtc: _parseUtc(
        row.lastSuccessfulSyncAtUtc,
        'lastSuccessfulSyncAtUtc',
      ),
      lastCloudUpdateAtUtc: _parseUtc(
        row.lastCloudUpdateAtUtc,
        'lastCloudUpdateAtUtc',
      ),
      lastErrorCode: row.lastErrorCode,
      lastErrorMessage: row.lastErrorMessage,
    );
  }

  static LocalOutboxOperation _mapOutbox(LocalOutboxOperationRow row) {
    return LocalOutboxOperation(
      operationId: row.operationId,
      aggregateId: row.aggregateId,
      aggregateType: row.aggregateType,
      operationType: row.operationType,
      localSequence: row.localSequence,
      expectedCloudVersion: row.expectedCloudVersion,
      payloadJson: row.payloadJson,
      payloadHash: row.payloadHash,
      status: OutboxOperationStatus.fromStorage(row.status),
      attemptCount: row.attemptCount,
      createdAtDeviceUtc: _parseUtc(
        row.createdAtDeviceUtc,
        'createdAtDeviceUtc',
      ),
      lastAttemptAtUtc: _parseNullableUtc(
        row.lastAttemptAtUtc,
        'lastAttemptAtUtc',
      ),
      acceptedAtUtc: _parseNullableUtc(row.acceptedAtUtc, 'acceptedAtUtc'),
      lastErrorCode: row.lastErrorCode,
    );
  }

  static PocControlCommand _mapControl(PocControlCommandRow row) {
    return PocControlCommand(
      commandId: row.commandId,
      sourceKey: row.sourceKey,
      actingDeviceId: row.actingDeviceId,
      commandType: PocControlCommandType.fromStorage(row.commandType),
      sessionId: row.sessionId,
      expectedCloudVersion: row.expectedCloudVersion,
      leaseId: row.leaseId,
      status: PocControlCommandStatus.fromStorage(row.status),
      attemptCount: row.attemptCount,
      nextAttemptAtUtc: _parseNullableUtc(
        row.nextAttemptAtUtc,
        'nextAttemptAtUtc',
      ),
      lastAttemptAtUtc: _parseNullableUtc(
        row.lastAttemptAtUtc,
        'lastAttemptAtUtc',
      ),
      lastErrorCode: row.lastErrorCode,
      lastErrorMessage: row.lastErrorMessage,
      successfulResponseJson: row.successfulResponseJson,
      createdAtUtc: _parseUtc(row.createdAtUtc, 'createdAtUtc'),
      updatedAtUtc: _parseUtc(row.updatedAtUtc, 'updatedAtUtc'),
    );
  }

  static RemoteReceivingSessionProjection _mapProjection(
    RemoteReceivingSessionProjectionRow row,
    List<RemoteReceivingEntrySummary> entries,
  ) {
    return RemoteReceivingSessionProjection(
      sessionId: row.sessionId,
      cloudReference: row.cloudReference,
      status: row.status,
      editorDeviceId: row.editorDeviceId,
      leaseExpiresAtUtc: _parseNullableUtc(
        row.leaseExpiresAtUtc,
        'leaseExpiresAtUtc',
      ),
      entryCount: row.entryCount,
      processedTotalWeightKg: row.processedTotalWeightKg,
      cloudVersion: row.cloudVersion,
      approvedByDeviceId: row.approvedByDeviceId,
      finalizationId: row.finalizationId,
      lastCloudUpdateAtUtc: _parseUtc(
        row.lastCloudUpdateAtUtc,
        'lastCloudUpdateAtUtc',
      ),
      recentEntries: entries,
    );
  }

  static RemoteReceivingEntrySummary _mapEntrySummary(
    RemoteReceivingEntrySummaryRow row,
  ) {
    return RemoteReceivingEntrySummary(
      entryId: row.entryId,
      localSequence: row.localSequence,
      productReference: row.productReference,
      bagTypeReference: row.bagTypeReference,
      bagCount: row.bagCount,
      rawWeightKg: row.rawWeightKg,
      processedWeightKg: row.processedWeightKg,
      displayWeightKg: row.displayWeightKg,
      decimalPlaces: row.decimalPlaces,
      processingMethod: row.processingMethod,
      weightSource: row.weightSource,
      capturedAtDeviceUtc: _parseNullableUtc(
        row.capturedAtDeviceUtc,
        'capturedAtDeviceUtc',
      ),
      acceptedAtServerUtc: _parseNullableUtc(
        row.acceptedAtServerUtc,
        'acceptedAtServerUtc',
      ),
    );
  }

  static String _requiredString(Map<String, Object?> map, String key) {
    final value = map[key];
    if (value is! String || value.isEmpty) {
      throw FormatException('$key must be a non-empty string.');
    }
    return value;
  }

  static int _requiredPositiveInt(Map<String, Object?> map, String key) {
    final value = map[key];
    if (value is! int || value <= 0) {
      throw FormatException('$key must be a positive integer.');
    }
    return value;
  }

  static int _requiredNonNegativeInt(Map<String, Object?> map, String key) {
    final value = map[key];
    if (value is! int || value < 0) {
      throw FormatException('$key must be a non-negative integer.');
    }
    return value;
  }

  static String _canonicalWeight(String value) {
    ExactWeight.parseCanonical(value);
    return value;
  }

  static String _utc(DateTime value) {
    requireUtc(value, 'timestamp');
    return value.toIso8601String();
  }

  static String? _nullableUtc(DateTime? value) =>
      value == null ? null : _utc(value);

  static DateTime _parseUtc(String value, String fieldName) {
    final parsed = DateTime.tryParse(value);
    if (parsed == null || !parsed.isUtc) {
      throw FormatException('$fieldName is not stored UTC text.');
    }
    return parsed;
  }

  static DateTime? _parseNullableUtc(String? value, String fieldName) {
    return value == null ? null : _parseUtc(value, fieldName);
  }

  static String? _trimToNull(String? value) {
    final trimmed = value?.trim();
    return trimmed == null || trimmed.isEmpty ? null : trimmed;
  }
}
