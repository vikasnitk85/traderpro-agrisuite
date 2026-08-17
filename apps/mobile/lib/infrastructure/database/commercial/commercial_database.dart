import 'package:drift/drift.dart';

import '../../../core/security/commercial_provisioning_marker.dart';
import '../../../core/security/commercial_storage_failure.dart';

part 'commercial_database.g.dart';

class CommercialStorageMetadata extends Table {
  IntColumn get singletonId => integer()
      .named('singleton_id')
      .check(const CustomExpression<bool>('singleton_id = 1'))();

  IntColumn get schemaContractVersion =>
      integer().named('schema_contract_version')();

  IntColumn get storageContractVersion =>
      integer().named('storage_contract_version')();

  TextColumn get installationId => text().named('installation_id')();

  TextColumn get databaseInstanceId => text().named('database_instance_id')();

  IntColumn get keyAliasVersion => integer().named('key_alias_version')();

  IntColumn get createdAtUtcMicros =>
      integer().named('created_at_utc_micros')();

  @override
  Set<Column<Object>> get primaryKey => <Column<Object>>{singletonId};
}

class CommercialIdentityBindings extends Table {
  @override
  String get tableName => 'commercial_identity_binding';

  IntColumn get singletonId => integer()
      .named('singleton_id')
      .check(const CustomExpression<bool>('singleton_id = 1'))();

  IntColumn get bindingContractVersion =>
      integer().named('binding_contract_version')();

  TextColumn get apiOrigin => text().named('api_origin')();

  TextColumn get workspaceId => text().named('workspace_id')();

  TextColumn get companyId => text().named('company_id')();

  TextColumn get defaultBranchId => text().named('default_branch_id')();

  TextColumn get userId => text().named('user_id')();

  TextColumn get deviceId => text().named('device_id')();

  IntColumn get firstBoundAtUtcMicros =>
      integer().named('first_bound_at_utc_micros')();

  @override
  Set<Column<Object>> get primaryKey => <Column<Object>>{singletonId};
}

class CommercialIdentitySnapshots extends Table {
  @override
  String get tableName => 'commercial_identity_snapshot';

  IntColumn get singletonId => integer()
      .named('singleton_id')
      .check(const CustomExpression<bool>('singleton_id = 1'))
      .references(CommercialIdentityBindings, #singletonId)();

  TextColumn get workspaceCode => text().named('workspace_code')();

  TextColumn get userDisplayName => text().named('user_display_name')();

  TextColumn get role => text().named('role')();

  TextColumn get deviceLabel => text().named('device_label')();

  IntColumn get lastConfirmedAtUtcMicros =>
      integer().named('last_confirmed_at_utc_micros')();

  @override
  Set<Column<Object>> get primaryKey => <Column<Object>>{singletonId};
}

@DriftDatabase(
  tables: [
    CommercialStorageMetadata,
    CommercialIdentityBindings,
    CommercialIdentitySnapshots,
  ],
)
final class CommercialDatabase extends _$CommercialDatabase {
  CommercialDatabase(super.executor);

  static const int foundationSchemaContractVersion = 1;
  static const int currentSchemaVersion = 2;
  static const int identityBindingContractVersion = 1;
  static const int storageContractVersion = 1;
  static const int keyAliasVersion = 1;

  @override
  int get schemaVersion => currentSchemaVersion;

  @override
  MigrationStrategy get migration => MigrationStrategy(
    onCreate: (migrator) async {
      await migrator.createAll();
      await _createFoundationMetadataTriggers();
      await _createIdentityBindingTriggers();
    },
    onUpgrade: (migrator, from, to) async {
      if (from != 1 || to != 2) {
        throw const CommercialStorageException(
          CommercialStorageFailureCode.databaseMigrationFailed,
          phase: 'schema-upgrade-not-authorized',
        );
      }
      await transaction(() async {
        await migrator.createTable(commercialIdentityBindings);
        await migrator.createTable(commercialIdentitySnapshots);
        await _createIdentityBindingTriggers();
      });
    },
    beforeOpen: (details) async {
      await _verifySchemaContract(verifyUserVersion: false);
    },
  );

  Future<void> _createFoundationMetadataTriggers() async {
    await customStatement('''
        CREATE TRIGGER commercial_storage_metadata_no_update
        BEFORE UPDATE ON commercial_storage_metadata
        BEGIN
          SELECT RAISE(ABORT, 'commercial storage metadata is immutable');
        END
      ''');
    await customStatement('''
        CREATE TRIGGER commercial_storage_metadata_no_delete
        BEFORE DELETE ON commercial_storage_metadata
        BEGIN
          SELECT RAISE(ABORT, 'commercial storage metadata is immutable');
        END
      ''');
  }

