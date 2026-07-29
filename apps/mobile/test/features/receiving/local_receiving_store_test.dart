import 'dart:convert';

import 'package:drift/drift.dart' hide isNull;
import 'package:flutter_test/flutter_test.dart';
import 'package:sqlite3/common.dart';
import 'package:traderpro_agrisuite_mobile/core/database/database.dart';
import 'package:traderpro_agrisuite_mobile/core/measurements/weight_processing_exception.dart';
import 'package:traderpro_agrisuite_mobile/core/sync/sync.dart';
import 'package:traderpro_agrisuite_mobile/features/receiving/receiving.dart';

import '../../support/offline_store_test_harness.dart';

void main() {
  late OfflineStoreTestHarness harness;

  setUp(() {
    harness = OfflineStoreTestHarness.inMemory();
  });

  tearDown(() async {
    await harness.database.close();
  });

  group('session creation atomicity', () {
    test('creates the session and start outbox operation together', () async {
      final result = await harness.store.createLocalReceivingSession();
      final pending = await harness.store.listPendingOperations(
        aggregateId: result.session.id,
      );

      expect(result.session.cloudId, isNull);
      expect(result.session.temporaryReference, startsWith('TMP-RCV-TEST01-'));
      expect(
        result.session.temporaryReference.substring('TMP-RCV-TEST01-'.length),
        hasLength(32),
      );
      expect(result.session.localStatus, LocalReceivingStatus.open);
      expect(result.session.localVersion, 1);
      expect(result.session.nextLocalSequence, 2);
      expect(result.session.activeEntryCount, 0);
      expect(result.session.processedTotalWeightKg, '0.000000');
      expect(result.outboxOperation.operationType, 'StartReceivingSession');
      expect(result.outboxOperation.localSequence, 1);
      expect(result.outboxOperation.status, OutboxOperationStatus.pending);
      expect(pending, hasLength(1));
    });

    test(
      'temporary references use the compact full UUID, not its final eight characters',
      () async {
        final store = LocalReceivingStore(
          database: harness.database,
          installReference: 'test-01',
          clock: harness.clock,
          idGenerator: QueuedLocalIdGenerator([
            '019fad0f-2d6a-7000-8001-1111deadbeef',
            '019fad0f-2d6a-7000-9001-000000000001',
            '019fad0f-2d6a-7000-8002-2222deadbeef',
            '019fad0f-2d6a-7000-9002-000000000002',
          ]),
        );

        final first = await store.createLocalReceivingSession();
        final second = await store.createLocalReceivingSession();

        expect(first.session.id, endsWith('deadbeef'));
        expect(second.session.id, endsWith('deadbeef'));
        expect(
          first.session.temporaryReference,
          'TMP-RCV-TEST01-019FAD0F2D6A700080011111DEADBEEF',
        );
        expect(
          second.session.temporaryReference,
          'TMP-RCV-TEST01-019FAD0F2D6A700080022222DEADBEEF',
        );
        expect(
          first.session.temporaryReference,
          isNot(second.session.temporaryReference),
        );
      },
    );

    test('outbox constraint failure rolls back the session', () async {
      await harness.database.customStatement('''
        CREATE TRIGGER test_fail_start_outbox
        BEFORE INSERT ON local_outbox_operations
        WHEN NEW.operation_type = 'StartReceivingSession'
        BEGIN
          SELECT RAISE(ABORT, 'TEST_START_OUTBOX_FAILURE');
        END
      ''');

      await expectLater(
        harness.store.createLocalReceivingSession(),
        throwsA(
          isA<LocalStoreException>().having(
            (error) => error.errorCode,
            'errorCode',
            LocalStoreException.storageFailure,
          ),
        ),
      );

      expect(
        await harness.database
            .select(harness.database.localReceivingSessions)
            .get(),
        isEmpty,
      );
      expect(
        await harness.database
            .select(harness.database.localOutboxOperations)
            .get(),
        isEmpty,
      );
    });
  });

  group('weight save transaction and captured values', () {
    test('saves entry, outbox, and projection atomically', () async {
      final created = await harness.store.createLocalReceivingSession();
      final accepted = await harness.store.recordWeightLocally(
        harness.recordCommand(sessionId: created.session.id),
      );
      final session = await harness.store.getSession(created.session.id);

      expect(accepted.wasDuplicate, isFalse);
      expect(accepted.entry.localSequence, 2);
      expect(accepted.outboxOperation.localSequence, 2);
      expect(session!.activeEntryCount, 1);
      expect(session.processedTotalWeightKg, '50.230000');
      expect(session.localVersion, 2);
      expect(session.nextLocalSequence, 3);
    });

    test('outbox failure rolls back entry and projection', () async {
      final created = await harness.store.createLocalReceivingSession();
      await harness.database.customStatement('''
        CREATE TRIGGER test_fail_record_outbox
        BEFORE INSERT ON local_outbox_operations
        WHEN NEW.operation_type = 'RecordReceivingEntry'
        BEGIN
          SELECT RAISE(ABORT, 'TEST_RECORD_OUTBOX_FAILURE');
        END
      ''');

      await expectLater(
        harness.store.recordWeightLocally(
          harness.recordCommand(sessionId: created.session.id),
        ),
        throwsA(
          isA<LocalStoreException>().having(
            (error) => error.errorCode,
            'errorCode',
            LocalStoreException.storageFailure,
          ),
        ),
      );

      expect(await harness.store.listEntries(created.session.id), isEmpty);
      final session = await harness.store.getSession(created.session.id);
      expect(session!.activeEntryCount, 0);
      expect(session.processedTotalWeightKg, '0.000000');
      expect(session.localVersion, 1);
      expect(session.nextLocalSequence, 2);
      expect(
        await harness.store.listPendingOperations(
          aggregateId: created.session.id,
        ),
        hasLength(1),
      );
    });

    test('invalid weight produces no writes', () async {
      final created = await harness.store.createLocalReceivingSession();

      await expectLater(
        harness.store.recordWeightLocally(
          harness.recordCommand(
            sessionId: created.session.id,
            rawWeightKg: '50,237',
          ),
        ),
        throwsA(isA<WeightProcessingException>()),
      );

      expect(await harness.store.listEntries(created.session.id), isEmpty);
      expect(
        await harness.store.listPendingOperations(
          aggregateId: created.session.id,
        ),
        hasLength(1),
      );
      expect(
        (await harness.store.getSession(created.session.id))!.localVersion,
        1,
      );
    });

    test('preserves raw, processed, display, and payload values', () async {
      final created = await harness.store.createLocalReceivingSession();
      final accepted = await harness.store.recordWeightLocally(
        harness.recordCommand(
          sessionId: created.session.id,
          operationId: '019fad0f-2d6a-7000-8000-00000000aa01',
          rawWeightKg: '50.237',
        ),
      );
      final payload =
          jsonDecode(accepted.outboxOperation.payloadJson)!
              as Map<String, Object?>;

      expect(accepted.entry.rawWeightKg, '50.237');
      expect(accepted.entry.processedWeightKg, '50.230000');
      expect(accepted.entry.displayWeightKg, '50.23');
      expect(payload['rawWeightKg'], '50.237');
      expect(payload['processedWeightKg'], '50.230000');
      expect(payload['displayWeightKg'], '50.23');
      expect(payload.keys.toList(), [
        'operationId',
        'localSessionId',
        'cloudSessionId',
        'localSequence',
        'productReference',
        'bagTypeReference',
        'bagCount',
        'rawWeightKg',
        'processedWeightKg',
        'displayWeightKg',
        'decimalPlaces',
        'processingMethod',
        'weightSource',
        'capturedAtDeviceUtc',
      ]);
      expect(accepted.outboxOperation.payloadHash, hasLength(64));
    });
  });

  group('duplicate operation behavior', () {
    test('same ID and payload returns the existing local result', () async {
      final created = await harness.store.createLocalReceivingSession();
      const operationId = '019fad0f-2d6a-7000-8000-00000000bb01';
      final command = harness.recordCommand(
        sessionId: created.session.id,
        operationId: operationId,
      );

      final first = await harness.store.recordWeightLocally(command);
      final duplicate = await harness.store.recordWeightLocally(command);
      final session = await harness.store.getSession(created.session.id);

      expect(duplicate.wasDuplicate, isTrue);
      expect(duplicate.entry.id, first.entry.id);
      expect(
        duplicate.outboxOperation.payloadHash,
        first.outboxOperation.payloadHash,
      );
      expect(await harness.store.listEntries(created.session.id), hasLength(1));
      expect(session!.activeEntryCount, 1);
      expect(session.processedTotalWeightKg, '50.230000');
      expect(session.localVersion, 2);
      expect(session.nextLocalSequence, 3);
    });

    test('same ID with changed payload raises a stable conflict', () async {
      final created = await harness.store.createLocalReceivingSession();
      const operationId = '019fad0f-2d6a-7000-8000-00000000bb02';
      await harness.store.recordWeightLocally(
        harness.recordCommand(
          sessionId: created.session.id,
          operationId: operationId,
        ),
      );

      await expectLater(
        harness.store.recordWeightLocally(
          harness.recordCommand(
            sessionId: created.session.id,
            operationId: operationId,
            bagCount: 11,
          ),
        ),
        throwsA(
          isA<LocalStoreException>().having(
            (error) => error.errorCode,
            'errorCode',
            LocalStoreException.operationPayloadConflict,
          ),
        ),
      );

      final session = await harness.store.getSession(created.session.id);
      expect(await harness.store.listEntries(created.session.id), hasLength(1));
      expect(session!.activeEntryCount, 1);
      expect(session.processedTotalWeightKg, '50.230000');
      expect(session.localVersion, 2);
      expect(session.nextLocalSequence, 3);
    });
  });

  group('ordering and isolation', () {
    test('start and entry operations have strict local order', () async {
      final created = await harness.store.createLocalReceivingSession();
      await harness.store.recordWeightLocally(
        harness.recordCommand(
          sessionId: created.session.id,
          operationId: '019fad0f-2d6a-7000-8000-00000000cc01',
        ),
      );
      await harness.store.recordWeightLocally(
        harness.recordCommand(
          sessionId: created.session.id,
          operationId: '019fad0f-2d6a-7000-8000-00000000cc02',
          rawWeightKg: '20.111',
        ),
      );

      final pending = await harness.store.listPendingOperations(
        aggregateId: created.session.id,
      );
      final entries = await harness.store.listEntries(created.session.id);
      expect(pending.map((operation) => operation.localSequence), [1, 2, 3]);
      expect(entries.map((entry) => entry.localSequence), [2, 3]);
      expect(
        (await harness.store.getSession(created.session.id))!.nextLocalSequence,
        4,
      );
    });

    test(
      'session pending order follows sequence when device time moves backward',
      () async {
        harness.clock.value = DateTime.utc(2026, 7, 29, 10);
        final created = await harness.store.createLocalReceivingSession();
        harness.clock.value = DateTime.utc(2026, 7, 29, 9);
        await harness.store.recordWeightLocally(
          harness.recordCommand(
            sessionId: created.session.id,
            operationId: '019fad0f-2d6a-7000-8000-00000000ce01',
          ),
        );
        harness.clock.value = DateTime.utc(2026, 7, 29, 8);
        await harness.store.recordWeightLocally(
          harness.recordCommand(
            sessionId: created.session.id,
            operationId: '019fad0f-2d6a-7000-8000-00000000ce02',
            rawWeightKg: '20.111',
          ),
        );

        final pending = await harness.store.listPendingOperations(
          aggregateId: created.session.id,
        );

        expect(pending.map((operation) => operation.localSequence), [1, 2, 3]);
        expect(pending.map((operation) => operation.createdAtDeviceUtc), [
          DateTime.utc(2026, 7, 29, 10),
          DateTime.utc(2026, 7, 29, 9),
          DateTime.utc(2026, 7, 29, 8),
        ]);
      },
    );

    test(
      'global pending query returns only each aggregate safe head',
      () async {
        final sessionA = await harness.store.createLocalReceivingSession();
        await harness.store.recordWeightLocally(
          harness.recordCommand(
            sessionId: sessionA.session.id,
            operationId: '019fad0f-2d6a-7000-8000-00000000cf01',
          ),
        );
        final sessionB = await harness.store.createLocalReceivingSession();
        await harness.store.recordWeightLocally(
          harness.recordCommand(
            sessionId: sessionB.session.id,
            operationId: '019fad0f-2d6a-7000-8000-00000000cf02',
          ),
        );

        var global = await harness.store.listPendingOperations();
        expect(global, hasLength(2));
        final expectedAggregateOrder = [
          sessionA.session.id,
          sessionB.session.id,
        ]..sort();
        expect(
          global.map((operation) => operation.aggregateId),
          orderedEquals(expectedAggregateOrder),
        );
        expect(
          global
              .where(
                (operation) => operation.aggregateId == sessionA.session.id,
              )
              .single
              .localSequence,
          1,
        );
        expect(
          global
              .where(
                (operation) => operation.aggregateId == sessionB.session.id,
              )
              .single
              .localSequence,
          1,
        );

        await harness.store.markOperationSending(
          sessionA.outboxOperation.operationId,
        );
        global = await harness.store.listPendingOperations();
        expect(
          global.where(
            (operation) => operation.aggregateId == sessionA.session.id,
          ),
          isEmpty,
        );

        await harness.store.markOperationAccepted(
          sessionA.outboxOperation.operationId,
        );
        global = await harness.store.listPendingOperations();
        expect(
          global
              .where(
                (operation) => operation.aggregateId == sessionA.session.id,
              )
              .single
              .localSequence,
          2,
        );
      },
    );

    test('database enforces unique aggregate sequence', () async {
      final created = await harness.store.createLocalReceivingSession();

      await expectLater(
        harness.database
            .into(harness.database.localOutboxOperations)
            .insert(
              LocalOutboxOperationsCompanion.insert(
                operationId: '019fad0f-2d6a-7000-8000-00000000cc03',
                aggregateId: created.session.id,
                aggregateType: 'ReceivingSession',
                operationType: 'RecordReceivingEntry',
                localSequence: 1,
                payloadJson: '{}',
                payloadHash: List.filled(64, '0').join(),
                status: OutboxOperationStatus.pending.storageValue,
                attemptCount: 0,
                createdAtDeviceUtc: DateTime.utc(2026, 7, 29).toIso8601String(),
              ),
            ),
        throwsA(isA<SqliteException>()),
      );
    });

    test('sessions allocate sequences and projections independently', () async {
      final sessionA = await harness.store.createLocalReceivingSession();
      final sessionB = await harness.store.createLocalReceivingSession();
      final entryA = await harness.store.recordWeightLocally(
        harness.recordCommand(
          sessionId: sessionA.session.id,
          operationId: '019fad0f-2d6a-7000-8000-00000000dd01',
          rawWeightKg: '10.129',
        ),
      );
      final entryB = await harness.store.recordWeightLocally(
        harness.recordCommand(
          sessionId: sessionB.session.id,
          operationId: '019fad0f-2d6a-7000-8000-00000000dd02',
          rawWeightKg: '20.239',
        ),
      );

      expect(entryA.entry.localSequence, 2);
      expect(entryB.entry.localSequence, 2);
      expect(
        (await harness.store.getSession(
          sessionA.session.id,
        ))!.processedTotalWeightKg,
        '10.120000',
      );
      expect(
        (await harness.store.getSession(
          sessionB.session.id,
        ))!.processedTotalWeightKg,
        '20.230000',
      );
      expect(
        (await harness.store.listPendingOperations(
          aggregateId: sessionA.session.id,
        )).map((operation) => operation.localSequence),
        [1, 2],
      );
      expect(
        (await harness.store.listPendingOperations(
          aggregateId: sessionB.session.id,
        )).map((operation) => operation.localSequence),
        [1, 2],
      );
    });
  });

  test('projection comparison and rebuild use immutable entry facts', () async {
    final created = await harness.store.createLocalReceivingSession();
    await harness.store.recordWeightLocally(
      harness.recordCommand(sessionId: created.session.id),
    );
    await harness.store.recordWeightLocally(
      harness.recordCommand(
        sessionId: created.session.id,
        rawWeightKg: '20.111',
      ),
    );
    final beforeCorruption = await harness.store.getSession(created.session.id);

    await (harness.database.update(
      harness.database.localReceivingSessions,
    )..where((table) => table.id.equals(created.session.id))).write(
      const LocalReceivingSessionsCompanion(
        activeEntryCount: Value(99),
        processedTotalWeightKg: Value('999.000000'),
      ),
    );

    final comparison = await harness.store.compareReceivingSessionProjection(
      created.session.id,
    );
    expect(comparison.matches, isFalse);
    expect(comparison.derived.activeEntryCount, 2);
    expect(comparison.derived.processedTotalWeightKg, '70.340000');

    final rebuilt = await harness.store.rebuildReceivingSessionProjection(
      created.session.id,
    );
    final repaired = await harness.store.getSession(created.session.id);
    expect(rebuilt.activeEntryCount, 2);
    expect(rebuilt.processedTotalWeightKg, '70.340000');
    expect(repaired!.activeEntryCount, 2);
    expect(repaired.processedTotalWeightKg, '70.340000');
    expect(repaired.localVersion, beforeCorruption!.localVersion);
    expect(repaired.nextLocalSequence, beforeCorruption.nextLocalSequence);
    expect(repaired.updatedAtDeviceUtc, beforeCorruption.updatedAtDeviceUtc);
  });
}
