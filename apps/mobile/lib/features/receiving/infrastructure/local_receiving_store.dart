import 'dart:convert';

import 'package:drift/drift.dart';
import 'package:sqlite3/common.dart';

import '../../../core/database/local_device_clock.dart';
import '../../../core/database/local_id_generator.dart';
import '../../../core/database/local_store_exception.dart';
import '../../../core/database/trader_pro_local_database.dart';
import '../../../core/measurements/weight_processing_result.dart';
import '../../../core/measurements/weight_processor.dart';
import '../../../core/sync/local_outbox_operation.dart';
import '../../../core/sync/outbox_operation_status.dart';
import '../application/local_receiving_commands.dart';
import '../domain/exact_weight.dart';
import '../domain/local_receiving_records.dart';
import '../domain/local_receiving_status.dart';
import '../domain/weight_source.dart';
import 'receiving_outbox_payload.dart';

final class LocalReceivingStore {
  factory LocalReceivingStore({
    required TraderProLocalDatabase database,
    required String installReference,
    LocalDeviceClock clock = const SystemLocalDeviceClock(),
    LocalIdGenerator? idGenerator,
  }) {
    return LocalReceivingStore._(
      database,
      _normalizeInstallReference(installReference),
      clock,
      idGenerator ?? UuidV7LocalIdGenerator(),
    );
  }

  LocalReceivingStore._(
    this._database,
    this._installReference,
    this._clock,
    this._idGenerator,
  );

  static const _aggregateType = 'ReceivingSession';
  static const _startOperationType = 'StartReceivingSession';
  static const _recordOperationType = 'RecordReceivingEntry';
  static const _submitOperationType = 'SubmitReceivingSession';

  final TraderProLocalDatabase _database;
  final String _installReference;
  final LocalDeviceClock _clock;
  final LocalIdGenerator _idGenerator;

  Future<CreateLocalReceivingSessionResult>
  createLocalReceivingSession() async {
    return _guardStorage(() {
      return _database.transaction(() async {
        final sessionId = _idGenerator.newUuidV7();
        final operationId = _idGenerator.newUuidV7();
        final now = _utcText(_clock.nowUtc());
        final temporaryReference =
            'TMP-RCV-$_installReference-${_compactUuid(sessionId)}';
        final payloadJson = ReceivingOutboxPayload.startSession(
          operationId: operationId,
          localSessionId: sessionId,
          temporaryReference: temporaryReference,
          createdAtDeviceUtc: now,
        );

        await _database
            .into(_database.localReceivingSessions)
            .insert(
              LocalReceivingSessionsCompanion.insert(
                id: sessionId,
                temporaryReference: temporaryReference,
                localStatus: LocalReceivingStatus.open.storageValue,
                localVersion: 1,
                nextLocalSequence: 1,
                activeEntryCount: 0,
                processedTotalWeightKg: ExactWeight.zero,
                createdAtDeviceUtc: now,
                updatedAtDeviceUtc: now,
              ),
            );

        await _database
            .into(_database.localOutboxOperations)
            .insert(
              LocalOutboxOperationsCompanion.insert(
                operationId: operationId,
                aggregateId: sessionId,
                aggregateType: _aggregateType,
                operationType: _startOperationType,
                localSequence: 1,
                payloadJson: payloadJson,
                payloadHash: ReceivingOutboxPayload.hash(payloadJson),
                status: OutboxOperationStatus.pending.storageValue,
                attemptCount: 0,
                createdAtDeviceUtc: now,
              ),
            );

        await (_database.update(
          _database.localReceivingSessions,
        )..where((table) => table.id.equals(sessionId))).write(
          const LocalReceivingSessionsCompanion(nextLocalSequence: Value(2)),
        );

        final session = await _findSessionRow(sessionId);
        final operation = await _findOutboxRow(operationId);
        return CreateLocalReceivingSessionResult(
          session: _mapSession(session!),
          outboxOperation: _mapOutbox(operation!),
        );
      });
    });
  }

