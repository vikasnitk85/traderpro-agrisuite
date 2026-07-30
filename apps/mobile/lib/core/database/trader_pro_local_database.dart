import 'package:drift/drift.dart';

part 'trader_pro_local_database.g.dart';

@DataClassName('LocalReceivingSessionRow')
class LocalReceivingSessions extends Table {
  @override
  String get tableName => 'local_receiving_sessions';

  TextColumn get id => text()();

  TextColumn get cloudId => text().nullable()();

  TextColumn get temporaryReference => text().unique()();

  TextColumn get localStatus => text().customConstraint(
    "NOT NULL CHECK (local_status IN ('Open', 'Closed'))",
  )();

  TextColumn get cloudStatus => text().nullable()();

  IntColumn get localVersion =>
      integer().customConstraint('NOT NULL CHECK (local_version >= 1)')();

  IntColumn get cloudVersion => integer().nullable()();

  IntColumn get nextLocalSequence =>
      integer().customConstraint('NOT NULL CHECK (next_local_sequence >= 1)')();

  IntColumn get activeEntryCount =>
      integer().customConstraint('NOT NULL CHECK (active_entry_count >= 0)')();

  TextColumn get processedTotalWeightKg => text()();

  TextColumn get createdAtDeviceUtc => text()();

  TextColumn get updatedAtDeviceUtc => text()();

  TextColumn get lastCloudSyncAtUtc => text().nullable()();

  @override
  Set<Column<Object>> get primaryKey => {id};
}

@DataClassName('LocalReceivingEntryRow')
class LocalReceivingEntries extends Table {
  @override
  String get tableName => 'local_receiving_entries';

  TextColumn get id => text()();

  TextColumn get receivingSessionId => text().references(
    LocalReceivingSessions,
    #id,
    onDelete: KeyAction.restrict,
  )();

  TextColumn get operationId => text().unique()();

  IntColumn get localSequence =>
      integer().customConstraint('NOT NULL CHECK (local_sequence > 0)')();

  TextColumn get productReference => text()();

  TextColumn get bagTypeReference => text()();

  IntColumn get bagCount =>
      integer().customConstraint('NOT NULL CHECK (bag_count > 0)')();

  TextColumn get rawWeightKg => text()();

  TextColumn get processedWeightKg => text()();

  TextColumn get displayWeightKg => text()();

  IntColumn get decimalPlaces => integer().customConstraint(
    'NOT NULL CHECK (decimal_places IN (1, 2, 3))',
  )();

  TextColumn get processingMethod => text().customConstraint(
    "NOT NULL CHECK (processing_method IN ('Standard', 'Floor', 'Ceiling'))",
  )();

  TextColumn get weightSource => text().customConstraint(
    "NOT NULL CHECK (weight_source IN ('ManualSpike', 'TestScale'))",
  )();

  TextColumn get entryStatus => text().customConstraint(
    "NOT NULL CHECK (entry_status IN ('Active', 'Reversed'))",
  )();

  TextColumn get reversalOfEntryId => text().nullable()();

  TextColumn get capturedAtDeviceUtc => text()();

  TextColumn get createdAtDeviceUtc => text()();

  @override
  Set<Column<Object>> get primaryKey => {id};

  @override
  List<Set<Column<Object>>> get uniqueKeys => [
    {receivingSessionId, localSequence},
  ];
}

@DataClassName('LocalOutboxOperationRow')
class LocalOutboxOperations extends Table {
  @override
  String get tableName => 'local_outbox_operations';

  TextColumn get operationId => text()();

  TextColumn get aggregateId => text()();

  TextColumn get aggregateType => text()();

  TextColumn get operationType => text()();

  IntColumn get localSequence =>
      integer().customConstraint('NOT NULL CHECK (local_sequence > 0)')();

  IntColumn get expectedCloudVersion => integer().nullable()();

  TextColumn get payloadJson => text()();

  TextColumn get payloadHash => text()();

