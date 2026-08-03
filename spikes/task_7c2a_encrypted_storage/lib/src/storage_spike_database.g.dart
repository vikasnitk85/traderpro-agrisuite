// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'storage_spike_database.dart';

// ignore_for_file: type=lint
class $SyntheticMarkersTable extends SyntheticMarkers
    with TableInfo<$SyntheticMarkersTable, SyntheticMarker> {
  @override
  final GeneratedDatabase attachedDatabase;
  final String? _alias;
  $SyntheticMarkersTable(this.attachedDatabase, [this._alias]);
  static const VerificationMeta _idMeta = const VerificationMeta('id');
  @override
  late final GeneratedColumn<int> id = GeneratedColumn<int>(
    'id',
    aliasedName,
    false,
    hasAutoIncrement: true,
    type: DriftSqlType.int,
    requiredDuringInsert: false,
    defaultConstraints: GeneratedColumn.constraintIsAlways(
      'PRIMARY KEY AUTOINCREMENT',
    ),
  );
  static const VerificationMeta _markerMeta = const VerificationMeta('marker');
  @override
  late final GeneratedColumn<String> marker = GeneratedColumn<String>(
    'marker',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
  );
  static const VerificationMeta _createdAtUtcMeta = const VerificationMeta(
    'createdAtUtc',
  );
  @override
  late final GeneratedColumn<String> createdAtUtc = GeneratedColumn<String>(
    'created_at_utc',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
  );
  @override
  List<GeneratedColumn> get $columns => [id, marker, createdAtUtc];
  @override
  String get aliasedName => _alias ?? actualTableName;
  @override
  String get actualTableName => $name;
  static const String $name = 'synthetic_markers';
  @override
  VerificationContext validateIntegrity(
    Insertable<SyntheticMarker> instance, {
    bool isInserting = false,
  }) {
    final context = VerificationContext();
    final data = instance.toColumns(true);
    if (data.containsKey('id')) {
      context.handle(_idMeta, id.isAcceptableOrUnknown(data['id']!, _idMeta));
    }
    if (data.containsKey('marker')) {
      context.handle(
        _markerMeta,
        marker.isAcceptableOrUnknown(data['marker']!, _markerMeta),
      );
    } else if (isInserting) {
      context.missing(_markerMeta);
    }
    if (data.containsKey('created_at_utc')) {
      context.handle(
        _createdAtUtcMeta,
        createdAtUtc.isAcceptableOrUnknown(
          data['created_at_utc']!,
          _createdAtUtcMeta,
        ),
      );
    } else if (isInserting) {
      context.missing(_createdAtUtcMeta);
    }
    return context;
  }

  @override
  Set<GeneratedColumn> get $primaryKey => {id};
  @override
  SyntheticMarker map(Map<String, dynamic> data, {String? tablePrefix}) {
    final effectivePrefix = tablePrefix != null ? '$tablePrefix.' : '';
    return SyntheticMarker(
      id: attachedDatabase.typeMapping.read(
        DriftSqlType.int,
        data['${effectivePrefix}id'],
      )!,
      marker: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}marker'],
      )!,
      createdAtUtc: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}created_at_utc'],
      )!,
    );
  }

  @override
  $SyntheticMarkersTable createAlias(String alias) {
    return $SyntheticMarkersTable(attachedDatabase, alias);
  }
}

class SyntheticMarker extends DataClass implements Insertable<SyntheticMarker> {
  final int id;
  final String marker;
  final String createdAtUtc;
  const SyntheticMarker({
    required this.id,
    required this.marker,
    required this.createdAtUtc,
  });
  @override
  Map<String, Expression> toColumns(bool nullToAbsent) {
    final map = <String, Expression>{};
    map['id'] = Variable<int>(id);
    map['marker'] = Variable<String>(marker);
    map['created_at_utc'] = Variable<String>(createdAtUtc);
    return map;
  }

  SyntheticMarkersCompanion toCompanion(bool nullToAbsent) {
    return SyntheticMarkersCompanion(
      id: Value(id),
      marker: Value(marker),
      createdAtUtc: Value(createdAtUtc),
    );
  }

