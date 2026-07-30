import 'dart:io';

import 'package:drift/drift.dart' show Variable;
import 'package:flutter_test/flutter_test.dart';
import 'package:sqlite3/sqlite3.dart';
import 'package:traderpro_agrisuite_mobile/core/database/database.dart';

void main() {
  test('schema 1 data migrates directly to schema 3 byte-for-byte', () async {
    final directory = await Directory.systemTemp.createTemp(
      'traderpro-poc-migration-',
    );
    final file = File('${directory.path}${Platform.pathSeparator}local.sqlite');
    const sessionId = '019fad0f-2d6a-7000-8000-00000000a001';
    const entryId = '019fad0f-2d6a-7000-8000-00000000a002';
    const startOperation = '019fad0f-2d6a-7000-8000-00000000a003';
    const entryOperation = '019fad0f-2d6a-7000-8000-00000000a004';
    const payloadOne =
        '{"operationId":"019fad0f-2d6a-7000-8000-00000000a003",'
        '"localSessionId":"019fad0f-2d6a-7000-8000-00000000a001"}';
    const payloadTwo =
        '{"rawWeightKg":"00050.2370","processedWeightKg":"50.230000"}';
    const hashOne =
        '1111111111111111111111111111111111111111111111111111111111111111';
    const hashTwo =
        '2222222222222222222222222222222222222222222222222222222222222222';

    try {
      final sqlite = sqlite3.open(file.path);
      try {
        _createSchemaOne(sqlite);
        sqlite.execute(
          'INSERT INTO local_receiving_sessions '
          '(id, cloud_id, temporary_reference, local_status, cloud_status, '
          'local_version, cloud_version, next_local_sequence, '
          'active_entry_count, processed_total_weight_kg, '
          'created_at_device_utc, updated_at_device_utc, '
          'last_cloud_sync_at_utc) VALUES (?, NULL, ?, ?, NULL, 2, NULL, 3, '
          '1, ?, ?, ?, NULL)',
          [
            sessionId,
            'TMP-RCV-MIGRATION-019FAD0F2D6A7000800000000000A001',
            'Open',
            '50.230000',
            '2026-07-29T09:00:00.000Z',
            '2026-07-29T09:01:00.000Z',
          ],
        );
        sqlite.execute(
          'INSERT INTO local_receiving_entries '
          '(id, receiving_session_id, operation_id, local_sequence, '
          'product_reference, bag_type_reference, bag_count, raw_weight_kg, '
          'processed_weight_kg, display_weight_kg, decimal_places, '
          'processing_method, weight_source, entry_status, '
          'reversal_of_entry_id, captured_at_device_utc, '
          'created_at_device_utc) VALUES (?, ?, ?, 2, ?, ?, 7, ?, ?, ?, 2, '
          "'Floor', 'ManualSpike', 'Active', NULL, ?, ?)",
          [
            entryId,
            sessionId,
            entryOperation,
            'PADDY-MIGRATION',
            'JUTE-50',
            '00050.2370',
            '50.230000',
            '50.23',
            '2026-07-29T09:01:00.000Z',
            '2026-07-29T09:01:00.000Z',
          ],
        );
        sqlite.execute(
          'INSERT INTO local_outbox_operations '
          '(operation_id, aggregate_id, aggregate_type, operation_type, '
          'local_sequence, expected_cloud_version, payload_json, '
          'payload_hash, status, attempt_count, created_at_device_utc, '
          'last_attempt_at_utc, accepted_at_utc, last_error_code) VALUES '
          "(?, ?, 'ReceivingSession', 'StartReceivingSession', 1, NULL, ?, ?, "
          "'Accepted', 0, ?, ?, ?, NULL), "
          "(?, ?, 'ReceivingSession', 'RecordReceivingEntry', 2, NULL, ?, ?, "
          "'Pending', 1, ?, ?, NULL, 'POC_NETWORK_AMBIGUOUS')",
          [
            startOperation,
            sessionId,
            payloadOne,
            hashOne,
            '2026-07-29T09:00:00.000Z',
            '2026-07-29T09:02:00.000Z',
            '2026-07-29T09:02:00.000Z',
            entryOperation,
            sessionId,
            payloadTwo,
            hashTwo,
            '2026-07-29T09:01:00.000Z',
            '2026-07-29T09:03:00.000Z',
          ],
        );
        sqlite.execute(
          "INSERT INTO local_sync_state "
          "(key, event_cursor, last_successful_sync_at_utc, updated_at_utc) "
          "VALUES ('mobile', '41', '2026-07-29T09:04:00.000Z', "
          "'2026-07-29T09:04:00.000Z')",
        );
        sqlite.execute('PRAGMA user_version = 1');
      } finally {
        sqlite.close();
      }

      var database = LocalDatabaseOpeners.openFileForTest(file);
      final operations = await database
          .customSelect(
            'SELECT operation_id, local_sequence, payload_json, payload_hash, '
            'status FROM local_outbox_operations ORDER BY local_sequence',
          )
          .get();
      final entry = await database
          .customSelect(
            'SELECT id, operation_id, local_sequence, raw_weight_kg, '
            'processed_weight_kg FROM local_receiving_entries',
          )
          .getSingle();
      final cursor = await database
          .customSelect(
            "SELECT event_cursor FROM local_sync_state WHERE key = 'mobile'",
          )
          .getSingle();
      final version = await database
          .customSelect('PRAGMA user_version')
          .getSingle();
      final tables = await database
          .customSelect("SELECT name FROM sqlite_master WHERE type = 'table'")
          .get();

      expect(version.read<int>('user_version'), 3);
      expect(operations.map((row) => row.read<String>('operation_id')), [
        startOperation,
        entryOperation,
      ]);
      expect(operations.map((row) => row.read<int>('local_sequence')), [1, 2]);
      expect(operations.map((row) => row.read<String>('payload_json')), [
        payloadOne,
        payloadTwo,
      ]);
      expect(operations.map((row) => row.read<String>('payload_hash')), [
        hashOne,
        hashTwo,
      ]);
      expect(operations.map((row) => row.read<String>('status')), [
        'Accepted',
        'Pending',
      ]);
      expect(entry.read<String>('id'), entryId);
      expect(entry.read<String>('operation_id'), entryOperation);
      expect(entry.read<int>('local_sequence'), 2);
      expect(entry.read<String>('raw_weight_kg'), '00050.2370');
      expect(entry.read<String>('processed_weight_kg'), '50.230000');
      expect(cursor.read<String>('event_cursor'), '41');
      expect(
        tables.map((row) => row.read<String>('name')),
        containsAll([
          'poc_device_profiles',
          'receiving_session_cloud_states',
          'mobile_sync_event_inbox',
          'remote_receiving_session_projections',
          'poc_control_commands',
        ]),
      );

      await database.close();
      database = LocalDatabaseOpeners.openFileForTest(file);
      expect(
        (await database.customSelect('PRAGMA user_version').getSingle())
            .read<int>('user_version'),
        3,
      );
      expect(
        await database
            .customSelect(
              'SELECT COUNT(*) AS count FROM local_receiving_entries',
            )
            .map((row) => row.read<int>('count'))
            .getSingle(),
        1,
      );
      await database.close();
    } finally {
      if (directory.existsSync()) {
        await directory.delete(recursive: true);
      }
    }
  });

  test('schema 2 POC rows migrate to schema 3 with exact context', () async {
    final directory = await Directory.systemTemp.createTemp(
      'traderpro-poc-v2-migration-',
    );
    final file = File('${directory.path}${Platform.pathSeparator}local.sqlite');
    const sourceKey =
        'aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa';
    const workspaceId = '019fad0f-2d6a-7000-8000-00000000b001';
    const deviceId = '019fad0f-2d6a-7000-8000-00000000b002';
    const sessionId = '019fad0f-2d6a-7000-8000-00000000b003';
    const commandId = '019fad0f-2d6a-7000-8000-00000000b004';
    const eventId = '019fad0f-2d6a-7000-8000-00000000b005';
    const payload = '{ "exact" : "schema-two-payload" }';

    try {
      final sqlite = sqlite3.open(file.path);
      try {
        _createSchemaOne(sqlite);
        _createSchemaTwoPoc(sqlite);
        sqlite.execute(
          'INSERT INTO local_receiving_sessions '
          '(id, cloud_id, temporary_reference, local_status, cloud_status, '
          'local_version, cloud_version, next_local_sequence, '
          'active_entry_count, processed_total_weight_kg, '
          'created_at_device_utc, updated_at_device_utc, '
          'last_cloud_sync_at_utc) VALUES (?, NULL, ?, ?, NULL, 1, NULL, 1, '
          '0, ?, ?, ?, NULL)',
          [
            sessionId,
            'TMP-RCV-SCHEMA2-019FAD0F2D6A7000800000000000B003',
            'Open',
            '0.000000',
            '2026-07-30T08:00:00.000Z',
            '2026-07-30T08:00:00.000Z',
          ],
        );
        sqlite.execute(
          'INSERT INTO poc_device_profiles '
          '(profile_key, backend_base_url, workspace_id, device_id, '
          'display_role, display_label, automatic_sync_paused, '
          'created_at_utc, updated_at_utc) VALUES '
          "('active', 'http://192.168.1.50:5000', ?, ?, 'Operator', "
          "'Phone A', 1, ?, ?)",
          [
            workspaceId,
            deviceId,
            '2026-07-30T08:00:00.000Z',
            '2026-07-30T08:01:00.000Z',
          ],
        );
        sqlite.execute(
          'INSERT INTO poc_sync_sources '
          '(source_key, backend_base_url, workspace_id, event_cursor, '
          'last_successful_poll_at_utc, last_error_code, last_error_message, '
          'updated_at_utc) VALUES '
          "(?, 'http://192.168.1.50:5000', ?, 17, ?, NULL, NULL, ?)",
          [
            sourceKey,
            workspaceId,
            '2026-07-30T08:02:00.000Z',
            '2026-07-30T08:02:00.000Z',
          ],
        );
        sqlite.execute(
          'INSERT INTO receiving_session_cloud_states '
          '(local_session_id, cloud_session_id, cloud_reference, '
          'cloud_status, cloud_version, lease_id, lease_expires_at_utc, '
          'editor_device_id, last_successful_sync_at_utc, '
          'last_cloud_update_at_utc, last_error_code, last_error_message) '
          "VALUES (?, ?, 'RS-POC-SCHEMA2', 'ReceivingInProgress', 7, ?, ?, ?, "
          '?, ?, NULL, NULL)',
          [
            sessionId,
            sessionId,
            '019fad0f-2d6a-7000-8000-00000000b006',
            '2026-07-30T08:10:00.000Z',
            deviceId,
            '2026-07-30T08:03:00.000Z',
            '2026-07-30T08:03:00.000Z',
          ],
        );
        sqlite.execute(
          'INSERT INTO mobile_sync_event_inbox '
          '(source_key, event_sequence, event_id, event_type, event_version, '
          'aggregate_type, aggregate_id, aggregate_version, occurred_at_utc, '
          'correlation_id, payload_json, received_at_utc, applied_at_utc, '
          'apply_status) VALUES '
          "(?, 17, ?, 'FuturePocEvent', 1, 'ReceivingSessionPoc', ?, 7, ?, "
          "'schema-two-correlation', ?, ?, ?, 'SkippedUnknown')",
          [
            sourceKey,
            eventId,
            sessionId,
            '2026-07-30T08:02:00.000Z',
            payload,
            '2026-07-30T08:02:01.000Z',
            '2026-07-30T08:02:01.000Z',
          ],
        );
        sqlite.execute(
          'INSERT INTO remote_receiving_session_projections '
          '(source_key, session_id, cloud_reference, status, editor_device_id, '
          'lease_expires_at_utc, entry_count, processed_total_weight_kg, '
          'cloud_version, approved_by_device_id, finalization_id, '
          'last_cloud_update_at_utc) VALUES '
          "(?, ?, 'RS-POC-SCHEMA2', 'ReceivingInProgress', ?, ?, 1, "
          "'12.340000', 7, NULL, NULL, ?)",
          [
            sourceKey,
            sessionId,
            deviceId,
            '2026-07-30T08:10:00.000Z',
            '2026-07-30T08:03:00.000Z',
          ],
        );
        sqlite.execute(
          'INSERT INTO remote_receiving_entry_summaries '
          '(source_key, session_id, entry_id, event_sequence, local_sequence, '
          'product_reference, bag_type_reference, bag_count, raw_weight_kg, '
          'processed_weight_kg, display_weight_kg, decimal_places, '
          'processing_method, weight_source, captured_at_device_utc, '
          'accepted_at_server_utc) VALUES '
          "(?, ?, ?, 16, 2, 'PADDY-V2', 'JUTE-V2', 3, '0012.3400', "
          "'12.340000', '12.34', 2, 'Floor', 'ManualSpike', ?, ?)",
          [
            sourceKey,
            sessionId,
            '019fad0f-2d6a-7000-8000-00000000b007',
            '2026-07-30T08:01:00.000Z',
            '2026-07-30T08:01:01.000Z',
          ],
        );
        sqlite.execute(
          'INSERT INTO poc_control_commands '
          '(command_id, command_type, session_id, expected_cloud_version, '
          'lease_id, status, attempt_count, next_attempt_at_utc, '
          'last_attempt_at_utc, last_error_code, last_error_message, '
          'successful_response_json, created_at_utc, updated_at_utc) VALUES '
          "(?, 'Finalize', ?, 7, NULL, 'Completed', 2, NULL, ?, NULL, NULL, "
          "'{\"result\":{\"status\":\"Finalized\"}}', ?, ?)",
          [
            commandId,
            sessionId,
            '2026-07-30T08:04:00.000Z',
            '2026-07-30T08:03:30.000Z',
            '2026-07-30T08:04:00.000Z',
          ],
        );
        sqlite.execute('PRAGMA user_version = 2');
      } finally {
        sqlite.close();
      }

      var database = LocalDatabaseOpeners.openFileForTest(file);
      final version = await database
          .customSelect('PRAGMA user_version')
          .getSingle();
      final cloud = await database
          .customSelect(
            'SELECT * FROM receiving_session_cloud_states '
            'WHERE local_session_id = ?',
            variables: [Variable<String>(sessionId)],
          )
          .getSingle();
      final command = await database
          .customSelect(
            'SELECT * FROM poc_control_commands WHERE command_id = ?',
            variables: [Variable<String>(commandId)],
          )
          .getSingle();
      final event = await database
          .customSelect(
            'SELECT payload_json FROM mobile_sync_event_inbox '
            'WHERE event_id = ?',
            variables: [Variable<String>(eventId)],
          )
          .getSingle();
      final entry = await database
          .customSelect(
            'SELECT raw_weight_kg, processed_weight_kg '
            'FROM remote_receiving_entry_summaries',
          )
          .getSingle();

      expect(version.read<int>('user_version'), 3);
      expect(cloud.read<String>('source_key'), sourceKey);
      expect(cloud.read<String>('bound_device_id'), deviceId);
      expect(cloud.read<String>('cloud_reference'), 'RS-POC-SCHEMA2');
      expect(cloud.read<int>('cloud_version'), 7);
      expect(command.read<String>('source_key'), sourceKey);
      expect(command.read<String>('acting_device_id'), deviceId);
      expect(
        command.read<String>('successful_response_json'),
        contains('"Finalized"'),
      );
      expect(event.read<String>('payload_json'), payload);
      expect(entry.read<String>('raw_weight_kg'), '0012.3400');
      expect(entry.read<String>('processed_weight_kg'), '12.340000');

      await database.close();
      database = LocalDatabaseOpeners.openFileForTest(file);
      expect(
        (await database.customSelect('PRAGMA user_version').getSingle())
            .read<int>('user_version'),
        3,
      );
      expect(
        await database
            .customSelect('SELECT COUNT(*) AS count FROM poc_control_commands')
            .map((row) => row.read<int>('count'))
            .getSingle(),
        1,
      );
      await database.close();
    } finally {
      if (directory.existsSync()) {
        await directory.delete(recursive: true);
      }
    }
  });

  test(
    'schema 2 context backfill fails explicitly when it cannot bind',
    () async {
      final directory = await Directory.systemTemp.createTemp(
        'traderpro-poc-v2-unbound-',
      );
      final file = File(
        '${directory.path}${Platform.pathSeparator}local.sqlite',
      );
      const sessionId = '019fad0f-2d6a-7000-8000-00000000bc01';
      TraderProLocalDatabase? database;
      try {
        final sqlite = sqlite3.open(file.path);
        try {
          _createSchemaOne(sqlite);
          _createSchemaTwoPoc(sqlite);
          sqlite.execute(
            'INSERT INTO local_receiving_sessions '
            '(id, cloud_id, temporary_reference, local_status, cloud_status, '
            'local_version, cloud_version, next_local_sequence, '
            'active_entry_count, processed_total_weight_kg, '
            'created_at_device_utc, updated_at_device_utc, '
            'last_cloud_sync_at_utc) VALUES '
            "(?, NULL, 'TMP-UNBOUND', 'Open', NULL, 1, NULL, 1, 0, "
            "'0.000000', ?, ?, NULL)",
            [sessionId, '2026-07-30T08:00:00.000Z', '2026-07-30T08:00:00.000Z'],
          );
          sqlite.execute(
            'INSERT INTO receiving_session_cloud_states '
            '(local_session_id, cloud_session_id, cloud_reference, '
            'cloud_status, cloud_version, lease_id, lease_expires_at_utc, '
            'editor_device_id, last_successful_sync_at_utc, '
            'last_cloud_update_at_utc, last_error_code, last_error_message) '
            "VALUES (?, ?, 'RS-UNBOUND', 'ReceivingInProgress', 1, "
            "'019fad0f-2d6a-7000-8000-00000000bc02', ?, NULL, ?, ?, NULL, NULL)",
            [
              sessionId,
              sessionId,
              '2026-07-30T08:05:00.000Z',
              '2026-07-30T08:00:00.000Z',
              '2026-07-30T08:00:00.000Z',
            ],
          );
          sqlite.execute('PRAGMA user_version = 2');
        } finally {
          sqlite.close();
        }

        database = LocalDatabaseOpeners.openFileForTest(file);
        await expectLater(
          database.customSelect('SELECT 1').getSingle(),
          throwsA(
            predicate<Object>(
              (error) =>
                  error.toString().contains('POC_CONTEXT_BACKFILL_REQUIRED'),
            ),
          ),
        );
      } finally {
        await database?.close();
        if (directory.existsSync()) {
          await directory.delete(recursive: true);
        }
      }
    },
  );
}