  Future<AcceptedLocalReceivingEntry> recordWeightLocally(
    RecordWeightLocallyCommand command,
  ) async {
    _validateRecordCommand(command);
    final processed = WeightProcessor.process(
      rawWeightKg: command.rawWeightKg,
      decimalPlaces: command.decimalPlaces,
      method: command.processingMethod.contractName,
    );
    final operationId = command.operationId ?? _idGenerator.newUuidV7();

    return _guardStorage(() {
      return _database.transaction(() async {
        final existingOperation = await _findOutboxRow(operationId);
        if (existingOperation != null) {
          return _resolveDuplicate(
            command: command,
            operationId: operationId,
            processed: processed,
            existingOperation: existingOperation,
          );
        }

        final session = await _findSessionRow(command.sessionId);
        if (session == null) {
          throw const LocalStoreException(
            LocalStoreException.sessionNotFound,
            'The local Receiving Session does not exist.',
          );
        }
        if (session.localStatus != LocalReceivingStatus.open.storageValue) {
          throw const LocalStoreException(
            LocalStoreException.sessionNotEditable,
            'The local Receiving Session is not editable.',
          );
        }

        final localSequence = session.nextLocalSequence;
        final entryId = _idGenerator.newUuidV7();
        final now = _utcText(_clock.nowUtc());
        final capturedAt = _utcText(command.capturedAtDeviceUtc);
        final payloadJson = ReceivingOutboxPayload.recordEntry(
          operationId: operationId,
          localSessionId: command.sessionId,
          cloudSessionId: session.cloudId,
          localSequence: localSequence,
          productReference: command.productReference,
          bagTypeReference: command.bagTypeReference,
          bagCount: command.bagCount,
          rawWeightKg: processed.rawWeightKg,
          processedWeightKg: processed.processedWeightKg,
          displayWeightKg: processed.displayWeightKg,
          decimalPlaces: processed.decimalPlaces,
          processingMethod: processed.method.contractName,
          weightSource: command.weightSource.storageValue,
          capturedAtDeviceUtc: capturedAt,
        );

        await _database
            .into(_database.localReceivingEntries)
            .insert(
              LocalReceivingEntriesCompanion.insert(
                id: entryId,
                receivingSessionId: command.sessionId,
                operationId: operationId,
                localSequence: localSequence,
                productReference: command.productReference,
                bagTypeReference: command.bagTypeReference,
                bagCount: command.bagCount,
                rawWeightKg: processed.rawWeightKg,
                processedWeightKg: processed.processedWeightKg,
                displayWeightKg: processed.displayWeightKg,
                decimalPlaces: processed.decimalPlaces,
                processingMethod: processed.method.contractName,
                weightSource: command.weightSource.storageValue,
                entryStatus: LocalReceivingEntryStatus.active.storageValue,
                capturedAtDeviceUtc: capturedAt,
                createdAtDeviceUtc: now,
              ),
            );

        await _database
            .into(_database.localOutboxOperations)
            .insert(
              LocalOutboxOperationsCompanion.insert(
                operationId: operationId,
                aggregateId: command.sessionId,
                aggregateType: _aggregateType,
                operationType: _recordOperationType,
                localSequence: localSequence,
                expectedCloudVersion: Value(session.cloudVersion),
                payloadJson: payloadJson,
                payloadHash: ReceivingOutboxPayload.hash(payloadJson),
                status: OutboxOperationStatus.pending.storageValue,
                attemptCount: 0,
                createdAtDeviceUtc: now,
              ),
            );

        final updated =
            await (_database.update(_database.localReceivingSessions)..where(
                  (table) =>
                      table.id.equals(command.sessionId) &
                      table.nextLocalSequence.equals(localSequence),
                ))
                .write(
                  LocalReceivingSessionsCompanion(
                    localVersion: Value(session.localVersion + 1),
                    nextLocalSequence: Value(localSequence + 1),
                    activeEntryCount: Value(session.activeEntryCount + 1),
                    processedTotalWeightKg: Value(
                      ExactWeight.add(
                        session.processedTotalWeightKg,
                        processed.processedWeightKg,
                      ),
                    ),
                    updatedAtDeviceUtc: Value(now),
                  ),
                );
        if (updated != 1) {
          throw const LocalStoreException(
            LocalStoreException.sequenceConflict,
            'The next local Receiving Session sequence changed.',
          );
        }

        final entry = await _findEntryByOperationId(operationId);
        final operation = await _findOutboxRow(operationId);
        return AcceptedLocalReceivingEntry(
          entry: _mapEntry(entry!),
          outboxOperation: _mapOutbox(operation!),
          wasDuplicate: false,
        );
      });
    });
  }