  factory SyntheticMarker.fromJson(
    Map<String, dynamic> json, {
    ValueSerializer? serializer,
  }) {
    serializer ??= driftRuntimeOptions.defaultSerializer;
    return SyntheticMarker(
      id: serializer.fromJson<int>(json['id']),
      marker: serializer.fromJson<String>(json['marker']),
      createdAtUtc: serializer.fromJson<String>(json['createdAtUtc']),
    );
  }
  @override
  Map<String, dynamic> toJson({ValueSerializer? serializer}) {
    serializer ??= driftRuntimeOptions.defaultSerializer;
    return <String, dynamic>{
      'id': serializer.toJson<int>(id),
      'marker': serializer.toJson<String>(marker),
      'createdAtUtc': serializer.toJson<String>(createdAtUtc),
    };
  }

  SyntheticMarker copyWith({int? id, String? marker, String? createdAtUtc}) =>
      SyntheticMarker(
        id: id ?? this.id,
        marker: marker ?? this.marker,
        createdAtUtc: createdAtUtc ?? this.createdAtUtc,
      );
  SyntheticMarker copyWithCompanion(SyntheticMarkersCompanion data) {
    return SyntheticMarker(
      id: data.id.present ? data.id.value : this.id,
      marker: data.marker.present ? data.marker.value : this.marker,
      createdAtUtc: data.createdAtUtc.present
          ? data.createdAtUtc.value
          : this.createdAtUtc,
    );
  }

  @override
  String toString() {
    return (StringBuffer('SyntheticMarker(')
          ..write('id: $id, ')
          ..write('marker: $marker, ')
          ..write('createdAtUtc: $createdAtUtc')
          ..write(')'))
        .toString();
  }

  @override
  int get hashCode => Object.hash(id, marker, createdAtUtc);
  @override
  bool operator ==(Object other) =>
      identical(this, other) ||
      (other is SyntheticMarker &&
          other.id == this.id &&
          other.marker == this.marker &&
          other.createdAtUtc == this.createdAtUtc);
}

class SyntheticMarkersCompanion extends UpdateCompanion<SyntheticMarker> {
  final Value<int> id;
  final Value<String> marker;
  final Value<String> createdAtUtc;
  const SyntheticMarkersCompanion({
    this.id = const Value.absent(),
    this.marker = const Value.absent(),
    this.createdAtUtc = const Value.absent(),
  });
  SyntheticMarkersCompanion.insert({
    this.id = const Value.absent(),
    required String marker,
    required String createdAtUtc,
  }) : marker = Value(marker),
       createdAtUtc = Value(createdAtUtc);
  static Insertable<SyntheticMarker> custom({
    Expression<int>? id,
    Expression<String>? marker,
    Expression<String>? createdAtUtc,
  }) {
    return RawValuesInsertable({
      if (id != null) 'id': id,
      if (marker != null) 'marker': marker,
      if (createdAtUtc != null) 'created_at_utc': createdAtUtc,
    });
  }

  SyntheticMarkersCompanion copyWith({
    Value<int>? id,
    Value<String>? marker,
    Value<String>? createdAtUtc,
  }) {
    return SyntheticMarkersCompanion(
      id: id ?? this.id,
      marker: marker ?? this.marker,
      createdAtUtc: createdAtUtc ?? this.createdAtUtc,
    );
  }

  @override
  Map<String, Expression> toColumns(bool nullToAbsent) {
    final map = <String, Expression>{};
    if (id.present) {
      map['id'] = Variable<int>(id.value);
    }
    if (marker.present) {
      map['marker'] = Variable<String>(marker.value);
    }
    if (createdAtUtc.present) {
      map['created_at_utc'] = Variable<String>(createdAtUtc.value);
    }
    return map;
  }

  @override
  String toString() {
    return (StringBuffer('SyntheticMarkersCompanion(')
          ..write('id: $id, ')
          ..write('marker: $marker, ')
          ..write('createdAtUtc: $createdAtUtc')
          ..write(')'))
        .toString();
  }
}