void _createSchemaOne(Database database) {
  database.execute('PRAGMA foreign_keys = ON');
  database.execute('''
    CREATE TABLE local_receiving_sessions (
      id TEXT NOT NULL PRIMARY KEY,
      cloud_id TEXT NULL,
      temporary_reference TEXT NOT NULL UNIQUE,
      local_status TEXT NOT NULL CHECK (local_status IN ('Open', 'Closed')),
      cloud_status TEXT NULL,
      local_version INTEGER NOT NULL CHECK (local_version >= 1),
      cloud_version INTEGER NULL,
      next_local_sequence INTEGER NOT NULL CHECK (next_local_sequence >= 1),
      active_entry_count INTEGER NOT NULL CHECK (active_entry_count >= 0),
      processed_total_weight_kg TEXT NOT NULL,
      created_at_device_utc TEXT NOT NULL,
      updated_at_device_utc TEXT NOT NULL,
      last_cloud_sync_at_utc TEXT NULL
    )
  ''');
  database.execute('''
    CREATE TABLE local_receiving_entries (
      id TEXT NOT NULL PRIMARY KEY,
      receiving_session_id TEXT NOT NULL REFERENCES
        local_receiving_sessions (id) ON DELETE RESTRICT,
      operation_id TEXT NOT NULL UNIQUE,
      local_sequence INTEGER NOT NULL CHECK (local_sequence > 0),
      product_reference TEXT NOT NULL,
      bag_type_reference TEXT NOT NULL,
      bag_count INTEGER NOT NULL CHECK (bag_count > 0),
      raw_weight_kg TEXT NOT NULL,
      processed_weight_kg TEXT NOT NULL,
      display_weight_kg TEXT NOT NULL,
      decimal_places INTEGER NOT NULL CHECK (decimal_places IN (1, 2, 3)),
      processing_method TEXT NOT NULL CHECK
        (processing_method IN ('Standard', 'Floor', 'Ceiling')),
      weight_source TEXT NOT NULL CHECK
        (weight_source IN ('ManualSpike', 'TestScale')),
      entry_status TEXT NOT NULL CHECK
        (entry_status IN ('Active', 'Reversed')),
      reversal_of_entry_id TEXT NULL,
      captured_at_device_utc TEXT NOT NULL,
      created_at_device_utc TEXT NOT NULL,
      UNIQUE (receiving_session_id, local_sequence)
    )
  ''');
  database.execute('''
    CREATE TABLE local_outbox_operations (
      operation_id TEXT NOT NULL PRIMARY KEY,
      aggregate_id TEXT NOT NULL,
      aggregate_type TEXT NOT NULL,
      operation_type TEXT NOT NULL,
      local_sequence INTEGER NOT NULL CHECK (local_sequence > 0),
      expected_cloud_version INTEGER NULL,
      payload_json TEXT NOT NULL,
      payload_hash TEXT NOT NULL,
      status TEXT NOT NULL CHECK
        (status IN ('Pending', 'Sending', 'Accepted', 'NeedsAttention',
          'Rejected', 'Superseded')),
      attempt_count INTEGER NOT NULL CHECK (attempt_count >= 0),
      created_at_device_utc TEXT NOT NULL,
      last_attempt_at_utc TEXT NULL,
      accepted_at_utc TEXT NULL,
      last_error_code TEXT NULL,
      UNIQUE (aggregate_id, local_sequence)
    )
  ''');
  database.execute('''
    CREATE TABLE local_sync_state (
      key TEXT NOT NULL PRIMARY KEY,
      event_cursor TEXT NULL,
      last_successful_sync_at_utc TEXT NULL,
      updated_at_utc TEXT NOT NULL
    )
  ''');
  database.execute(
    'CREATE INDEX idx_local_receiving_entries_session '
    'ON local_receiving_entries (receiving_session_id, local_sequence)',
  );
  database.execute(
    'CREATE INDEX idx_local_outbox_operations_pending '
    'ON local_outbox_operations (status, aggregate_id, local_sequence)',
  );
  database.execute('''
    CREATE TRIGGER local_outbox_identity_is_immutable
    BEFORE UPDATE OF operation_id, aggregate_id, aggregate_type,
      operation_type, local_sequence, expected_cloud_version, payload_json,
      payload_hash, created_at_device_utc
    ON local_outbox_operations
    BEGIN
      SELECT RAISE(ABORT, 'LOCAL_OUTBOX_IDENTITY_IMMUTABLE');
    END
  ''');
  database.execute('''
    CREATE TRIGGER local_queued_outbox_operations_cannot_be_deleted
    BEFORE DELETE ON local_outbox_operations
    WHEN OLD.status IN ('Pending', 'Sending', 'NeedsAttention')
    BEGIN
      SELECT RAISE(ABORT, 'LOCAL_QUEUED_OUTBOX_DELETE_FORBIDDEN');
    END
  ''');
  database.execute('''
    CREATE TRIGGER local_receiving_entries_cannot_be_deleted
    BEFORE DELETE ON local_receiving_entries
    BEGIN
      SELECT RAISE(ABORT, 'LOCAL_PHYSICAL_FACT_DELETE_FORBIDDEN');
    END
  ''');
  database.execute('''
    CREATE TRIGGER local_receiving_entry_fact_is_immutable
    BEFORE UPDATE ON local_receiving_entries
    BEGIN
      SELECT RAISE(ABORT, 'LOCAL_PHYSICAL_FACT_UPDATE_FORBIDDEN');
    END
  ''');
  database.execute('''
    CREATE TRIGGER local_receiving_sessions_cannot_be_deleted
    BEFORE DELETE ON local_receiving_sessions
    BEGIN
      SELECT RAISE(ABORT, 'LOCAL_RECEIVING_SESSION_DELETE_FORBIDDEN');
    END
  ''');
}

