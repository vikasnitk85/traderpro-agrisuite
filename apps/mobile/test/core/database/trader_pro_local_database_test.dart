import 'dart:io';

import 'package:drift/drift.dart' hide isNotNull, isNull;
import 'package:flutter_test/flutter_test.dart';
import 'package:sqlite3/common.dart';
import 'package:traderpro_agrisuite_mobile/core/database/database.dart';

import '../../support/offline_store_test_harness.dart';

void main() {
  late OfflineStoreTestHarness harness;

  setUp(() {
    harness = OfflineStoreTestHarness.inMemory();
  });

  tearDown(() async {
    await harness.database.close();
  });

  test('schema version one contains only the required local tables', () async {
    final versionRows = await harness.database
        .customSelect('PRAGMA user_version')
        .get();
    final schemaRows = await harness.database
        .customSelect(
          "SELECT type, name FROM sqlite_master "
          "WHERE type IN ('table', 'index', 'trigger') "
          "ORDER BY type, name",
        )
        .get();
    final tables = schemaRows
        .where((row) => row.read<String>('type') == 'table')
        .map((row) => row.read<String>('name'))
        .where((name) => !name.startsWith('sqlite_'))
        .toSet();
    final indexes = schemaRows
        .where((row) => row.read<String>('type') == 'index')
        .map((row) => row.read<String>('name'))
        .toSet();
    final triggers = schemaRows
        .where((row) => row.read<String>('type') == 'trigger')
        .map((row) => row.read<String>('name'))
        .toSet();

    expect(harness.database.schemaVersion, 1);
    expect(versionRows.single.read<int>('user_version'), 1);
    expect(tables, {
      'local_receiving_sessions',
      'local_receiving_entries',
      'local_outbox_operations',
      'local_sync_state',
    });
    expect(indexes, contains('idx_local_receiving_entries_session'));
    expect(indexes, contains('idx_local_outbox_operations_pending'));
    expect(
      triggers,
      containsAll({
        'local_outbox_identity_is_immutable',
        'local_queued_outbox_operations_cannot_be_deleted',
        'local_receiving_entry_fact_is_immutable',
        'local_receiving_entries_cannot_be_deleted',
        'local_receiving_sessions_cannot_be_deleted',
      }),
    );
  });

  test('all weight columns use SQLite TEXT affinity', () async {
    final sessionColumns = await _columns(
      harness.database,
      'local_receiving_sessions',
    );
    final entryColumns = await _columns(
      harness.database,
      'local_receiving_entries',
    );

    expect(sessionColumns['processed_total_weight_kg'], 'TEXT');
    expect(entryColumns['raw_weight_kg'], 'TEXT');
    expect(entryColumns['processed_weight_kg'], 'TEXT');
    expect(entryColumns['display_weight_kg'], 'TEXT');
    expect(sessionColumns.values, isNot(contains('REAL')));
    expect(entryColumns.values, isNot(contains('REAL')));
  });

  test(
    'SQLite constraints reject invalid controlled and numeric values',
    () async {
      final created = await harness.store.createLocalReceivingSession();

      await expectLater(
        harness.database.customStatement(
          'INSERT INTO local_receiving_entries '
          '(id, receiving_session_id, operation_id, local_sequence, '
          'product_reference, bag_type_reference, bag_count, raw_weight_kg, '
          'processed_weight_kg, display_weight_kg, decimal_places, '
          'processing_method, weight_source, entry_status, '
          'captured_at_device_utc, created_at_device_utc) '
          "VALUES ('bad-entry', ?, 'bad-operation', 2, 'P', 'B', 0, "
          "'1', '1.000000', '1.00', 2, 'Floor', 'ManualSpike', 'Active', "
          "'2026-07-29T00:00:00.000Z', '2026-07-29T00:00:00.000Z')",
          [created.session.id],
        ),
        throwsA(isA<SqliteException>()),
      );
    },
  );

  test('outbox identity fields cannot be changed after insertion', () async {
    final created = await harness.store.createLocalReceivingSession();
    final original = created.outboxOperation;
    final updates = <({String column, Object value})>[
      (column: 'operation_id', value: '019fad0f-2d6a-7000-8000-00000000fa01'),
      (column: 'aggregate_id', value: '019fad0f-2d6a-7000-8000-00000000fa02'),
      (column: 'aggregate_type', value: 'ChangedAggregate'),
      (column: 'operation_type', value: 'ChangedOperation'),
      (column: 'local_sequence', value: 99),
      (column: 'expected_cloud_version', value: 7),
      (column: 'payload_json', value: '{"changed":true}'),
      (column: 'payload_hash', value: List.filled(64, 'f').join()),
      (column: 'created_at_device_utc', value: '2026-07-29T01:00:00.000Z'),
    ];

    for (final update in updates) {
      await expectLater(
        harness.database.customStatement(
          'UPDATE local_outbox_operations '
          'SET ${update.column} = ? WHERE operation_id = ?',
          [update.value, original.operationId],
        ),
        throwsA(_sqliteFailure('LOCAL_OUTBOX_IDENTITY_IMMUTABLE')),
        reason: '${update.column} must be immutable',
      );
    }

    final unchanged = await harness.store.getOutboxOperation(
      original.operationId,
    );
    expect(unchanged!.operationId, original.operationId);
    expect(unchanged.aggregateId, original.aggregateId);
    expect(unchanged.aggregateType, original.aggregateType);
    expect(unchanged.operationType, original.operationType);
    expect(unchanged.localSequence, original.localSequence);
    expect(unchanged.expectedCloudVersion, original.expectedCloudVersion);
    expect(unchanged.payloadJson, original.payloadJson);
    expect(unchanged.payloadHash, original.payloadHash);
    expect(unchanged.createdAtDeviceUtc, original.createdAtDeviceUtc);
  });

  test('queued outbox operations cannot be physically deleted', () async {
    final pending = await harness.store.createLocalReceivingSession();
    final sending = await harness.store.createLocalReceivingSession();
    await harness.store.markOperationSending(
      sending.outboxOperation.operationId,
    );
    final needsAttention = await harness.store.createLocalReceivingSession();
    await harness.store.markOperationSending(
      needsAttention.outboxOperation.operationId,
    );
    await harness.store.markOperationNeedsAttention(
      needsAttention.outboxOperation.operationId,
      errorCode: 'REQUIRES_REVIEW',
    );

    final operationIds = [
      pending.outboxOperation.operationId,
      sending.outboxOperation.operationId,
      needsAttention.outboxOperation.operationId,
    ];
    for (final operationId in operationIds) {
      await expectLater(
        (harness.database.delete(
          harness.database.localOutboxOperations,
        )..where((table) => table.operationId.equals(operationId))).go(),
        throwsA(_sqliteFailure('LOCAL_QUEUED_OUTBOX_DELETE_FORBIDDEN')),
      );
      expect(await harness.store.getOutboxOperation(operationId), isNotNull);
    }
  });

  test('accepted physical facts cannot be physically deleted', () async {
    final created = await harness.store.createLocalReceivingSession();
    final accepted = await harness.store.recordWeightLocally(
      harness.recordCommand(sessionId: created.session.id),
    );

    await expectLater(
      (harness.database.delete(
        harness.database.localReceivingEntries,
      )..where((table) => table.id.equals(accepted.entry.id))).go(),
      throwsA(_sqliteFailure('LOCAL_PHYSICAL_FACT_DELETE_FORBIDDEN')),
    );
    expect(await harness.store.listEntries(created.session.id), hasLength(1));
  });

  test('captured physical fact fields cannot be overwritten', () async {
    final created = await harness.store.createLocalReceivingSession();
    final accepted = await harness.store.recordWeightLocally(
      harness.recordCommand(sessionId: created.session.id),
    );

    await expectLater(
      (harness.database.update(
        harness.database.localReceivingEntries,
      )..where((table) => table.id.equals(accepted.entry.id))).write(
        const LocalReceivingEntriesCompanion(rawWeightKg: Value('999')),
      ),
      throwsA(_sqliteFailure('LOCAL_PHYSICAL_FACT_UPDATE_FORBIDDEN')),
    );
    expect(
      (await harness.store.listEntries(created.session.id)).single.rawWeightKg,
      '50.237',
    );
  });

  test('entry status and reversal reference cannot be changed', () async {
    final created = await harness.store.createLocalReceivingSession();
    final accepted = await harness.store.recordWeightLocally(
      harness.recordCommand(sessionId: created.session.id),
    );

    await expectLater(
      (harness.database.update(
        harness.database.localReceivingEntries,
      )..where((table) => table.id.equals(accepted.entry.id))).write(
        const LocalReceivingEntriesCompanion(entryStatus: Value('Reversed')),
      ),
      throwsA(_sqliteFailure('LOCAL_PHYSICAL_FACT_UPDATE_FORBIDDEN')),
    );
    await expectLater(
      (harness.database.update(
        harness.database.localReceivingEntries,
      )..where((table) => table.id.equals(accepted.entry.id))).write(
        LocalReceivingEntriesCompanion(
          reversalOfEntryId: Value(accepted.entry.id),
        ),
      ),
      throwsA(_sqliteFailure('LOCAL_PHYSICAL_FACT_UPDATE_FORBIDDEN')),
    );

    final unchanged = (await harness.store.listEntries(
      created.session.id,
    )).single;
    expect(unchanged.entryStatus.storageValue, 'Active');
    expect(unchanged.reversalOfEntryId, isNull);
  });

  test('Receiving Sessions cannot be physically deleted', () async {
    final created = await harness.store.createLocalReceivingSession();

    await expectLater(
      (harness.database.delete(
        harness.database.localReceivingSessions,
      )..where((table) => table.id.equals(created.session.id))).go(),
      throwsA(_sqliteFailure('LOCAL_RECEIVING_SESSION_DELETE_FORBIDDEN')),
    );
    expect(await harness.store.getSession(created.session.id), isNotNull);
  });

  test('malformed and non-UTC stored timestamps are rejected', () async {
    final created = await harness.store.createLocalReceivingSession();

    for (final invalidTimestamp in [
      'not-a-timestamp',
      '2026-07-29T09:30:00.000',
    ]) {
      await (harness.database.update(
        harness.database.localReceivingSessions,
      )..where((table) => table.id.equals(created.session.id))).write(
        LocalReceivingSessionsCompanion(
          createdAtDeviceUtc: Value(invalidTimestamp),
        ),
      );

      await expectLater(
        harness.store.getSession(created.session.id),
        throwsA(
          isA<LocalStoreException>()
              .having(
                (error) => error.errorCode,
                'errorCode',
                LocalStoreException.storedTimestampInvalid,
              )
              .having(
                (error) => error.message,
                'message',
                contains('created_at_device_utc'),
              ),
        ),
      );
    }

    await (harness.database.update(
      harness.database.localReceivingSessions,
    )..where((table) => table.id.equals(created.session.id))).write(
      LocalReceivingSessionsCompanion(
        createdAtDeviceUtc: Value(DateTime.utc(2026, 7, 29).toIso8601String()),
        lastCloudSyncAtUtc: const Value('2026-07-29T09:30:00.000'),
      ),
    );
    await expectLater(
      harness.store.getSession(created.session.id),
      throwsA(
        isA<LocalStoreException>().having(
          (error) => error.message,
          'message',
          contains('last_cloud_sync_at_utc'),
        ),
      ),
    );
  });

  test('local-store sources exclude UI, BLE, network, and platform imports', () {
    final mobileRoot = Directory.current.absolute;
    final sourceRoots = [
      Directory('${mobileRoot.path}/lib/core/database'),
      Directory('${mobileRoot.path}/lib/core/sync'),
      Directory('${mobileRoot.path}/lib/features/receiving/application'),
      Directory('${mobileRoot.path}/lib/features/receiving/domain'),
      Directory('${mobileRoot.path}/lib/features/receiving/infrastructure'),
    ];
    final forbidden = RegExp(
      r'''import\s+['"](?:package:flutter/(?:widgets|material|services)\.dart|'''
      r'''package:(?:http|dio|signalr|flutter_blue|flutter_reactive_ble)/|'''
      r'''dart:(?:html|js|js_interop))''',
    );

    final violations = <String>[];
    for (final root in sourceRoots) {
      for (final entity in root.listSync(recursive: true)) {
        if (entity is! File ||
            !entity.path.endsWith('.dart') ||
            entity.path.endsWith('.g.dart')) {
          continue;
        }
        if (forbidden.hasMatch(entity.readAsStringSync())) {
          violations.add(entity.path);
        }
      }
    }

    expect(violations, isEmpty);
  });
}

Future<Map<String, String>> _columns(
  TraderProLocalDatabase database,
  String table,
) async {
  final rows = await database.customSelect("PRAGMA table_info('$table')").get();
  return {
    for (final row in rows)
      row.read<String>('name'): row.read<String>('type').toUpperCase(),
  };
}

Matcher _sqliteFailure(String message) {
  return isA<SqliteException>().having(
    (exception) => exception.message,
    'message',
    contains(message),
  );
}