  TextColumn get status => text().customConstraint(
    "NOT NULL CHECK (status IN ('Pending', 'Sending', 'Accepted', "
    "'NeedsAttention', 'Rejected', 'Superseded'))",
  )();

  IntColumn get attemptCount =>
      integer().customConstraint('NOT NULL CHECK (attempt_count >= 0)')();

  TextColumn get createdAtDeviceUtc => text()();

  TextColumn get lastAttemptAtUtc => text().nullable()();

  TextColumn get acceptedAtUtc => text().nullable()();

  TextColumn get lastErrorCode => text().nullable()();

  @override
  Set<Column<Object>> get primaryKey => {operationId};

  @override
  List<Set<Column<Object>>> get uniqueKeys => [
    {aggregateId, localSequence},
  ];
}

@DataClassName('LocalSyncStateRow')
class LocalSyncStates extends Table {
  @override
  String get tableName => 'local_sync_state';

  TextColumn get key => text()();

  TextColumn get eventCursor => text().nullable()();

  TextColumn get lastSuccessfulSyncAtUtc => text().nullable()();

  TextColumn get updatedAtUtc => text()();

  @override
  Set<Column<Object>> get primaryKey => {key};
}

@DataClassName('PocDeviceProfileRow')
class PocDeviceProfiles extends Table {
  @override
  String get tableName => 'poc_device_profiles';

  TextColumn get profileKey => text()();

  TextColumn get backendBaseUrl => text()();

  TextColumn get workspaceId => text()();

  TextColumn get deviceId => text()();

  TextColumn get displayRole => text().customConstraint(
    "NOT NULL CHECK (display_role IN ('Operator', 'Owner'))",
  )();

  TextColumn get displayLabel => text().nullable()();

  BoolColumn get automaticSyncPaused =>
      boolean().withDefault(const Constant(false))();

  TextColumn get createdAtUtc => text()();

  TextColumn get updatedAtUtc => text()();

  @override
  Set<Column<Object>> get primaryKey => {profileKey};
}

@DataClassName('ReceivingSessionCloudStateRow')
class ReceivingSessionCloudStates extends Table {
  @override
  String get tableName => 'receiving_session_cloud_states';

  TextColumn get sourceKey => text().references(
    PocSyncSources,
    #sourceKey,
    onDelete: KeyAction.restrict,
  )();

  TextColumn get boundDeviceId => text()();

  TextColumn get localSessionId => text().references(
    LocalReceivingSessions,
    #id,
    onDelete: KeyAction.restrict,
  )();

  TextColumn get cloudSessionId => text()();

  TextColumn get cloudReference => text()();

  TextColumn get cloudStatus => text()();

  IntColumn get cloudVersion =>
      integer().customConstraint('NOT NULL CHECK (cloud_version > 0)')();

  TextColumn get leaseId => text().nullable()();

  TextColumn get leaseExpiresAtUtc => text().nullable()();

  TextColumn get editorDeviceId => text().nullable()();

  TextColumn get lastSuccessfulSyncAtUtc => text()();

  TextColumn get lastCloudUpdateAtUtc => text()();

  TextColumn get lastErrorCode => text().nullable()();

  TextColumn get lastErrorMessage => text().nullable()();

  @override
  Set<Column<Object>> get primaryKey => {localSessionId};

  @override
  List<String> get customConstraints => [
    'CHECK (cloud_session_id = local_session_id)',
    "CHECK ((cloud_status = 'ReceivingInProgress' AND lease_id IS NOT NULL "
        "AND lease_expires_at_utc IS NOT NULL) OR "
        "(cloud_status IN ('SubmittedForReview', 'Approved', 'Finalized') "
        'AND lease_id IS NULL AND lease_expires_at_utc IS NULL))',
  ];
}

@DataClassName('PocSyncSourceRow')
class PocSyncSources extends Table {
  @override
  String get tableName => 'poc_sync_sources';

  TextColumn get sourceKey => text()();

  TextColumn get backendBaseUrl => text()();

  TextColumn get workspaceId => text()();

  IntColumn get eventCursor => integer().customConstraint(
    'NOT NULL DEFAULT 0 CHECK (event_cursor >= 0)',
  )();

