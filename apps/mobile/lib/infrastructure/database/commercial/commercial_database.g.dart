// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'commercial_database.dart';

// ignore_for_file: type=lint
class $CommercialStorageMetadataTable extends CommercialStorageMetadata
    with
        TableInfo<
          $CommercialStorageMetadataTable,
          CommercialStorageMetadataData
        > {
  @override
  final GeneratedDatabase attachedDatabase;
  final String? _alias;
  $CommercialStorageMetadataTable(this.attachedDatabase, [this._alias]);
  static const VerificationMeta _singletonIdMeta = const VerificationMeta(
    'singletonId',
  );
  @override
  late final GeneratedColumn<int> singletonId = GeneratedColumn<int>(
    'singleton_id',
    aliasedName,
    false,
    check: () => const CustomExpression<bool>('singleton_id = 1'),
    type: DriftSqlType.int,
    requiredDuringInsert: false,
  );
  static const VerificationMeta _schemaContractVersionMeta =
      const VerificationMeta('schemaContractVersion');
  @override
  late final GeneratedColumn<int> schemaContractVersion = GeneratedColumn<int>(
    'schema_contract_version',
    aliasedName,
    false,
    type: DriftSqlType.int,
    requiredDuringInsert: true,
  );
  static const VerificationMeta _storageContractVersionMeta =
      const VerificationMeta('storageContractVersion');
  @override
  late final GeneratedColumn<int> storageContractVersion = GeneratedColumn<int>(
    'storage_contract_version',
    aliasedName,
    false,
    type: DriftSqlType.int,
    requiredDuringInsert: true,
  );
  static const VerificationMeta _installationIdMeta = const VerificationMeta(
    'installationId',
  );
  @override
  late final GeneratedColumn<String> installationId = GeneratedColumn<String>(
    'installation_id',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
  );
  static const VerificationMeta _databaseInstanceIdMeta =
      const VerificationMeta('databaseInstanceId');
  @override
  late final GeneratedColumn<String> databaseInstanceId =
      GeneratedColumn<String>(
        'database_instance_id',
        aliasedName,
        false,
        type: DriftSqlType.string,
        requiredDuringInsert: true,
      );
  static const VerificationMeta _keyAliasVersionMeta = const VerificationMeta(
    'keyAliasVersion',
  );
  @override
  late final GeneratedColumn<int> keyAliasVersion = GeneratedColumn<int>(
    'key_alias_version',
    aliasedName,
    false,
    type: DriftSqlType.int,
    requiredDuringInsert: true,
  );
  static const VerificationMeta _createdAtUtcMicrosMeta =
      const VerificationMeta('createdAtUtcMicros');
  @override
  late final GeneratedColumn<int> createdAtUtcMicros = GeneratedColumn<int>(
    'created_at_utc_micros',
    aliasedName,
    false,
    type: DriftSqlType.int,
    requiredDuringInsert: true,
  );
  @override
  List<GeneratedColumn> get $columns => [
    singletonId,
    schemaContractVersion,
    storageContractVersion,
    installationId,
    databaseInstanceId,
    keyAliasVersion,
    createdAtUtcMicros,
  ];
  @override
  String get aliasedName => _alias ?? actualTableName;
  @override
  String get actualTableName => $name;
  static const String $name = 'commercial_storage_metadata';
  @override
  VerificationContext validateIntegrity(
    Insertable<CommercialStorageMetadataData> instance, {
    bool isInserting = false,
  }) {
    final context = VerificationContext();
    final data = instance.toColumns(true);
    if (data.containsKey('singleton_id')) {
      context.handle(
        _singletonIdMeta,
        singletonId.isAcceptableOrUnknown(
          data['singleton_id']!,
          _singletonIdMeta,
        ),
      );
    }
    if (data.containsKey('schema_contract_version')) {
      context.handle(
        _schemaContractVersionMeta,
        schemaContractVersion.isAcceptableOrUnknown(
          data['schema_contract_version']!,
          _schemaContractVersionMeta,
        ),
      );
    } else if (isInserting) {
      context.missing(_schemaContractVersionMeta);
    }
    if (data.containsKey('storage_contract_version')) {
      context.handle(
        _storageContractVersionMeta,
        storageContractVersion.isAcceptableOrUnknown(
          data['storage_contract_version']!,
          _storageContractVersionMeta,
        ),
      );
    } else if (isInserting) {
      context.missing(_storageContractVersionMeta);
    }
    if (data.containsKey('installation_id')) {
      context.handle(
        _installationIdMeta,
        installationId.isAcceptableOrUnknown(
          data['installation_id']!,
          _installationIdMeta,
        ),
      );
    } else if (isInserting) {
      context.missing(_installationIdMeta);
    }
    if (data.containsKey('database_instance_id')) {
      context.handle(
        _databaseInstanceIdMeta,
        databaseInstanceId.isAcceptableOrUnknown(
          data['database_instance_id']!,
          _databaseInstanceIdMeta,
        ),
      );
    } else if (isInserting) {
      context.missing(_databaseInstanceIdMeta);
    }
    if (data.containsKey('key_alias_version')) {
      context.handle(
        _keyAliasVersionMeta,
        keyAliasVersion.isAcceptableOrUnknown(
          data['key_alias_version']!,
          _keyAliasVersionMeta,
        ),
      );
    } else if (isInserting) {
      context.missing(_keyAliasVersionMeta);
    }
    if (data.containsKey('created_at_utc_micros')) {
      context.handle(
        _createdAtUtcMicrosMeta,
        createdAtUtcMicros.isAcceptableOrUnknown(
          data['created_at_utc_micros']!,
          _createdAtUtcMicrosMeta,
        ),
      );
    } else if (isInserting) {
      context.missing(_createdAtUtcMicrosMeta);
    }
    return context;
  }

  @override
  Set<GeneratedColumn> get $primaryKey => {singletonId};
  @override
  CommercialStorageMetadataData map(
    Map<String, dynamic> data, {
    String? tablePrefix,
  }) {
    final effectivePrefix = tablePrefix != null ? '$tablePrefix.' : '';
    return CommercialStorageMetadataData(
      singletonId: attachedDatabase.typeMapping.read(
        DriftSqlType.int,
        data['${effectivePrefix}singleton_id'],
      )!,
      schemaContractVersion: attachedDatabase.typeMapping.read(
        DriftSqlType.int,
        data['${effectivePrefix}schema_contract_version'],
      )!,
      storageContractVersion: attachedDatabase.typeMapping.read(
        DriftSqlType.int,
        data['${effectivePrefix}storage_contract_version'],
      )!,
      installationId: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}installation_id'],
      )!,
      databaseInstanceId: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}database_instance_id'],
      )!,
      keyAliasVersion: attachedDatabase.typeMapping.read(
        DriftSqlType.int,
        data['${effectivePrefix}key_alias_version'],
      )!,
      createdAtUtcMicros: attachedDatabase.typeMapping.read(
        DriftSqlType.int,
        data['${effectivePrefix}created_at_utc_micros'],
      )!,
    );
  }

  @override
  $CommercialStorageMetadataTable createAlias(String alias) {
    return $CommercialStorageMetadataTable(attachedDatabase, alias);
  }
}