  Future<SubmitLocalReceivingSessionResult> submitSessionLocally({
    required String sessionId,
    String? operationId,
  }) async {
    _requireNonEmpty(sessionId, 'sessionId');
    if (operationId != null) {
      _requireNonEmpty(operationId, 'operationId');
    }
    final resolvedOperationId = operationId ?? _idGenerator.newUuidV7();

    return _guardStorage(() {
      return _database.transaction(() async {
        final existing = await _findOutboxRow(resolvedOperationId);
        if (existing != null) {
          if (existing.aggregateId != sessionId ||
              existing.operationType != _submitOperationType) {
            throw const LocalStoreException(
              LocalStoreException.operationPayloadConflict,
              'The operation ID is already used by another local operation.',
            );
          }
          final candidate = ReceivingOutboxPayload.submitSession(
            operationId: resolvedOperationId,
            localSessionId: sessionId,
          );
          if (ReceivingOutboxPayload.hash(candidate) != existing.payloadHash) {
            throw const LocalStoreException(
              LocalStoreException.operationPayloadConflict,
              'The operation ID exists with a different immutable payload.',
            );
          }
          final duplicateSession = await _findSessionRow(sessionId);
          if (duplicateSession == null) {
            throw const LocalStoreException(
              LocalStoreException.sessionNotFound,
              'The local Receiving Session does not exist.',
            );
          }
          return SubmitLocalReceivingSessionResult(
            session: _mapSession(duplicateSession),
            outboxOperation: _mapOutbox(existing),
            wasDuplicate: true,
          );
        }

        final session = await _findSessionRow(sessionId);
        if (session == null) {
          throw const LocalStoreException(
            LocalStoreException.sessionNotFound,
            'The local Receiving Session does not exist.',
          );
        }
        if (session.localStatus != LocalReceivingStatus.open.storageValue) {
          throw const LocalStoreException(
            LocalStoreException.sessionNotEditable,
            'The local Receiving Session is not editable.',
          );
        }
        if (session.activeEntryCount == 0) {
          throw const LocalStoreException(
            LocalStoreException.invalidInput,
            'At least one immutable receiving entry is required.',
          );
        }

        final now = _utcText(_clock.nowUtc());
        final payloadJson = ReceivingOutboxPayload.submitSession(
          operationId: resolvedOperationId,
          localSessionId: sessionId,
        );
        final localSequence = session.nextLocalSequence;
        await _database
            .into(_database.localOutboxOperations)
            .insert(
              LocalOutboxOperationsCompanion.insert(
                operationId: resolvedOperationId,
                aggregateId: sessionId,
                aggregateType: _aggregateType,
                operationType: _submitOperationType,
                localSequence: localSequence,
                expectedCloudVersion: Value(session.cloudVersion),
                payloadJson: payloadJson,
                payloadHash: ReceivingOutboxPayload.hash(payloadJson),
                status: OutboxOperationStatus.pending.storageValue,
                attemptCount: 0,
                createdAtDeviceUtc: now,
              ),
            );

        final updated =
            await (_database.update(_database.localReceivingSessions)..where(
                  (table) =>
                      table.id.equals(sessionId) &
                      table.nextLocalSequence.equals(localSequence) &
                      table.localStatus.equals(
                        LocalReceivingStatus.open.storageValue,
                      ),
                ))
                .write(
                  LocalReceivingSessionsCompanion(
                    localStatus: Value(
                      LocalReceivingStatus.closed.storageValue,
                    ),
                    localVersion: Value(session.localVersion + 1),
                    nextLocalSequence: Value(localSequence + 1),
                    updatedAtDeviceUtc: Value(now),
                  ),
                );
        if (updated != 1) {
          throw const LocalStoreException(
            LocalStoreException.sequenceConflict,
            'The local Receiving Session changed before submission.',
          );
        }

        return SubmitLocalReceivingSessionResult(
          session: _mapSession((await _findSessionRow(sessionId))!),
          outboxOperation: _mapOutbox(
            (await _findOutboxRow(resolvedOperationId))!,
          ),
          wasDuplicate: false,
        );
      });
    });
  }

  Future<List<LocalReceivingSessionRecord>> listSessions() async {
    final rows = await (_database.select(
      _database.localReceivingSessions,
    )..orderBy([(table) => OrderingTerm.desc(table.updatedAtDeviceUtc)])).get();
    return rows.map(_mapSession).toList(growable: false);
  }