  TextColumn get lastSuccessfulPollAtUtc => text().nullable()();

  TextColumn get lastErrorCode => text().nullable()();

  TextColumn get lastErrorMessage => text().nullable()();

  TextColumn get updatedAtUtc => text()();

  @override
  Set<Column<Object>> get primaryKey => {sourceKey};

  @override
  List<Set<Column<Object>>> get uniqueKeys => [
    {backendBaseUrl, workspaceId},
  ];
}

@DataClassName('MobileSyncEventInboxRow')
class MobileSyncEventInbox extends Table {
  @override
  String get tableName => 'mobile_sync_event_inbox';

  TextColumn get sourceKey => text().references(
    PocSyncSources,
    #sourceKey,
    onDelete: KeyAction.restrict,
  )();

  IntColumn get eventSequence =>
      integer().customConstraint('NOT NULL CHECK (event_sequence > 0)')();

  TextColumn get eventId => text()();

  TextColumn get eventType => text()();

  IntColumn get eventVersion =>
      integer().customConstraint('NOT NULL CHECK (event_version > 0)')();

  TextColumn get aggregateType => text()();

  TextColumn get aggregateId => text()();

  IntColumn get aggregateVersion =>
      integer().customConstraint('NOT NULL CHECK (aggregate_version > 0)')();

  TextColumn get occurredAtUtc => text()();

  TextColumn get correlationId => text()();

  TextColumn get payloadJson => text()();

  TextColumn get receivedAtUtc => text()();

  TextColumn get appliedAtUtc => text().nullable()();

  TextColumn get applyStatus => text().customConstraint(
    "NOT NULL CHECK (apply_status IN ('Pending', 'Applied', "
    "'SkippedUnknown'))",
  )();

  @override
  Set<Column<Object>> get primaryKey => {sourceKey, eventSequence};

  @override
  List<Set<Column<Object>>> get uniqueKeys => [
    {sourceKey, eventId},
  ];
}

@DataClassName('RemoteReceivingSessionProjectionRow')
class RemoteReceivingSessionProjections extends Table {
  @override
  String get tableName => 'remote_receiving_session_projections';

  TextColumn get sourceKey => text().references(
    PocSyncSources,
    #sourceKey,
    onDelete: KeyAction.restrict,
  )();

  TextColumn get sessionId => text()();

  TextColumn get cloudReference => text()();

  TextColumn get status => text()();

  TextColumn get editorDeviceId => text()();

  TextColumn get leaseExpiresAtUtc => text().nullable()();

  IntColumn get entryCount =>
      integer().customConstraint('NOT NULL CHECK (entry_count >= 0)')();

  TextColumn get processedTotalWeightKg => text()();

  IntColumn get cloudVersion =>
      integer().customConstraint('NOT NULL CHECK (cloud_version > 0)')();

  TextColumn get approvedByDeviceId => text().nullable()();

  TextColumn get finalizationId => text().nullable()();

  TextColumn get lastCloudUpdateAtUtc => text()();

  @override
  Set<Column<Object>> get primaryKey => {sourceKey, sessionId};
}

@DataClassName('RemoteReceivingEntrySummaryRow')
class RemoteReceivingEntrySummaries extends Table {
  @override
  String get tableName => 'remote_receiving_entry_summaries';

  TextColumn get sourceKey => text()();

  TextColumn get sessionId => text()();

  TextColumn get entryId => text()();

  IntColumn get eventSequence => integer().nullable()();

  IntColumn get localSequence =>
      integer().customConstraint('NOT NULL CHECK (local_sequence > 0)')();

  TextColumn get productReference => text()();

  TextColumn get bagTypeReference => text()();

  IntColumn get bagCount =>
      integer().customConstraint('NOT NULL CHECK (bag_count > 0)')();

  TextColumn get rawWeightKg => text().nullable()();

  TextColumn get processedWeightKg => text()();

  TextColumn get displayWeightKg => text().nullable()();

  IntColumn get decimalPlaces => integer().nullable()();