class CommercialStorageMetadataData extends DataClass
    implements Insertable<CommercialStorageMetadataData> {
  final int singletonId;
  final int schemaContractVersion;
  final int storageContractVersion;
  final String installationId;
  final String databaseInstanceId;
  final int keyAliasVersion;
  final int createdAtUtcMicros;
  const CommercialStorageMetadataData({
    required this.singletonId,
    required this.schemaContractVersion,
    required this.storageContractVersion,
    required this.installationId,
    required this.databaseInstanceId,
    required this.keyAliasVersion,
    required this.createdAtUtcMicros,
  });
  @override
  Map<String, Expression> toColumns(bool nullToAbsent) {
    final map = <String, Expression>{};
    map['singleton_id'] = Variable<int>(singletonId);
    map['schema_contract_version'] = Variable<int>(schemaContractVersion);
    map['storage_contract_version'] = Variable<int>(storageContractVersion);
    map['installation_id'] = Variable<String>(installationId);
    map['database_instance_id'] = Variable<String>(databaseInstanceId);
    map['key_alias_version'] = Variable<int>(keyAliasVersion);
    map['created_at_utc_micros'] = Variable<int>(createdAtUtcMicros);
    return map;
  }

  CommercialStorageMetadataCompanion toCompanion(bool nullToAbsent) {
    return CommercialStorageMetadataCompanion(
      singletonId: Value(singletonId),
      schemaContractVersion: Value(schemaContractVersion),
      storageContractVersion: Value(storageContractVersion),
      installationId: Value(installationId),
      databaseInstanceId: Value(databaseInstanceId),
      keyAliasVersion: Value(keyAliasVersion),
      createdAtUtcMicros: Value(createdAtUtcMicros),
    );
  }

  factory CommercialStorageMetadataData.fromJson(
    Map<String, dynamic> json, {
    ValueSerializer? serializer,
  }) {
    serializer ??= driftRuntimeOptions.defaultSerializer;
    return CommercialStorageMetadataData(
      singletonId: serializer.fromJson<int>(json['singletonId']),
      schemaContractVersion: serializer.fromJson<int>(
        json['schemaContractVersion'],
      ),
      storageContractVersion: serializer.fromJson<int>(
        json['storageContractVersion'],
      ),
      installationId: serializer.fromJson<String>(json['installationId']),
      databaseInstanceId: serializer.fromJson<String>(
        json['databaseInstanceId'],
      ),
      keyAliasVersion: serializer.fromJson<int>(json['keyAliasVersion']),
      createdAtUtcMicros: serializer.fromJson<int>(json['createdAtUtcMicros']),
    );
  }
  @override
  Map<String, dynamic> toJson({ValueSerializer? serializer}) {
    serializer ??= driftRuntimeOptions.defaultSerializer;
    return <String, dynamic>{
      'singletonId': serializer.toJson<int>(singletonId),
      'schemaContractVersion': serializer.toJson<int>(schemaContractVersion),
      'storageContractVersion': serializer.toJson<int>(storageContractVersion),
      'installationId': serializer.toJson<String>(installationId),
      'databaseInstanceId': serializer.toJson<String>(databaseInstanceId),
      'keyAliasVersion': serializer.toJson<int>(keyAliasVersion),
      'createdAtUtcMicros': serializer.toJson<int>(createdAtUtcMicros),
    };
  }

  CommercialStorageMetadataData copyWith({
    int? singletonId,
    int? schemaContractVersion,
    int? storageContractVersion,
    String? installationId,
    String? databaseInstanceId,
    int? keyAliasVersion,
    int? createdAtUtcMicros,
  }) => CommercialStorageMetadataData(
    singletonId: singletonId ?? this.singletonId,
    schemaContractVersion: schemaContractVersion ?? this.schemaContractVersion,
    storageContractVersion:
        storageContractVersion ?? this.storageContractVersion,
    installationId: installationId ?? this.installationId,
    databaseInstanceId: databaseInstanceId ?? this.databaseInstanceId,
    keyAliasVersion: keyAliasVersion ?? this.keyAliasVersion,
    createdAtUtcMicros: createdAtUtcMicros ?? this.createdAtUtcMicros,
  );
  CommercialStorageMetadataData copyWithCompanion(
    CommercialStorageMetadataCompanion data,
  ) {
    return CommercialStorageMetadataData(
      singletonId: data.singletonId.present
          ? data.singletonId.value
          : this.singletonId,
      schemaContractVersion: data.schemaContractVersion.present
          ? data.schemaContractVersion.value
          : this.schemaContractVersion,
      storageContractVersion: data.storageContractVersion.present
          ? data.storageContractVersion.value
          : this.storageContractVersion,
      installationId: data.installationId.present
          ? data.installationId.value
          : this.installationId,
      databaseInstanceId: data.databaseInstanceId.present
          ? data.databaseInstanceId.value
          : this.databaseInstanceId,
      keyAliasVersion: data.keyAliasVersion.present
          ? data.keyAliasVersion.value
          : this.keyAliasVersion,
      createdAtUtcMicros: data.createdAtUtcMicros.present
          ? data.createdAtUtcMicros.value
          : this.createdAtUtcMicros,
    );
  }

  @override
  String toString() {
    return (StringBuffer('CommercialStorageMetadataData(')
          ..write('singletonId: $singletonId, ')
          ..write('schemaContractVersion: $schemaContractVersion, ')
          ..write('storageContractVersion: $storageContractVersion, ')
          ..write('installationId: $installationId, ')
          ..write('databaseInstanceId: $databaseInstanceId, ')
          ..write('keyAliasVersion: $keyAliasVersion, ')
          ..write('createdAtUtcMicros: $createdAtUtcMicros')
          ..write(')'))
        .toString();
  }

  @override
  int get hashCode => Object.hash(
    singletonId,
    schemaContractVersion,
    storageContractVersion,
    installationId,
    databaseInstanceId,
    keyAliasVersion,
    createdAtUtcMicros,
  );
  @override
  bool operator ==(Object other) =>
      identical(this, other) ||
      (other is CommercialStorageMetadataData &&
          other.singletonId == this.singletonId &&
          other.schemaContractVersion == this.schemaContractVersion &&
          other.storageContractVersion == this.storageContractVersion &&
          other.installationId == this.installationId &&
          other.databaseInstanceId == this.databaseInstanceId &&
          other.keyAliasVersion == this.keyAliasVersion &&
          other.createdAtUtcMicros == this.createdAtUtcMicros);
}