  Future<LocalReceivingSessionRecord?> getSession(String sessionId) async {
    final row = await _findSessionRow(sessionId);
    return row == null ? null : _mapSession(row);
  }

  Future<List<LocalReceivingEntryRecord>> listEntries(String sessionId) async {
    final rows =
        await (_database.select(_database.localReceivingEntries)
              ..where((table) => table.receivingSessionId.equals(sessionId))
              ..orderBy([(table) => OrderingTerm.asc(table.localSequence)]))
            .get();
    return rows.map(_mapEntry).toList(growable: false);
  }

  Future<List<LocalOutboxOperation>> listPendingOperations({
    String? aggregateId,
  }) async {
    if (aggregateId != null) {
      final rows =
          await (_database.select(_database.localOutboxOperations)
                ..where(
                  (table) =>
                      table.aggregateId.equals(aggregateId) &
                      table.status.equals(
                        OutboxOperationStatus.pending.storageValue,
                      ),
                )
                ..orderBy([(table) => OrderingTerm.asc(table.localSequence)]))
              .get();
      return rows.map(_mapOutbox).toList(growable: false);
    }

    final orderedRows =
        await (_database.select(_database.localOutboxOperations)..orderBy([
              (table) => OrderingTerm.asc(table.aggregateId),
              (table) => OrderingTerm.asc(table.localSequence),
            ]))
            .get();
    final eligibleHeads = <LocalOutboxOperationRow>[];
    String? currentAggregateId;
    var aggregateBlocked = false;

    for (final row in orderedRows) {
      if (row.aggregateId != currentAggregateId) {
        currentAggregateId = row.aggregateId;
        aggregateBlocked = false;
      }
      if (aggregateBlocked ||
          row.status == OutboxOperationStatus.accepted.storageValue) {
        continue;
      }

      aggregateBlocked = true;
      if (row.status == OutboxOperationStatus.pending.storageValue) {
        eligibleHeads.add(row);
      }
    }

    return eligibleHeads.map(_mapOutbox).toList(growable: false);
  }

  Future<LocalOutboxOperation?> getOutboxOperation(String operationId) async {
    final row = await _findOutboxRow(operationId);
    return row == null ? null : _mapOutbox(row);
  }

  Future<LocalOutboxOperation> markOperationSending(String operationId) {
    return _transitionOperation(
      operationId: operationId,
      requiredStatus: OutboxOperationStatus.pending,
      nextStatus: OutboxOperationStatus.sending,
      companion: LocalOutboxOperationsCompanion(
        lastAttemptAtUtc: Value(_utcText(_clock.nowUtc())),
        lastErrorCode: const Value(null),
      ),
    );
  }

  Future<LocalOutboxOperation> markOperationAccepted(String operationId) {
    return _transitionOperation(
      operationId: operationId,
      requiredStatus: OutboxOperationStatus.sending,
      nextStatus: OutboxOperationStatus.accepted,
      companion: LocalOutboxOperationsCompanion(
        acceptedAtUtc: Value(_utcText(_clock.nowUtc())),
        lastErrorCode: const Value(null),
      ),
    );
  }

  Future<LocalOutboxOperation> markOperationNeedsAttention(
    String operationId, {
    required String errorCode,
  }) {
    _requireNonEmpty(errorCode, 'errorCode');
    return _failureTransition(
      operationId: operationId,
      nextStatus: OutboxOperationStatus.needsAttention,
      errorCode: errorCode,
    );
  }

  Future<LocalOutboxOperation> recordRetryableFailure(
    String operationId, {
    required String errorCode,
  }) {
    _requireNonEmpty(errorCode, 'errorCode');
    return _failureTransition(
      operationId: operationId,
      nextStatus: OutboxOperationStatus.pending,
      errorCode: errorCode,
    );
  }

  Future<ReceivingSessionProjectionComparison>
  compareReceivingSessionProjection(String sessionId) async {
    final session = await _findSessionRow(sessionId);
    if (session == null) {
      throw const LocalStoreException(
        LocalStoreException.sessionNotFound,
        'The local Receiving Session does not exist.',
      );
    }
    final derived = await _deriveProjection(sessionId);
    return ReceivingSessionProjectionComparison(
      cached: ReceivingSessionProjection(
        activeEntryCount: session.activeEntryCount,
        processedTotalWeightKg: session.processedTotalWeightKg,
      ),
      derived: derived,
    );
  }