  TextColumn get processingMethod => text().nullable()();

  TextColumn get weightSource => text().nullable()();

  TextColumn get capturedAtDeviceUtc => text().nullable()();

  TextColumn get acceptedAtServerUtc => text().nullable()();

  @override
  Set<Column<Object>> get primaryKey => {sourceKey, sessionId, entryId};

  @override
  List<Set<Column<Object>>> get uniqueKeys => [
    {sourceKey, sessionId, localSequence},
  ];

  @override
  List<String> get customConstraints => [
    'FOREIGN KEY (source_key, session_id) REFERENCES '
        'remote_receiving_session_projections (source_key, session_id) '
        'ON DELETE CASCADE',
  ];
}

@DataClassName('PocControlCommandRow')
class PocControlCommands extends Table {
  @override
  String get tableName => 'poc_control_commands';

  TextColumn get commandId => text()();

  TextColumn get sourceKey => text().references(
    PocSyncSources,
    #sourceKey,
    onDelete: KeyAction.restrict,
  )();

  TextColumn get actingDeviceId => text()();

  TextColumn get commandType => text().customConstraint(
    "NOT NULL CHECK (command_type IN ('Heartbeat', 'Approve', 'Finalize'))",
  )();

  TextColumn get sessionId => text()();

  IntColumn get expectedCloudVersion => integer().nullable()();

  TextColumn get leaseId => text().nullable()();

  TextColumn get status => text().customConstraint(
    "NOT NULL CHECK (status IN ('Pending', 'Sending', 'Completed', "
    "'NeedsAttention'))",
  )();

  IntColumn get attemptCount =>
      integer().customConstraint('NOT NULL CHECK (attempt_count >= 0)')();

  TextColumn get nextAttemptAtUtc => text().nullable()();

  TextColumn get lastAttemptAtUtc => text().nullable()();

  TextColumn get lastErrorCode => text().nullable()();

  TextColumn get lastErrorMessage => text().nullable()();

  TextColumn get successfulResponseJson => text().nullable()();

  TextColumn get createdAtUtc => text()();

  TextColumn get updatedAtUtc => text()();

  @override
  Set<Column<Object>> get primaryKey => {commandId};

  @override
  List<String> get customConstraints => [
    "CHECK ((command_type = 'Heartbeat' AND lease_id IS NOT NULL "
        'AND expected_cloud_version IS NULL) OR '
        "(command_type IN ('Approve', 'Finalize') AND lease_id IS NULL "
        'AND expected_cloud_version > 0))',
  ];
}

@DriftDatabase(
  tables: [
    LocalReceivingSessions,
    LocalReceivingEntries,
    LocalOutboxOperations,
    LocalSyncStates,
    PocDeviceProfiles,
    ReceivingSessionCloudStates,
    PocSyncSources,
    MobileSyncEventInbox,
    RemoteReceivingSessionProjections,
    RemoteReceivingEntrySummaries,
    PocControlCommands,
  ],
)
class TraderProLocalDatabase extends _$TraderProLocalDatabase {
  TraderProLocalDatabase(super.executor);

  @override
  int get schemaVersion => 3;

  @override
  MigrationStrategy get migration => MigrationStrategy(
    onCreate: (migrator) async {
      await migrator.createAll();
      await _createSchemaVersionOneObjects();
      await _createSchemaVersionTwoObjects();
      await _createSchemaVersionThreeObjects();
    },
    onUpgrade: (migrator, from, to) async {
      if (from == 1 && to == 3) {
        await _upgradeFromOneToThree(migrator);
        return;
      }
      if (from == 2 && to == 3) {
        await _upgradeFromTwoToThree();
        return;
      }
      throw StateError(
        'No local database migration is registered from schema $from to $to. '
        'Upgrades must use explicit, non-destructive migration steps.',
      );
    },
    beforeOpen: (details) async {
      await customStatement('PRAGMA foreign_keys = ON');
    },
  );

