import 'dart:convert';

import 'package:drift/drift.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:traderpro_agrisuite_mobile/core/database/database.dart';
import 'package:traderpro_agrisuite_mobile/core/measurements/weight_processing_method.dart';
import 'package:traderpro_agrisuite_mobile/features/procurement_poc/procurement_poc.dart';
import 'package:traderpro_agrisuite_mobile/features/receiving/receiving.dart';

import '../../support/offline_store_test_harness.dart';

void main() {
  test(
    'two independent clients complete one five-entry finalization',
    () async {
      final previousWarningSetting =
          driftRuntimeOptions.dontWarnAboutMultipleDatabases;
      driftRuntimeOptions.dontWarnAboutMultipleDatabases = true;
      addTearDown(() {
        driftRuntimeOptions.dontWarnAboutMultipleDatabases =
            previousWarningSetting;
      });
      final clock = _TwoClientClock(DateTime.utc(2026, 7, 29, 10));
      final server = _TwoClientServer(clock);
      final operatorDatabase = LocalDatabaseOpeners.openInMemoryForTest();
      final ownerDatabase = LocalDatabaseOpeners.openInMemoryForTest();
      addTearDown(operatorDatabase.close);
      addTearDown(ownerDatabase.close);
      final operatorReceiving = LocalReceivingStore(
        database: operatorDatabase,
        installReference: 'phone-a',
        clock: clock,
        idGenerator: SequentialLocalIdGenerator(),
      );
      final ownerReceiving = LocalReceivingStore(
        database: ownerDatabase,
        installReference: 'phone-b',
        clock: clock,
        idGenerator: SequentialLocalIdGenerator(),
      );
      final operatorRepository = ProcurementPocLocalRepository(
        database: operatorDatabase,
        localReceivingStore: operatorReceiving,
      );
      final ownerRepository = ProcurementPocLocalRepository(
        database: ownerDatabase,
        localReceivingStore: ownerReceiving,
      );
      final operatorProfile = _profile(
        deviceId: '019fad0f-2d6a-7000-8000-000000000501',
        role: PocDisplayRole.operator,
        clock: clock,
      );
      final ownerProfile = _profile(
        deviceId: '019fad0f-2d6a-7000-8000-000000000502',
        role: PocDisplayRole.owner,
        clock: clock,
      );
      await operatorRepository.saveActiveProfile(operatorProfile);
      await ownerRepository.saveActiveProfile(ownerProfile);

      final created = await operatorReceiving.createLocalReceivingSession();
      for (final raw in ['10.005', '20.005', '30.005', '40.005', '50.005']) {
        await operatorReceiving.recordWeightLocally(
          RecordWeightLocallyCommand(
            sessionId: created.session.id,
            productReference: 'PADDY-TWO-CLIENT',
            bagTypeReference: 'JUTE-50',
            bagCount: 10,
            rawWeightKg: raw,
            decimalPlaces: 2,
            processingMethod: WeightProcessingMethod.standard,
            weightSource: WeightSource.manualSpike,
            capturedAtDeviceUtc: clock.nowUtc(),
          ),
        );
      }
      await operatorReceiving.submitSessionLocally(
        sessionId: created.session.id,
      );
      final operatorSync = ProcurementPocSyncEngine(
        feature: _enabled,
        profileStore: operatorRepository,
        syncStore: operatorRepository,
        apiFactory: (profile) => server.client(profile),
        clock: clock,
      );
      final operatorResult = await operatorSync.synchronize();
      expect(operatorResult.completed, 7);
      expect(server.sessions, hasLength(1));
      expect(server.entryCount, 5);
      expect(server.total, '150.050000');
      expect(server.status, 'SubmittedForReview');

      final ownerPoller = ProcurementPocEventPoller(
        feature: _enabled,
        profileStore: ownerRepository,
        eventStore: ownerRepository,
        apiFactory: (profile) => server.client(profile),
        clock: clock,
      );
      final firstPoll = await ownerPoller.pollOnce();
      expect(firstPoll.received, 7);
      var ownerProjection = await ownerRepository.getRemoteSession(
        ownerProfile,
        created.session.id,
      );
      expect(ownerProjection!.status, 'SubmittedForReview');
      expect(ownerProjection.entryCount, 5);
      expect(ownerProjection.processedTotalWeightKg, '150.050000');

      final controls = ProcurementPocControlService(
        feature: _enabled,
        profileStore: ownerRepository,
        syncStore: ownerRepository,
        controlStore: ownerRepository,
        apiFactory: (profile) => server.client(profile),
        clock: clock,
        idGenerator: SequentialLocalIdGenerator(),
      );
      final approval = await controls.queueApproval(
        sessionId: created.session.id,
        expectedVersion: ownerProjection.cloudVersion,
      );
      await controls.executePending();
      expect(server.approvalKeys, [approval.commandId]);
      ownerProjection = await ownerRepository.getRemoteSession(
        ownerProfile,
        created.session.id,
      );
      expect(ownerProjection!.status, 'Approved');

      final finalization = await controls.queueFinalization(
        sessionId: created.session.id,
        expectedVersion: ownerProjection.cloudVersion,
      );
      await controls.executePending();
      expect(server.finalizationCount, 1);
      final firstFinalizationId = server.finalizationId;
      await controls.retrySameFinalization(created.session.id);
      await controls.executePending();
      expect(server.finalizationCount, 1);
      expect(server.finalizationKeys, [
        finalization.commandId,
        finalization.commandId,
      ]);
      expect(server.finalizationId, firstFinalizationId);
      expect(
        (await ownerRepository.listControlCommands(ownerProfile)).where(
          (command) => command.commandType == PocControlCommandType.finalize,
        ),
        hasLength(1),
      );
      expect(server.events.map((event) => event.eventType), [
        'ReceivingSessionPocStarted',
        'ReceivingEntryPocAccepted',
        'ReceivingEntryPocAccepted',
        'ReceivingEntryPocAccepted',
        'ReceivingEntryPocAccepted',
        'ReceivingEntryPocAccepted',
        'ReceivingSessionPocSubmitted',
        'ReceivingSessionPocApproved',
        'ReceivingSessionPocFinalized',
      ]);
    },
  );
}