  Future<ReceivingSessionProjection> rebuildReceivingSessionProjection(
    String sessionId,
  ) async {
    return _guardStorage(() {
      return _database.transaction(() async {
        final session = await _findSessionRow(sessionId);
        if (session == null) {
          throw const LocalStoreException(
            LocalStoreException.sessionNotFound,
            'The local Receiving Session does not exist.',
          );
        }
        final projection = await _deriveProjection(sessionId);
        await (_database.update(
          _database.localReceivingSessions,
        )..where((table) => table.id.equals(sessionId))).write(
          LocalReceivingSessionsCompanion(
            activeEntryCount: Value(projection.activeEntryCount),
            processedTotalWeightKg: Value(projection.processedTotalWeightKg),
          ),
        );
        return projection;
      });
    });
  }

  Future<AcceptedLocalReceivingEntry> _resolveDuplicate({
    required RecordWeightLocallyCommand command,
    required String operationId,
    required WeightProcessingResult processed,
    required LocalOutboxOperationRow existingOperation,
  }) async {
    final entry = await _findEntryByOperationId(operationId);
    if (existingOperation.aggregateId != command.sessionId ||
        existingOperation.operationType != _recordOperationType ||
        entry == null) {
      throw const LocalStoreException(
        LocalStoreException.operationPayloadConflict,
        'The operation ID is already used by another local operation.',
      );
    }

    final storedPayload =
        jsonDecode(existingOperation.payloadJson)! as Map<String, Object?>;
    final candidateJson = ReceivingOutboxPayload.recordEntry(
      operationId: operationId,
      localSessionId: command.sessionId,
      cloudSessionId: storedPayload['cloudSessionId'] as String?,
      localSequence: existingOperation.localSequence,
      productReference: command.productReference,
      bagTypeReference: command.bagTypeReference,
      bagCount: command.bagCount,
      rawWeightKg: processed.rawWeightKg,
      processedWeightKg: processed.processedWeightKg,
      displayWeightKg: processed.displayWeightKg,
      decimalPlaces: processed.decimalPlaces,
      processingMethod: processed.method.contractName,
      weightSource: command.weightSource.storageValue,
      capturedAtDeviceUtc: _utcText(command.capturedAtDeviceUtc),
    );
    if (ReceivingOutboxPayload.hash(candidateJson) !=
        existingOperation.payloadHash) {
      throw const LocalStoreException(
        LocalStoreException.operationPayloadConflict,
        'The operation ID exists with a different immutable payload.',
      );
    }

    return AcceptedLocalReceivingEntry(
      entry: _mapEntry(entry),
      outboxOperation: _mapOutbox(existingOperation),
      wasDuplicate: true,
    );
  }

  Future<LocalOutboxOperation> _failureTransition({
    required String operationId,
    required OutboxOperationStatus nextStatus,
    required String errorCode,
  }) {
    return _guardStorage(() {
      return _database.transaction(() async {
        final row = await _requiredOutboxRow(operationId);
        _requireStatus(row, OutboxOperationStatus.sending);
        await (_database.update(
          _database.localOutboxOperations,
        )..where((table) => table.operationId.equals(operationId))).write(
          LocalOutboxOperationsCompanion(
            status: Value(nextStatus.storageValue),
            attemptCount: Value(row.attemptCount + 1),
            lastAttemptAtUtc: Value(_utcText(_clock.nowUtc())),
            lastErrorCode: Value(errorCode),
          ),
        );
        return _mapOutbox((await _findOutboxRow(operationId))!);
      });
    });
  }

  Future<LocalOutboxOperation> _transitionOperation({
    required String operationId,
    required OutboxOperationStatus requiredStatus,
    required OutboxOperationStatus nextStatus,
    required LocalOutboxOperationsCompanion companion,
  }) {
    return _guardStorage(() {
      return _database.transaction(() async {
        final row = await _requiredOutboxRow(operationId);
        _requireStatus(row, requiredStatus);
        await (_database.update(_database.localOutboxOperations)
              ..where((table) => table.operationId.equals(operationId)))
            .write(companion.copyWith(status: Value(nextStatus.storageValue)));
        return _mapOutbox((await _findOutboxRow(operationId))!);
      });
    });
  }