void _createSchemaTwoPoc(Database database) {
  database.execute('''
    CREATE TABLE poc_device_profiles (
      profile_key TEXT NOT NULL PRIMARY KEY,
      backend_base_url TEXT NOT NULL,
      workspace_id TEXT NOT NULL,
      device_id TEXT NOT NULL,
      display_role TEXT NOT NULL CHECK (display_role IN ('Operator', 'Owner')),
      display_label TEXT NULL,
      automatic_sync_paused INTEGER NOT NULL DEFAULT 0,
      created_at_utc TEXT NOT NULL,
      updated_at_utc TEXT NOT NULL
    )
  ''');
  database.execute('''
    CREATE TABLE poc_sync_sources (
      source_key TEXT NOT NULL PRIMARY KEY,
      backend_base_url TEXT NOT NULL,
      workspace_id TEXT NOT NULL,
      event_cursor INTEGER NOT NULL DEFAULT 0 CHECK (event_cursor >= 0),
      last_successful_poll_at_utc TEXT NULL,
      last_error_code TEXT NULL,
      last_error_message TEXT NULL,
      updated_at_utc TEXT NOT NULL,
      UNIQUE (backend_base_url, workspace_id)
    )
  ''');
  database.execute('''
    CREATE TABLE receiving_session_cloud_states (
      local_session_id TEXT NOT NULL PRIMARY KEY REFERENCES
        local_receiving_sessions (id) ON DELETE RESTRICT,
      cloud_session_id TEXT NOT NULL,
      cloud_reference TEXT NOT NULL,
      cloud_status TEXT NOT NULL,
      cloud_version INTEGER NOT NULL CHECK (cloud_version > 0),
      lease_id TEXT NULL,
      lease_expires_at_utc TEXT NULL,
      editor_device_id TEXT NULL,
      last_successful_sync_at_utc TEXT NOT NULL,
      last_cloud_update_at_utc TEXT NOT NULL,
      last_error_code TEXT NULL,
      last_error_message TEXT NULL
    )
  ''');
  database.execute('''
    CREATE TABLE mobile_sync_event_inbox (
      source_key TEXT NOT NULL REFERENCES poc_sync_sources (source_key)
        ON DELETE RESTRICT,
      event_sequence INTEGER NOT NULL CHECK (event_sequence > 0),
      event_id TEXT NOT NULL,
      event_type TEXT NOT NULL,
      event_version INTEGER NOT NULL CHECK (event_version > 0),
      aggregate_type TEXT NOT NULL,
      aggregate_id TEXT NOT NULL,
      aggregate_version INTEGER NOT NULL CHECK (aggregate_version > 0),
      occurred_at_utc TEXT NOT NULL,
      correlation_id TEXT NOT NULL,
      payload_json TEXT NOT NULL,
      received_at_utc TEXT NOT NULL,
      applied_at_utc TEXT NULL,
      apply_status TEXT NOT NULL CHECK (
        apply_status IN ('Pending', 'Applied', 'SkippedUnknown')
      ),
      PRIMARY KEY (source_key, event_sequence),
      UNIQUE (source_key, event_id)
    )
  ''');
  database.execute('''
    CREATE TABLE remote_receiving_session_projections (
      source_key TEXT NOT NULL REFERENCES poc_sync_sources (source_key)
        ON DELETE RESTRICT,
      session_id TEXT NOT NULL,
      cloud_reference TEXT NOT NULL,
      status TEXT NOT NULL,
      editor_device_id TEXT NOT NULL,
      lease_expires_at_utc TEXT NULL,
      entry_count INTEGER NOT NULL CHECK (entry_count >= 0),
      processed_total_weight_kg TEXT NOT NULL,
      cloud_version INTEGER NOT NULL CHECK (cloud_version > 0),
      approved_by_device_id TEXT NULL,
      finalization_id TEXT NULL,
      last_cloud_update_at_utc TEXT NOT NULL,
      PRIMARY KEY (source_key, session_id)
    )
  ''');
  database.execute('''
    CREATE TABLE remote_receiving_entry_summaries (
      source_key TEXT NOT NULL,
      session_id TEXT NOT NULL,
      entry_id TEXT NOT NULL,
      event_sequence INTEGER NULL,
      local_sequence INTEGER NOT NULL CHECK (local_sequence > 0),
      product_reference TEXT NOT NULL,
      bag_type_reference TEXT NOT NULL,
      bag_count INTEGER NOT NULL CHECK (bag_count > 0),
      raw_weight_kg TEXT NULL,
      processed_weight_kg TEXT NOT NULL,
      display_weight_kg TEXT NULL,
      decimal_places INTEGER NULL,
      processing_method TEXT NULL,
      weight_source TEXT NULL,
      captured_at_device_utc TEXT NULL,
      accepted_at_server_utc TEXT NULL,
      PRIMARY KEY (source_key, session_id, entry_id),
      UNIQUE (source_key, session_id, local_sequence),
      FOREIGN KEY (source_key, session_id) REFERENCES
        remote_receiving_session_projections (source_key, session_id)
        ON DELETE CASCADE
    )
  ''');
  database.execute('''
    CREATE TABLE poc_control_commands (
      command_id TEXT NOT NULL PRIMARY KEY,
      command_type TEXT NOT NULL CHECK (
        command_type IN ('Heartbeat', 'Approve', 'Finalize')
      ),
      session_id TEXT NOT NULL,
      expected_cloud_version INTEGER NULL,
      lease_id TEXT NULL,
      status TEXT NOT NULL CHECK (
        status IN ('Pending', 'Sending', 'Completed', 'NeedsAttention')
      ),
      attempt_count INTEGER NOT NULL CHECK (attempt_count >= 0),
      next_attempt_at_utc TEXT NULL,
      last_attempt_at_utc TEXT NULL,
      last_error_code TEXT NULL,
      last_error_message TEXT NULL,
      successful_response_json TEXT NULL,
      created_at_utc TEXT NOT NULL,
      updated_at_utc TEXT NOT NULL
    )
  ''');
  database.execute('''
    CREATE TRIGGER poc_control_command_identity_is_immutable
    BEFORE UPDATE OF command_id, command_type, session_id,
      expected_cloud_version, lease_id, created_at_utc
    ON poc_control_commands
    BEGIN
      SELECT RAISE(ABORT, 'POC_CONTROL_COMMAND_IDENTITY_IMMUTABLE');
    END
  ''');
  database.execute('''
    CREATE TRIGGER poc_control_commands_cannot_be_deleted
    BEFORE DELETE ON poc_control_commands
    BEGIN
      SELECT RAISE(ABORT, 'POC_CONTROL_COMMAND_DELETE_FORBIDDEN');
    END
  ''');
}