class $MigrationEvidenceTable extends MigrationEvidence
    with TableInfo<$MigrationEvidenceTable, MigrationEvidenceData> {
  @override
  final GeneratedDatabase attachedDatabase;
  final String? _alias;
  $MigrationEvidenceTable(this.attachedDatabase, [this._alias]);
  static const VerificationMeta _idMeta = const VerificationMeta('id');
  @override
  late final GeneratedColumn<int> id = GeneratedColumn<int>(
    'id',
    aliasedName,
    false,
    hasAutoIncrement: true,
    type: DriftSqlType.int,
    requiredDuringInsert: false,
    defaultConstraints: GeneratedColumn.constraintIsAlways(
      'PRIMARY KEY AUTOINCREMENT',
    ),
  );
  static const VerificationMeta _evidenceMeta = const VerificationMeta(
    'evidence',
  );
  @override
  late final GeneratedColumn<String> evidence = GeneratedColumn<String>(
    'evidence',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
  );
  @override
  List<GeneratedColumn> get $columns => [id, evidence];
  @override
  String get aliasedName => _alias ?? actualTableName;
  @override
  String get actualTableName => $name;
  static const String $name = 'migration_evidence';
  @override
  VerificationContext validateIntegrity(
    Insertable<MigrationEvidenceData> instance, {
    bool isInserting = false,
  }) {
    final context = VerificationContext();
    final data = instance.toColumns(true);
    if (data.containsKey('id')) {
      context.handle(_idMeta, id.isAcceptableOrUnknown(data['id']!, _idMeta));
    }
    if (data.containsKey('evidence')) {
      context.handle(
        _evidenceMeta,
        evidence.isAcceptableOrUnknown(data['evidence']!, _evidenceMeta),
      );
    } else if (isInserting) {
      context.missing(_evidenceMeta);
    }
    return context;
  }

  @override
  Set<GeneratedColumn> get $primaryKey => {id};
  @override
  MigrationEvidenceData map(Map<String, dynamic> data, {String? tablePrefix}) {
    final effectivePrefix = tablePrefix != null ? '$tablePrefix.' : '';
    return MigrationEvidenceData(
      id: attachedDatabase.typeMapping.read(
        DriftSqlType.int,
        data['${effectivePrefix}id'],
      )!,
      evidence: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}evidence'],
      )!,
    );
  }

  @override
  $MigrationEvidenceTable createAlias(String alias) {
    return $MigrationEvidenceTable(attachedDatabase, alias);
  }
}

class MigrationEvidenceData extends DataClass
    implements Insertable<MigrationEvidenceData> {
  final int id;
  final String evidence;
  const MigrationEvidenceData({required this.id, required this.evidence});
  @override
  Map<String, Expression> toColumns(bool nullToAbsent) {
    final map = <String, Expression>{};
    map['id'] = Variable<int>(id);
    map['evidence'] = Variable<String>(evidence);
    return map;
  }

  MigrationEvidenceCompanion toCompanion(bool nullToAbsent) {
    return MigrationEvidenceCompanion(id: Value(id), evidence: Value(evidence));
  }

  factory MigrationEvidenceData.fromJson(
    Map<String, dynamic> json, {
    ValueSerializer? serializer,
  }) {
    serializer ??= driftRuntimeOptions.defaultSerializer;
    return MigrationEvidenceData(
      id: serializer.fromJson<int>(json['id']),
      evidence: serializer.fromJson<String>(json['evidence']),
    );
  }
  @override
  Map<String, dynamic> toJson({ValueSerializer? serializer}) {
    serializer ??= driftRuntimeOptions.defaultSerializer;
    return <String, dynamic>{
      'id': serializer.toJson<int>(id),
      'evidence': serializer.toJson<String>(evidence),
    };
  }

  MigrationEvidenceData copyWith({int? id, String? evidence}) =>
      MigrationEvidenceData(
        id: id ?? this.id,
        evidence: evidence ?? this.evidence,
      );
  MigrationEvidenceData copyWithCompanion(MigrationEvidenceCompanion data) {
    return MigrationEvidenceData(
      id: data.id.present ? data.id.value : this.id,
      evidence: data.evidence.present ? data.evidence.value : this.evidence,
    );
  }

  @override
  String toString() {
    return (StringBuffer('MigrationEvidenceData(')
          ..write('id: $id, ')
          ..write('evidence: $evidence')
          ..write(')'))
        .toString();
  }

  @override
  int get hashCode => Object.hash(id, evidence);
  @override
  bool operator ==(Object other) =>
      identical(this, other) ||
      (other is MigrationEvidenceData &&
          other.id == this.id &&
          other.evidence == this.evidence);
}