  Future<ReceivingSessionProjection> _deriveProjection(String sessionId) async {
    final rows =
        await (_database.select(_database.localReceivingEntries)..where(
              (table) =>
                  table.receivingSessionId.equals(sessionId) &
                  table.entryStatus.equals(
                    LocalReceivingEntryStatus.active.storageValue,
                  ) &
                  table.reversalOfEntryId.isNull(),
            ))
            .get();
    var total = BigInt.zero;
    for (final row in rows) {
      total += ExactWeight.parseCanonical(row.processedWeightKg);
    }
    return ReceivingSessionProjection(
      activeEntryCount: rows.length,
      processedTotalWeightKg: ExactWeight.format(total),
    );
  }

  Future<LocalReceivingSessionRow?> _findSessionRow(String sessionId) {
    return (_database.select(
      _database.localReceivingSessions,
    )..where((table) => table.id.equals(sessionId))).getSingleOrNull();
  }

  Future<LocalReceivingEntryRow?> _findEntryByOperationId(String operationId) {
    return (_database.select(_database.localReceivingEntries)
          ..where((table) => table.operationId.equals(operationId)))
        .getSingleOrNull();
  }

  Future<LocalOutboxOperationRow?> _findOutboxRow(String operationId) {
    return (_database.select(_database.localOutboxOperations)
          ..where((table) => table.operationId.equals(operationId)))
        .getSingleOrNull();
  }

  Future<LocalOutboxOperationRow> _requiredOutboxRow(String operationId) async {
    final row = await _findOutboxRow(operationId);
    if (row == null) {
      throw const LocalStoreException(
        LocalStoreException.operationNotFound,
        'The local outbox operation does not exist.',
      );
    }
    return row;
  }

  static void _requireStatus(
    LocalOutboxOperationRow row,
    OutboxOperationStatus required,
  ) {
    if (row.status != required.storageValue) {
      throw LocalStoreException(
        LocalStoreException.operationStateConflict,
        'Operation ${row.operationId} is ${row.status}; '
        '${required.storageValue} is required.',
      );
    }
  }

  static void _validateRecordCommand(RecordWeightLocallyCommand command) {
    _requireNonEmpty(command.sessionId, 'sessionId');
    _requireNonEmpty(command.productReference, 'productReference');
    _requireNonEmpty(command.bagTypeReference, 'bagTypeReference');
    if (command.operationId != null) {
      _requireNonEmpty(command.operationId!, 'operationId');
    }
    if (command.bagCount <= 0) {
      throw const LocalStoreException(
        LocalStoreException.invalidInput,
        'bagCount must be a positive whole number.',
      );
    }
    if (!command.capturedAtDeviceUtc.isUtc) {
      throw const LocalStoreException(
        LocalStoreException.invalidInput,
        'capturedAtDeviceUtc must be UTC.',
      );
    }
  }

  static void _requireNonEmpty(String value, String name) {
    if (value.trim().isEmpty) {
      throw LocalStoreException(
        LocalStoreException.invalidInput,
        '$name is required.',
      );
    }
  }

  Future<T> _guardStorage<T>(Future<T> Function() action) async {
    try {
      return await action();
    } on LocalStoreException {
      rethrow;
    } on SqliteException catch (exception) {
      final message = exception.message;
      if (message.contains('local_sequence') &&
          (message.contains('aggregate_id') ||
              message.contains('receiving_session_id'))) {
        throw LocalStoreException(
          LocalStoreException.sequenceConflict,
          'The local operation sequence already exists.',
          exception,
        );
      }
      throw LocalStoreException(
        LocalStoreException.storageFailure,
        'The local SQLite transaction failed.',
        exception,
      );
    } on Exception catch (exception) {
      throw LocalStoreException(
        LocalStoreException.storageFailure,
        'The local SQLite transaction failed.',
        exception,
      );
    }
  }

  static String _normalizeInstallReference(String value) {
    final normalized = value.toUpperCase().replaceAll(RegExp('[^A-Z0-9]'), '');
    if (normalized.isEmpty) {
      throw const LocalStoreException(
        LocalStoreException.invalidInput,
        'installReference must contain an ASCII letter or digit.',
      );
    }
    return normalized.length <= 8 ? normalized : normalized.substring(0, 8);
  }

