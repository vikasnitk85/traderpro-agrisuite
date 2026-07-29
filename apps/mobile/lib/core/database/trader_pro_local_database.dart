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

@DriftDatabase(
  tables: [
    LocalReceivingSessions,
    LocalReceivingEntries,
    LocalOutboxOperations,
    LocalSyncStates,
  ],
)
class TraderProLocalDatabase extends _$TraderProLocalDatabase {
  TraderProLocalDatabase(super.executor);

  @override
  int get schemaVersion => 1;

  @override
  MigrationStrategy get migration => MigrationStrategy(
    onCreate: (migrator) async {
      await migrator.createAll();
      await _createSchemaVersionOneObjects();
    },
    onUpgrade: (migrator, from, to) async {
      throw StateError(
        'No local database migration is registered from schema $from to $to. '
        'Future upgrades must add explicit, non-destructive migration steps.',
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
}