class MigrationEvidenceCompanion
    extends UpdateCompanion<MigrationEvidenceData> {
  final Value<int> id;
  final Value<String> evidence;
  const MigrationEvidenceCompanion({
    this.id = const Value.absent(),
    this.evidence = const Value.absent(),
  });
  MigrationEvidenceCompanion.insert({
    this.id = const Value.absent(),
    required String evidence,
  }) : evidence = Value(evidence);
  static Insertable<MigrationEvidenceData> custom({
    Expression<int>? id,
    Expression<String>? evidence,
  }) {
    return RawValuesInsertable({
      if (id != null) 'id': id,
      if (evidence != null) 'evidence': evidence,
    });
  }

  MigrationEvidenceCompanion copyWith({
    Value<int>? id,
    Value<String>? evidence,
  }) {
    return MigrationEvidenceCompanion(
      id: id ?? this.id,
      evidence: evidence ?? this.evidence,
    );
  }

  @override
  Map<String, Expression> toColumns(bool nullToAbsent) {
    final map = <String, Expression>{};
    if (id.present) {
      map['id'] = Variable<int>(id.value);
    }
    if (evidence.present) {
      map['evidence'] = Variable<String>(evidence.value);
    }
    return map;
  }

  @override
  String toString() {
    return (StringBuffer('MigrationEvidenceCompanion(')
          ..write('id: $id, ')
          ..write('evidence: $evidence')
          ..write(')'))
        .toString();
  }
}

abstract class _$StorageSpikeDatabase extends GeneratedDatabase {
  _$StorageSpikeDatabase(QueryExecutor e) : super(e);
  $StorageSpikeDatabaseManager get managers =>
      $StorageSpikeDatabaseManager(this);
  late final $SyntheticMarkersTable syntheticMarkers = $SyntheticMarkersTable(
    this,
  );
  late final $MigrationEvidenceTable migrationEvidence =
      $MigrationEvidenceTable(this);
  @override
  Iterable<TableInfo<Table, Object?>> get allTables =>
      allSchemaEntities.whereType<TableInfo<Table, Object?>>();
  @override
  List<DatabaseSchemaEntity> get allSchemaEntities => [
    syntheticMarkers,
    migrationEvidence,
  ];
}

typedef $$SyntheticMarkersTableCreateCompanionBuilder =
    SyntheticMarkersCompanion Function({
      Value<int> id,
      required String marker,
      required String createdAtUtc,
    });
typedef $$SyntheticMarkersTableUpdateCompanionBuilder =
    SyntheticMarkersCompanion Function({
      Value<int> id,
      Value<String> marker,
      Value<String> createdAtUtc,
    });