  static String _compactUuid(String id) {
    final compact = id.replaceAll('-', '').toUpperCase();
    if (compact.length != 32 || !RegExp(r'^[0-9A-F]{32}$').hasMatch(compact)) {
      throw const LocalStoreException(
        LocalStoreException.invalidInput,
        'Local IDs must be UUID values.',
      );
    }
    return compact;
  }

  static String _utcText(DateTime value) {
    if (!value.isUtc) {
      throw const LocalStoreException(
        LocalStoreException.invalidInput,
        'Local-store timestamps must be UTC.',
      );
    }
    return value.toIso8601String();
  }

  static LocalReceivingSessionRecord _mapSession(LocalReceivingSessionRow row) {
    return LocalReceivingSessionRecord(
      id: row.id,
      cloudId: row.cloudId,
      temporaryReference: row.temporaryReference,
      localStatus: LocalReceivingStatus.fromStorage(row.localStatus),
      cloudStatus: row.cloudStatus,
      localVersion: row.localVersion,
      cloudVersion: row.cloudVersion,
      nextLocalSequence: row.nextLocalSequence,
      activeEntryCount: row.activeEntryCount,
      processedTotalWeightKg: row.processedTotalWeightKg,
      createdAtDeviceUtc: _parseStoredUtc(
        row.createdAtDeviceUtc,
        'local_receiving_sessions.created_at_device_utc',
      ),
      updatedAtDeviceUtc: _parseStoredUtc(
        row.updatedAtDeviceUtc,
        'local_receiving_sessions.updated_at_device_utc',
      ),
      lastCloudSyncAtUtc: _parseNullableStoredUtc(
        row.lastCloudSyncAtUtc,
        'local_receiving_sessions.last_cloud_sync_at_utc',
      ),
    );
  }

  static LocalReceivingEntryRecord _mapEntry(LocalReceivingEntryRow row) {
    return LocalReceivingEntryRecord(
      id: row.id,
      receivingSessionId: row.receivingSessionId,
      operationId: row.operationId,
      localSequence: row.localSequence,
      productReference: row.productReference,
      bagTypeReference: row.bagTypeReference,
      bagCount: row.bagCount,
      rawWeightKg: row.rawWeightKg,
      processedWeightKg: row.processedWeightKg,
      displayWeightKg: row.displayWeightKg,
      decimalPlaces: row.decimalPlaces,
      processingMethod: row.processingMethod,
      weightSource: switch (row.weightSource) {
        'ManualSpike' => WeightSource.manualSpike,
        'TestScale' => WeightSource.testScale,
        _ => throw StateError('Unsupported weight source: ${row.weightSource}'),
      },
      entryStatus: LocalReceivingEntryStatus.fromStorage(row.entryStatus),
      reversalOfEntryId: row.reversalOfEntryId,
      capturedAtDeviceUtc: _parseStoredUtc(
        row.capturedAtDeviceUtc,
        'local_receiving_entries.captured_at_device_utc',
      ),
      createdAtDeviceUtc: _parseStoredUtc(
        row.createdAtDeviceUtc,
        'local_receiving_entries.created_at_device_utc',
      ),
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
      createdAtDeviceUtc: _parseStoredUtc(
        row.createdAtDeviceUtc,
        'local_outbox_operations.created_at_device_utc',
      ),
      lastAttemptAtUtc: _parseNullableStoredUtc(
        row.lastAttemptAtUtc,
        'local_outbox_operations.last_attempt_at_utc',
      ),
      acceptedAtUtc: _parseNullableStoredUtc(
        row.acceptedAtUtc,
        'local_outbox_operations.accepted_at_utc',
      ),
      lastErrorCode: row.lastErrorCode,
    );
  }

  static DateTime _parseStoredUtc(String value, String fieldName) {
    try {
      final parsed = DateTime.parse(value);
      if (!parsed.isUtc) {
        throw LocalStoreException(
          LocalStoreException.storedTimestampInvalid,
          'Stored timestamp $fieldName must include a UTC offset.',
        );
      }
      return parsed;
    } on LocalStoreException {
      rethrow;
    } on FormatException catch (exception) {
      throw LocalStoreException(
        LocalStoreException.storedTimestampInvalid,
        'Stored timestamp $fieldName is not valid ISO-8601 UTC text.',
        exception,
      );
    }
  }

  static DateTime? _parseNullableStoredUtc(String? value, String fieldName) {
    return value == null ? null : _parseStoredUtc(value, fieldName);
  }
}