const _enabled = ProcurementPocFeatureConfiguration(
  compileTimeEnabled: true,
  releaseMode: false,
);

PocDeviceProfile _profile({
  required String deviceId,
  required PocDisplayRole role,
  required _TwoClientClock clock,
}) {
  return PocDeviceProfile(
    backendBaseUrl: 'http://192.168.1.50:5000',
    workspaceId: '019fad0f-2d6a-7000-8000-000000000500',
    deviceId: deviceId,
    displayRole: role,
    createdAtUtc: clock.nowUtc(),
    updatedAtUtc: clock.nowUtc(),
  );
}

final class _TwoClientClock implements LocalDeviceClock, ProcurementPocClock {
  _TwoClientClock(this.value);

  DateTime value;

  @override
  DateTime nowUtc() => value;
}

final class _TwoClientServer {
  _TwoClientServer(this.clock);

  final _TwoClientClock clock;
  final events = <MobileSyncEvent>[];
  final sessions = <String>{};
  final processedOperations = <String, MobileSyncOperationResult>{};
  final approvalKeys = <String>[];
  final finalizationKeys = <String>[];
  String status = '';
  String total = '0.000000';
  String? editorDeviceId;
  String? sessionId;
  String? finalizationId;
  var version = 0;
  var entryCount = 0;
  var finalizationCount = 0;
  var _eventSequence = 0;
  var _idSequence = 600;

  ProcurementPocApi client(PocDeviceProfile profile) =>
      _TwoClientApi(this, profile);

  String _id() {
    final suffix = (_idSequence++).toString().padLeft(12, '0');
    return '019fad0f-2d6a-7000-8000-$suffix';
  }

  MobileSyncEvent _event(
    String type,
    String aggregateId,
    Map<String, Object?> payload,
  ) {
    return MobileSyncEvent(
      sequence: ++_eventSequence,
      eventId: _id(),
      eventType: type,
      eventVersion: 1,
      aggregateType: 'ReceivingSessionPoc',
      aggregateId: aggregateId,
      aggregateVersion: version,
      occurredAtUtc: clock.nowUtc(),
      correlationId: 'fake-correlation',
      payloadJson: jsonEncode(payload),
    );
  }

  ProcurementPocCommandResult _commandResult({
    required String idempotencyStatus,
  }) {
    return ProcurementPocCommandResult(
      sessionId: sessionId!,
      cloudReference: 'RS-POC-000001',
      status: status,
      editorDeviceId: editorDeviceId!,
      leaseId: status == 'ReceivingInProgress'
          ? '019fad0f-2d6a-7000-8000-000000000599'
          : null,
      leaseExpiresAtUtc: status == 'ReceivingInProgress'
          ? clock.nowUtc().add(const Duration(minutes: 5))
          : null,
      entryCount: entryCount,
      processedTotalWeightKg: total,
      version: version,
      finalizationId: finalizationId,
      idempotencyStatus: idempotencyStatus,
      rawResponseJson: jsonEncode(<String, Object?>{
        'result': <String, Object?>{
          'finalizationId': finalizationId,
          'processedTotalWeightKg': total,
        },
      }),
    );
  }
}

final class _TwoClientApi implements ProcurementPocApi {
  _TwoClientApi(this.server, this.profile);

  final _TwoClientServer server;
  final PocDeviceProfile profile;