  Future<void> _createSchemaVersionOneObjects() async {
    await customStatement(
      'CREATE INDEX idx_local_receiving_entries_session '
      'ON local_receiving_entries (receiving_session_id, local_sequence)',
    );
    await customStatement(
      'CREATE INDEX idx_local_outbox_operations_pending '
      'ON local_outbox_operations (status, aggregate_id, local_sequence)',
    );
    await customStatement('''
      CREATE TRIGGER local_outbox_identity_is_immutable
      BEFORE UPDATE OF
        operation_id,
        aggregate_id,
        aggregate_type,
        operation_type,
        local_sequence,
        expected_cloud_version,
        payload_json,
        payload_hash,
        created_at_device_utc
      ON local_outbox_operations
      BEGIN
        SELECT RAISE(ABORT, 'LOCAL_OUTBOX_IDENTITY_IMMUTABLE');
      END
    ''');
    await customStatement('''
      CREATE TRIGGER local_queued_outbox_operations_cannot_be_deleted
      BEFORE DELETE ON local_outbox_operations
      WHEN OLD.status IN ('Pending', 'Sending', 'NeedsAttention')
      BEGIN
        SELECT RAISE(ABORT, 'LOCAL_QUEUED_OUTBOX_DELETE_FORBIDDEN');
      END
    ''');
    await customStatement('''
      CREATE TRIGGER local_receiving_entries_cannot_be_deleted
      BEFORE DELETE ON local_receiving_entries
      BEGIN
        SELECT RAISE(ABORT, 'LOCAL_PHYSICAL_FACT_DELETE_FORBIDDEN');
      END
    ''');
    await customStatement('''
      CREATE TRIGGER local_receiving_entry_fact_is_immutable
      BEFORE UPDATE ON local_receiving_entries
      BEGIN
        SELECT RAISE(ABORT, 'LOCAL_PHYSICAL_FACT_UPDATE_FORBIDDEN');
      END
    ''');
    await customStatement('''
      CREATE TRIGGER local_receiving_sessions_cannot_be_deleted
      BEFORE DELETE ON local_receiving_sessions
      BEGIN
        SELECT RAISE(ABORT, 'LOCAL_RECEIVING_SESSION_DELETE_FORBIDDEN');
      END
    ''');
  }

  Future<void> _upgradeFromOneToThree(Migrator migrator) async {
    await migrator.createTable(pocDeviceProfiles);
    await migrator.createTable(receivingSessionCloudStates);
    await migrator.createTable(pocSyncSources);
    await migrator.createTable(mobileSyncEventInbox);
    await migrator.createTable(remoteReceivingSessionProjections);
    await migrator.createTable(remoteReceivingEntrySummaries);
    await migrator.createTable(pocControlCommands);
    await _createSchemaVersionTwoObjects();
    await _createSchemaVersionThreeObjects();
  }

