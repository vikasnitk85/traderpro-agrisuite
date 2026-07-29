import 'dart:io';

import 'package:flutter_test/flutter_test.dart';
import 'package:traderpro_agrisuite_mobile/core/database/database.dart';
import 'package:traderpro_agrisuite_mobile/core/measurements/weight_processing_method.dart';
import 'package:traderpro_agrisuite_mobile/core/sync/sync.dart';
import 'package:traderpro_agrisuite_mobile/features/receiving/receiving.dart';

import '../../support/offline_store_test_harness.dart';

void main() {
  test(
    'file-backed facts, projections, and pending outbox survive reopen',
    () async {
      final temporaryDirectory = await Directory.systemTemp.createTemp(
        'traderpro-offline-store-restart-',
      );
      final databaseFile = File(
        '${temporaryDirectory.path}${Platform.pathSeparator}local.sqlite',
      );
      final clock = FixedLocalDeviceClock();
      final ids = SequentialLocalIdGenerator();
      var database = LocalDatabaseOpeners.openFileForTest(databaseFile);
      var store = LocalReceivingStore(
        database: database,
        installReference: 'restart',
        clock: clock,
        idGenerator: ids,
      );

      try {
        final created = await store.createLocalReceivingSession();
        await store.recordWeightLocally(
          RecordWeightLocallyCommand(
            operationId: '019fad0f-2d6a-7000-8000-00000000ee01',
            sessionId: created.session.id,
            productReference: 'PADDY-RESTART',
            bagTypeReference: 'JUTE-50',
            bagCount: 7,
            rawWeightKg: '00050.2370',
            decimalPlaces: 2,
            processingMethod: WeightProcessingMethod.floor,
            weightSource: WeightSource.testScale,
            capturedAtDeviceUtc: DateTime.utc(2026, 7, 29, 9, 29),
          ),
        );
        await database.close();

        database = LocalDatabaseOpeners.openFileForTest(databaseFile);
        store = LocalReceivingStore(
          database: database,
          installReference: 'restart',
          clock: clock,
          idGenerator: ids,
        );

        final session = await store.getSession(created.session.id);
        final entries = await store.listEntries(created.session.id);
        final pending = await store.listPendingOperations(
          aggregateId: created.session.id,
        );

        expect(session, isNotNull);
        expect(session!.localVersion, 2);
        expect(session.nextLocalSequence, 3);
        expect(session.activeEntryCount, 1);
        expect(session.processedTotalWeightKg, '50.230000');
        expect(entries, hasLength(1));
        expect(entries.single.rawWeightKg, '00050.2370');
        expect(entries.single.processedWeightKg, '50.230000');
        expect(entries.single.displayWeightKg, '50.23');
        expect(pending.map((operation) => operation.localSequence), [1, 2]);
        expect(pending.map((operation) => operation.status).toSet(), {
          OutboxOperationStatus.pending,
        });
      } finally {
        await database.close();
        if (temporaryDirectory.existsSync()) {
          await temporaryDirectory.delete(recursive: true);
        }
      }
    },
  );

  test('outbox transitions and failure counts survive reopen', () async {
    final temporaryDirectory = await Directory.systemTemp.createTemp(
      'traderpro-outbox-restart-',
    );
    final databaseFile = File(
      '${temporaryDirectory.path}${Platform.pathSeparator}local.sqlite',
    );
    final clock = FixedLocalDeviceClock();
    final ids = SequentialLocalIdGenerator();
    var database = LocalDatabaseOpeners.openFileForTest(databaseFile);
    var store = LocalReceivingStore(
      database: database,
      installReference: 'outbox',
      clock: clock,
      idGenerator: ids,
    );

    try {
      final created = await store.createLocalReceivingSession();
      final operationId = created.outboxOperation.operationId;
      final originalPayload = created.outboxOperation.payloadJson;
      final originalHash = created.outboxOperation.payloadHash;

      var operation = await store.markOperationSending(operationId);
      expect(operation.status, OutboxOperationStatus.sending);
      expect(operation.attemptCount, 0);

      operation = await store.recordRetryableFailure(
        operationId,
        errorCode: 'TEMPORARY_NETWORK_FAILURE',
      );
      expect(operation.status, OutboxOperationStatus.pending);
      expect(operation.attemptCount, 1);
      expect(operation.lastErrorCode, 'TEMPORARY_NETWORK_FAILURE');

      await store.markOperationSending(operationId);
      operation = await store.markOperationAccepted(operationId);
      expect(operation.status, OutboxOperationStatus.accepted);
      expect(operation.attemptCount, 1);
      expect(operation.payloadJson, originalPayload);
      expect(operation.payloadHash, originalHash);

      final needsAttentionSession = await store.createLocalReceivingSession();
      await store.markOperationSending(
        needsAttentionSession.outboxOperation.operationId,
      );
      final needsAttention = await store.markOperationNeedsAttention(
        needsAttentionSession.outboxOperation.operationId,
        errorCode: 'CLOUD_VERSION_CONFLICT',
      );
      expect(needsAttention.status, OutboxOperationStatus.needsAttention);
      expect(needsAttention.attemptCount, 1);

      await database.close();
      database = LocalDatabaseOpeners.openFileForTest(databaseFile);
      store = LocalReceivingStore(
        database: database,
        installReference: 'outbox',
        clock: clock,
        idGenerator: ids,
      );

      final reopened = await store.getOutboxOperation(operationId);
      expect(reopened!.status, OutboxOperationStatus.accepted);
      expect(reopened.attemptCount, 1);
      expect(reopened.payloadJson, originalPayload);
      expect(reopened.payloadHash, originalHash);
      expect(reopened.acceptedAtUtc, isNotNull);
      expect(
        (await store.getOutboxOperation(
          needsAttentionSession.outboxOperation.operationId,
        ))!.status,
        OutboxOperationStatus.needsAttention,
      );

      await expectLater(
        store.markOperationSending(operationId),
        throwsA(
          isA<LocalStoreException>().having(
            (error) => error.errorCode,
            'errorCode',
            LocalStoreException.operationStateConflict,
          ),
        ),
      );
    } finally {
      await database.close();
      if (temporaryDirectory.existsSync()) {
        await temporaryDirectory.delete(recursive: true);
      }
    }
  });
}