  @override
  Future<MobileSyncBatchResult> sendOperations(
    List<MobileSyncOperationEnvelope> operations,
  ) async {
    final results = <MobileSyncOperationResult>[];
    for (final operation in operations) {
      final previous = server.processedOperations[operation.operationId];
      if (previous != null) {
        results.add(
          MobileSyncOperationResult(
            operationId: previous.operationId,
            aggregateId: previous.aggregateId,
            localSequence: previous.localSequence,
            resultStatus: 'PreviouslyProcessed',
            cloudAggregateVersion: previous.cloudAggregateVersion,
            cloudReference: previous.cloudReference,
            leaseId: previous.leaseId,
            leaseExpiresAtUtc: previous.leaseExpiresAtUtc,
            errorCode: null,
            message: null,
          ),
        );
        continue;
      }
      server.sessionId ??= operation.aggregateId;
      server.version++;
      if (operation.operationType == 'StartReceivingSession') {
        server.sessions.add(operation.aggregateId);
        server.editorDeviceId = profile.deviceId;
        server.status = 'ReceivingInProgress';
        server.events.add(
          server._event(
            'ReceivingSessionPocStarted',
            operation.aggregateId,
            <String, Object?>{
              'sessionId': operation.aggregateId,
              'cloudReference': 'RS-POC-000001',
              'status': server.status,
              'editorDeviceId': profile.deviceId,
              'leaseExpiresAtUtc': server.clock
                  .nowUtc()
                  .add(const Duration(minutes: 5))
                  .toIso8601String(),
              'version': server.version,
            },
          ),
        );
      } else if (operation.operationType == 'RecordReceivingEntry') {
        final payload =
            jsonDecode(operation.payloadJson) as Map<String, Object?>;
        server.entryCount++;
        server.total = ExactWeight.add(
          server.total,
          payload['processedWeightKg']! as String,
        );
        server.events.add(
          server._event(
            'ReceivingEntryPocAccepted',
            operation.aggregateId,
            <String, Object?>{
              'sessionId': operation.aggregateId,
              'entryId': server._id(),
              'localSequence': operation.localSequence,
              'productReference': payload['productReference'],
              'bagTypeReference': payload['bagTypeReference'],
              'bagCount': payload['bagCount'],
              'processedWeightKg': payload['processedWeightKg'],
              'entryCount': server.entryCount,
              'processedTotalWeightKg': server.total,
              'version': server.version,
            },
          ),
        );
      } else {
        server.status = 'SubmittedForReview';
        server.events.add(
          server._event(
            'ReceivingSessionPocSubmitted',
            operation.aggregateId,
            <String, Object?>{
              'sessionId': operation.aggregateId,
              'status': server.status,
              'entryCount': server.entryCount,
              'processedTotalWeightKg': server.total,
              'version': server.version,
            },
          ),
        );
      }
      final isSubmit = operation.operationType == 'SubmitReceivingSession';
      final result = MobileSyncOperationResult(
        operationId: operation.operationId,
        aggregateId: operation.aggregateId,
        localSequence: operation.localSequence,
        resultStatus: 'Accepted',
        cloudAggregateVersion: server.version,
        cloudReference: 'RS-POC-000001',
        leaseId: isSubmit ? null : '019fad0f-2d6a-7000-8000-000000000599',
        leaseExpiresAtUtc: isSubmit
            ? null
            : server.clock.nowUtc().add(const Duration(minutes: 5)),
        errorCode: null,
        message: null,
      );
      server.processedOperations[operation.operationId] = result;
      results.add(result);
    }
    return MobileSyncBatchResult(
      operations: results,
      rawResponseJson: '{"fake":true}',
    );
  }

  @override
  Future<MobileSyncEventPage> readEvents({
    required int after,
    required int limit,
  }) async {
    final selected = server.events
        .where((event) => event.sequence > after)
        .take(limit)
        .toList(growable: false);
    return MobileSyncEventPage(
      events: selected,
      nextCursor: selected.isEmpty ? after : selected.last.sequence,
      hasMore: server.events.any(
        (event) =>
            event.sequence >
            (selected.isEmpty ? after : selected.last.sequence),
      ),
    );
  }

  @override
  Future<ProcurementPocCommandResult> approve({
    required String sessionId,
    required int expectedVersion,
    required String idempotencyKey,
  }) async {
    server.approvalKeys.add(idempotencyKey);
    server.version++;
    server.status = 'Approved';
    server.events.add(
      server._event('ReceivingSessionPocApproved', sessionId, <String, Object?>{
        'sessionId': sessionId,
        'status': server.status,
        'approvedByDeviceId': profile.deviceId,
        'version': server.version,
      }),
    );
    return server._commandResult(idempotencyStatus: 'Accepted');
  }

  @override
  Future<ProcurementPocCommandResult> finalize({
    required String sessionId,
    required int expectedVersion,
    required String idempotencyKey,
  }) async {
    server.finalizationKeys.add(idempotencyKey);
    if (server.finalizationId != null) {
      return server._commandResult(idempotencyStatus: 'PreviouslyProcessed');
    }
    server.finalizationCount++;
    server.finalizationId = server._id();
    server.version++;
    server.status = 'Finalized';
    server.events.add(
      server
          ._event('ReceivingSessionPocFinalized', sessionId, <String, Object?>{
            'sessionId': sessionId,
            'finalizationId': server.finalizationId,
            'status': server.status,
            'finalEntryCount': server.entryCount,
            'finalProcessedTotalWeightKg': server.total,
            'version': server.version,
          }),
    );
    return server._commandResult(idempotencyStatus: 'Accepted');
  }

  @override
  void dispose() {}

  @override
  dynamic noSuchMethod(Invocation invocation) => super.noSuchMethod(invocation);
}
