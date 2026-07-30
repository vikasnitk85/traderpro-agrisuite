import 'dart:io';

import 'package:drift/drift.dart' show Variable;
import 'package:flutter_test/flutter_test.dart';
import 'package:traderpro_agrisuite_mobile/core/database/database.dart';
import 'package:traderpro_agrisuite_mobile/core/measurements/weight_processing_method.dart';
import 'package:traderpro_agrisuite_mobile/core/sync/outbox_operation_status.dart';
import 'package:traderpro_agrisuite_mobile/features/procurement_poc/procurement_poc.dart';
import 'package:traderpro_agrisuite_mobile/features/receiving/receiving.dart';

import '../../support/offline_store_test_harness.dart';

const _enabledFeature = ProcurementPocFeatureConfiguration(
  compileTimeEnabled: true,
  releaseMode: false,
);

void main() {
  test(
    'unrelated outbox rows are never recovered, sent, or diagnosed',
    () async {
      final database = LocalDatabaseOpeners.openInMemoryForTest();
      addTearDown(database.close);
      final clock = _PocTestClock(DateTime.utc(2026, 7, 30, 9, 30));
      final receiving = LocalReceivingStore(
        database: database,
        installReference: 'scope-poc',
        clock: clock,
        idGenerator: SequentialLocalIdGenerator(),
      );
      final repository = ProcurementPocLocalRepository(
        database: database,
        localReceivingStore: receiving,
      );
      final profile = await repository.saveActiveProfile(
        PocDeviceProfile(
          backendBaseUrl: 'http://192.168.1.50:5000',
          workspaceId: '019fad0f-2d6a-7000-8000-0000000001a0',
          deviceId: '019fad0f-2d6a-7000-8000-0000000001a1',
          displayRole: PocDisplayRole.operator,
          createdAtUtc: clock.nowUtc(),
          updatedAtUtc: clock.nowUtc(),
        ),
      );
      await receiving.createLocalReceivingSession();
      const unrelatedId = '019fad0f-2d6a-7000-8000-0000000001a2';
      const unrelatedAggregate = '019fad0f-2d6a-7000-8000-0000000001a3';
      const unrelatedPayload = '{ "future" : "inventory-operation" }';
      const unrelatedHash =
          'cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc';
      await database.customStatement(
        'INSERT INTO local_outbox_operations '
        '(operation_id, aggregate_id, aggregate_type, operation_type, '
        'local_sequence, expected_cloud_version, payload_json, payload_hash, '
        'status, attempt_count, created_at_device_utc, last_attempt_at_utc, '
        'accepted_at_utc, last_error_code) VALUES '
        "(?, ?, 'Inventory', 'FutureInventoryOperation', 1, NULL, ?, ?, "
        "'Sending', 4, ?, ?, NULL, 'FUTURE_MODULE_STATE')",
        [
          unrelatedId,
          unrelatedAggregate,
          unrelatedPayload,
          unrelatedHash,
          clock.nowUtc().toIso8601String(),
          clock.nowUtc().toIso8601String(),
        ],
      );
      final api = _FakePocApi(clock);
      final engine = ProcurementPocSyncEngine(
        feature: _enabledFeature,
        profileStore: repository,
        syncStore: repository,
        apiFactory: (_) => api,
        clock: clock,
      );

      final pocOperation =
          (await repository.listLocalOutboxOperations()).single;
      await repository.markMobileOperationsSending([
        pocOperation.operationId,
      ], clock.nowUtc());
      await repository.recoverAmbiguousMobileOperations(clock.nowUtc());
      final recoveredPoc =
          (await repository.listLocalOutboxOperations()).single;
      expect(recoveredPoc.status, OutboxOperationStatus.pending);
      expect(recoveredPoc.attemptCount, 1);

      expect((await engine.synchronize()).completed, 1);
      expect(
        api.batches.expand((batch) => batch).map((item) => item.operationId),
        isNot(contains(unrelatedId)),
      );
      final unrelated = await database
          .customSelect(
            'SELECT payload_json, payload_hash, status, attempt_count, '
            'last_error_code FROM local_outbox_operations '
            'WHERE operation_id = ?',
            variables: [Variable<String>(unrelatedId)],
          )
          .getSingle();
      expect(unrelated.read<String>('payload_json'), unrelatedPayload);
      expect(unrelated.read<String>('payload_hash'), unrelatedHash);
      expect(unrelated.read<String>('status'), 'Sending');
      expect(unrelated.read<int>('attempt_count'), 4);
      expect(unrelated.read<String>('last_error_code'), 'FUTURE_MODULE_STATE');
      final diagnostics = await repository.loadDiagnostics(profile);
      expect(diagnostics.pending, 0);
      expect(diagnostics.sending, 0);
      expect(diagnostics.completed, 1);
    },
  );

  test(
    'cloud identity mismatch and regressive versions are rejected',
    () async {
      final database = LocalDatabaseOpeners.openInMemoryForTest();
      addTearDown(database.close);
      final clock = _PocTestClock(DateTime.utc(2026, 7, 30, 9, 30));
      final receiving = LocalReceivingStore(
        database: database,
        installReference: 'contract-poc',
        clock: clock,
        idGenerator: SequentialLocalIdGenerator(),
      );
      final repository = ProcurementPocLocalRepository(
        database: database,
        localReceivingStore: receiving,
      );
      final profile = await repository.saveActiveProfile(
        PocDeviceProfile(
          backendBaseUrl: 'http://192.168.1.50:5000',
          workspaceId: '019fad0f-2d6a-7000-8000-0000000001b0',
          deviceId: '019fad0f-2d6a-7000-8000-0000000001b1',
          displayRole: PocDisplayRole.operator,
          createdAtUtc: clock.nowUtc(),
          updatedAtUtc: clock.nowUtc(),
        ),
      );
      final created = await receiving.createLocalReceivingSession();
      final api = _FakePocApi(clock);
      final engine = ProcurementPocSyncEngine(
        feature: _enabledFeature,
        profileStore: repository,
        syncStore: repository,
        apiFactory: (_) => api,
        clock: clock,
      );
      expect((await engine.synchronize()).completed, 1);
      await database.customStatement(
        'UPDATE receiving_session_cloud_states SET cloud_version = 5 '
        'WHERE local_session_id = ?',
        [created.session.id],
      );
      await receiving.recordWeightLocally(
        RecordWeightLocallyCommand(
          sessionId: created.session.id,
          productReference: 'REGRESSION',
          bagTypeReference: 'JUTE',
          bagCount: 1,
          rawWeightKg: '10.00',
          decimalPlaces: 2,
          processingMethod: WeightProcessingMethod.standard,
          weightSource: WeightSource.manualSpike,
          capturedAtDeviceUtc: clock.nowUtc(),
        ),
      );

      final regressive = await engine.synchronize();

      expect(regressive.networkAmbiguous, isTrue);
      final cloud = await repository.getCloudState(profile, created.session.id);
      expect(cloud!.cloudVersion, 5);
      expect(
        (await repository.listLocalOutboxOperations(
          aggregateId: created.session.id,
        )).last.status,
        OutboxOperationStatus.pending,
      );

      final other = await receiving.createLocalReceivingSession();
      await expectLater(
        database.customStatement(
          'INSERT INTO receiving_session_cloud_states '
          '(source_key, bound_device_id, local_session_id, cloud_session_id, '
          'cloud_reference, cloud_status, cloud_version, lease_id, '
          'lease_expires_at_utc, editor_device_id, '
          'last_successful_sync_at_utc, last_cloud_update_at_utc, '
          'last_error_code, last_error_message) VALUES '
          "(?, ?, ?, ?, 'RS-MISMATCH', 'ReceivingInProgress', 1, ?, ?, ?, "
          '?, ?, NULL, NULL)',
          [
            profile.sourceKey,
            profile.deviceId,
            other.session.id,
            '019fad0f-2d6a-7000-8000-0000000001bf',
            '019fad0f-2d6a-7000-8000-0000000001be',
            clock.nowUtc().add(const Duration(minutes: 5)).toIso8601String(),
            profile.deviceId,
            clock.nowUtc().toIso8601String(),
            clock.nowUtc().toIso8601String(),
          ],
        ),
        throwsA(anything),
      );
    },
  );

  test(
    'Needs Attention blocks one aggregate while another continues',
    () async {
      final database = LocalDatabaseOpeners.openInMemoryForTest();
      addTearDown(database.close);
      final clock = _PocTestClock(DateTime.utc(2026, 7, 29, 9, 30));
      final ids = SequentialLocalIdGenerator();
      final receiving = LocalReceivingStore(
        database: database,
        installReference: 'batch-poc',
        clock: clock,
        idGenerator: ids,
      );
      final repository = ProcurementPocLocalRepository(
        database: database,
        localReceivingStore: receiving,
      );
      await repository.saveActiveProfile(
        PocDeviceProfile(
          backendBaseUrl: 'http://192.168.1.50:5000',
          workspaceId: '019fad0f-2d6a-7000-8000-000000000110',
          deviceId: '019fad0f-2d6a-7000-8000-000000000111',
          displayRole: PocDisplayRole.operator,
          createdAtUtc: clock.nowUtc(),
          updatedAtUtc: clock.nowUtc(),
        ),
      );
      final first = await receiving.createLocalReceivingSession();
      final second = await receiving.createLocalReceivingSession();
      await receiving.recordWeightLocally(
        RecordWeightLocallyCommand(
          sessionId: first.session.id,
          productReference: 'FIRST',
          bagTypeReference: 'JUTE',
          bagCount: 1,
          rawWeightKg: '10.00',
          decimalPlaces: 2,
          processingMethod: WeightProcessingMethod.standard,
          weightSource: WeightSource.manualSpike,
          capturedAtDeviceUtc: clock.nowUtc(),
        ),
      );
      await receiving.recordWeightLocally(
        RecordWeightLocallyCommand(
          sessionId: second.session.id,
          productReference: 'SECOND',
          bagTypeReference: 'JUTE',
          bagCount: 1,
          rawWeightKg: '20.00',
          decimalPlaces: 2,
          processingMethod: WeightProcessingMethod.standard,
          weightSource: WeightSource.manualSpike,
          capturedAtDeviceUtc: clock.nowUtc(),
        ),
      );
      final api = _FakePocApi(clock)
        ..needsAttentionAggregate = first.session.id;
      final engine = ProcurementPocSyncEngine(
        feature: _enabledFeature,
        profileStore: repository,
        syncStore: repository,
        apiFactory: (_) => api,
        clock: clock,
      );
      final result = await engine.synchronize();

      expect(result.needsAttention, 1);
      final firstOperations = await repository.listLocalOutboxOperations(
        aggregateId: first.session.id,
      );
      final secondOperations = await repository.listLocalOutboxOperations(
        aggregateId: second.session.id,
      );
      expect(firstOperations.last.status, OutboxOperationStatus.needsAttention);
      expect(secondOperations.last.status, OutboxOperationStatus.accepted);
      expect(api.batches.every((batch) => batch.length <= 50), isTrue);
    },
  );

  test(
    'offline restart, Start-first, five entries, and response-loss replay',
    () async {
      final directory = await Directory.systemTemp.createTemp(
        'traderpro-poc-sync-',
      );
      final file = File(
        '${directory.path}${Platform.pathSeparator}local.sqlite',
      );
      final clock = _PocTestClock(DateTime.utc(2026, 7, 29, 9, 30));
      final ids = SequentialLocalIdGenerator();
      final fakeServer = _FakePocApi(clock)..dropFirstDependentResponse = true;
      var database = LocalDatabaseOpeners.openFileForTest(file);
      var receiving = LocalReceivingStore(
        database: database,
        installReference: 'sync-poc',
        clock: clock,
        idGenerator: ids,
      );
      var repository = ProcurementPocLocalRepository(
        database: database,
        localReceivingStore: receiving,
      );

      try {
        final profile = await repository.saveActiveProfile(
          PocDeviceProfile(
            backendBaseUrl: 'http://192.168.1.50:5000/',
            workspaceId: '019fad0f-2d6a-7000-8000-000000000100',
            deviceId: '019fad0f-2d6a-7000-8000-000000000101',
            displayRole: PocDisplayRole.operator,
            createdAtUtc: clock.nowUtc(),
            updatedAtUtc: clock.nowUtc(),
          ),
        );
        final created = await receiving.createLocalReceivingSession();
        for (final raw in ['10.005', '20.004', '30.999', '40.001', '50.237']) {
          await receiving.recordWeightLocally(
            RecordWeightLocallyCommand(
              sessionId: created.session.id,
              productReference: 'PADDY-POC',
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
        await receiving.submitSessionLocally(sessionId: created.session.id);
        final originalOperations = await repository.listLocalOutboxOperations(
          aggregateId: created.session.id,
        );
        final originalPayloads = {
          for (final operation in originalOperations)
            operation.operationId: (
              operation.payloadJson,
              operation.payloadHash,
            ),
        };
        expect(originalOperations, hasLength(7));
        expect(
          (await receiving.getSession(
            created.session.id,
          ))!.processedTotalWeightKg,
          '151.250000',
        );

        await database.close();
        database = LocalDatabaseOpeners.openFileForTest(file);
        receiving = LocalReceivingStore(
          database: database,
          installReference: 'sync-poc',
          clock: clock,
          idGenerator: ids,
        );
        repository = ProcurementPocLocalRepository(
          database: database,
          localReceivingStore: receiving,
        );
        var engine = ProcurementPocSyncEngine(
          feature: _enabledFeature,
          profileStore: repository,
          syncStore: repository,
          apiFactory: (_) => fakeServer,
          clock: clock,
        );

        final lost = await engine.synchronize();
        expect(lost.networkAmbiguous, isTrue);
        expect(fakeServer.batches.first, hasLength(1));
        expect(
          fakeServer.batches.first.single.operationType,
          'StartReceivingSession',
        );
        expect(fakeServer.batches.first.single.leaseId, isNull);
        expect(fakeServer.batches[1], hasLength(6));
        expect(fakeServer.batches[1].map((item) => item.localSequence), [
          2,
          3,
          4,
          5,
          6,
          7,
        ]);
        for (final operation in fakeServer.batches[1]) {
          expect(operation.leaseId, '019fad0f-2d6a-7000-8000-000000000199');
          expect(operation.payloadJson, isNot(contains('"leaseId"')));
        }

        await database.close();
        database = LocalDatabaseOpeners.openFileForTest(file);
        receiving = LocalReceivingStore(
          database: database,
          installReference: 'sync-poc',
          clock: clock,
          idGenerator: ids,
        );
        repository = ProcurementPocLocalRepository(
          database: database,
          localReceivingStore: receiving,
        );
        engine = ProcurementPocSyncEngine(
          feature: _enabledFeature,
          profileStore: repository,
          syncStore: repository,
          apiFactory: (_) => fakeServer,
          clock: clock,
        );
        final recovered = await engine.synchronize();

        expect(recovered.networkAmbiguous, isFalse);
        expect(recovered.completed, 6);
        expect(
          fakeServer.batches[2].map((item) => item.operationId),
          fakeServer.batches[1].map((item) => item.operationId),
        );
        for (final operation in fakeServer.batches[2]) {
          final original = originalPayloads[operation.operationId]!;
          expect(operation.payloadJson, original.$1);
          expect(operation.payloadHash, original.$2);
        }
        final finalOperations = await repository.listLocalOutboxOperations(
          aggregateId: created.session.id,
        );
        expect(finalOperations.map((item) => item.status).toSet(), {
          OutboxOperationStatus.accepted,
        });
        expect(finalOperations, hasLength(7));
        expect(
          (await repository.getCloudState(
            profile,
            created.session.id,
          ))!.cloudStatus,
          'SubmittedForReview',
        );
        expect(
          (await repository.getCloudState(
            profile,
            created.session.id,
          ))!.leaseId,
          isNull,
        );
        expect(
          (await receiving.getSession(
            created.session.id,
          ))!.processedTotalWeightKg,
          '151.250000',
        );
      } finally {
        await database.close();
        if (directory.existsSync()) {
          await directory.delete(recursive: true);
        }
      }
    },
  );
}

final class _PocTestClock implements LocalDeviceClock, ProcurementPocClock {
  _PocTestClock(this.value);

  DateTime value;

  @override
  DateTime nowUtc() => value;
}

final class _FakePocApi implements ProcurementPocApi {
  _FakePocApi(this.clock);

  final _PocTestClock clock;
  final batches = <List<MobileSyncOperationEnvelope>>[];
  final _results = <String, MobileSyncOperationResult>{};
  final _versions = <String, int>{};
  var dropFirstDependentResponse = false;
  String? needsAttentionAggregate;
  var _dropped = false;

  @override
  Future<MobileSyncBatchResult> sendOperations(
    List<MobileSyncOperationEnvelope> operations,
  ) async {
    batches.add(List.of(operations));
    final results = <MobileSyncOperationResult>[];
    for (final operation in operations) {
      if (operation.aggregateId == needsAttentionAggregate &&
          operation.operationType == 'RecordReceivingEntry') {
        results.add(
          MobileSyncOperationResult(
            operationId: operation.operationId,
            aggregateId: operation.aggregateId,
            localSequence: operation.localSequence,
            resultStatus: 'NeedsAttention',
            cloudAggregateVersion: null,
            cloudReference: null,
            leaseId: null,
            leaseExpiresAtUtc: null,
            errorCode: 'RECEIVING_POC_SEQUENCE_CONFLICT',
            message: 'Fake aggregate attention.',
          ),
        );
        continue;
      }
      final previous = _results[operation.operationId];
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
      final version = (_versions[operation.aggregateId] ?? 0) + 1;
      _versions[operation.aggregateId] = version;
      final isSubmit = operation.operationType == 'SubmitReceivingSession';
      final result = MobileSyncOperationResult(
        operationId: operation.operationId,
        aggregateId: operation.aggregateId,
        localSequence: operation.localSequence,
        resultStatus: 'Accepted',
        cloudAggregateVersion: version,
        cloudReference: 'RS-POC-000001',
        leaseId: isSubmit ? null : '019fad0f-2d6a-7000-8000-000000000199',
        leaseExpiresAtUtc: isSubmit
            ? null
            : clock.nowUtc().add(const Duration(minutes: 5)),
        errorCode: null,
        message: null,
      );
      _results[operation.operationId] = result;
      results.add(result);
    }
    if (dropFirstDependentResponse &&
        !_dropped &&
        operations.any(
          (operation) => operation.operationType == 'RecordReceivingEntry',
        )) {
      _dropped = true;
      throw const ProcurementPocApiException(
        code: 'POC_NETWORK_AMBIGUOUS',
        message: 'Fake response lost after commit.',
        retryable: true,
        responseAmbiguous: true,
      );
    }
    return MobileSyncBatchResult(
      operations: results,
      rawResponseJson: '{"fake":true}',
    );
  }

  @override
  void dispose() {}

  @override
  dynamic noSuchMethod(Invocation invocation) => super.noSuchMethod(invocation);
}