class CommercialStorageMetadataCompanion
    extends UpdateCompanion<CommercialStorageMetadataData> {
  final Value<int> singletonId;
  final Value<int> schemaContractVersion;
  final Value<int> storageContractVersion;
  final Value<String> installationId;
  final Value<String> databaseInstanceId;
  final Value<int> keyAliasVersion;
  final Value<int> createdAtUtcMicros;
  const CommercialStorageMetadataCompanion({
    this.singletonId = const Value.absent(),
    this.schemaContractVersion = const Value.absent(),
    this.storageContractVersion = const Value.absent(),
    this.installationId = const Value.absent(),
    this.databaseInstanceId = const Value.absent(),
    this.keyAliasVersion = const Value.absent(),
    this.createdAtUtcMicros = const Value.absent(),
  });
  CommercialStorageMetadataCompanion.insert({
    this.singletonId = const Value.absent(),
    required int schemaContractVersion,
    required int storageContractVersion,
    required String installationId,
    required String databaseInstanceId,
    required int keyAliasVersion,
    required int createdAtUtcMicros,
  }) : schemaContractVersion = Value(schemaContractVersion),
       storageContractVersion = Value(storageContractVersion),
       installationId = Value(installationId),
       databaseInstanceId = Value(databaseInstanceId),
       keyAliasVersion = Value(keyAliasVersion),
       createdAtUtcMicros = Value(createdAtUtcMicros);
  static Insertable<CommercialStorageMetadataData> custom({
    Expression<int>? singletonId,
    Expression<int>? schemaContractVersion,
    Expression<int>? storageContractVersion,
    Expression<String>? installationId,
    Expression<String>? databaseInstanceId,
    Expression<int>? keyAliasVersion,
    Expression<int>? createdAtUtcMicros,
  }) {
    return RawValuesInsertable({
      if (singletonId != null) 'singleton_id': singletonId,
      if (schemaContractVersion != null)
        'schema_contract_version': schemaContractVersion,
      if (storageContractVersion != null)
        'storage_contract_version': storageContractVersion,
      if (installationId != null) 'installation_id': installationId,
      if (databaseInstanceId != null)
        'database_instance_id': databaseInstanceId,
      if (keyAliasVersion != null) 'key_alias_version': keyAliasVersion,
      if (createdAtUtcMicros != null)
        'created_at_utc_micros': createdAtUtcMicros,
    });
  }

  CommercialStorageMetadataCompanion copyWith({
    Value<int>? singletonId,
    Value<int>? schemaContractVersion,
    Value<int>? storageContractVersion,
    Value<String>? installationId,
    Value<String>? databaseInstanceId,
    Value<int>? keyAliasVersion,
    Value<int>? createdAtUtcMicros,
  }) {
    return CommercialStorageMetadataCompanion(
      singletonId: singletonId ?? this.singletonId,
      schemaContractVersion:
          schemaContractVersion ?? this.schemaContractVersion,
      storageContractVersion:
          storageContractVersion ?? this.storageContractVersion,
      installationId: installationId ?? this.installationId,
      databaseInstanceId: databaseInstanceId ?? this.databaseInstanceId,
      keyAliasVersion: keyAliasVersion ?? this.keyAliasVersion,
      createdAtUtcMicros: createdAtUtcMicros ?? this.createdAtUtcMicros,
    );
  }

  @override
  Map<String, Expression> toColumns(bool nullToAbsent) {
    final map = <String, Expression>{};
    if (singletonId.present) {
      map['singleton_id'] = Variable<int>(singletonId.value);
    }
    if (schemaContractVersion.present) {
      map['schema_contract_version'] = Variable<int>(
        schemaContractVersion.value,
      );
    }
    if (storageContractVersion.present) {
      map['storage_contract_version'] = Variable<int>(
        storageContractVersion.value,
      );
    }
    if (installationId.present) {
      map['installation_id'] = Variable<String>(installationId.value);
    }
    if (databaseInstanceId.present) {
      map['database_instance_id'] = Variable<String>(databaseInstanceId.value);
    }
    if (keyAliasVersion.present) {
      map['key_alias_version'] = Variable<int>(keyAliasVersion.value);
    }
    if (createdAtUtcMicros.present) {
      map['created_at_utc_micros'] = Variable<int>(createdAtUtcMicros.value);
    }
    return map;
  }

  @override
  String toString() {
    return (StringBuffer('CommercialStorageMetadataCompanion(')
          ..write('singletonId: $singletonId, ')
          ..write('schemaContractVersion: $schemaContractVersion, ')
          ..write('storageContractVersion: $storageContractVersion, ')
          ..write('installationId: $installationId, ')
          ..write('databaseInstanceId: $databaseInstanceId, ')
          ..write('keyAliasVersion: $keyAliasVersion, ')
          ..write('createdAtUtcMicros: $createdAtUtcMicros')
          ..write(')'))
        .toString();
  }
}