class $$SyntheticMarkersTableFilterComposer
    extends Composer<_$StorageSpikeDatabase, $SyntheticMarkersTable> {
  $$SyntheticMarkersTableFilterComposer({
    required super.$db,
    required super.$table,
    super.joinBuilder,
    super.$addJoinBuilderToRootComposer,
    super.$removeJoinBuilderFromRootComposer,
  });
  ColumnFilters<int> get id => $composableBuilder(
    column: $table.id,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get marker => $composableBuilder(
    column: $table.marker,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get createdAtUtc => $composableBuilder(
    column: $table.createdAtUtc,
    builder: (column) => ColumnFilters(column),
  );
}

class $$SyntheticMarkersTableOrderingComposer
    extends Composer<_$StorageSpikeDatabase, $SyntheticMarkersTable> {
  $$SyntheticMarkersTableOrderingComposer({
    required super.$db,
    required super.$table,
    super.joinBuilder,
    super.$addJoinBuilderToRootComposer,
    super.$removeJoinBuilderFromRootComposer,
  });
  ColumnOrderings<int> get id => $composableBuilder(
    column: $table.id,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get marker => $composableBuilder(
    column: $table.marker,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get createdAtUtc => $composableBuilder(
    column: $table.createdAtUtc,
    builder: (column) => ColumnOrderings(column),
  );
}

class $$SyntheticMarkersTableAnnotationComposer
    extends Composer<_$StorageSpikeDatabase, $SyntheticMarkersTable> {
  $$SyntheticMarkersTableAnnotationComposer({
    required super.$db,
    required super.$table,
    super.joinBuilder,
    super.$addJoinBuilderToRootComposer,
    super.$removeJoinBuilderFromRootComposer,
  });
  GeneratedColumn<int> get id =>
      $composableBuilder(column: $table.id, builder: (column) => column);

  GeneratedColumn<String> get marker =>
      $composableBuilder(column: $table.marker, builder: (column) => column);

  GeneratedColumn<String> get createdAtUtc => $composableBuilder(
    column: $table.createdAtUtc,
    builder: (column) => column,
  );
}

class $$SyntheticMarkersTableTableManager
    extends
        RootTableManager<
          _$StorageSpikeDatabase,
          $SyntheticMarkersTable,
          SyntheticMarker,
          $$SyntheticMarkersTableFilterComposer,
          $$SyntheticMarkersTableOrderingComposer,
          $$SyntheticMarkersTableAnnotationComposer,
          $$SyntheticMarkersTableCreateCompanionBuilder,
          $$SyntheticMarkersTableUpdateCompanionBuilder,
          (
            SyntheticMarker,
            BaseReferences<
              _$StorageSpikeDatabase,
              $SyntheticMarkersTable,
              SyntheticMarker
            >,
          ),
          SyntheticMarker,
          PrefetchHooks Function()
        > {
  $$SyntheticMarkersTableTableManager(
    _$StorageSpikeDatabase db,
    $SyntheticMarkersTable table,
  ) : super(
        TableManagerState(
          db: db,
          table: table,
          createFilteringComposer: () =>
              $$SyntheticMarkersTableFilterComposer($db: db, $table: table),
          createOrderingComposer: () =>
              $$SyntheticMarkersTableOrderingComposer($db: db, $table: table),
          createComputedFieldComposer: () =>
              $$SyntheticMarkersTableAnnotationComposer($db: db, $table: table),
          updateCompanionCallback:
              ({
                Value<int> id = const Value.absent(),
                Value<String> marker = const Value.absent(),
                Value<String> createdAtUtc = const Value.absent(),
              }) => SyntheticMarkersCompanion(
                id: id,
                marker: marker,
                createdAtUtc: createdAtUtc,
              ),
          createCompanionCallback:
              ({
                Value<int> id = const Value.absent(),
                required String marker,
                required String createdAtUtc,
              }) => SyntheticMarkersCompanion.insert(
                id: id,
                marker: marker,
                createdAtUtc: createdAtUtc,
              ),
          withReferenceMapper: (p0) => p0
              .map((e) => (e.readTable(table), BaseReferences(db, table, e)))
              .toList(),
          prefetchHooksCallback: null,
        ),
      );
}

typedef $$SyntheticMarkersTableProcessedTableManager =
    ProcessedTableManager<
      _$StorageSpikeDatabase,
      $SyntheticMarkersTable,
      SyntheticMarker,
      $$SyntheticMarkersTableFilterComposer,
      $$SyntheticMarkersTableOrderingComposer,
      $$SyntheticMarkersTableAnnotationComposer,
      $$SyntheticMarkersTableCreateCompanionBuilder,
      $$SyntheticMarkersTableUpdateCompanionBuilder,
      (
        SyntheticMarker,
        BaseReferences<
          _$StorageSpikeDatabase,
          $SyntheticMarkersTable,
          SyntheticMarker
        >,
      ),
      SyntheticMarker,
      PrefetchHooks Function()
    >;
typedef $$MigrationEvidenceTableCreateCompanionBuilder =
    MigrationEvidenceCompanion Function({
      Value<int> id,
      required String evidence,
    });
typedef $$MigrationEvidenceTableUpdateCompanionBuilder =
    MigrationEvidenceCompanion Function({
      Value<int> id,
      Value<String> evidence,
    });

class $$MigrationEvidenceTableFilterComposer
    extends Composer<_$StorageSpikeDatabase, $MigrationEvidenceTable> {
  $$MigrationEvidenceTableFilterComposer({
    required super.$db,
    required super.$table,
    super.joinBuilder,
    super.$addJoinBuilderToRootComposer,
    super.$removeJoinBuilderFromRootComposer,
  });
  ColumnFilters<int> get id => $composableBuilder(
    column: $table.id,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get evidence => $composableBuilder(
    column: $table.evidence,
    builder: (column) => ColumnFilters(column),
  );
}

class $$MigrationEvidenceTableOrderingComposer
    extends Composer<_$StorageSpikeDatabase, $MigrationEvidenceTable> {
  $$MigrationEvidenceTableOrderingComposer({
    required super.$db,
    required super.$table,
    super.joinBuilder,
    super.$addJoinBuilderToRootComposer,
    super.$removeJoinBuilderFromRootComposer,
  });
  ColumnOrderings<int> get id => $composableBuilder(
    column: $table.id,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get evidence => $composableBuilder(
    column: $table.evidence,
    builder: (column) => ColumnOrderings(column),
  );
}

class $$MigrationEvidenceTableAnnotationComposer
    extends Composer<_$StorageSpikeDatabase, $MigrationEvidenceTable> {
  $$MigrationEvidenceTableAnnotationComposer({
    required super.$db,
    required super.$table,
    super.joinBuilder,
    super.$addJoinBuilderToRootComposer,
    super.$removeJoinBuilderFromRootComposer,
  });
  GeneratedColumn<int> get id =>
      $composableBuilder(column: $table.id, builder: (column) => column);

  GeneratedColumn<String> get evidence =>
      $composableBuilder(column: $table.evidence, builder: (column) => column);
}

class $$MigrationEvidenceTableTableManager
    extends
        RootTableManager<
          _$StorageSpikeDatabase,
          $MigrationEvidenceTable,
          MigrationEvidenceData,
          $$MigrationEvidenceTableFilterComposer,
          $$MigrationEvidenceTableOrderingComposer,
          $$MigrationEvidenceTableAnnotationComposer,
          $$MigrationEvidenceTableCreateCompanionBuilder,
          $$MigrationEvidenceTableUpdateCompanionBuilder,
          (
            MigrationEvidenceData,
            BaseReferences<
              _$StorageSpikeDatabase,
              $MigrationEvidenceTable,
              MigrationEvidenceData
            >,
          ),
          MigrationEvidenceData,
          PrefetchHooks Function()
        > {
  $$MigrationEvidenceTableTableManager(
    _$StorageSpikeDatabase db,
    $MigrationEvidenceTable table,
  ) : super(
        TableManagerState(
          db: db,
          table: table,
          createFilteringComposer: () =>
              $$MigrationEvidenceTableFilterComposer($db: db, $table: table),
          createOrderingComposer: () =>
              $$MigrationEvidenceTableOrderingComposer($db: db, $table: table),
          createComputedFieldComposer: () =>
              $$MigrationEvidenceTableAnnotationComposer(
                $db: db,
                $table: table,
              ),
          updateCompanionCallback:
              ({
                Value<int> id = const Value.absent(),
                Value<String> evidence = const Value.absent(),
              }) => MigrationEvidenceCompanion(id: id, evidence: evidence),
          createCompanionCallback:
              ({
                Value<int> id = const Value.absent(),
                required String evidence,
              }) =>
                  MigrationEvidenceCompanion.insert(id: id, evidence: evidence),
          withReferenceMapper: (p0) => p0
              .map((e) => (e.readTable(table), BaseReferences(db, table, e)))
              .toList(),
          prefetchHooksCallback: null,
        ),
      );
}

typedef $$MigrationEvidenceTableProcessedTableManager =
    ProcessedTableManager<
      _$StorageSpikeDatabase,
      $MigrationEvidenceTable,
      MigrationEvidenceData,
      $$MigrationEvidenceTableFilterComposer,
      $$MigrationEvidenceTableOrderingComposer,
      $$MigrationEvidenceTableAnnotationComposer,
      $$MigrationEvidenceTableCreateCompanionBuilder,
      $$MigrationEvidenceTableUpdateCompanionBuilder,
      (
        MigrationEvidenceData,
        BaseReferences<
          _$StorageSpikeDatabase,
          $MigrationEvidenceTable,
          MigrationEvidenceData
        >,
      ),
      MigrationEvidenceData,
      PrefetchHooks Function()
    >;

class $StorageSpikeDatabaseManager {
  final _$StorageSpikeDatabase _db;
  $StorageSpikeDatabaseManager(this._db);
  $$SyntheticMarkersTableTableManager get syntheticMarkers =>
      $$SyntheticMarkersTableTableManager(_db, _db.syntheticMarkers);
  $$MigrationEvidenceTableTableManager get migrationEvidence =>
      $$MigrationEvidenceTableTableManager(_db, _db.migrationEvidence);
}