  Future<void> _createIdentityBindingTriggers() async {
    await customStatement('''
      CREATE TRIGGER commercial_identity_binding_no_update
      BEFORE UPDATE ON commercial_identity_binding
      BEGIN
        SELECT RAISE(ABORT, 'commercial identity binding is immutable');
      END
    ''');
    await customStatement('''
      CREATE TRIGGER commercial_identity_binding_no_delete
      BEFORE DELETE ON commercial_identity_binding
      BEGIN
        SELECT RAISE(ABORT, 'commercial identity binding is immutable');
      END
    ''');
  }

  Future<void> _verifySchemaContract({bool verifyUserVersion = true}) async {
    if (verifyUserVersion) {
      final version = await customSelect('PRAGMA user_version').getSingle();
      if (version.read<int>('user_version') != currentSchemaVersion) {
        throw const CommercialStorageException(
          CommercialStorageFailureCode.databaseConfigurationMismatch,
          phase: 'schema-version',
        );
      }
    }
    const requiredColumns = <String, Set<String>>{
      'commercial_storage_metadata': <String>{
        'singleton_id',
        'schema_contract_version',
        'storage_contract_version',
        'installation_id',
        'database_instance_id',
        'key_alias_version',
        'created_at_utc_micros',
      },
      'commercial_identity_binding': <String>{
        'singleton_id',
        'binding_contract_version',
        'api_origin',
        'workspace_id',
        'company_id',
        'default_branch_id',
        'user_id',
        'device_id',
        'first_bound_at_utc_micros',
      },
      'commercial_identity_snapshot': <String>{
        'singleton_id',
        'workspace_code',
        'user_display_name',
        'role',
        'device_label',
        'last_confirmed_at_utc_micros',
      },
    };
    for (final entry in requiredColumns.entries) {
      final table = entry.key;
      final row = await customSelect(
        'SELECT count(*) AS count FROM sqlite_master '
        'WHERE type = ? AND name = ?',
        variables: <Variable<Object>>[
          const Variable<String>('table'),
          Variable<String>(table),
        ],
      ).getSingle();
      if (row.read<int>('count') != 1) {
        throw const CommercialStorageException(
          CommercialStorageFailureCode.databaseConfigurationMismatch,
          phase: 'schema-object-missing',
        );
      }
      final columns = (await customSelect(
        'PRAGMA table_info($table)',
      ).get()).map((column) => column.read<String>('name')).toSet();
      if (columns.length != entry.value.length ||
          !columns.containsAll(entry.value)) {
        throw const CommercialStorageException(
          CommercialStorageFailureCode.databaseConfigurationMismatch,
          phase: 'schema-columns-invalid',
        );
      }
    }
  }

  Future<void> ensureFoundationMetadata({
    required CommercialProvisioningMarker marker,
    required int createdAtUtcMicros,
  }) => transaction(() async {
    final existing = await (select(
      commercialStorageMetadata,
    )..where((row) => row.singletonId.equals(1))).getSingleOrNull();
    if (existing == null) {
      await into(commercialStorageMetadata).insert(
        CommercialStorageMetadataCompanion.insert(
          singletonId: const Value(1),
          schemaContractVersion: foundationSchemaContractVersion,
          storageContractVersion: storageContractVersion,
          installationId: marker.installationId,
          databaseInstanceId: marker.databaseInstanceId,
          keyAliasVersion: keyAliasVersion,
          createdAtUtcMicros: createdAtUtcMicros,
        ),
      );
      return;
    }
    _verifyMetadata(existing, marker: marker);
  });

  Future<void> verifyFoundationMetadata(
    CommercialProvisioningMarker marker,
  ) async {
    final rows = await select(commercialStorageMetadata).get();
    if (rows.length != 1) {
      throw const CommercialStorageException(
        CommercialStorageFailureCode.databaseCorrupt,
        phase: 'metadata-cardinality',
      );
    }
    _verifyMetadata(rows.single, marker: marker);
    await _verifySchemaContract();
  }

  Future<void> checkpointWal() async {
    final row = await customSelect('PRAGMA wal_checkpoint(FULL)').getSingle();
    final busy = row.read<int>('busy');
    final logFrames = row.read<int>('log');
    final checkpointedFrames = row.read<int>('checkpointed');
    if (busy != 0 || logFrames != checkpointedFrames) {
      throw const CommercialStorageException(
        CommercialStorageFailureCode.storageUnavailable,
        phase: 'wal-checkpoint',
      );
    }
  }

  void _verifyMetadata(
    CommercialStorageMetadataData existing, {
    required CommercialProvisioningMarker marker,
  }) {
    if (existing.singletonId != 1 ||
        existing.schemaContractVersion != foundationSchemaContractVersion ||
        existing.storageContractVersion != storageContractVersion ||
        existing.keyAliasVersion != keyAliasVersion ||
        existing.installationId != marker.installationId ||
        existing.databaseInstanceId != marker.databaseInstanceId) {
      throw const CommercialStorageException(
        CommercialStorageFailureCode.databaseConfigurationMismatch,
        phase: 'metadata-binding',
      );
    }
  }
}