abstract class _$CommercialDatabase extends GeneratedDatabase {
  _$CommercialDatabase(QueryExecutor e) : super(e);
  $CommercialDatabaseManager get managers => $CommercialDatabaseManager(this);
  late final $CommercialStorageMetadataTable commercialStorageMetadata =
      $CommercialStorageMetadataTable(this);
  @override
  Iterable<TableInfo<Table, Object?>> get allTables =>
      allSchemaEntities.whereType<TableInfo<Table, Object?>>();
  @override
  List<DatabaseSchemaEntity> get allSchemaEntities => [
    commercialStorageMetadata,
  ];
}

typedef $$CommercialStorageMetadataTableCreateCompanionBuilder =
    CommercialStorageMetadataCompanion Function({
      Value<int> singletonId,
      required int schemaContractVersion,
      required int storageContractVersion,
      required String installationId,
      required String databaseInstanceId,
      required int keyAliasVersion,
      required int createdAtUtcMicros,
    });
typedef $$CommercialStorageMetadataTableUpdateCompanionBuilder =
    CommercialStorageMetadataCompanion Function({
      Value<int> singletonId,
      Value<int> schemaContractVersion,
      Value<int> storageContractVersion,
      Value<String> installationId,
      Value<String> databaseInstanceId,
      Value<int> keyAliasVersion,
      Value<int> createdAtUtcMicros,
    });