  Future<void> _upgradeFromTwoToThree() async {
    final cloudCount = await _rowCount('receiving_session_cloud_states');
    final controlCount = await _rowCount('poc_control_commands');
    String? sourceKey;
    String? deviceId;
    if (cloudCount != 0 || controlCount != 0) {
      final profile = await customSelect(
        "SELECT backend_base_url, workspace_id, device_id "
        "FROM poc_device_profiles WHERE profile_key = 'active'",
      ).getSingleOrNull();
      if (profile == null) {
        throw StateError(
          'POC_CONTEXT_BACKFILL_REQUIRED: schema-2 cloud/control rows exist '
          'without one active device profile.',
        );
      }
      final source = await customSelect(
        'SELECT source_key FROM poc_sync_sources '
        'WHERE backend_base_url = ? AND workspace_id = ?',
        variables: [
          Variable<String>(profile.read<String>('backend_base_url')),
          Variable<String>(profile.read<String>('workspace_id')),
        ],
      ).getSingleOrNull();
      if (source == null) {
        throw StateError(
          'POC_CONTEXT_BACKFILL_REQUIRED: the active schema-2 profile has no '
          'matching persisted sync source.',
        );
      }
      sourceKey = source.read<String>('source_key');
      deviceId = profile.read<String>('device_id');
    }

    await customStatement(
      'DROP TRIGGER IF EXISTS poc_control_command_identity_is_immutable',
    );
    await customStatement(
      'DROP TRIGGER IF EXISTS poc_control_commands_cannot_be_deleted',
    );
    await customStatement(
      'DROP INDEX IF EXISTS idx_poc_control_commands_pending',
    );
    await customStatement('''
      CREATE TABLE receiving_session_cloud_states_v3 (
        source_key TEXT NOT NULL REFERENCES poc_sync_sources (source_key)
          ON DELETE RESTRICT,
        bound_device_id TEXT NOT NULL,
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
        last_error_message TEXT NULL,
        CHECK (cloud_session_id = local_session_id),
        CHECK (
          (cloud_status = 'ReceivingInProgress'
            AND lease_id IS NOT NULL
            AND lease_expires_at_utc IS NOT NULL)
          OR
          (cloud_status IN ('SubmittedForReview', 'Approved', 'Finalized')
            AND lease_id IS NULL
            AND lease_expires_at_utc IS NULL)
        )
      )
    ''');
    if (cloudCount != 0) {
      await customStatement(
        'INSERT INTO receiving_session_cloud_states_v3 '
        '(source_key, bound_device_id, local_session_id, cloud_session_id, '
        'cloud_reference, cloud_status, cloud_version, lease_id, '
        'lease_expires_at_utc, editor_device_id, '
        'last_successful_sync_at_utc, last_cloud_update_at_utc, '
        'last_error_code, last_error_message) '
        'SELECT ?, ?, local_session_id, cloud_session_id, cloud_reference, '
        'cloud_status, cloud_version, lease_id, lease_expires_at_utc, '
        'editor_device_id, last_successful_sync_at_utc, '
        'last_cloud_update_at_utc, last_error_code, last_error_message '
        'FROM receiving_session_cloud_states',
        [sourceKey, deviceId],
      );
    }
    await customStatement('DROP TABLE receiving_session_cloud_states');
    await customStatement(
      'ALTER TABLE receiving_session_cloud_states_v3 '
      'RENAME TO receiving_session_cloud_states',
    );

    await customStatement('''
      CREATE TABLE poc_control_commands_v3 (
        command_id TEXT NOT NULL PRIMARY KEY,
        source_key TEXT NOT NULL REFERENCES poc_sync_sources (source_key)
          ON DELETE RESTRICT,
        acting_device_id TEXT NOT NULL,
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
        updated_at_utc TEXT NOT NULL,
        CHECK (
          (command_type = 'Heartbeat'
            AND lease_id IS NOT NULL
            AND expected_cloud_version IS NULL)
          OR
          (command_type IN ('Approve', 'Finalize')
            AND lease_id IS NULL
            AND expected_cloud_version > 0)
        )
      )
    ''');
    if (controlCount != 0) {
      await customStatement(
        'INSERT INTO poc_control_commands_v3 '
        '(command_id, source_key, acting_device_id, command_type, session_id, '
        'expected_cloud_version, lease_id, status, attempt_count, '
        'next_attempt_at_utc, last_attempt_at_utc, last_error_code, '
        'last_error_message, successful_response_json, created_at_utc, '
        'updated_at_utc) '
        'SELECT command_id, ?, ?, command_type, session_id, '
        'expected_cloud_version, lease_id, status, attempt_count, '
        'next_attempt_at_utc, last_attempt_at_utc, last_error_code, '
        'last_error_message, successful_response_json, created_at_utc, '
        'updated_at_utc FROM poc_control_commands',
        [sourceKey, deviceId],
      );
    }
    await customStatement('DROP TABLE poc_control_commands');
    await customStatement(
      'ALTER TABLE poc_control_commands_v3 RENAME TO poc_control_commands',
    );
    await _createSchemaVersionTwoObjects();
    await _createSchemaVersionThreeObjects();
  }

  Future<int> _rowCount(String tableName) async {
    final row = await customSelect(
      'SELECT COUNT(*) AS row_count FROM $tableName',
    ).getSingle();
    return row.read<int>('row_count');
  }

