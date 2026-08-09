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

@DriftDatabase(tables: [CommercialStorageMetadata])
final class CommercialDatabase extends _$CommercialDatabase {
  CommercialDatabase(super.executor);

  static const int currentSchemaVersion = 1;
  static const int storageContractVersion = 1;
  static const int keyAliasVersion = 1;

  @override
  int get schemaVersion => currentSchemaVersion;

  @override
  MigrationStrategy get migration => MigrationStrategy(
    onCreate: (migrator) async {
      await migrator.createAll();
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
    },
    onUpgrade: (migrator, from, to) {
      throw const CommercialStorageException(
        CommercialStorageFailureCode.databaseMigrationFailed,
        phase: 'schema-upgrade-not-authorized',
      );
    },
  );

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
          schemaContractVersion: currentSchemaVersion,
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
    final version = await customSelect('PRAGMA user_version').getSingle();
    if (version.read<int>('user_version') != currentSchemaVersion) {
      throw const CommercialStorageException(
        CommercialStorageFailureCode.databaseConfigurationMismatch,
        phase: 'schema-version',
      );
    }
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
        existing.schemaContractVersion != currentSchemaVersion ||
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