class $$CommercialStorageMetadataTableFilterComposer
    extends Composer<_$CommercialDatabase, $CommercialStorageMetadataTable> {
  $$CommercialStorageMetadataTableFilterComposer({
    required super.$db,
    required super.$table,
    super.joinBuilder,
    super.$addJoinBuilderToRootComposer,
    super.$removeJoinBuilderFromRootComposer,
  });
  ColumnFilters<int> get singletonId => $composableBuilder(
    column: $table.singletonId,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<int> get schemaContractVersion => $composableBuilder(
    column: $table.schemaContractVersion,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<int> get storageContractVersion => $composableBuilder(
    column: $table.storageContractVersion,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get installationId => $composableBuilder(
    column: $table.installationId,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get databaseInstanceId => $composableBuilder(
    column: $table.databaseInstanceId,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<int> get keyAliasVersion => $composableBuilder(
    column: $table.keyAliasVersion,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<int> get createdAtUtcMicros => $composableBuilder(
    column: $table.createdAtUtcMicros,
    builder: (column) => ColumnFilters(column),
  );
}

class $$CommercialStorageMetadataTableOrderingComposer
    extends Composer<_$CommercialDatabase, $CommercialStorageMetadataTable> {
  $$CommercialStorageMetadataTableOrderingComposer({
    required super.$db,
    required super.$table,
    super.joinBuilder,
    super.$addJoinBuilderToRootComposer,
    super.$removeJoinBuilderFromRootComposer,
  });
  ColumnOrderings<int> get singletonId => $composableBuilder(
    column: $table.singletonId,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<int> get schemaContractVersion => $composableBuilder(
    column: $table.schemaContractVersion,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<int> get storageContractVersion => $composableBuilder(
    column: $table.storageContractVersion,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get installationId => $composableBuilder(
    column: $table.installationId,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get databaseInstanceId => $composableBuilder(
    column: $table.databaseInstanceId,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<int> get keyAliasVersion => $composableBuilder(
    column: $table.keyAliasVersion,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<int> get createdAtUtcMicros => $composableBuilder(
    column: $table.createdAtUtcMicros,
    builder: (column) => ColumnOrderings(column),
  );
}

class $$CommercialStorageMetadataTableAnnotationComposer
    extends Composer<_$CommercialDatabase, $CommercialStorageMetadataTable> {
  $$CommercialStorageMetadataTableAnnotationComposer({
    required super.$db,
    required super.$table,
    super.joinBuilder,
    super.$addJoinBuilderToRootComposer,
    super.$removeJoinBuilderFromRootComposer,
  });
  GeneratedColumn<int> get singletonId => $composableBuilder(
    column: $table.singletonId,
    builder: (column) => column,
  );

  GeneratedColumn<int> get schemaContractVersion => $composableBuilder(
    column: $table.schemaContractVersion,
    builder: (column) => column,
  );

  GeneratedColumn<int> get storageContractVersion => $composableBuilder(
    column: $table.storageContractVersion,
    builder: (column) => column,
  );

  GeneratedColumn<String> get installationId => $composableBuilder(
    column: $table.installationId,
    builder: (column) => column,
  );

  GeneratedColumn<String> get databaseInstanceId => $composableBuilder(
    column: $table.databaseInstanceId,
    builder: (column) => column,
  );

  GeneratedColumn<int> get keyAliasVersion => $composableBuilder(
    column: $table.keyAliasVersion,
    builder: (column) => column,
  );

  GeneratedColumn<int> get createdAtUtcMicros => $composableBuilder(
    column: $table.createdAtUtcMicros,
    builder: (column) => column,
  );
}

class $$CommercialStorageMetadataTableTableManager
    extends
        RootTableManager<
          _$CommercialDatabase,
          $CommercialStorageMetadataTable,
          CommercialStorageMetadataData,
          $$CommercialStorageMetadataTableFilterComposer,
          $$CommercialStorageMetadataTableOrderingComposer,
          $$CommercialStorageMetadataTableAnnotationComposer,
          $$CommercialStorageMetadataTableCreateCompanionBuilder,
          $$CommercialStorageMetadataTableUpdateCompanionBuilder,
          (
            CommercialStorageMetadataData,
            BaseReferences<
              _$CommercialDatabase,
              $CommercialStorageMetadataTable,
              CommercialStorageMetadataData
            >,
          ),
          CommercialStorageMetadataData,
          PrefetchHooks Function()
        > {
  $$CommercialStorageMetadataTableTableManager(
    _$CommercialDatabase db,
    $CommercialStorageMetadataTable table,
  ) : super(
        TableManagerState(
          db: db,
          table: table,
          createFilteringComposer: () =>
              $$CommercialStorageMetadataTableFilterComposer(
                $db: db,
                $table: table,
              ),
          createOrderingComposer: () =>
              $$CommercialStorageMetadataTableOrderingComposer(
                $db: db,
                $table: table,
              ),
          createComputedFieldComposer: () =>
              $$CommercialStorageMetadataTableAnnotationComposer(
                $db: db,
                $table: table,
              ),
          updateCompanionCallback:
              ({
                Value<int> singletonId = const Value.absent(),
                Value<int> schemaContractVersion = const Value.absent(),
                Value<int> storageContractVersion = const Value.absent(),
                Value<String> installationId = const Value.absent(),
                Value<String> databaseInstanceId = const Value.absent(),
                Value<int> keyAliasVersion = const Value.absent(),
                Value<int> createdAtUtcMicros = const Value.absent(),
              }) => CommercialStorageMetadataCompanion(
                singletonId: singletonId,
                schemaContractVersion: schemaContractVersion,
                storageContractVersion: storageContractVersion,
                installationId: installationId,
                databaseInstanceId: databaseInstanceId,
                keyAliasVersion: keyAliasVersion,
                createdAtUtcMicros: createdAtUtcMicros,
              ),
          createCompanionCallback:
              ({
                Value<int> singletonId = const Value.absent(),
                required int schemaContractVersion,
                required int storageContractVersion,
                required String installationId,
                required String databaseInstanceId,
                required int keyAliasVersion,
                required int createdAtUtcMicros,
              }) => CommercialStorageMetadataCompanion.insert(
                singletonId: singletonId,
                schemaContractVersion: schemaContractVersion,
                storageContractVersion: storageContractVersion,
                installationId: installationId,
                databaseInstanceId: databaseInstanceId,
                keyAliasVersion: keyAliasVersion,
                createdAtUtcMicros: createdAtUtcMicros,
              ),
          withReferenceMapper: (p0) => p0
              .map((e) => (e.readTable(table), BaseReferences(db, table, e)))
              .toList(),
          prefetchHooksCallback: null,
        ),
      );
}

typedef $$CommercialStorageMetadataTableProcessedTableManager =
    ProcessedTableManager<
      _$CommercialDatabase,
      $CommercialStorageMetadataTable,
      CommercialStorageMetadataData,
      $$CommercialStorageMetadataTableFilterComposer,
      $$CommercialStorageMetadataTableOrderingComposer,
      $$CommercialStorageMetadataTableAnnotationComposer,
      $$CommercialStorageMetadataTableCreateCompanionBuilder,
      $$CommercialStorageMetadataTableUpdateCompanionBuilder,
      (
        CommercialStorageMetadataData,
        BaseReferences<
          _$CommercialDatabase,
          $CommercialStorageMetadataTable,
          CommercialStorageMetadataData
        >,
      ),
      CommercialStorageMetadataData,
      PrefetchHooks Function()
    >;

class $CommercialDatabaseManager {
  final _$CommercialDatabase _db;
  $CommercialDatabaseManager(this._db);
  $$CommercialStorageMetadataTableTableManager get commercialStorageMetadata =>
      $$CommercialStorageMetadataTableTableManager(
        _db,
        _db.commercialStorageMetadata,
      );
}