  Future<void> _createSchemaVersionTwoObjects() async {
    await customStatement(
      'CREATE INDEX IF NOT EXISTS idx_mobile_sync_event_inbox_aggregate '
      'ON mobile_sync_event_inbox '
      '(source_key, aggregate_id, event_sequence)',
    );
    await customStatement(
      'CREATE INDEX IF NOT EXISTS idx_remote_receiving_sessions_status '
      'ON remote_receiving_session_projections '
      '(source_key, status, last_cloud_update_at_utc)',
    );
    await customStatement(
      'CREATE INDEX IF NOT EXISTS idx_remote_receiving_entries_recent '
      'ON remote_receiving_entry_summaries '
      '(source_key, session_id, local_sequence DESC)',
    );
    await customStatement(
      'CREATE INDEX IF NOT EXISTS idx_poc_control_commands_pending '
      'ON poc_control_commands (status, next_attempt_at_utc, created_at_utc)',
    );
    await customStatement('''
      CREATE TRIGGER IF NOT EXISTS mobile_sync_event_facts_are_immutable
      BEFORE UPDATE OF
        source_key,
        event_sequence,
        event_id,
        event_type,
        event_version,
        aggregate_type,
        aggregate_id,
        aggregate_version,
        occurred_at_utc,
        correlation_id,
        payload_json,
        received_at_utc
      ON mobile_sync_event_inbox
      BEGIN
        SELECT RAISE(ABORT, 'MOBILE_SYNC_EVENT_FACT_IMMUTABLE');
      END
    ''');
    await customStatement('''
      CREATE TRIGGER IF NOT EXISTS poc_control_command_identity_is_immutable
      BEFORE UPDATE OF
        command_id,
        source_key,
        acting_device_id,
        command_type,
        session_id,
        expected_cloud_version,
        lease_id,
        created_at_utc
      ON poc_control_commands
      BEGIN
        SELECT RAISE(ABORT, 'POC_CONTROL_COMMAND_IDENTITY_IMMUTABLE');
      END
    ''');
    await customStatement('''
      CREATE TRIGGER IF NOT EXISTS poc_control_commands_cannot_be_deleted
      BEFORE DELETE ON poc_control_commands
      BEGIN
        SELECT RAISE(ABORT, 'POC_CONTROL_COMMAND_DELETE_FORBIDDEN');
      END
    ''');
  }

  Future<void> _createSchemaVersionThreeObjects() async {
    await customStatement(
      'CREATE INDEX IF NOT EXISTS idx_receiving_cloud_state_context '
      'ON receiving_session_cloud_states '
      '(source_key, bound_device_id, cloud_status)',
    );
    await customStatement(
      'CREATE INDEX IF NOT EXISTS idx_poc_control_commands_context '
      'ON poc_control_commands '
      '(source_key, acting_device_id, status, created_at_utc)',
    );
    await customStatement(
      'CREATE UNIQUE INDEX IF NOT EXISTS idx_poc_control_one_sending '
      'ON poc_control_commands '
      '(source_key, acting_device_id, command_type, session_id) '
      "WHERE status IN ('Pending', 'Sending')",
    );
    await customStatement('''
      CREATE TRIGGER IF NOT EXISTS receiving_cloud_state_context_is_immutable
      BEFORE UPDATE OF
        source_key,
        bound_device_id,
        local_session_id,
        cloud_session_id
      ON receiving_session_cloud_states
      BEGIN
        SELECT RAISE(ABORT, 'RECEIVING_CLOUD_CONTEXT_IMMUTABLE');
      END
    ''');
    await customStatement('''
      CREATE TRIGGER IF NOT EXISTS receiving_cloud_version_cannot_regress
      BEFORE UPDATE OF cloud_version ON receiving_session_cloud_states
      WHEN NEW.cloud_version < OLD.cloud_version
      BEGIN
        SELECT RAISE(ABORT, 'RECEIVING_CLOUD_VERSION_REGRESSION');
      END
    ''');
  }
}
