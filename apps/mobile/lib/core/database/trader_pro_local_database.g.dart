// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'trader_pro_local_database.dart';

// ignore_for_file: type=lint
class $LocalReceivingSessionsTable extends LocalReceivingSessions
    with TableInfo<$LocalReceivingSessionsTable, LocalReceivingSessionRow> {
  @override
  final GeneratedDatabase attachedDatabase;
  final String? _alias;
  $LocalReceivingSessionsTable(this.attachedDatabase, [this._alias]);
  static const VerificationMeta _idMeta = const VerificationMeta('id');
  @override
  late final GeneratedColumn<String> id = GeneratedColumn<String>(
    'id',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
  );
  static const VerificationMeta _cloudIdMeta = const VerificationMeta(
    'cloudId',
  );
  @override
  late final GeneratedColumn<String> cloudId = GeneratedColumn<String>(
    'cloud_id',
    aliasedName,
    true,
    type: DriftSqlType.string,
    requiredDuringInsert: false,
  );
  static const VerificationMeta _temporaryReferenceMeta =
      const VerificationMeta('temporaryReference');
  @override
  late final GeneratedColumn<String> temporaryReference =
      GeneratedColumn<String>(
        'temporary_reference',
        aliasedName,
        false,
        type: DriftSqlType.string,
        requiredDuringInsert: true,
        defaultConstraints: GeneratedColumn.constraintIsAlways('UNIQUE'),
      );
  static const VerificationMeta _localStatusMeta = const VerificationMeta(
    'localStatus',
  );
  @override
  late final GeneratedColumn<String> localStatus = GeneratedColumn<String>(
    'local_status',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
    $customConstraints:
        'NOT NULL CHECK (local_status IN (\'Open\', \'Closed\'))',
  );
  static const VerificationMeta _cloudStatusMeta = const VerificationMeta(
    'cloudStatus',
  );
  @override
  late final GeneratedColumn<String> cloudStatus = GeneratedColumn<String>(
    'cloud_status',
    aliasedName,
    true,
    type: DriftSqlType.string,
    requiredDuringInsert: false,
  );
  static const VerificationMeta _localVersionMeta = const VerificationMeta(
    'localVersion',
  );
  @override
  late final GeneratedColumn<int> localVersion = GeneratedColumn<int>(
    'local_version',
    aliasedName,
    false,
    type: DriftSqlType.int,
    requiredDuringInsert: true,
    $customConstraints: 'NOT NULL CHECK (local_version >= 1)',
  );
  static const VerificationMeta _cloudVersionMeta = const VerificationMeta(
    'cloudVersion',
  );
  @override
  late final GeneratedColumn<int> cloudVersion = GeneratedColumn<int>(
    'cloud_version',
    aliasedName,
    true,
    type: DriftSqlType.int,
    requiredDuringInsert: false,
  );
  static const VerificationMeta _nextLocalSequenceMeta = const VerificationMeta(
    'nextLocalSequence',
  );
  @override
  late final GeneratedColumn<int> nextLocalSequence = GeneratedColumn<int>(
    'next_local_sequence',
    aliasedName,
    false,
    type: DriftSqlType.int,
    requiredDuringInsert: true,
    $customConstraints: 'NOT NULL CHECK (next_local_sequence >= 1)',
  );
  static const VerificationMeta _activeEntryCountMeta = const VerificationMeta(
    'activeEntryCount',
  );
  @override
  late final GeneratedColumn<int> activeEntryCount = GeneratedColumn<int>(
    'active_entry_count',
    aliasedName,
    false,
    type: DriftSqlType.int,
    requiredDuringInsert: true,
    $customConstraints: 'NOT NULL CHECK (active_entry_count >= 0)',
  );
  static const VerificationMeta _processedTotalWeightKgMeta =
      const VerificationMeta('processedTotalWeightKg');
  @override
  late final GeneratedColumn<String> processedTotalWeightKg =
      GeneratedColumn<String>(
        'processed_total_weight_kg',
        aliasedName,
        false,
        type: DriftSqlType.string,
        requiredDuringInsert: true,
      );
  static const VerificationMeta _createdAtDeviceUtcMeta =
      const VerificationMeta('createdAtDeviceUtc');
  @override
  late final GeneratedColumn<String> createdAtDeviceUtc =
      GeneratedColumn<String>(
        'created_at_device_utc',
        aliasedName,
        false,
        type: DriftSqlType.string,
        requiredDuringInsert: true,
      );
  static const VerificationMeta _updatedAtDeviceUtcMeta =
      const VerificationMeta('updatedAtDeviceUtc');
  @override
  late final GeneratedColumn<String> updatedAtDeviceUtc =
      GeneratedColumn<String>(
        'updated_at_device_utc',
        aliasedName,
        false,
        type: DriftSqlType.string,
        requiredDuringInsert: true,
      );
  static const VerificationMeta _lastCloudSyncAtUtcMeta =
      const VerificationMeta('lastCloudSyncAtUtc');
  @override
  late final GeneratedColumn<String> lastCloudSyncAtUtc =
      GeneratedColumn<String>(
        'last_cloud_sync_at_utc',
        aliasedName,
        true,
        type: DriftSqlType.string,
        requiredDuringInsert: false,
      );
  @override
  List<GeneratedColumn> get $columns => [
    id,
    cloudId,
    temporaryReference,
    localStatus,
    cloudStatus,
    localVersion,
    cloudVersion,
    nextLocalSequence,
    activeEntryCount,
    processedTotalWeightKg,
    createdAtDeviceUtc,
    updatedAtDeviceUtc,
    lastCloudSyncAtUtc,
  ];
  @override
  String get aliasedName => _alias ?? actualTableName;
  @override
  String get actualTableName => $name;
  static const String $name = 'local_receiving_sessions';
  @override
  VerificationContext validateIntegrity(
    Insertable<LocalReceivingSessionRow> instance, {
    bool isInserting = false,
  }) {
    final context = VerificationContext();
    final data = instance.toColumns(true);
    if (data.containsKey('id')) {
      context.handle(_idMeta, id.isAcceptableOrUnknown(data['id']!, _idMeta));
    } else if (isInserting) {
      context.missing(_idMeta);
    }
    if (data.containsKey('cloud_id')) {
      context.handle(
        _cloudIdMeta,
        cloudId.isAcceptableOrUnknown(data['cloud_id']!, _cloudIdMeta),
      );
    }
    if (data.containsKey('temporary_reference')) {
      context.handle(
        _temporaryReferenceMeta,
        temporaryReference.isAcceptableOrUnknown(
          data['temporary_reference']!,
          _temporaryReferenceMeta,
        ),
      );
    } else if (isInserting) {
      context.missing(_temporaryReferenceMeta);
    }
    if (data.containsKey('local_status')) {
      context.handle(
        _localStatusMeta,
        localStatus.isAcceptableOrUnknown(
          data['local_status']!,
          _localStatusMeta,
        ),
      );
    } else if (isInserting) {
      context.missing(_localStatusMeta);
    }
    if (data.containsKey('cloud_status')) {
      context.handle(
        _cloudStatusMeta,
        cloudStatus.isAcceptableOrUnknown(
          data['cloud_status']!,
          _cloudStatusMeta,
        ),
      );
    }
    if (data.containsKey('local_version')) {
      context.handle(
        _localVersionMeta,
        localVersion.isAcceptableOrUnknown(
          data['local_version']!,
          _localVersionMeta,
        ),
      );
    } else if (isInserting) {
      context.missing(_localVersionMeta);
    }
    if (data.containsKey('cloud_version')) {
      context.handle(
        _cloudVersionMeta,
        cloudVersion.isAcceptableOrUnknown(
          data['cloud_version']!,
          _cloudVersionMeta,
        ),
      );
    }
    if (data.containsKey('next_local_sequence')) {
      context.handle(
        _nextLocalSequenceMeta,
        nextLocalSequence.isAcceptableOrUnknown(
          data['next_local_sequence']!,
          _nextLocalSequenceMeta,
        ),
      );
    } else if (isInserting) {
      context.missing(_nextLocalSequenceMeta);
    }
    if (data.containsKey('active_entry_count')) {
      context.handle(
        _activeEntryCountMeta,
        activeEntryCount.isAcceptableOrUnknown(
          data['active_entry_count']!,
          _activeEntryCountMeta,
        ),
      );
    } else if (isInserting) {
      context.missing(_activeEntryCountMeta);
    }
    if (data.containsKey('processed_total_weight_kg')) {
      context.handle(
        _processedTotalWeightKgMeta,
        processedTotalWeightKg.isAcceptableOrUnknown(
          data['processed_total_weight_kg']!,
          _processedTotalWeightKgMeta,
        ),
      );
    } else if (isInserting) {
      context.missing(_processedTotalWeightKgMeta);
    }
    if (data.containsKey('created_at_device_utc')) {
      context.handle(
        _createdAtDeviceUtcMeta,
        createdAtDeviceUtc.isAcceptableOrUnknown(
          data['created_at_device_utc']!,
          _createdAtDeviceUtcMeta,
        ),
      );
    } else if (isInserting) {
      context.missing(_createdAtDeviceUtcMeta);
    }
    if (data.containsKey('updated_at_device_utc')) {
      context.handle(
        _updatedAtDeviceUtcMeta,
        updatedAtDeviceUtc.isAcceptableOrUnknown(
          data['updated_at_device_utc']!,
          _updatedAtDeviceUtcMeta,
        ),
      );
    } else if (isInserting) {
      context.missing(_updatedAtDeviceUtcMeta);
    }
    if (data.containsKey('last_cloud_sync_at_utc')) {
      context.handle(
        _lastCloudSyncAtUtcMeta,
        lastCloudSyncAtUtc.isAcceptableOrUnknown(
          data['last_cloud_sync_at_utc']!,
          _lastCloudSyncAtUtcMeta,
        ),
      );
    }
    return context;
  }

  @override
  Set<GeneratedColumn> get $primaryKey => {id};
  @override
  LocalReceivingSessionRow map(
    Map<String, dynamic> data, {
    String? tablePrefix,
  }) {
    final effectivePrefix = tablePrefix != null ? '$tablePrefix.' : '';
    return LocalReceivingSessionRow(
      id: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}id'],
      )!,
      cloudId: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}cloud_id'],
      ),
      temporaryReference: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}temporary_reference'],
      )!,
      localStatus: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}local_status'],
      )!,
      cloudStatus: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}cloud_status'],
      ),
      localVersion: attachedDatabase.typeMapping.read(
        DriftSqlType.int,
        data['${effectivePrefix}local_version'],
      )!,
      cloudVersion: attachedDatabase.typeMapping.read(
        DriftSqlType.int,
        data['${effectivePrefix}cloud_version'],
      ),
      nextLocalSequence: attachedDatabase.typeMapping.read(
        DriftSqlType.int,
        data['${effectivePrefix}next_local_sequence'],
      )!,
      activeEntryCount: attachedDatabase.typeMapping.read(
        DriftSqlType.int,
        data['${effectivePrefix}active_entry_count'],
      )!,
      processedTotalWeightKg: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}processed_total_weight_kg'],
      )!,
      createdAtDeviceUtc: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}created_at_device_utc'],
      )!,
      updatedAtDeviceUtc: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}updated_at_device_utc'],
      )!,
      lastCloudSyncAtUtc: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}last_cloud_sync_at_utc'],
      ),
    );
  }

  @override
  $LocalReceivingSessionsTable createAlias(String alias) {
    return $LocalReceivingSessionsTable(attachedDatabase, alias);
  }
}

class LocalReceivingSessionRow extends DataClass
    implements Insertable<LocalReceivingSessionRow> {
  final String id;
  final String? cloudId;
  final String temporaryReference;
  final String localStatus;
  final String? cloudStatus;
  final int localVersion;
  final int? cloudVersion;
  final int nextLocalSequence;
  final int activeEntryCount;
  final String processedTotalWeightKg;
  final String createdAtDeviceUtc;
  final String updatedAtDeviceUtc;
  final String? lastCloudSyncAtUtc;
  const LocalReceivingSessionRow({
    required this.id,
    this.cloudId,
    required this.temporaryReference,
    required this.localStatus,
    this.cloudStatus,
    required this.localVersion,
    this.cloudVersion,
    required this.nextLocalSequence,
    required this.activeEntryCount,
    required this.processedTotalWeightKg,
    required this.createdAtDeviceUtc,
    required this.updatedAtDeviceUtc,
    this.lastCloudSyncAtUtc,
  });
  @override
  Map<String, Expression> toColumns(bool nullToAbsent) {
    final map = <String, Expression>{};
    map['id'] = Variable<String>(id);
    if (!nullToAbsent || cloudId != null) {
      map['cloud_id'] = Variable<String>(cloudId);
    }
    map['temporary_reference'] = Variable<String>(temporaryReference);
    map['local_status'] = Variable<String>(localStatus);
    if (!nullToAbsent || cloudStatus != null) {
      map['cloud_status'] = Variable<String>(cloudStatus);
    }
    map['local_version'] = Variable<int>(localVersion);
    if (!nullToAbsent || cloudVersion != null) {
      map['cloud_version'] = Variable<int>(cloudVersion);
    }
    map['next_local_sequence'] = Variable<int>(nextLocalSequence);
    map['active_entry_count'] = Variable<int>(activeEntryCount);
    map['processed_total_weight_kg'] = Variable<String>(processedTotalWeightKg);
    map['created_at_device_utc'] = Variable<String>(createdAtDeviceUtc);
    map['updated_at_device_utc'] = Variable<String>(updatedAtDeviceUtc);
    if (!nullToAbsent || lastCloudSyncAtUtc != null) {
      map['last_cloud_sync_at_utc'] = Variable<String>(lastCloudSyncAtUtc);
    }
    return map;
  }

  LocalReceivingSessionsCompanion toCompanion(bool nullToAbsent) {
    return LocalReceivingSessionsCompanion(
      id: Value(id),
      cloudId: cloudId == null && nullToAbsent
          ? const Value.absent()
          : Value(cloudId),
      temporaryReference: Value(temporaryReference),
      localStatus: Value(localStatus),
      cloudStatus: cloudStatus == null && nullToAbsent
          ? const Value.absent()
          : Value(cloudStatus),
      localVersion: Value(localVersion),
      cloudVersion: cloudVersion == null && nullToAbsent
          ? const Value.absent()
          : Value(cloudVersion),
      nextLocalSequence: Value(nextLocalSequence),
      activeEntryCount: Value(activeEntryCount),
      processedTotalWeightKg: Value(processedTotalWeightKg),
      createdAtDeviceUtc: Value(createdAtDeviceUtc),
      updatedAtDeviceUtc: Value(updatedAtDeviceUtc),
      lastCloudSyncAtUtc: lastCloudSyncAtUtc == null && nullToAbsent
          ? const Value.absent()
          : Value(lastCloudSyncAtUtc),
    );
  }

  factory LocalReceivingSessionRow.fromJson(
    Map<String, dynamic> json, {
    ValueSerializer? serializer,
  }) {
    serializer ??= driftRuntimeOptions.defaultSerializer;
    return LocalReceivingSessionRow(
      id: serializer.fromJson<String>(json['id']),
      cloudId: serializer.fromJson<String?>(json['cloudId']),
      temporaryReference: serializer.fromJson<String>(
        json['temporaryReference'],
      ),
      localStatus: serializer.fromJson<String>(json['localStatus']),
      cloudStatus: serializer.fromJson<String?>(json['cloudStatus']),
      localVersion: serializer.fromJson<int>(json['localVersion']),
      cloudVersion: serializer.fromJson<int?>(json['cloudVersion']),
      nextLocalSequence: serializer.fromJson<int>(json['nextLocalSequence']),
      activeEntryCount: serializer.fromJson<int>(json['activeEntryCount']),
      processedTotalWeightKg: serializer.fromJson<String>(
        json['processedTotalWeightKg'],
      ),
      createdAtDeviceUtc: serializer.fromJson<String>(
        json['createdAtDeviceUtc'],
      ),
      updatedAtDeviceUtc: serializer.fromJson<String>(
        json['updatedAtDeviceUtc'],
      ),
      lastCloudSyncAtUtc: serializer.fromJson<String?>(
        json['lastCloudSyncAtUtc'],
      ),
    );
  }
  @override
  Map<String, dynamic> toJson({ValueSerializer? serializer}) {
    serializer ??= driftRuntimeOptions.defaultSerializer;
    return <String, dynamic>{
      'id': serializer.toJson<String>(id),
      'cloudId': serializer.toJson<String?>(cloudId),
      'temporaryReference': serializer.toJson<String>(temporaryReference),
      'localStatus': serializer.toJson<String>(localStatus),
      'cloudStatus': serializer.toJson<String?>(cloudStatus),
      'localVersion': serializer.toJson<int>(localVersion),
      'cloudVersion': serializer.toJson<int?>(cloudVersion),
      'nextLocalSequence': serializer.toJson<int>(nextLocalSequence),
      'activeEntryCount': serializer.toJson<int>(activeEntryCount),
      'processedTotalWeightKg': serializer.toJson<String>(
        processedTotalWeightKg,
      ),
      'createdAtDeviceUtc': serializer.toJson<String>(createdAtDeviceUtc),
      'updatedAtDeviceUtc': serializer.toJson<String>(updatedAtDeviceUtc),
      'lastCloudSyncAtUtc': serializer.toJson<String?>(lastCloudSyncAtUtc),
    };
  }

  LocalReceivingSessionRow copyWith({
    String? id,
    Value<String?> cloudId = const Value.absent(),
    String? temporaryReference,
    String? localStatus,
    Value<String?> cloudStatus = const Value.absent(),
    int? localVersion,
    Value<int?> cloudVersion = const Value.absent(),
    int? nextLocalSequence,
    int? activeEntryCount,
    String? processedTotalWeightKg,
    String? createdAtDeviceUtc,
    String? updatedAtDeviceUtc,
    Value<String?> lastCloudSyncAtUtc = const Value.absent(),
  }) => LocalReceivingSessionRow(
    id: id ?? this.id,
    cloudId: cloudId.present ? cloudId.value : this.cloudId,
    temporaryReference: temporaryReference ?? this.temporaryReference,
    localStatus: localStatus ?? this.localStatus,
    cloudStatus: cloudStatus.present ? cloudStatus.value : this.cloudStatus,
    localVersion: localVersion ?? this.localVersion,
    cloudVersion: cloudVersion.present ? cloudVersion.value : this.cloudVersion,
    nextLocalSequence: nextLocalSequence ?? this.nextLocalSequence,
    activeEntryCount: activeEntryCount ?? this.activeEntryCount,
    processedTotalWeightKg:
        processedTotalWeightKg ?? this.processedTotalWeightKg,
    createdAtDeviceUtc: createdAtDeviceUtc ?? this.createdAtDeviceUtc,
    updatedAtDeviceUtc: updatedAtDeviceUtc ?? this.updatedAtDeviceUtc,
    lastCloudSyncAtUtc: lastCloudSyncAtUtc.present
        ? lastCloudSyncAtUtc.value
        : this.lastCloudSyncAtUtc,
  );
  LocalReceivingSessionRow copyWithCompanion(
    LocalReceivingSessionsCompanion data,
  ) {
    return LocalReceivingSessionRow(
      id: data.id.present ? data.id.value : this.id,
      cloudId: data.cloudId.present ? data.cloudId.value : this.cloudId,
      temporaryReference: data.temporaryReference.present
          ? data.temporaryReference.value
          : this.temporaryReference,
      localStatus: data.localStatus.present
          ? data.localStatus.value
          : this.localStatus,
      cloudStatus: data.cloudStatus.present
          ? data.cloudStatus.value
          : this.cloudStatus,
      localVersion: data.localVersion.present
          ? data.localVersion.value
          : this.localVersion,
      cloudVersion: data.cloudVersion.present
          ? data.cloudVersion.value
          : this.cloudVersion,
      nextLocalSequence: data.nextLocalSequence.present
          ? data.nextLocalSequence.value
          : this.nextLocalSequence,
      activeEntryCount: data.activeEntryCount.present
          ? data.activeEntryCount.value
          : this.activeEntryCount,
      processedTotalWeightKg: data.processedTotalWeightKg.present
          ? data.processedTotalWeightKg.value
          : this.processedTotalWeightKg,
      createdAtDeviceUtc: data.createdAtDeviceUtc.present
          ? data.createdAtDeviceUtc.value
          : this.createdAtDeviceUtc,
      updatedAtDeviceUtc: data.updatedAtDeviceUtc.present
          ? data.updatedAtDeviceUtc.value
          : this.updatedAtDeviceUtc,
      lastCloudSyncAtUtc: data.lastCloudSyncAtUtc.present
          ? data.lastCloudSyncAtUtc.value
          : this.lastCloudSyncAtUtc,
    );
  }

  @override
  String toString() {
    return (StringBuffer('LocalReceivingSessionRow(')
          ..write('id: $id, ')
          ..write('cloudId: $cloudId, ')
          ..write('temporaryReference: $temporaryReference, ')
          ..write('localStatus: $localStatus, ')
          ..write('cloudStatus: $cloudStatus, ')
          ..write('localVersion: $localVersion, ')
          ..write('cloudVersion: $cloudVersion, ')
          ..write('nextLocalSequence: $nextLocalSequence, ')
          ..write('activeEntryCount: $activeEntryCount, ')
          ..write('processedTotalWeightKg: $processedTotalWeightKg, ')
          ..write('createdAtDeviceUtc: $createdAtDeviceUtc, ')
          ..write('updatedAtDeviceUtc: $updatedAtDeviceUtc, ')
          ..write('lastCloudSyncAtUtc: $lastCloudSyncAtUtc')
          ..write(')'))
        .toString();
  }

  @override
  int get hashCode => Object.hash(
    id,
    cloudId,
    temporaryReference,
    localStatus,
    cloudStatus,
    localVersion,
    cloudVersion,
    nextLocalSequence,
    activeEntryCount,
    processedTotalWeightKg,
    createdAtDeviceUtc,
    updatedAtDeviceUtc,
    lastCloudSyncAtUtc,
  );
  @override
  bool operator ==(Object other) =>
      identical(this, other) ||
      (other is LocalReceivingSessionRow &&
          other.id == this.id &&
          other.cloudId == this.cloudId &&
          other.temporaryReference == this.temporaryReference &&
          other.localStatus == this.localStatus &&
          other.cloudStatus == this.cloudStatus &&
          other.localVersion == this.localVersion &&
          other.cloudVersion == this.cloudVersion &&
          other.nextLocalSequence == this.nextLocalSequence &&
          other.activeEntryCount == this.activeEntryCount &&
          other.processedTotalWeightKg == this.processedTotalWeightKg &&
          other.createdAtDeviceUtc == this.createdAtDeviceUtc &&
          other.updatedAtDeviceUtc == this.updatedAtDeviceUtc &&
          other.lastCloudSyncAtUtc == this.lastCloudSyncAtUtc);
}

class LocalReceivingSessionsCompanion
    extends UpdateCompanion<LocalReceivingSessionRow> {
  final Value<String> id;
  final Value<String?> cloudId;
  final Value<String> temporaryReference;
  final Value<String> localStatus;
  final Value<String?> cloudStatus;
  final Value<int> localVersion;
  final Value<int?> cloudVersion;
  final Value<int> nextLocalSequence;
  final Value<int> activeEntryCount;
  final Value<String> processedTotalWeightKg;
  final Value<String> createdAtDeviceUtc;
  final Value<String> updatedAtDeviceUtc;
  final Value<String?> lastCloudSyncAtUtc;
  final Value<int> rowid;
  const LocalReceivingSessionsCompanion({
    this.id = const Value.absent(),
    this.cloudId = const Value.absent(),
    this.temporaryReference = const Value.absent(),
    this.localStatus = const Value.absent(),
    this.cloudStatus = const Value.absent(),
    this.localVersion = const Value.absent(),
    this.cloudVersion = const Value.absent(),
    this.nextLocalSequence = const Value.absent(),
    this.activeEntryCount = const Value.absent(),
    this.processedTotalWeightKg = const Value.absent(),
    this.createdAtDeviceUtc = const Value.absent(),
    this.updatedAtDeviceUtc = const Value.absent(),
    this.lastCloudSyncAtUtc = const Value.absent(),
    this.rowid = const Value.absent(),
  });
  LocalReceivingSessionsCompanion.insert({
    required String id,
    this.cloudId = const Value.absent(),
    required String temporaryReference,
    required String localStatus,
    this.cloudStatus = const Value.absent(),
    required int localVersion,
    this.cloudVersion = const Value.absent(),
    required int nextLocalSequence,
    required int activeEntryCount,
    required String processedTotalWeightKg,
    required String createdAtDeviceUtc,
    required String updatedAtDeviceUtc,
    this.lastCloudSyncAtUtc = const Value.absent(),
    this.rowid = const Value.absent(),
  }) : id = Value(id),
       temporaryReference = Value(temporaryReference),
       localStatus = Value(localStatus),
       localVersion = Value(localVersion),
       nextLocalSequence = Value(nextLocalSequence),
       activeEntryCount = Value(activeEntryCount),
       processedTotalWeightKg = Value(processedTotalWeightKg),
       createdAtDeviceUtc = Value(createdAtDeviceUtc),
       updatedAtDeviceUtc = Value(updatedAtDeviceUtc);
  static Insertable<LocalReceivingSessionRow> custom({
    Expression<String>? id,
    Expression<String>? cloudId,
    Expression<String>? temporaryReference,
    Expression<String>? localStatus,
    Expression<String>? cloudStatus,
    Expression<int>? localVersion,
    Expression<int>? cloudVersion,
    Expression<int>? nextLocalSequence,
    Expression<int>? activeEntryCount,
    Expression<String>? processedTotalWeightKg,
    Expression<String>? createdAtDeviceUtc,
    Expression<String>? updatedAtDeviceUtc,
    Expression<String>? lastCloudSyncAtUtc,
    Expression<int>? rowid,
  }) {
    return RawValuesInsertable({
      if (id != null) 'id': id,
      if (cloudId != null) 'cloud_id': cloudId,
      if (temporaryReference != null) 'temporary_reference': temporaryReference,
      if (localStatus != null) 'local_status': localStatus,
      if (cloudStatus != null) 'cloud_status': cloudStatus,
      if (localVersion != null) 'local_version': localVersion,
      if (cloudVersion != null) 'cloud_version': cloudVersion,
      if (nextLocalSequence != null) 'next_local_sequence': nextLocalSequence,
      if (activeEntryCount != null) 'active_entry_count': activeEntryCount,
      if (processedTotalWeightKg != null)
        'processed_total_weight_kg': processedTotalWeightKg,
      if (createdAtDeviceUtc != null)
        'created_at_device_utc': createdAtDeviceUtc,
      if (updatedAtDeviceUtc != null)
        'updated_at_device_utc': updatedAtDeviceUtc,
      if (lastCloudSyncAtUtc != null)
        'last_cloud_sync_at_utc': lastCloudSyncAtUtc,
      if (rowid != null) 'rowid': rowid,
    });
  }

  LocalReceivingSessionsCompanion copyWith({
    Value<String>? id,
    Value<String?>? cloudId,
    Value<String>? temporaryReference,
    Value<String>? localStatus,
    Value<String?>? cloudStatus,
    Value<int>? localVersion,
    Value<int?>? cloudVersion,
    Value<int>? nextLocalSequence,
    Value<int>? activeEntryCount,
    Value<String>? processedTotalWeightKg,
    Value<String>? createdAtDeviceUtc,
    Value<String>? updatedAtDeviceUtc,
    Value<String?>? lastCloudSyncAtUtc,
    Value<int>? rowid,
  }) {
    return LocalReceivingSessionsCompanion(
      id: id ?? this.id,
      cloudId: cloudId ?? this.cloudId,
      temporaryReference: temporaryReference ?? this.temporaryReference,
      localStatus: localStatus ?? this.localStatus,
      cloudStatus: cloudStatus ?? this.cloudStatus,
      localVersion: localVersion ?? this.localVersion,
      cloudVersion: cloudVersion ?? this.cloudVersion,
      nextLocalSequence: nextLocalSequence ?? this.nextLocalSequence,
      activeEntryCount: activeEntryCount ?? this.activeEntryCount,
      processedTotalWeightKg:
          processedTotalWeightKg ?? this.processedTotalWeightKg,
      createdAtDeviceUtc: createdAtDeviceUtc ?? this.createdAtDeviceUtc,
      updatedAtDeviceUtc: updatedAtDeviceUtc ?? this.updatedAtDeviceUtc,
      lastCloudSyncAtUtc: lastCloudSyncAtUtc ?? this.lastCloudSyncAtUtc,
      rowid: rowid ?? this.rowid,
    );
  }

  @override
  Map<String, Expression> toColumns(bool nullToAbsent) {
    final map = <String, Expression>{};
    if (id.present) {
      map['id'] = Variable<String>(id.value);
    }
    if (cloudId.present) {
      map['cloud_id'] = Variable<String>(cloudId.value);
    }
    if (temporaryReference.present) {
      map['temporary_reference'] = Variable<String>(temporaryReference.value);
    }
    if (localStatus.present) {
      map['local_status'] = Variable<String>(localStatus.value);
    }
    if (cloudStatus.present) {
      map['cloud_status'] = Variable<String>(cloudStatus.value);
    }
    if (localVersion.present) {
      map['local_version'] = Variable<int>(localVersion.value);
    }
    if (cloudVersion.present) {
      map['cloud_version'] = Variable<int>(cloudVersion.value);
    }
    if (nextLocalSequence.present) {
      map['next_local_sequence'] = Variable<int>(nextLocalSequence.value);
    }
    if (activeEntryCount.present) {
      map['active_entry_count'] = Variable<int>(activeEntryCount.value);
    }
    if (processedTotalWeightKg.present) {
      map['processed_total_weight_kg'] = Variable<String>(
        processedTotalWeightKg.value,
      );
    }
    if (createdAtDeviceUtc.present) {
      map['created_at_device_utc'] = Variable<String>(createdAtDeviceUtc.value);
    }
    if (updatedAtDeviceUtc.present) {
      map['updated_at_device_utc'] = Variable<String>(updatedAtDeviceUtc.value);
    }
    if (lastCloudSyncAtUtc.present) {
      map['last_cloud_sync_at_utc'] = Variable<String>(
        lastCloudSyncAtUtc.value,
      );
    }
    if (rowid.present) {
      map['rowid'] = Variable<int>(rowid.value);
    }
    return map;
  }

  @override
  String toString() {
    return (StringBuffer('LocalReceivingSessionsCompanion(')
          ..write('id: $id, ')
          ..write('cloudId: $cloudId, ')
          ..write('temporaryReference: $temporaryReference, ')
          ..write('localStatus: $localStatus, ')
          ..write('cloudStatus: $cloudStatus, ')
          ..write('localVersion: $localVersion, ')
          ..write('cloudVersion: $cloudVersion, ')
          ..write('nextLocalSequence: $nextLocalSequence, ')
          ..write('activeEntryCount: $activeEntryCount, ')
          ..write('processedTotalWeightKg: $processedTotalWeightKg, ')
          ..write('createdAtDeviceUtc: $createdAtDeviceUtc, ')
          ..write('updatedAtDeviceUtc: $updatedAtDeviceUtc, ')
          ..write('lastCloudSyncAtUtc: $lastCloudSyncAtUtc, ')
          ..write('rowid: $rowid')
          ..write(')'))
        .toString();
  }
}

class $LocalReceivingEntriesTable extends LocalReceivingEntries
    with TableInfo<$LocalReceivingEntriesTable, LocalReceivingEntryRow> {
  @override
  final GeneratedDatabase attachedDatabase;
  final String? _alias;
  $LocalReceivingEntriesTable(this.attachedDatabase, [this._alias]);
  static const VerificationMeta _idMeta = const VerificationMeta('id');
  @override
  late final GeneratedColumn<String> id = GeneratedColumn<String>(
    'id',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
  );
  static const VerificationMeta _receivingSessionIdMeta =
      const VerificationMeta('receivingSessionId');
  @override
  late final GeneratedColumn<String> receivingSessionId =
      GeneratedColumn<String>(
        'receiving_session_id',
        aliasedName,
        false,
        type: DriftSqlType.string,
        requiredDuringInsert: true,
        defaultConstraints: GeneratedColumn.constraintIsAlways(
          'REFERENCES local_receiving_sessions (id) ON DELETE RESTRICT',
        ),
      );
  static const VerificationMeta _operationIdMeta = const VerificationMeta(
    'operationId',
  );
  @override
  late final GeneratedColumn<String> operationId = GeneratedColumn<String>(
    'operation_id',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
    defaultConstraints: GeneratedColumn.constraintIsAlways('UNIQUE'),
  );
  static const VerificationMeta _localSequenceMeta = const VerificationMeta(
    'localSequence',
  );
  @override
  late final GeneratedColumn<int> localSequence = GeneratedColumn<int>(
    'local_sequence',
    aliasedName,
    false,
    type: DriftSqlType.int,
    requiredDuringInsert: true,
    $customConstraints: 'NOT NULL CHECK (local_sequence > 0)',
  );
  static const VerificationMeta _productReferenceMeta = const VerificationMeta(
    'productReference',
  );
  @override
  late final GeneratedColumn<String> productReference = GeneratedColumn<String>(
    'product_reference',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
  );
  static const VerificationMeta _bagTypeReferenceMeta = const VerificationMeta(
    'bagTypeReference',
  );
  @override
  late final GeneratedColumn<String> bagTypeReference = GeneratedColumn<String>(
    'bag_type_reference',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
  );
  static const VerificationMeta _bagCountMeta = const VerificationMeta(
    'bagCount',
  );
  @override
  late final GeneratedColumn<int> bagCount = GeneratedColumn<int>(
    'bag_count',
    aliasedName,
    false,
    type: DriftSqlType.int,
    requiredDuringInsert: true,
    $customConstraints: 'NOT NULL CHECK (bag_count > 0)',
  );
  static const VerificationMeta _rawWeightKgMeta = const VerificationMeta(
    'rawWeightKg',
  );
  @override
  late final GeneratedColumn<String> rawWeightKg = GeneratedColumn<String>(
    'raw_weight_kg',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
  );
  static const VerificationMeta _processedWeightKgMeta = const VerificationMeta(
    'processedWeightKg',
  );
  @override
  late final GeneratedColumn<String> processedWeightKg =
      GeneratedColumn<String>(
        'processed_weight_kg',
        aliasedName,
        false,
        type: DriftSqlType.string,
        requiredDuringInsert: true,
      );
  static const VerificationMeta _displayWeightKgMeta = const VerificationMeta(
    'displayWeightKg',
  );
  @override
  late final GeneratedColumn<String> displayWeightKg = GeneratedColumn<String>(
    'display_weight_kg',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
  );
  static const VerificationMeta _decimalPlacesMeta = const VerificationMeta(
    'decimalPlaces',
  );
  @override
  late final GeneratedColumn<int> decimalPlaces = GeneratedColumn<int>(
    'decimal_places',
    aliasedName,
    false,
    type: DriftSqlType.int,
    requiredDuringInsert: true,
    $customConstraints: 'NOT NULL CHECK (decimal_places IN (1, 2, 3))',
  );
  static const VerificationMeta _processingMethodMeta = const VerificationMeta(
    'processingMethod',
  );
  @override
  late final GeneratedColumn<String> processingMethod = GeneratedColumn<String>(
    'processing_method',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
    $customConstraints:
        'NOT NULL CHECK (processing_method IN (\'Standard\', \'Floor\', \'Ceiling\'))',
  );
  static const VerificationMeta _weightSourceMeta = const VerificationMeta(
    'weightSource',
  );
  @override
  late final GeneratedColumn<String> weightSource = GeneratedColumn<String>(
    'weight_source',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
    $customConstraints:
        'NOT NULL CHECK (weight_source IN (\'ManualSpike\', \'TestScale\'))',
  );
  static const VerificationMeta _entryStatusMeta = const VerificationMeta(
    'entryStatus',
  );
  @override
  late final GeneratedColumn<String> entryStatus = GeneratedColumn<String>(
    'entry_status',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
    $customConstraints:
        'NOT NULL CHECK (entry_status IN (\'Active\', \'Reversed\'))',
  );
  static const VerificationMeta _reversalOfEntryIdMeta = const VerificationMeta(
    'reversalOfEntryId',
  );
  @override
  late final GeneratedColumn<String> reversalOfEntryId =
      GeneratedColumn<String>(
        'reversal_of_entry_id',
        aliasedName,
        true,
        type: DriftSqlType.string,
        requiredDuringInsert: false,
      );
  static const VerificationMeta _capturedAtDeviceUtcMeta =
      const VerificationMeta('capturedAtDeviceUtc');
  @override
  late final GeneratedColumn<String> capturedAtDeviceUtc =
      GeneratedColumn<String>(
        'captured_at_device_utc',
        aliasedName,
        false,
        type: DriftSqlType.string,
        requiredDuringInsert: true,
      );
  static const VerificationMeta _createdAtDeviceUtcMeta =
      const VerificationMeta('createdAtDeviceUtc');
  @override
  late final GeneratedColumn<String> createdAtDeviceUtc =
      GeneratedColumn<String>(
        'created_at_device_utc',
        aliasedName,
        false,
        type: DriftSqlType.string,
        requiredDuringInsert: true,
      );
  @override
  List<GeneratedColumn> get $columns => [
    id,
    receivingSessionId,
    operationId,
    localSequence,
    productReference,
    bagTypeReference,
    bagCount,
    rawWeightKg,
    processedWeightKg,
    displayWeightKg,
    decimalPlaces,
    processingMethod,
    weightSource,
    entryStatus,
    reversalOfEntryId,
    capturedAtDeviceUtc,
    createdAtDeviceUtc,
  ];
  @override
  String get aliasedName => _alias ?? actualTableName;
  @override
  String get actualTableName => $name;
  static const String $name = 'local_receiving_entries';
  @override
  VerificationContext validateIntegrity(
    Insertable<LocalReceivingEntryRow> instance, {
    bool isInserting = false,
  }) {
    final context = VerificationContext();
    final data = instance.toColumns(true);
    if (data.containsKey('id')) {
      context.handle(_idMeta, id.isAcceptableOrUnknown(data['id']!, _idMeta));
    } else if (isInserting) {
      context.missing(_idMeta);
    }
    if (data.containsKey('receiving_session_id')) {
      context.handle(
        _receivingSessionIdMeta,
        receivingSessionId.isAcceptableOrUnknown(
          data['receiving_session_id']!,
          _receivingSessionIdMeta,
        ),
      );
    } else if (isInserting) {
      context.missing(_receivingSessionIdMeta);
    }
    if (data.containsKey('operation_id')) {
      context.handle(
        _operationIdMeta,
        operationId.isAcceptableOrUnknown(
          data['operation_id']!,
          _operationIdMeta,
        ),
      );
    } else if (isInserting) {
      context.missing(_operationIdMeta);
    }
    if (data.containsKey('local_sequence')) {
      context.handle(
        _localSequenceMeta,
        localSequence.isAcceptableOrUnknown(
          data['local_sequence']!,
          _localSequenceMeta,
        ),
      );
    } else if (isInserting) {
      context.missing(_localSequenceMeta);
    }
    if (data.containsKey('product_reference')) {
      context.handle(
        _productReferenceMeta,
        productReference.isAcceptableOrUnknown(
          data['product_reference']!,
          _productReferenceMeta,
        ),
      );
    } else if (isInserting) {
      context.missing(_productReferenceMeta);
    }
    if (data.containsKey('bag_type_reference')) {
      context.handle(
        _bagTypeReferenceMeta,
        bagTypeReference.isAcceptableOrUnknown(
          data['bag_type_reference']!,
          _bagTypeReferenceMeta,
        ),
      );
    } else if (isInserting) {
      context.missing(_bagTypeReferenceMeta);
    }
    if (data.containsKey('bag_count')) {
      context.handle(
        _bagCountMeta,
        bagCount.isAcceptableOrUnknown(data['bag_count']!, _bagCountMeta),
      );
    } else if (isInserting) {
      context.missing(_bagCountMeta);
    }
    if (data.containsKey('raw_weight_kg')) {
      context.handle(
        _rawWeightKgMeta,
        rawWeightKg.isAcceptableOrUnknown(
          data['raw_weight_kg']!,
          _rawWeightKgMeta,
        ),
      );
    } else if (isInserting) {
      context.missing(_rawWeightKgMeta);
    }
    if (data.containsKey('processed_weight_kg')) {
      context.handle(
        _processedWeightKgMeta,
        processedWeightKg.isAcceptableOrUnknown(
          data['processed_weight_kg']!,
          _processedWeightKgMeta,
        ),
      );
    } else if (isInserting) {
      context.missing(_processedWeightKgMeta);
    }
    if (data.containsKey('display_weight_kg')) {
      context.handle(
        _displayWeightKgMeta,
        displayWeightKg.isAcceptableOrUnknown(
          data['display_weight_kg']!,
          _displayWeightKgMeta,
        ),
      );
    } else if (isInserting) {
      context.missing(_displayWeightKgMeta);
    }
    if (data.containsKey('decimal_places')) {
      context.handle(
        _decimalPlacesMeta,
        decimalPlaces.isAcceptableOrUnknown(
          data['decimal_places']!,
          _decimalPlacesMeta,
        ),
      );
    } else if (isInserting) {
      context.missing(_decimalPlacesMeta);
    }
    if (data.containsKey('processing_method')) {
      context.handle(
        _processingMethodMeta,
        processingMethod.isAcceptableOrUnknown(
          data['processing_method']!,
          _processingMethodMeta,
        ),
      );
    } else if (isInserting) {
      context.missing(_processingMethodMeta);
    }
    if (data.containsKey('weight_source')) {
      context.handle(
        _weightSourceMeta,
        weightSource.isAcceptableOrUnknown(
          data['weight_source']!,
          _weightSourceMeta,
        ),
      );
    } else if (isInserting) {
      context.missing(_weightSourceMeta);
    }
    if (data.containsKey('entry_status')) {
      context.handle(
        _entryStatusMeta,
        entryStatus.isAcceptableOrUnknown(
          data['entry_status']!,
          _entryStatusMeta,
        ),
      );
    } else if (isInserting) {
      context.missing(_entryStatusMeta);
    }
    if (data.containsKey('reversal_of_entry_id')) {
      context.handle(
        _reversalOfEntryIdMeta,
        reversalOfEntryId.isAcceptableOrUnknown(
          data['reversal_of_entry_id']!,
          _reversalOfEntryIdMeta,
        ),
      );
    }
    if (data.containsKey('captured_at_device_utc')) {
      context.handle(
        _capturedAtDeviceUtcMeta,
        capturedAtDeviceUtc.isAcceptableOrUnknown(
          data['captured_at_device_utc']!,
          _capturedAtDeviceUtcMeta,
        ),
      );
    } else if (isInserting) {
      context.missing(_capturedAtDeviceUtcMeta);
    }
    if (data.containsKey('created_at_device_utc')) {
      context.handle(
        _createdAtDeviceUtcMeta,
        createdAtDeviceUtc.isAcceptableOrUnknown(
          data['created_at_device_utc']!,
          _createdAtDeviceUtcMeta,
        ),
      );
    } else if (isInserting) {
      context.missing(_createdAtDeviceUtcMeta);
    }
    return context;
  }

  @override
  Set<GeneratedColumn> get $primaryKey => {id};
  @override
  List<Set<GeneratedColumn>> get uniqueKeys => [
    {receivingSessionId, localSequence},
  ];
  @override
  LocalReceivingEntryRow map(Map<String, dynamic> data, {String? tablePrefix}) {
    final effectivePrefix = tablePrefix != null ? '$tablePrefix.' : '';
    return LocalReceivingEntryRow(
      id: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}id'],
      )!,
      receivingSessionId: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}receiving_session_id'],
      )!,
      operationId: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}operation_id'],
      )!,
      localSequence: attachedDatabase.typeMapping.read(
        DriftSqlType.int,
        data['${effectivePrefix}local_sequence'],
      )!,
      productReference: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}product_reference'],
      )!,
      bagTypeReference: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}bag_type_reference'],
      )!,
      bagCount: attachedDatabase.typeMapping.read(
        DriftSqlType.int,
        data['${effectivePrefix}bag_count'],
      )!,
      rawWeightKg: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}raw_weight_kg'],
      )!,
      processedWeightKg: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}processed_weight_kg'],
      )!,
      displayWeightKg: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}display_weight_kg'],
      )!,
      decimalPlaces: attachedDatabase.typeMapping.read(
        DriftSqlType.int,
        data['${effectivePrefix}decimal_places'],
      )!,
      processingMethod: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}processing_method'],
      )!,
      weightSource: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}weight_source'],
      )!,
      entryStatus: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}entry_status'],
      )!,
      reversalOfEntryId: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}reversal_of_entry_id'],
      ),
      capturedAtDeviceUtc: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}captured_at_device_utc'],
      )!,
      createdAtDeviceUtc: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}created_at_device_utc'],
      )!,
    );
  }

  @override
  $LocalReceivingEntriesTable createAlias(String alias) {
    return $LocalReceivingEntriesTable(attachedDatabase, alias);
  }
}

class LocalReceivingEntryRow extends DataClass
    implements Insertable<LocalReceivingEntryRow> {
  final String id;
  final String receivingSessionId;
  final String operationId;
  final int localSequence;
  final String productReference;
  final String bagTypeReference;
  final int bagCount;
  final String rawWeightKg;
  final String processedWeightKg;
  final String displayWeightKg;
  final int decimalPlaces;
  final String processingMethod;
  final String weightSource;
  final String entryStatus;
  final String? reversalOfEntryId;
  final String capturedAtDeviceUtc;
  final String createdAtDeviceUtc;
  const LocalReceivingEntryRow({
    required this.id,
    required this.receivingSessionId,
    required this.operationId,
    required this.localSequence,
    required this.productReference,
    required this.bagTypeReference,
    required this.bagCount,
    required this.rawWeightKg,
    required this.processedWeightKg,
    required this.displayWeightKg,
    required this.decimalPlaces,
    required this.processingMethod,
    required this.weightSource,
    required this.entryStatus,
    this.reversalOfEntryId,
    required this.capturedAtDeviceUtc,
    required this.createdAtDeviceUtc,
  });
  @override
  Map<String, Expression> toColumns(bool nullToAbsent) {
    final map = <String, Expression>{};
    map['id'] = Variable<String>(id);
    map['receiving_session_id'] = Variable<String>(receivingSessionId);
    map['operation_id'] = Variable<String>(operationId);
    map['local_sequence'] = Variable<int>(localSequence);
    map['product_reference'] = Variable<String>(productReference);
    map['bag_type_reference'] = Variable<String>(bagTypeReference);
    map['bag_count'] = Variable<int>(bagCount);
    map['raw_weight_kg'] = Variable<String>(rawWeightKg);
    map['processed_weight_kg'] = Variable<String>(processedWeightKg);
    map['display_weight_kg'] = Variable<String>(displayWeightKg);
    map['decimal_places'] = Variable<int>(decimalPlaces);
    map['processing_method'] = Variable<String>(processingMethod);
    map['weight_source'] = Variable<String>(weightSource);
    map['entry_status'] = Variable<String>(entryStatus);
    if (!nullToAbsent || reversalOfEntryId != null) {
      map['reversal_of_entry_id'] = Variable<String>(reversalOfEntryId);
    }
    map['captured_at_device_utc'] = Variable<String>(capturedAtDeviceUtc);
    map['created_at_device_utc'] = Variable<String>(createdAtDeviceUtc);
    return map;
  }

  LocalReceivingEntriesCompanion toCompanion(bool nullToAbsent) {
    return LocalReceivingEntriesCompanion(
      id: Value(id),
      receivingSessionId: Value(receivingSessionId),
      operationId: Value(operationId),
      localSequence: Value(localSequence),
      productReference: Value(productReference),
      bagTypeReference: Value(bagTypeReference),
      bagCount: Value(bagCount),
      rawWeightKg: Value(rawWeightKg),
      processedWeightKg: Value(processedWeightKg),
      displayWeightKg: Value(displayWeightKg),
      decimalPlaces: Value(decimalPlaces),
      processingMethod: Value(processingMethod),
      weightSource: Value(weightSource),
      entryStatus: Value(entryStatus),
      reversalOfEntryId: reversalOfEntryId == null && nullToAbsent
          ? const Value.absent()
          : Value(reversalOfEntryId),
      capturedAtDeviceUtc: Value(capturedAtDeviceUtc),
      createdAtDeviceUtc: Value(createdAtDeviceUtc),
    );
  }

  factory LocalReceivingEntryRow.fromJson(
    Map<String, dynamic> json, {
    ValueSerializer? serializer,
  }) {
    serializer ??= driftRuntimeOptions.defaultSerializer;
    return LocalReceivingEntryRow(
      id: serializer.fromJson<String>(json['id']),
      receivingSessionId: serializer.fromJson<String>(
        json['receivingSessionId'],
      ),
      operationId: serializer.fromJson<String>(json['operationId']),
      localSequence: serializer.fromJson<int>(json['localSequence']),
      productReference: serializer.fromJson<String>(json['productReference']),
      bagTypeReference: serializer.fromJson<String>(json['bagTypeReference']),
      bagCount: serializer.fromJson<int>(json['bagCount']),
      rawWeightKg: serializer.fromJson<String>(json['rawWeightKg']),
      processedWeightKg: serializer.fromJson<String>(json['processedWeightKg']),
      displayWeightKg: serializer.fromJson<String>(json['displayWeightKg']),
      decimalPlaces: serializer.fromJson<int>(json['decimalPlaces']),
      processingMethod: serializer.fromJson<String>(json['processingMethod']),
      weightSource: serializer.fromJson<String>(json['weightSource']),
      entryStatus: serializer.fromJson<String>(json['entryStatus']),
      reversalOfEntryId: serializer.fromJson<String?>(
        json['reversalOfEntryId'],
      ),
      capturedAtDeviceUtc: serializer.fromJson<String>(
        json['capturedAtDeviceUtc'],
      ),
      createdAtDeviceUtc: serializer.fromJson<String>(
        json['createdAtDeviceUtc'],
      ),
    );
  }
  @override
  Map<String, dynamic> toJson({ValueSerializer? serializer}) {
    serializer ??= driftRuntimeOptions.defaultSerializer;
    return <String, dynamic>{
      'id': serializer.toJson<String>(id),
      'receivingSessionId': serializer.toJson<String>(receivingSessionId),
      'operationId': serializer.toJson<String>(operationId),
      'localSequence': serializer.toJson<int>(localSequence),
      'productReference': serializer.toJson<String>(productReference),
      'bagTypeReference': serializer.toJson<String>(bagTypeReference),
      'bagCount': serializer.toJson<int>(bagCount),
      'rawWeightKg': serializer.toJson<String>(rawWeightKg),
      'processedWeightKg': serializer.toJson<String>(processedWeightKg),
      'displayWeightKg': serializer.toJson<String>(displayWeightKg),
      'decimalPlaces': serializer.toJson<int>(decimalPlaces),
      'processingMethod': serializer.toJson<String>(processingMethod),
      'weightSource': serializer.toJson<String>(weightSource),
      'entryStatus': serializer.toJson<String>(entryStatus),
      'reversalOfEntryId': serializer.toJson<String?>(reversalOfEntryId),
      'capturedAtDeviceUtc': serializer.toJson<String>(capturedAtDeviceUtc),
      'createdAtDeviceUtc': serializer.toJson<String>(createdAtDeviceUtc),
    };
  }

  LocalReceivingEntryRow copyWith({
    String? id,
    String? receivingSessionId,
    String? operationId,
    int? localSequence,
    String? productReference,
    String? bagTypeReference,
    int? bagCount,
    String? rawWeightKg,
    String? processedWeightKg,
    String? displayWeightKg,
    int? decimalPlaces,
    String? processingMethod,
    String? weightSource,
    String? entryStatus,
    Value<String?> reversalOfEntryId = const Value.absent(),
    String? capturedAtDeviceUtc,
    String? createdAtDeviceUtc,
  }) => LocalReceivingEntryRow(
    id: id ?? this.id,
    receivingSessionId: receivingSessionId ?? this.receivingSessionId,
    operationId: operationId ?? this.operationId,
    localSequence: localSequence ?? this.localSequence,
    productReference: productReference ?? this.productReference,
    bagTypeReference: bagTypeReference ?? this.bagTypeReference,
    bagCount: bagCount ?? this.bagCount,
    rawWeightKg: rawWeightKg ?? this.rawWeightKg,
    processedWeightKg: processedWeightKg ?? this.processedWeightKg,
    displayWeightKg: displayWeightKg ?? this.displayWeightKg,
    decimalPlaces: decimalPlaces ?? this.decimalPlaces,
    processingMethod: processingMethod ?? this.processingMethod,
    weightSource: weightSource ?? this.weightSource,
    entryStatus: entryStatus ?? this.entryStatus,
    reversalOfEntryId: reversalOfEntryId.present
        ? reversalOfEntryId.value
        : this.reversalOfEntryId,
    capturedAtDeviceUtc: capturedAtDeviceUtc ?? this.capturedAtDeviceUtc,
    createdAtDeviceUtc: createdAtDeviceUtc ?? this.createdAtDeviceUtc,
  );
  LocalReceivingEntryRow copyWithCompanion(
    LocalReceivingEntriesCompanion data,
  ) {
    return LocalReceivingEntryRow(
      id: data.id.present ? data.id.value : this.id,
      receivingSessionId: data.receivingSessionId.present
          ? data.receivingSessionId.value
          : this.receivingSessionId,
      operationId: data.operationId.present
          ? data.operationId.value
          : this.operationId,
      localSequence: data.localSequence.present
          ? data.localSequence.value
          : this.localSequence,
      productReference: data.productReference.present
          ? data.productReference.value
          : this.productReference,
      bagTypeReference: data.bagTypeReference.present
          ? data.bagTypeReference.value
          : this.bagTypeReference,
      bagCount: data.bagCount.present ? data.bagCount.value : this.bagCount,
      rawWeightKg: data.rawWeightKg.present
          ? data.rawWeightKg.value
          : this.rawWeightKg,
      processedWeightKg: data.processedWeightKg.present
          ? data.processedWeightKg.value
          : this.processedWeightKg,
      displayWeightKg: data.displayWeightKg.present
          ? data.displayWeightKg.value
          : this.displayWeightKg,
      decimalPlaces: data.decimalPlaces.present
          ? data.decimalPlaces.value
          : this.decimalPlaces,
      processingMethod: data.processingMethod.present
          ? data.processingMethod.value
          : this.processingMethod,
      weightSource: data.weightSource.present
          ? data.weightSource.value
          : this.weightSource,
      entryStatus: data.entryStatus.present
          ? data.entryStatus.value
          : this.entryStatus,
      reversalOfEntryId: data.reversalOfEntryId.present
          ? data.reversalOfEntryId.value
          : this.reversalOfEntryId,
      capturedAtDeviceUtc: data.capturedAtDeviceUtc.present
          ? data.capturedAtDeviceUtc.value
          : this.capturedAtDeviceUtc,
      createdAtDeviceUtc: data.createdAtDeviceUtc.present
          ? data.createdAtDeviceUtc.value
          : this.createdAtDeviceUtc,
    );
  }

  @override
  String toString() {
    return (StringBuffer('LocalReceivingEntryRow(')
          ..write('id: $id, ')
          ..write('receivingSessionId: $receivingSessionId, ')
          ..write('operationId: $operationId, ')
          ..write('localSequence: $localSequence, ')
          ..write('productReference: $productReference, ')
          ..write('bagTypeReference: $bagTypeReference, ')
          ..write('bagCount: $bagCount, ')
          ..write('rawWeightKg: $rawWeightKg, ')
          ..write('processedWeightKg: $processedWeightKg, ')
          ..write('displayWeightKg: $displayWeightKg, ')
          ..write('decimalPlaces: $decimalPlaces, ')
          ..write('processingMethod: $processingMethod, ')
          ..write('weightSource: $weightSource, ')
          ..write('entryStatus: $entryStatus, ')
          ..write('reversalOfEntryId: $reversalOfEntryId, ')
          ..write('capturedAtDeviceUtc: $capturedAtDeviceUtc, ')
          ..write('createdAtDeviceUtc: $createdAtDeviceUtc')
          ..write(')'))
        .toString();
  }

  @override
  int get hashCode => Object.hash(
    id,
    receivingSessionId,
    operationId,
    localSequence,
    productReference,
    bagTypeReference,
    bagCount,
    rawWeightKg,
    processedWeightKg,
    displayWeightKg,
    decimalPlaces,
    processingMethod,
    weightSource,
    entryStatus,
    reversalOfEntryId,
    capturedAtDeviceUtc,
    createdAtDeviceUtc,
  );
  @override
  bool operator ==(Object other) =>
      identical(this, other) ||
      (other is LocalReceivingEntryRow &&
          other.id == this.id &&
          other.receivingSessionId == this.receivingSessionId &&
          other.operationId == this.operationId &&
          other.localSequence == this.localSequence &&
          other.productReference == this.productReference &&
          other.bagTypeReference == this.bagTypeReference &&
          other.bagCount == this.bagCount &&
          other.rawWeightKg == this.rawWeightKg &&
          other.processedWeightKg == this.processedWeightKg &&
          other.displayWeightKg == this.displayWeightKg &&
          other.decimalPlaces == this.decimalPlaces &&
          other.processingMethod == this.processingMethod &&
          other.weightSource == this.weightSource &&
          other.entryStatus == this.entryStatus &&
          other.reversalOfEntryId == this.reversalOfEntryId &&
          other.capturedAtDeviceUtc == this.capturedAtDeviceUtc &&
          other.createdAtDeviceUtc == this.createdAtDeviceUtc);
}

class LocalReceivingEntriesCompanion
    extends UpdateCompanion<LocalReceivingEntryRow> {
  final Value<String> id;
  final Value<String> receivingSessionId;
  final Value<String> operationId;
  final Value<int> localSequence;
  final Value<String> productReference;
  final Value<String> bagTypeReference;
  final Value<int> bagCount;
  final Value<String> rawWeightKg;
  final Value<String> processedWeightKg;
  final Value<String> displayWeightKg;
  final Value<int> decimalPlaces;
  final Value<String> processingMethod;
  final Value<String> weightSource;
  final Value<String> entryStatus;
  final Value<String?> reversalOfEntryId;
  final Value<String> capturedAtDeviceUtc;
  final Value<String> createdAtDeviceUtc;
  final Value<int> rowid;
  const LocalReceivingEntriesCompanion({
    this.id = const Value.absent(),
    this.receivingSessionId = const Value.absent(),
    this.operationId = const Value.absent(),
    this.localSequence = const Value.absent(),
    this.productReference = const Value.absent(),
    this.bagTypeReference = const Value.absent(),
    this.bagCount = const Value.absent(),
    this.rawWeightKg = const Value.absent(),
    this.processedWeightKg = const Value.absent(),
    this.displayWeightKg = const Value.absent(),
    this.decimalPlaces = const Value.absent(),
    this.processingMethod = const Value.absent(),
    this.weightSource = const Value.absent(),
    this.entryStatus = const Value.absent(),
    this.reversalOfEntryId = const Value.absent(),
    this.capturedAtDeviceUtc = const Value.absent(),
    this.createdAtDeviceUtc = const Value.absent(),
    this.rowid = const Value.absent(),
  });
  LocalReceivingEntriesCompanion.insert({
    required String id,
    required String receivingSessionId,
    required String operationId,
    required int localSequence,
    required String productReference,
    required String bagTypeReference,
    required int bagCount,
    required String rawWeightKg,
    required String processedWeightKg,
    required String displayWeightKg,
    required int decimalPlaces,
    required String processingMethod,
    required String weightSource,
    required String entryStatus,
    this.reversalOfEntryId = const Value.absent(),
    required String capturedAtDeviceUtc,
    required String createdAtDeviceUtc,
    this.rowid = const Value.absent(),
  }) : id = Value(id),
       receivingSessionId = Value(receivingSessionId),
       operationId = Value(operationId),
       localSequence = Value(localSequence),
       productReference = Value(productReference),
       bagTypeReference = Value(bagTypeReference),
       bagCount = Value(bagCount),
       rawWeightKg = Value(rawWeightKg),
       processedWeightKg = Value(processedWeightKg),
       displayWeightKg = Value(displayWeightKg),
       decimalPlaces = Value(decimalPlaces),
       processingMethod = Value(processingMethod),
       weightSource = Value(weightSource),
       entryStatus = Value(entryStatus),
       capturedAtDeviceUtc = Value(capturedAtDeviceUtc),
       createdAtDeviceUtc = Value(createdAtDeviceUtc);
  static Insertable<LocalReceivingEntryRow> custom({
    Expression<String>? id,
    Expression<String>? receivingSessionId,
    Expression<String>? operationId,
    Expression<int>? localSequence,
    Expression<String>? productReference,
    Expression<String>? bagTypeReference,
    Expression<int>? bagCount,
    Expression<String>? rawWeightKg,
    Expression<String>? processedWeightKg,
    Expression<String>? displayWeightKg,
    Expression<int>? decimalPlaces,
    Expression<String>? processingMethod,
    Expression<String>? weightSource,
    Expression<String>? entryStatus,
    Expression<String>? reversalOfEntryId,
    Expression<String>? capturedAtDeviceUtc,
    Expression<String>? createdAtDeviceUtc,
    Expression<int>? rowid,
  }) {
    return RawValuesInsertable({
      if (id != null) 'id': id,
      if (receivingSessionId != null)
        'receiving_session_id': receivingSessionId,
      if (operationId != null) 'operation_id': operationId,
      if (localSequence != null) 'local_sequence': localSequence,
      if (productReference != null) 'product_reference': productReference,
      if (bagTypeReference != null) 'bag_type_reference': bagTypeReference,
      if (bagCount != null) 'bag_count': bagCount,
      if (rawWeightKg != null) 'raw_weight_kg': rawWeightKg,
      if (processedWeightKg != null) 'processed_weight_kg': processedWeightKg,
      if (displayWeightKg != null) 'display_weight_kg': displayWeightKg,
      if (decimalPlaces != null) 'decimal_places': decimalPlaces,
      if (processingMethod != null) 'processing_method': processingMethod,
      if (weightSource != null) 'weight_source': weightSource,
      if (entryStatus != null) 'entry_status': entryStatus,
      if (reversalOfEntryId != null) 'reversal_of_entry_id': reversalOfEntryId,
      if (capturedAtDeviceUtc != null)
        'captured_at_device_utc': capturedAtDeviceUtc,
      if (createdAtDeviceUtc != null)
        'created_at_device_utc': createdAtDeviceUtc,
      if (rowid != null) 'rowid': rowid,
    });
  }

  LocalReceivingEntriesCompanion copyWith({
    Value<String>? id,
    Value<String>? receivingSessionId,
    Value<String>? operationId,
    Value<int>? localSequence,
    Value<String>? productReference,
    Value<String>? bagTypeReference,
    Value<int>? bagCount,
    Value<String>? rawWeightKg,
    Value<String>? processedWeightKg,
    Value<String>? displayWeightKg,
    Value<int>? decimalPlaces,
    Value<String>? processingMethod,
    Value<String>? weightSource,
    Value<String>? entryStatus,
    Value<String?>? reversalOfEntryId,
    Value<String>? capturedAtDeviceUtc,
    Value<String>? createdAtDeviceUtc,
    Value<int>? rowid,
  }) {
    return LocalReceivingEntriesCompanion(
      id: id ?? this.id,
      receivingSessionId: receivingSessionId ?? this.receivingSessionId,
      operationId: operationId ?? this.operationId,
      localSequence: localSequence ?? this.localSequence,
      productReference: productReference ?? this.productReference,
      bagTypeReference: bagTypeReference ?? this.bagTypeReference,
      bagCount: bagCount ?? this.bagCount,
      rawWeightKg: rawWeightKg ?? this.rawWeightKg,
      processedWeightKg: processedWeightKg ?? this.processedWeightKg,
      displayWeightKg: displayWeightKg ?? this.displayWeightKg,
      decimalPlaces: decimalPlaces ?? this.decimalPlaces,
      processingMethod: processingMethod ?? this.processingMethod,
      weightSource: weightSource ?? this.weightSource,
      entryStatus: entryStatus ?? this.entryStatus,
      reversalOfEntryId: reversalOfEntryId ?? this.reversalOfEntryId,
      capturedAtDeviceUtc: capturedAtDeviceUtc ?? this.capturedAtDeviceUtc,
      createdAtDeviceUtc: createdAtDeviceUtc ?? this.createdAtDeviceUtc,
      rowid: rowid ?? this.rowid,
    );
  }

  @override
  Map<String, Expression> toColumns(bool nullToAbsent) {
    final map = <String, Expression>{};
    if (id.present) {
      map['id'] = Variable<String>(id.value);
    }
    if (receivingSessionId.present) {
      map['receiving_session_id'] = Variable<String>(receivingSessionId.value);
    }
    if (operationId.present) {
      map['operation_id'] = Variable<String>(operationId.value);
    }
    if (localSequence.present) {
      map['local_sequence'] = Variable<int>(localSequence.value);
    }
    if (productReference.present) {
      map['product_reference'] = Variable<String>(productReference.value);
    }
    if (bagTypeReference.present) {
      map['bag_type_reference'] = Variable<String>(bagTypeReference.value);
    }
    if (bagCount.present) {
      map['bag_count'] = Variable<int>(bagCount.value);
    }
    if (rawWeightKg.present) {
      map['raw_weight_kg'] = Variable<String>(rawWeightKg.value);
    }
    if (processedWeightKg.present) {
      map['processed_weight_kg'] = Variable<String>(processedWeightKg.value);
    }
    if (displayWeightKg.present) {
      map['display_weight_kg'] = Variable<String>(displayWeightKg.value);
    }
    if (decimalPlaces.present) {
      map['decimal_places'] = Variable<int>(decimalPlaces.value);
    }
    if (processingMethod.present) {
      map['processing_method'] = Variable<String>(processingMethod.value);
    }
    if (weightSource.present) {
      map['weight_source'] = Variable<String>(weightSource.value);
    }
    if (entryStatus.present) {
      map['entry_status'] = Variable<String>(entryStatus.value);
    }
    if (reversalOfEntryId.present) {
      map['reversal_of_entry_id'] = Variable<String>(reversalOfEntryId.value);
    }
    if (capturedAtDeviceUtc.present) {
      map['captured_at_device_utc'] = Variable<String>(
        capturedAtDeviceUtc.value,
      );
    }
    if (createdAtDeviceUtc.present) {
      map['created_at_device_utc'] = Variable<String>(createdAtDeviceUtc.value);
    }
    if (rowid.present) {
      map['rowid'] = Variable<int>(rowid.value);
    }
    return map;
  }

  @override
  String toString() {
    return (StringBuffer('LocalReceivingEntriesCompanion(')
          ..write('id: $id, ')
          ..write('receivingSessionId: $receivingSessionId, ')
          ..write('operationId: $operationId, ')
          ..write('localSequence: $localSequence, ')
          ..write('productReference: $productReference, ')
          ..write('bagTypeReference: $bagTypeReference, ')
          ..write('bagCount: $bagCount, ')
          ..write('rawWeightKg: $rawWeightKg, ')
          ..write('processedWeightKg: $processedWeightKg, ')
          ..write('displayWeightKg: $displayWeightKg, ')
          ..write('decimalPlaces: $decimalPlaces, ')
          ..write('processingMethod: $processingMethod, ')
          ..write('weightSource: $weightSource, ')
          ..write('entryStatus: $entryStatus, ')
          ..write('reversalOfEntryId: $reversalOfEntryId, ')
          ..write('capturedAtDeviceUtc: $capturedAtDeviceUtc, ')
          ..write('createdAtDeviceUtc: $createdAtDeviceUtc, ')
          ..write('rowid: $rowid')
          ..write(')'))
        .toString();
  }
}

class $LocalOutboxOperationsTable extends LocalOutboxOperations
    with TableInfo<$LocalOutboxOperationsTable, LocalOutboxOperationRow> {
  @override
  final GeneratedDatabase attachedDatabase;
  final String? _alias;
  $LocalOutboxOperationsTable(this.attachedDatabase, [this._alias]);
  static const VerificationMeta _operationIdMeta = const VerificationMeta(
    'operationId',
  );
  @override
  late final GeneratedColumn<String> operationId = GeneratedColumn<String>(
    'operation_id',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
  );
  static const VerificationMeta _aggregateIdMeta = const VerificationMeta(
    'aggregateId',
  );
  @override
  late final GeneratedColumn<String> aggregateId = GeneratedColumn<String>(
    'aggregate_id',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
  );
  static const VerificationMeta _aggregateTypeMeta = const VerificationMeta(
    'aggregateType',
  );
  @override
  late final GeneratedColumn<String> aggregateType = GeneratedColumn<String>(
    'aggregate_type',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
  );
  static const VerificationMeta _operationTypeMeta = const VerificationMeta(
    'operationType',
  );
  @override
  late final GeneratedColumn<String> operationType = GeneratedColumn<String>(
    'operation_type',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
  );
  static const VerificationMeta _localSequenceMeta = const VerificationMeta(
    'localSequence',
  );
  @override
  late final GeneratedColumn<int> localSequence = GeneratedColumn<int>(
    'local_sequence',
    aliasedName,
    false,
    type: DriftSqlType.int,
    requiredDuringInsert: true,
    $customConstraints: 'NOT NULL CHECK (local_sequence > 0)',
  );
  static const VerificationMeta _expectedCloudVersionMeta =
      const VerificationMeta('expectedCloudVersion');
  @override
  late final GeneratedColumn<int> expectedCloudVersion = GeneratedColumn<int>(
    'expected_cloud_version',
    aliasedName,
    true,
    type: DriftSqlType.int,
    requiredDuringInsert: false,
  );
  static const VerificationMeta _payloadJsonMeta = const VerificationMeta(
    'payloadJson',
  );
  @override
  late final GeneratedColumn<String> payloadJson = GeneratedColumn<String>(
    'payload_json',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
  );
  static const VerificationMeta _payloadHashMeta = const VerificationMeta(
    'payloadHash',
  );
  @override
  late final GeneratedColumn<String> payloadHash = GeneratedColumn<String>(
    'payload_hash',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
  );
  static const VerificationMeta _statusMeta = const VerificationMeta('status');
  @override
  late final GeneratedColumn<String> status = GeneratedColumn<String>(
    'status',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
    $customConstraints:
        'NOT NULL CHECK (status IN (\'Pending\', \'Sending\', \'Accepted\', \'NeedsAttention\', \'Rejected\', \'Superseded\'))',
  );
  static const VerificationMeta _attemptCountMeta = const VerificationMeta(
    'attemptCount',
  );
  @override
  late final GeneratedColumn<int> attemptCount = GeneratedColumn<int>(
    'attempt_count',
    aliasedName,
    false,
    type: DriftSqlType.int,
    requiredDuringInsert: true,
    $customConstraints: 'NOT NULL CHECK (attempt_count >= 0)',
  );
  static const VerificationMeta _createdAtDeviceUtcMeta =
      const VerificationMeta('createdAtDeviceUtc');
  @override
  late final GeneratedColumn<String> createdAtDeviceUtc =
      GeneratedColumn<String>(
        'created_at_device_utc',
        aliasedName,
        false,
        type: DriftSqlType.string,
        requiredDuringInsert: true,
      );
  static const VerificationMeta _lastAttemptAtUtcMeta = const VerificationMeta(
    'lastAttemptAtUtc',
  );
  @override
  late final GeneratedColumn<String> lastAttemptAtUtc = GeneratedColumn<String>(
    'last_attempt_at_utc',
    aliasedName,
    true,
    type: DriftSqlType.string,
    requiredDuringInsert: false,
  );
  static const VerificationMeta _acceptedAtUtcMeta = const VerificationMeta(
    'acceptedAtUtc',
  );
  @override
  late final GeneratedColumn<String> acceptedAtUtc = GeneratedColumn<String>(
    'accepted_at_utc',
    aliasedName,
    true,
    type: DriftSqlType.string,
    requiredDuringInsert: false,
  );
  static const VerificationMeta _lastErrorCodeMeta = const VerificationMeta(
    'lastErrorCode',
  );
  @override
  late final GeneratedColumn<String> lastErrorCode = GeneratedColumn<String>(
    'last_error_code',
    aliasedName,
    true,
    type: DriftSqlType.string,
    requiredDuringInsert: false,
  );
  @override
  List<GeneratedColumn> get $columns => [
    operationId,
    aggregateId,
    aggregateType,
    operationType,
    localSequence,
    expectedCloudVersion,
    payloadJson,
    payloadHash,
    status,
    attemptCount,
    createdAtDeviceUtc,
    lastAttemptAtUtc,
    acceptedAtUtc,
    lastErrorCode,
  ];
  @override
  String get aliasedName => _alias ?? actualTableName;
  @override
  String get actualTableName => $name;
  static const String $name = 'local_outbox_operations';
  @override
  VerificationContext validateIntegrity(
    Insertable<LocalOutboxOperationRow> instance, {
    bool isInserting = false,
  }) {
    final context = VerificationContext();
    final data = instance.toColumns(true);
    if (data.containsKey('operation_id')) {
      context.handle(
        _operationIdMeta,
        operationId.isAcceptableOrUnknown(
          data['operation_id']!,
          _operationIdMeta,
        ),
      );
    } else if (isInserting) {
      context.missing(_operationIdMeta);
    }
    if (data.containsKey('aggregate_id')) {
      context.handle(
        _aggregateIdMeta,
        aggregateId.isAcceptableOrUnknown(
          data['aggregate_id']!,
          _aggregateIdMeta,
        ),
      );
    } else if (isInserting) {
      context.missing(_aggregateIdMeta);
    }
    if (data.containsKey('aggregate_type')) {
      context.handle(
        _aggregateTypeMeta,
        aggregateType.isAcceptableOrUnknown(
          data['aggregate_type']!,
          _aggregateTypeMeta,
        ),
      );
    } else if (isInserting) {
      context.missing(_aggregateTypeMeta);
    }
    if (data.containsKey('operation_type')) {
      context.handle(
        _operationTypeMeta,
        operationType.isAcceptableOrUnknown(
          data['operation_type']!,
          _operationTypeMeta,
        ),
      );
    } else if (isInserting) {
      context.missing(_operationTypeMeta);
    }
    if (data.containsKey('local_sequence')) {
      context.handle(
        _localSequenceMeta,
        localSequence.isAcceptableOrUnknown(
          data['local_sequence']!,
          _localSequenceMeta,
        ),
      );
    } else if (isInserting) {
      context.missing(_localSequenceMeta);
    }
    if (data.containsKey('expected_cloud_version')) {
      context.handle(
        _expectedCloudVersionMeta,
        expectedCloudVersion.isAcceptableOrUnknown(
          data['expected_cloud_version']!,
          _expectedCloudVersionMeta,
        ),
      );
    }
    if (data.containsKey('payload_json')) {
      context.handle(
        _payloadJsonMeta,
        payloadJson.isAcceptableOrUnknown(
          data['payload_json']!,
          _payloadJsonMeta,
        ),
      );
    } else if (isInserting) {
      context.missing(_payloadJsonMeta);
    }
    if (data.containsKey('payload_hash')) {
      context.handle(
        _payloadHashMeta,
        payloadHash.isAcceptableOrUnknown(
          data['payload_hash']!,
          _payloadHashMeta,
        ),
      );
    } else if (isInserting) {
      context.missing(_payloadHashMeta);
    }
    if (data.containsKey('status')) {
      context.handle(
        _statusMeta,
        status.isAcceptableOrUnknown(data['status']!, _statusMeta),
      );
    } else if (isInserting) {
      context.missing(_statusMeta);
    }
    if (data.containsKey('attempt_count')) {
      context.handle(
        _attemptCountMeta,
        attemptCount.isAcceptableOrUnknown(
          data['attempt_count']!,
          _attemptCountMeta,
        ),
      );
    } else if (isInserting) {
      context.missing(_attemptCountMeta);
    }
    if (data.containsKey('created_at_device_utc')) {
      context.handle(
        _createdAtDeviceUtcMeta,
        createdAtDeviceUtc.isAcceptableOrUnknown(
          data['created_at_device_utc']!,
          _createdAtDeviceUtcMeta,
        ),
      );
    } else if (isInserting) {
      context.missing(_createdAtDeviceUtcMeta);
    }
    if (data.containsKey('last_attempt_at_utc')) {
      context.handle(
        _lastAttemptAtUtcMeta,
        lastAttemptAtUtc.isAcceptableOrUnknown(
          data['last_attempt_at_utc']!,
          _lastAttemptAtUtcMeta,
        ),
      );
    }
    if (data.containsKey('accepted_at_utc')) {
      context.handle(
        _acceptedAtUtcMeta,
        acceptedAtUtc.isAcceptableOrUnknown(
          data['accepted_at_utc']!,
          _acceptedAtUtcMeta,
        ),
      );
    }
    if (data.containsKey('last_error_code')) {
      context.handle(
        _lastErrorCodeMeta,
        lastErrorCode.isAcceptableOrUnknown(
          data['last_error_code']!,
          _lastErrorCodeMeta,
        ),
      );
    }
    return context;
  }

  @override
  Set<GeneratedColumn> get $primaryKey => {operationId};
  @override
  List<Set<GeneratedColumn>> get uniqueKeys => [
    {aggregateId, localSequence},
  ];
  @override
  LocalOutboxOperationRow map(
    Map<String, dynamic> data, {
    String? tablePrefix,
  }) {
    final effectivePrefix = tablePrefix != null ? '$tablePrefix.' : '';
    return LocalOutboxOperationRow(
      operationId: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}operation_id'],
      )!,
      aggregateId: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}aggregate_id'],
      )!,
      aggregateType: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}aggregate_type'],
      )!,
      operationType: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}operation_type'],
      )!,
      localSequence: attachedDatabase.typeMapping.read(
        DriftSqlType.int,
        data['${effectivePrefix}local_sequence'],
      )!,
      expectedCloudVersion: attachedDatabase.typeMapping.read(
        DriftSqlType.int,
        data['${effectivePrefix}expected_cloud_version'],
      ),
      payloadJson: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}payload_json'],
      )!,
      payloadHash: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}payload_hash'],
      )!,
      status: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}status'],
      )!,
      attemptCount: attachedDatabase.typeMapping.read(
        DriftSqlType.int,
        data['${effectivePrefix}attempt_count'],
      )!,
      createdAtDeviceUtc: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}created_at_device_utc'],
      )!,
      lastAttemptAtUtc: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}last_attempt_at_utc'],
      ),
      acceptedAtUtc: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}accepted_at_utc'],
      ),
      lastErrorCode: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}last_error_code'],
      ),
    );
  }

  @override
  $LocalOutboxOperationsTable createAlias(String alias) {
    return $LocalOutboxOperationsTable(attachedDatabase, alias);
  }
}

class LocalOutboxOperationRow extends DataClass
    implements Insertable<LocalOutboxOperationRow> {
  final String operationId;
  final String aggregateId;
  final String aggregateType;
  final String operationType;
  final int localSequence;
  final int? expectedCloudVersion;
  final String payloadJson;
  final String payloadHash;
  final String status;
  final int attemptCount;
  final String createdAtDeviceUtc;
  final String? lastAttemptAtUtc;
  final String? acceptedAtUtc;
  final String? lastErrorCode;
  const LocalOutboxOperationRow({
    required this.operationId,
    required this.aggregateId,
    required this.aggregateType,
    required this.operationType,
    required this.localSequence,
    this.expectedCloudVersion,
    required this.payloadJson,
    required this.payloadHash,
    required this.status,
    required this.attemptCount,
    required this.createdAtDeviceUtc,
    this.lastAttemptAtUtc,
    this.acceptedAtUtc,
    this.lastErrorCode,
  });
  @override
  Map<String, Expression> toColumns(bool nullToAbsent) {
    final map = <String, Expression>{};
    map['operation_id'] = Variable<String>(operationId);
    map['aggregate_id'] = Variable<String>(aggregateId);
    map['aggregate_type'] = Variable<String>(aggregateType);
    map['operation_type'] = Variable<String>(operationType);
    map['local_sequence'] = Variable<int>(localSequence);
    if (!nullToAbsent || expectedCloudVersion != null) {
      map['expected_cloud_version'] = Variable<int>(expectedCloudVersion);
    }
    map['payload_json'] = Variable<String>(payloadJson);
    map['payload_hash'] = Variable<String>(payloadHash);
    map['status'] = Variable<String>(status);
    map['attempt_count'] = Variable<int>(attemptCount);
    map['created_at_device_utc'] = Variable<String>(createdAtDeviceUtc);
    if (!nullToAbsent || lastAttemptAtUtc != null) {
      map['last_attempt_at_utc'] = Variable<String>(lastAttemptAtUtc);
    }
    if (!nullToAbsent || acceptedAtUtc != null) {
      map['accepted_at_utc'] = Variable<String>(acceptedAtUtc);
    }
    if (!nullToAbsent || lastErrorCode != null) {
      map['last_error_code'] = Variable<String>(lastErrorCode);
    }
    return map;
  }

  LocalOutboxOperationsCompanion toCompanion(bool nullToAbsent) {
    return LocalOutboxOperationsCompanion(
      operationId: Value(operationId),
      aggregateId: Value(aggregateId),
      aggregateType: Value(aggregateType),
      operationType: Value(operationType),
      localSequence: Value(localSequence),
      expectedCloudVersion: expectedCloudVersion == null && nullToAbsent
          ? const Value.absent()
          : Value(expectedCloudVersion),
      payloadJson: Value(payloadJson),
      payloadHash: Value(payloadHash),
      status: Value(status),
      attemptCount: Value(attemptCount),
      createdAtDeviceUtc: Value(createdAtDeviceUtc),
      lastAttemptAtUtc: lastAttemptAtUtc == null && nullToAbsent
          ? const Value.absent()
          : Value(lastAttemptAtUtc),
      acceptedAtUtc: acceptedAtUtc == null && nullToAbsent
          ? const Value.absent()
          : Value(acceptedAtUtc),
      lastErrorCode: lastErrorCode == null && nullToAbsent
          ? const Value.absent()
          : Value(lastErrorCode),
    );
  }

  factory LocalOutboxOperationRow.fromJson(
    Map<String, dynamic> json, {
    ValueSerializer? serializer,
  }) {
    serializer ??= driftRuntimeOptions.defaultSerializer;
    return LocalOutboxOperationRow(
      operationId: serializer.fromJson<String>(json['operationId']),
      aggregateId: serializer.fromJson<String>(json['aggregateId']),
      aggregateType: serializer.fromJson<String>(json['aggregateType']),
      operationType: serializer.fromJson<String>(json['operationType']),
      localSequence: serializer.fromJson<int>(json['localSequence']),
      expectedCloudVersion: serializer.fromJson<int?>(
        json['expectedCloudVersion'],
      ),
      payloadJson: serializer.fromJson<String>(json['payloadJson']),
      payloadHash: serializer.fromJson<String>(json['payloadHash']),
      status: serializer.fromJson<String>(json['status']),
      attemptCount: serializer.fromJson<int>(json['attemptCount']),
      createdAtDeviceUtc: serializer.fromJson<String>(
        json['createdAtDeviceUtc'],
      ),
      lastAttemptAtUtc: serializer.fromJson<String?>(json['lastAttemptAtUtc']),
      acceptedAtUtc: serializer.fromJson<String?>(json['acceptedAtUtc']),
      lastErrorCode: serializer.fromJson<String?>(json['lastErrorCode']),
    );
  }
  @override
  Map<String, dynamic> toJson({ValueSerializer? serializer}) {
    serializer ??= driftRuntimeOptions.defaultSerializer;
    return <String, dynamic>{
      'operationId': serializer.toJson<String>(operationId),
      'aggregateId': serializer.toJson<String>(aggregateId),
      'aggregateType': serializer.toJson<String>(aggregateType),
      'operationType': serializer.toJson<String>(operationType),
      'localSequence': serializer.toJson<int>(localSequence),
      'expectedCloudVersion': serializer.toJson<int?>(expectedCloudVersion),
      'payloadJson': serializer.toJson<String>(payloadJson),
      'payloadHash': serializer.toJson<String>(payloadHash),
      'status': serializer.toJson<String>(status),
      'attemptCount': serializer.toJson<int>(attemptCount),
      'createdAtDeviceUtc': serializer.toJson<String>(createdAtDeviceUtc),
      'lastAttemptAtUtc': serializer.toJson<String?>(lastAttemptAtUtc),
      'acceptedAtUtc': serializer.toJson<String?>(acceptedAtUtc),
      'lastErrorCode': serializer.toJson<String?>(lastErrorCode),
    };
  }

  LocalOutboxOperationRow copyWith({
    String? operationId,
    String? aggregateId,
    String? aggregateType,
    String? operationType,
    int? localSequence,
    Value<int?> expectedCloudVersion = const Value.absent(),
    String? payloadJson,
    String? payloadHash,
    String? status,
    int? attemptCount,
    String? createdAtDeviceUtc,
    Value<String?> lastAttemptAtUtc = const Value.absent(),
    Value<String?> acceptedAtUtc = const Value.absent(),
    Value<String?> lastErrorCode = const Value.absent(),
  }) => LocalOutboxOperationRow(
    operationId: operationId ?? this.operationId,
    aggregateId: aggregateId ?? this.aggregateId,
    aggregateType: aggregateType ?? this.aggregateType,
    operationType: operationType ?? this.operationType,
    localSequence: localSequence ?? this.localSequence,
    expectedCloudVersion: expectedCloudVersion.present
        ? expectedCloudVersion.value
        : this.expectedCloudVersion,
    payloadJson: payloadJson ?? this.payloadJson,
    payloadHash: payloadHash ?? this.payloadHash,
    status: status ?? this.status,
    attemptCount: attemptCount ?? this.attemptCount,
    createdAtDeviceUtc: createdAtDeviceUtc ?? this.createdAtDeviceUtc,
    lastAttemptAtUtc: lastAttemptAtUtc.present
        ? lastAttemptAtUtc.value
        : this.lastAttemptAtUtc,
    acceptedAtUtc: acceptedAtUtc.present
        ? acceptedAtUtc.value
        : this.acceptedAtUtc,
    lastErrorCode: lastErrorCode.present
        ? lastErrorCode.value
        : this.lastErrorCode,
  );
  LocalOutboxOperationRow copyWithCompanion(
    LocalOutboxOperationsCompanion data,
  ) {
    return LocalOutboxOperationRow(
      operationId: data.operationId.present
          ? data.operationId.value
          : this.operationId,
      aggregateId: data.aggregateId.present
          ? data.aggregateId.value
          : this.aggregateId,
      aggregateType: data.aggregateType.present
          ? data.aggregateType.value
          : this.aggregateType,
      operationType: data.operationType.present
          ? data.operationType.value
          : this.operationType,
      localSequence: data.localSequence.present
          ? data.localSequence.value
          : this.localSequence,
      expectedCloudVersion: data.expectedCloudVersion.present
          ? data.expectedCloudVersion.value
          : this.expectedCloudVersion,
      payloadJson: data.payloadJson.present
          ? data.payloadJson.value
          : this.payloadJson,
      payloadHash: data.payloadHash.present
          ? data.payloadHash.value
          : this.payloadHash,
      status: data.status.present ? data.status.value : this.status,
      attemptCount: data.attemptCount.present
          ? data.attemptCount.value
          : this.attemptCount,
      createdAtDeviceUtc: data.createdAtDeviceUtc.present
          ? data.createdAtDeviceUtc.value
          : this.createdAtDeviceUtc,
      lastAttemptAtUtc: data.lastAttemptAtUtc.present
          ? data.lastAttemptAtUtc.value
          : this.lastAttemptAtUtc,
      acceptedAtUtc: data.acceptedAtUtc.present
          ? data.acceptedAtUtc.value
          : this.acceptedAtUtc,
      lastErrorCode: data.lastErrorCode.present
          ? data.lastErrorCode.value
          : this.lastErrorCode,
    );
  }

  @override
  String toString() {
    return (StringBuffer('LocalOutboxOperationRow(')
          ..write('operationId: $operationId, ')
          ..write('aggregateId: $aggregateId, ')
          ..write('aggregateType: $aggregateType, ')
          ..write('operationType: $operationType, ')
          ..write('localSequence: $localSequence, ')
          ..write('expectedCloudVersion: $expectedCloudVersion, ')
          ..write('payloadJson: $payloadJson, ')
          ..write('payloadHash: $payloadHash, ')
          ..write('status: $status, ')
          ..write('attemptCount: $attemptCount, ')
          ..write('createdAtDeviceUtc: $createdAtDeviceUtc, ')
          ..write('lastAttemptAtUtc: $lastAttemptAtUtc, ')
          ..write('acceptedAtUtc: $acceptedAtUtc, ')
          ..write('lastErrorCode: $lastErrorCode')
          ..write(')'))
        .toString();
  }

  @override
  int get hashCode => Object.hash(
    operationId,
    aggregateId,
    aggregateType,
    operationType,
    localSequence,
    expectedCloudVersion,
    payloadJson,
    payloadHash,
    status,
    attemptCount,
    createdAtDeviceUtc,
    lastAttemptAtUtc,
    acceptedAtUtc,
    lastErrorCode,
  );
  @override
  bool operator ==(Object other) =>
      identical(this, other) ||
      (other is LocalOutboxOperationRow &&
          other.operationId == this.operationId &&
          other.aggregateId == this.aggregateId &&
          other.aggregateType == this.aggregateType &&
          other.operationType == this.operationType &&
          other.localSequence == this.localSequence &&
          other.expectedCloudVersion == this.expectedCloudVersion &&
          other.payloadJson == this.payloadJson &&
          other.payloadHash == this.payloadHash &&
          other.status == this.status &&
          other.attemptCount == this.attemptCount &&
          other.createdAtDeviceUtc == this.createdAtDeviceUtc &&
          other.lastAttemptAtUtc == this.lastAttemptAtUtc &&
          other.acceptedAtUtc == this.acceptedAtUtc &&
          other.lastErrorCode == this.lastErrorCode);
}

class LocalOutboxOperationsCompanion
    extends UpdateCompanion<LocalOutboxOperationRow> {
  final Value<String> operationId;
  final Value<String> aggregateId;
  final Value<String> aggregateType;
  final Value<String> operationType;
  final Value<int> localSequence;
  final Value<int?> expectedCloudVersion;
  final Value<String> payloadJson;
  final Value<String> payloadHash;
  final Value<String> status;
  final Value<int> attemptCount;
  final Value<String> createdAtDeviceUtc;
  final Value<String?> lastAttemptAtUtc;
  final Value<String?> acceptedAtUtc;
  final Value<String?> lastErrorCode;
  final Value<int> rowid;
  const LocalOutboxOperationsCompanion({
    this.operationId = const Value.absent(),
    this.aggregateId = const Value.absent(),
    this.aggregateType = const Value.absent(),
    this.operationType = const Value.absent(),
    this.localSequence = const Value.absent(),
    this.expectedCloudVersion = const Value.absent(),
    this.payloadJson = const Value.absent(),
    this.payloadHash = const Value.absent(),
    this.status = const Value.absent(),
    this.attemptCount = const Value.absent(),
    this.createdAtDeviceUtc = const Value.absent(),
    this.lastAttemptAtUtc = const Value.absent(),
    this.acceptedAtUtc = const Value.absent(),
    this.lastErrorCode = const Value.absent(),
    this.rowid = const Value.absent(),
  });
  LocalOutboxOperationsCompanion.insert({
    required String operationId,
    required String aggregateId,
    required String aggregateType,
    required String operationType,
    required int localSequence,
    this.expectedCloudVersion = const Value.absent(),
    required String payloadJson,
    required String payloadHash,
    required String status,
    required int attemptCount,
    required String createdAtDeviceUtc,
    this.lastAttemptAtUtc = const Value.absent(),
    this.acceptedAtUtc = const Value.absent(),
    this.lastErrorCode = const Value.absent(),
    this.rowid = const Value.absent(),
  }) : operationId = Value(operationId),
       aggregateId = Value(aggregateId),
       aggregateType = Value(aggregateType),
       operationType = Value(operationType),
       localSequence = Value(localSequence),
       payloadJson = Value(payloadJson),
       payloadHash = Value(payloadHash),
       status = Value(status),
       attemptCount = Value(attemptCount),
       createdAtDeviceUtc = Value(createdAtDeviceUtc);
  static Insertable<LocalOutboxOperationRow> custom({
    Expression<String>? operationId,
    Expression<String>? aggregateId,
    Expression<String>? aggregateType,
    Expression<String>? operationType,
    Expression<int>? localSequence,
    Expression<int>? expectedCloudVersion,
    Expression<String>? payloadJson,
    Expression<String>? payloadHash,
    Expression<String>? status,
    Expression<int>? attemptCount,
    Expression<String>? createdAtDeviceUtc,
    Expression<String>? lastAttemptAtUtc,
    Expression<String>? acceptedAtUtc,
    Expression<String>? lastErrorCode,
    Expression<int>? rowid,
  }) {
    return RawValuesInsertable({
      if (operationId != null) 'operation_id': operationId,
      if (aggregateId != null) 'aggregate_id': aggregateId,
      if (aggregateType != null) 'aggregate_type': aggregateType,
      if (operationType != null) 'operation_type': operationType,
      if (localSequence != null) 'local_sequence': localSequence,
      if (expectedCloudVersion != null)
        'expected_cloud_version': expectedCloudVersion,
      if (payloadJson != null) 'payload_json': payloadJson,
      if (payloadHash != null) 'payload_hash': payloadHash,
      if (status != null) 'status': status,
      if (attemptCount != null) 'attempt_count': attemptCount,
      if (createdAtDeviceUtc != null)
        'created_at_device_utc': createdAtDeviceUtc,
      if (lastAttemptAtUtc != null) 'last_attempt_at_utc': lastAttemptAtUtc,
      if (acceptedAtUtc != null) 'accepted_at_utc': acceptedAtUtc,
      if (lastErrorCode != null) 'last_error_code': lastErrorCode,
      if (rowid != null) 'rowid': rowid,
    });
  }

  LocalOutboxOperationsCompanion copyWith({
    Value<String>? operationId,
    Value<String>? aggregateId,
    Value<String>? aggregateType,
    Value<String>? operationType,
    Value<int>? localSequence,
    Value<int?>? expectedCloudVersion,
    Value<String>? payloadJson,
    Value<String>? payloadHash,
    Value<String>? status,
    Value<int>? attemptCount,
    Value<String>? createdAtDeviceUtc,
    Value<String?>? lastAttemptAtUtc,
    Value<String?>? acceptedAtUtc,
    Value<String?>? lastErrorCode,
    Value<int>? rowid,
  }) {
    return LocalOutboxOperationsCompanion(
      operationId: operationId ?? this.operationId,
      aggregateId: aggregateId ?? this.aggregateId,
      aggregateType: aggregateType ?? this.aggregateType,
      operationType: operationType ?? this.operationType,
      localSequence: localSequence ?? this.localSequence,
      expectedCloudVersion: expectedCloudVersion ?? this.expectedCloudVersion,
      payloadJson: payloadJson ?? this.payloadJson,
      payloadHash: payloadHash ?? this.payloadHash,
      status: status ?? this.status,
      attemptCount: attemptCount ?? this.attemptCount,
      createdAtDeviceUtc: createdAtDeviceUtc ?? this.createdAtDeviceUtc,
      lastAttemptAtUtc: lastAttemptAtUtc ?? this.lastAttemptAtUtc,
      acceptedAtUtc: acceptedAtUtc ?? this.acceptedAtUtc,
      lastErrorCode: lastErrorCode ?? this.lastErrorCode,
      rowid: rowid ?? this.rowid,
    );
  }

  @override
  Map<String, Expression> toColumns(bool nullToAbsent) {
    final map = <String, Expression>{};
    if (operationId.present) {
      map['operation_id'] = Variable<String>(operationId.value);
    }
    if (aggregateId.present) {
      map['aggregate_id'] = Variable<String>(aggregateId.value);
    }
    if (aggregateType.present) {
      map['aggregate_type'] = Variable<String>(aggregateType.value);
    }
    if (operationType.present) {
      map['operation_type'] = Variable<String>(operationType.value);
    }
    if (localSequence.present) {
      map['local_sequence'] = Variable<int>(localSequence.value);
    }
    if (expectedCloudVersion.present) {
      map['expected_cloud_version'] = Variable<int>(expectedCloudVersion.value);
    }
    if (payloadJson.present) {
      map['payload_json'] = Variable<String>(payloadJson.value);
    }
    if (payloadHash.present) {
      map['payload_hash'] = Variable<String>(payloadHash.value);
    }
    if (status.present) {
      map['status'] = Variable<String>(status.value);
    }
    if (attemptCount.present) {
      map['attempt_count'] = Variable<int>(attemptCount.value);
    }
    if (createdAtDeviceUtc.present) {
      map['created_at_device_utc'] = Variable<String>(createdAtDeviceUtc.value);
    }
    if (lastAttemptAtUtc.present) {
      map['last_attempt_at_utc'] = Variable<String>(lastAttemptAtUtc.value);
    }
    if (acceptedAtUtc.present) {
      map['accepted_at_utc'] = Variable<String>(acceptedAtUtc.value);
    }
    if (lastErrorCode.present) {
      map['last_error_code'] = Variable<String>(lastErrorCode.value);
    }
    if (rowid.present) {
      map['rowid'] = Variable<int>(rowid.value);
    }
    return map;
  }

  @override
  String toString() {
    return (StringBuffer('LocalOutboxOperationsCompanion(')
          ..write('operationId: $operationId, ')
          ..write('aggregateId: $aggregateId, ')
          ..write('aggregateType: $aggregateType, ')
          ..write('operationType: $operationType, ')
          ..write('localSequence: $localSequence, ')
          ..write('expectedCloudVersion: $expectedCloudVersion, ')
          ..write('payloadJson: $payloadJson, ')
          ..write('payloadHash: $payloadHash, ')
          ..write('status: $status, ')
          ..write('attemptCount: $attemptCount, ')
          ..write('createdAtDeviceUtc: $createdAtDeviceUtc, ')
          ..write('lastAttemptAtUtc: $lastAttemptAtUtc, ')
          ..write('acceptedAtUtc: $acceptedAtUtc, ')
          ..write('lastErrorCode: $lastErrorCode, ')
          ..write('rowid: $rowid')
          ..write(')'))
        .toString();
  }
}

class $LocalSyncStatesTable extends LocalSyncStates
    with TableInfo<$LocalSyncStatesTable, LocalSyncStateRow> {
  @override
  final GeneratedDatabase attachedDatabase;
  final String? _alias;
  $LocalSyncStatesTable(this.attachedDatabase, [this._alias]);
  static const VerificationMeta _keyMeta = const VerificationMeta('key');
  @override
  late final GeneratedColumn<String> key = GeneratedColumn<String>(
    'key',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
  );
  static const VerificationMeta _eventCursorMeta = const VerificationMeta(
    'eventCursor',
  );
  @override
  late final GeneratedColumn<String> eventCursor = GeneratedColumn<String>(
    'event_cursor',
    aliasedName,
    true,
    type: DriftSqlType.string,
    requiredDuringInsert: false,
  );
  static const VerificationMeta _lastSuccessfulSyncAtUtcMeta =
      const VerificationMeta('lastSuccessfulSyncAtUtc');
  @override
  late final GeneratedColumn<String> lastSuccessfulSyncAtUtc =
      GeneratedColumn<String>(
        'last_successful_sync_at_utc',
        aliasedName,
        true,
        type: DriftSqlType.string,
        requiredDuringInsert: false,
      );
  static const VerificationMeta _updatedAtUtcMeta = const VerificationMeta(
    'updatedAtUtc',
  );
  @override
  late final GeneratedColumn<String> updatedAtUtc = GeneratedColumn<String>(
    'updated_at_utc',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
  );
  @override
  List<GeneratedColumn> get $columns => [
    key,
    eventCursor,
    lastSuccessfulSyncAtUtc,
    updatedAtUtc,
  ];
  @override
  String get aliasedName => _alias ?? actualTableName;
  @override
  String get actualTableName => $name;
  static const String $name = 'local_sync_state';
  @override
  VerificationContext validateIntegrity(
    Insertable<LocalSyncStateRow> instance, {
    bool isInserting = false,
  }) {
    final context = VerificationContext();
    final data = instance.toColumns(true);
    if (data.containsKey('key')) {
      context.handle(
        _keyMeta,
        key.isAcceptableOrUnknown(data['key']!, _keyMeta),
      );
    } else if (isInserting) {
      context.missing(_keyMeta);
    }
    if (data.containsKey('event_cursor')) {
      context.handle(
        _eventCursorMeta,
        eventCursor.isAcceptableOrUnknown(
          data['event_cursor']!,
          _eventCursorMeta,
        ),
      );
    }
    if (data.containsKey('last_successful_sync_at_utc')) {
      context.handle(
        _lastSuccessfulSyncAtUtcMeta,
        lastSuccessfulSyncAtUtc.isAcceptableOrUnknown(
          data['last_successful_sync_at_utc']!,
          _lastSuccessfulSyncAtUtcMeta,
        ),
      );
    }
    if (data.containsKey('updated_at_utc')) {
      context.handle(
        _updatedAtUtcMeta,
        updatedAtUtc.isAcceptableOrUnknown(
          data['updated_at_utc']!,
          _updatedAtUtcMeta,
        ),
      );
    } else if (isInserting) {
      context.missing(_updatedAtUtcMeta);
    }
    return context;
  }

  @override
  Set<GeneratedColumn> get $primaryKey => {key};
  @override
  LocalSyncStateRow map(Map<String, dynamic> data, {String? tablePrefix}) {
    final effectivePrefix = tablePrefix != null ? '$tablePrefix.' : '';
    return LocalSyncStateRow(
      key: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}key'],
      )!,
      eventCursor: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}event_cursor'],
      ),
      lastSuccessfulSyncAtUtc: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}last_successful_sync_at_utc'],
      ),
      updatedAtUtc: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}updated_at_utc'],
      )!,
    );
  }

  @override
  $LocalSyncStatesTable createAlias(String alias) {
    return $LocalSyncStatesTable(attachedDatabase, alias);
  }
}

class LocalSyncStateRow extends DataClass
    implements Insertable<LocalSyncStateRow> {
  final String key;
  final String? eventCursor;
  final String? lastSuccessfulSyncAtUtc;
  final String updatedAtUtc;
  const LocalSyncStateRow({
    required this.key,
    this.eventCursor,
    this.lastSuccessfulSyncAtUtc,
    required this.updatedAtUtc,
  });
  @override
  Map<String, Expression> toColumns(bool nullToAbsent) {
    final map = <String, Expression>{};
    map['key'] = Variable<String>(key);
    if (!nullToAbsent || eventCursor != null) {
      map['event_cursor'] = Variable<String>(eventCursor);
    }
    if (!nullToAbsent || lastSuccessfulSyncAtUtc != null) {
      map['last_successful_sync_at_utc'] = Variable<String>(
        lastSuccessfulSyncAtUtc,
      );
    }
    map['updated_at_utc'] = Variable<String>(updatedAtUtc);
    return map;
  }

  LocalSyncStatesCompanion toCompanion(bool nullToAbsent) {
    return LocalSyncStatesCompanion(
      key: Value(key),
      eventCursor: eventCursor == null && nullToAbsent
          ? const Value.absent()
          : Value(eventCursor),
      lastSuccessfulSyncAtUtc: lastSuccessfulSyncAtUtc == null && nullToAbsent
          ? const Value.absent()
          : Value(lastSuccessfulSyncAtUtc),
      updatedAtUtc: Value(updatedAtUtc),
    );
  }

  factory LocalSyncStateRow.fromJson(
    Map<String, dynamic> json, {
    ValueSerializer? serializer,
  }) {
    serializer ??= driftRuntimeOptions.defaultSerializer;
    return LocalSyncStateRow(
      key: serializer.fromJson<String>(json['key']),
      eventCursor: serializer.fromJson<String?>(json['eventCursor']),
      lastSuccessfulSyncAtUtc: serializer.fromJson<String?>(
        json['lastSuccessfulSyncAtUtc'],
      ),
      updatedAtUtc: serializer.fromJson<String>(json['updatedAtUtc']),
    );
  }
  @override
  Map<String, dynamic> toJson({ValueSerializer? serializer}) {
    serializer ??= driftRuntimeOptions.defaultSerializer;
    return <String, dynamic>{
      'key': serializer.toJson<String>(key),
      'eventCursor': serializer.toJson<String?>(eventCursor),
      'lastSuccessfulSyncAtUtc': serializer.toJson<String?>(
        lastSuccessfulSyncAtUtc,
      ),
      'updatedAtUtc': serializer.toJson<String>(updatedAtUtc),
    };
  }

  LocalSyncStateRow copyWith({
    String? key,
    Value<String?> eventCursor = const Value.absent(),
    Value<String?> lastSuccessfulSyncAtUtc = const Value.absent(),
    String? updatedAtUtc,
  }) => LocalSyncStateRow(
    key: key ?? this.key,
    eventCursor: eventCursor.present ? eventCursor.value : this.eventCursor,
    lastSuccessfulSyncAtUtc: lastSuccessfulSyncAtUtc.present
        ? lastSuccessfulSyncAtUtc.value
        : this.lastSuccessfulSyncAtUtc,
    updatedAtUtc: updatedAtUtc ?? this.updatedAtUtc,
  );
  LocalSyncStateRow copyWithCompanion(LocalSyncStatesCompanion data) {
    return LocalSyncStateRow(
      key: data.key.present ? data.key.value : this.key,
      eventCursor: data.eventCursor.present
          ? data.eventCursor.value
          : this.eventCursor,
      lastSuccessfulSyncAtUtc: data.lastSuccessfulSyncAtUtc.present
          ? data.lastSuccessfulSyncAtUtc.value
          : this.lastSuccessfulSyncAtUtc,
      updatedAtUtc: data.updatedAtUtc.present
          ? data.updatedAtUtc.value
          : this.updatedAtUtc,
    );
  }

  @override
  String toString() {
    return (StringBuffer('LocalSyncStateRow(')
          ..write('key: $key, ')
          ..write('eventCursor: $eventCursor, ')
          ..write('lastSuccessfulSyncAtUtc: $lastSuccessfulSyncAtUtc, ')
          ..write('updatedAtUtc: $updatedAtUtc')
          ..write(')'))
        .toString();
  }

  @override
  int get hashCode =>
      Object.hash(key, eventCursor, lastSuccessfulSyncAtUtc, updatedAtUtc);
  @override
  bool operator ==(Object other) =>
      identical(this, other) ||
      (other is LocalSyncStateRow &&
          other.key == this.key &&
          other.eventCursor == this.eventCursor &&
          other.lastSuccessfulSyncAtUtc == this.lastSuccessfulSyncAtUtc &&
          other.updatedAtUtc == this.updatedAtUtc);
}

class LocalSyncStatesCompanion extends UpdateCompanion<LocalSyncStateRow> {
  final Value<String> key;
  final Value<String?> eventCursor;
  final Value<String?> lastSuccessfulSyncAtUtc;
  final Value<String> updatedAtUtc;
  final Value<int> rowid;
  const LocalSyncStatesCompanion({
    this.key = const Value.absent(),
    this.eventCursor = const Value.absent(),
    this.lastSuccessfulSyncAtUtc = const Value.absent(),
    this.updatedAtUtc = const Value.absent(),
    this.rowid = const Value.absent(),
  });
  LocalSyncStatesCompanion.insert({
    required String key,
    this.eventCursor = const Value.absent(),
    this.lastSuccessfulSyncAtUtc = const Value.absent(),
    required String updatedAtUtc,
    this.rowid = const Value.absent(),
  }) : key = Value(key),
       updatedAtUtc = Value(updatedAtUtc);
  static Insertable<LocalSyncStateRow> custom({
    Expression<String>? key,
    Expression<String>? eventCursor,
    Expression<String>? lastSuccessfulSyncAtUtc,
    Expression<String>? updatedAtUtc,
    Expression<int>? rowid,
  }) {
    return RawValuesInsertable({
      if (key != null) 'key': key,
      if (eventCursor != null) 'event_cursor': eventCursor,
      if (lastSuccessfulSyncAtUtc != null)
        'last_successful_sync_at_utc': lastSuccessfulSyncAtUtc,
      if (updatedAtUtc != null) 'updated_at_utc': updatedAtUtc,
      if (rowid != null) 'rowid': rowid,
    });
  }

  LocalSyncStatesCompanion copyWith({
    Value<String>? key,
    Value<String?>? eventCursor,
    Value<String?>? lastSuccessfulSyncAtUtc,
    Value<String>? updatedAtUtc,
    Value<int>? rowid,
  }) {
    return LocalSyncStatesCompanion(
      key: key ?? this.key,
      eventCursor: eventCursor ?? this.eventCursor,
      lastSuccessfulSyncAtUtc:
          lastSuccessfulSyncAtUtc ?? this.lastSuccessfulSyncAtUtc,
      updatedAtUtc: updatedAtUtc ?? this.updatedAtUtc,
      rowid: rowid ?? this.rowid,
    );
  }

  @override
  Map<String, Expression> toColumns(bool nullToAbsent) {
    final map = <String, Expression>{};
    if (key.present) {
      map['key'] = Variable<String>(key.value);
    }
    if (eventCursor.present) {
      map['event_cursor'] = Variable<String>(eventCursor.value);
    }
    if (lastSuccessfulSyncAtUtc.present) {
      map['last_successful_sync_at_utc'] = Variable<String>(
        lastSuccessfulSyncAtUtc.value,
      );
    }
    if (updatedAtUtc.present) {
      map['updated_at_utc'] = Variable<String>(updatedAtUtc.value);
    }
    if (rowid.present) {
      map['rowid'] = Variable<int>(rowid.value);
    }
    return map;
  }

  @override
  String toString() {
    return (StringBuffer('LocalSyncStatesCompanion(')
          ..write('key: $key, ')
          ..write('eventCursor: $eventCursor, ')
          ..write('lastSuccessfulSyncAtUtc: $lastSuccessfulSyncAtUtc, ')
          ..write('updatedAtUtc: $updatedAtUtc, ')
          ..write('rowid: $rowid')
          ..write(')'))
        .toString();
  }
}

abstract class _$TraderProLocalDatabase extends GeneratedDatabase {
  _$TraderProLocalDatabase(QueryExecutor e) : super(e);
  $TraderProLocalDatabaseManager get managers =>
      $TraderProLocalDatabaseManager(this);
  late final $LocalReceivingSessionsTable localReceivingSessions =
      $LocalReceivingSessionsTable(this);
  late final $LocalReceivingEntriesTable localReceivingEntries =
      $LocalReceivingEntriesTable(this);
  late final $LocalOutboxOperationsTable localOutboxOperations =
      $LocalOutboxOperationsTable(this);
  late final $LocalSyncStatesTable localSyncStates = $LocalSyncStatesTable(
    this,
  );
  @override
  Iterable<TableInfo<Table, Object?>> get allTables =>
      allSchemaEntities.whereType<TableInfo<Table, Object?>>();
  @override
  List<DatabaseSchemaEntity> get allSchemaEntities => [
    localReceivingSessions,
    localReceivingEntries,
    localOutboxOperations,
    localSyncStates,
  ];
}

typedef $$LocalReceivingSessionsTableCreateCompanionBuilder =
    LocalReceivingSessionsCompanion Function({
      required String id,
      Value<String?> cloudId,
      required String temporaryReference,
      required String localStatus,
      Value<String?> cloudStatus,
      required int localVersion,
      Value<int?> cloudVersion,
      required int nextLocalSequence,
      required int activeEntryCount,
      required String processedTotalWeightKg,
      required String createdAtDeviceUtc,
      required String updatedAtDeviceUtc,
      Value<String?> lastCloudSyncAtUtc,
      Value<int> rowid,
    });
typedef $$LocalReceivingSessionsTableUpdateCompanionBuilder =
    LocalReceivingSessionsCompanion Function({
      Value<String> id,
      Value<String?> cloudId,
      Value<String> temporaryReference,
      Value<String> localStatus,
      Value<String?> cloudStatus,
      Value<int> localVersion,
      Value<int?> cloudVersion,
      Value<int> nextLocalSequence,
      Value<int> activeEntryCount,
      Value<String> processedTotalWeightKg,
      Value<String> createdAtDeviceUtc,
      Value<String> updatedAtDeviceUtc,
      Value<String?> lastCloudSyncAtUtc,
      Value<int> rowid,
    });

final class $$LocalReceivingSessionsTableReferences
    extends
        BaseReferences<
          _$TraderProLocalDatabase,
          $LocalReceivingSessionsTable,
          LocalReceivingSessionRow
        > {
  $$LocalReceivingSessionsTableReferences(
    super.$_db,
    super.$_table,
    super.$_typedResult,
  );

  static MultiTypedResultKey<
    $LocalReceivingEntriesTable,
    List<LocalReceivingEntryRow>
  >
  _localReceivingEntriesRefsTable(
    _$TraderProLocalDatabase db,
  ) => MultiTypedResultKey.fromTable(
    db.localReceivingEntries,
    aliasName:
        'local_receiving_sessions__id__local_receiving_entries__receiving_session_id',
  );

  $$LocalReceivingEntriesTableProcessedTableManager
  get localReceivingEntriesRefs {
    final manager =
        $$LocalReceivingEntriesTableTableManager(
          $_db,
          $_db.localReceivingEntries,
        ).filter(
          (f) => f.receivingSessionId.id.sqlEquals($_itemColumn<String>('id')!),
        );

    final cache = $_typedResult.readTableOrNull(
      _localReceivingEntriesRefsTable($_db),
    );
    return ProcessedTableManager(
      manager.$state.copyWith(prefetchedData: cache),
    );
  }
}

class $$LocalReceivingSessionsTableFilterComposer
    extends Composer<_$TraderProLocalDatabase, $LocalReceivingSessionsTable> {
  $$LocalReceivingSessionsTableFilterComposer({
    required super.$db,
    required super.$table,
    super.joinBuilder,
    super.$addJoinBuilderToRootComposer,
    super.$removeJoinBuilderFromRootComposer,
  });
  ColumnFilters<String> get id => $composableBuilder(
    column: $table.id,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get cloudId => $composableBuilder(
    column: $table.cloudId,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get temporaryReference => $composableBuilder(
    column: $table.temporaryReference,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get localStatus => $composableBuilder(
    column: $table.localStatus,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get cloudStatus => $composableBuilder(
    column: $table.cloudStatus,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<int> get localVersion => $composableBuilder(
    column: $table.localVersion,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<int> get cloudVersion => $composableBuilder(
    column: $table.cloudVersion,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<int> get nextLocalSequence => $composableBuilder(
    column: $table.nextLocalSequence,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<int> get activeEntryCount => $composableBuilder(
    column: $table.activeEntryCount,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get processedTotalWeightKg => $composableBuilder(
    column: $table.processedTotalWeightKg,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get createdAtDeviceUtc => $composableBuilder(
    column: $table.createdAtDeviceUtc,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get updatedAtDeviceUtc => $composableBuilder(
    column: $table.updatedAtDeviceUtc,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get lastCloudSyncAtUtc => $composableBuilder(
    column: $table.lastCloudSyncAtUtc,
    builder: (column) => ColumnFilters(column),
  );

  Expression<bool> localReceivingEntriesRefs(
    Expression<bool> Function($$LocalReceivingEntriesTableFilterComposer f) f,
  ) {
    final $$LocalReceivingEntriesTableFilterComposer composer =
        $composerBuilder(
          composer: this,
          getCurrentColumn: (t) => t.id,
          referencedTable: $db.localReceivingEntries,
          getReferencedColumn: (t) => t.receivingSessionId,
          builder:
              (
                joinBuilder, {
                $addJoinBuilderToRootComposer,
                $removeJoinBuilderFromRootComposer,
              }) => $$LocalReceivingEntriesTableFilterComposer(
                $db: $db,
                $table: $db.localReceivingEntries,
                $addJoinBuilderToRootComposer: $addJoinBuilderToRootComposer,
                joinBuilder: joinBuilder,
                $removeJoinBuilderFromRootComposer:
                    $removeJoinBuilderFromRootComposer,
              ),
        );
    return f(composer);
  }
}

class $$LocalReceivingSessionsTableOrderingComposer
    extends Composer<_$TraderProLocalDatabase, $LocalReceivingSessionsTable> {
  $$LocalReceivingSessionsTableOrderingComposer({
    required super.$db,
    required super.$table,
    super.joinBuilder,
    super.$addJoinBuilderToRootComposer,
    super.$removeJoinBuilderFromRootComposer,
  });
  ColumnOrderings<String> get id => $composableBuilder(
    column: $table.id,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get cloudId => $composableBuilder(
    column: $table.cloudId,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get temporaryReference => $composableBuilder(
    column: $table.temporaryReference,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get localStatus => $composableBuilder(
    column: $table.localStatus,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get cloudStatus => $composableBuilder(
    column: $table.cloudStatus,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<int> get localVersion => $composableBuilder(
    column: $table.localVersion,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<int> get cloudVersion => $composableBuilder(
    column: $table.cloudVersion,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<int> get nextLocalSequence => $composableBuilder(
    column: $table.nextLocalSequence,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<int> get activeEntryCount => $composableBuilder(
    column: $table.activeEntryCount,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get processedTotalWeightKg => $composableBuilder(
    column: $table.processedTotalWeightKg,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get createdAtDeviceUtc => $composableBuilder(
    column: $table.createdAtDeviceUtc,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get updatedAtDeviceUtc => $composableBuilder(
    column: $table.updatedAtDeviceUtc,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get lastCloudSyncAtUtc => $composableBuilder(
    column: $table.lastCloudSyncAtUtc,
    builder: (column) => ColumnOrderings(column),
  );
}

class $$LocalReceivingSessionsTableAnnotationComposer
    extends Composer<_$TraderProLocalDatabase, $LocalReceivingSessionsTable> {
  $$LocalReceivingSessionsTableAnnotationComposer({
    required super.$db,
    required super.$table,
    super.joinBuilder,
    super.$addJoinBuilderToRootComposer,
    super.$removeJoinBuilderFromRootComposer,
  });
  GeneratedColumn<String> get id =>
      $composableBuilder(column: $table.id, builder: (column) => column);

  GeneratedColumn<String> get cloudId =>
      $composableBuilder(column: $table.cloudId, builder: (column) => column);

  GeneratedColumn<String> get temporaryReference => $composableBuilder(
    column: $table.temporaryReference,
    builder: (column) => column,
  );

  GeneratedColumn<String> get localStatus => $composableBuilder(
    column: $table.localStatus,
    builder: (column) => column,
  );

  GeneratedColumn<String> get cloudStatus => $composableBuilder(
    column: $table.cloudStatus,
    builder: (column) => column,
  );

  GeneratedColumn<int> get localVersion => $composableBuilder(
    column: $table.localVersion,
    builder: (column) => column,
  );

  GeneratedColumn<int> get cloudVersion => $composableBuilder(
    column: $table.cloudVersion,
    builder: (column) => column,
  );

  GeneratedColumn<int> get nextLocalSequence => $composableBuilder(
    column: $table.nextLocalSequence,
    builder: (column) => column,
  );

  GeneratedColumn<int> get activeEntryCount => $composableBuilder(
    column: $table.activeEntryCount,
    builder: (column) => column,
  );

  GeneratedColumn<String> get processedTotalWeightKg => $composableBuilder(
    column: $table.processedTotalWeightKg,
    builder: (column) => column,
  );

  GeneratedColumn<String> get createdAtDeviceUtc => $composableBuilder(
    column: $table.createdAtDeviceUtc,
    builder: (column) => column,
  );

  GeneratedColumn<String> get updatedAtDeviceUtc => $composableBuilder(
    column: $table.updatedAtDeviceUtc,
    builder: (column) => column,
  );

  GeneratedColumn<String> get lastCloudSyncAtUtc => $composableBuilder(
    column: $table.lastCloudSyncAtUtc,
    builder: (column) => column,
  );

  Expression<T> localReceivingEntriesRefs<T extends Object>(
    Expression<T> Function($$LocalReceivingEntriesTableAnnotationComposer a) f,
  ) {
    final $$LocalReceivingEntriesTableAnnotationComposer composer =
        $composerBuilder(
          composer: this,
          getCurrentColumn: (t) => t.id,
          referencedTable: $db.localReceivingEntries,
          getReferencedColumn: (t) => t.receivingSessionId,
          builder:
              (
                joinBuilder, {
                $addJoinBuilderToRootComposer,
                $removeJoinBuilderFromRootComposer,
              }) => $$LocalReceivingEntriesTableAnnotationComposer(
                $db: $db,
                $table: $db.localReceivingEntries,
                $addJoinBuilderToRootComposer: $addJoinBuilderToRootComposer,
                joinBuilder: joinBuilder,
                $removeJoinBuilderFromRootComposer:
                    $removeJoinBuilderFromRootComposer,
              ),
        );
    return f(composer);
  }
}

class $$LocalReceivingSessionsTableTableManager
    extends
        RootTableManager<
          _$TraderProLocalDatabase,
          $LocalReceivingSessionsTable,
          LocalReceivingSessionRow,
          $$LocalReceivingSessionsTableFilterComposer,
          $$LocalReceivingSessionsTableOrderingComposer,
          $$LocalReceivingSessionsTableAnnotationComposer,
          $$LocalReceivingSessionsTableCreateCompanionBuilder,
          $$LocalReceivingSessionsTableUpdateCompanionBuilder,
          (LocalReceivingSessionRow, $$LocalReceivingSessionsTableReferences),
          LocalReceivingSessionRow,
          PrefetchHooks Function({bool localReceivingEntriesRefs})
        > {
  $$LocalReceivingSessionsTableTableManager(
    _$TraderProLocalDatabase db,
    $LocalReceivingSessionsTable table,
  ) : super(
        TableManagerState(
          db: db,
          table: table,
          createFilteringComposer: () =>
              $$LocalReceivingSessionsTableFilterComposer(
                $db: db,
                $table: table,
              ),
          createOrderingComposer: () =>
              $$LocalReceivingSessionsTableOrderingComposer(
                $db: db,
                $table: table,
              ),
          createComputedFieldComposer: () =>
              $$LocalReceivingSessionsTableAnnotationComposer(
                $db: db,
                $table: table,
              ),
          updateCompanionCallback:
              ({
                Value<String> id = const Value.absent(),
                Value<String?> cloudId = const Value.absent(),
                Value<String> temporaryReference = const Value.absent(),
                Value<String> localStatus = const Value.absent(),
                Value<String?> cloudStatus = const Value.absent(),
                Value<int> localVersion = const Value.absent(),
                Value<int?> cloudVersion = const Value.absent(),
                Value<int> nextLocalSequence = const Value.absent(),
                Value<int> activeEntryCount = const Value.absent(),
                Value<String> processedTotalWeightKg = const Value.absent(),
                Value<String> createdAtDeviceUtc = const Value.absent(),
                Value<String> updatedAtDeviceUtc = const Value.absent(),
                Value<String?> lastCloudSyncAtUtc = const Value.absent(),
                Value<int> rowid = const Value.absent(),
              }) => LocalReceivingSessionsCompanion(
                id: id,
                cloudId: cloudId,
                temporaryReference: temporaryReference,
                localStatus: localStatus,
                cloudStatus: cloudStatus,
                localVersion: localVersion,
                cloudVersion: cloudVersion,
                nextLocalSequence: nextLocalSequence,
                activeEntryCount: activeEntryCount,
                processedTotalWeightKg: processedTotalWeightKg,
                createdAtDeviceUtc: createdAtDeviceUtc,
                updatedAtDeviceUtc: updatedAtDeviceUtc,
                lastCloudSyncAtUtc: lastCloudSyncAtUtc,
                rowid: rowid,
              ),
          createCompanionCallback:
              ({
                required String id,
                Value<String?> cloudId = const Value.absent(),
                required String temporaryReference,
                required String localStatus,
                Value<String?> cloudStatus = const Value.absent(),
                required int localVersion,
                Value<int?> cloudVersion = const Value.absent(),
                required int nextLocalSequence,
                required int activeEntryCount,
                required String processedTotalWeightKg,
                required String createdAtDeviceUtc,
                required String updatedAtDeviceUtc,
                Value<String?> lastCloudSyncAtUtc = const Value.absent(),
                Value<int> rowid = const Value.absent(),
              }) => LocalReceivingSessionsCompanion.insert(
                id: id,
                cloudId: cloudId,
                temporaryReference: temporaryReference,
                localStatus: localStatus,
                cloudStatus: cloudStatus,
                localVersion: localVersion,
                cloudVersion: cloudVersion,
                nextLocalSequence: nextLocalSequence,
                activeEntryCount: activeEntryCount,
                processedTotalWeightKg: processedTotalWeightKg,
                createdAtDeviceUtc: createdAtDeviceUtc,
                updatedAtDeviceUtc: updatedAtDeviceUtc,
                lastCloudSyncAtUtc: lastCloudSyncAtUtc,
                rowid: rowid,
              ),
          withReferenceMapper: (p0) => p0
              .map(
                (e) => (
                  e.readTable(table),
                  $$LocalReceivingSessionsTableReferences(db, table, e),
                ),
              )
              .toList(),
          prefetchHooksCallback: ({localReceivingEntriesRefs = false}) {
            return PrefetchHooks(
              db: db,
              explicitlyWatchedTables: [
                if (localReceivingEntriesRefs) db.localReceivingEntries,
              ],
              addJoins: null,
              getPrefetchedDataCallback: (items) async {
                return [
                  if (localReceivingEntriesRefs)
                    await $_getPrefetchedData<
                      LocalReceivingSessionRow,
                      $LocalReceivingSessionsTable,
                      LocalReceivingEntryRow
                    >(
                      currentTable: table,
                      referencedTable: $$LocalReceivingSessionsTableReferences
                          ._localReceivingEntriesRefsTable(db),
                      managerFromTypedResult: (p0) =>
                          $$LocalReceivingSessionsTableReferences(
                            db,
                            table,
                            p0,
                          ).localReceivingEntriesRefs,
                      referencedItemsForCurrentItem: (item, referencedItems) =>
                          referencedItems.where(
                            (e) => e.receivingSessionId == item.id,
                          ),
                      typedResults: items,
                    ),
                ];
              },
            );
          },
        ),
      );
}

typedef $$LocalReceivingSessionsTableProcessedTableManager =
    ProcessedTableManager<
      _$TraderProLocalDatabase,
      $LocalReceivingSessionsTable,
      LocalReceivingSessionRow,
      $$LocalReceivingSessionsTableFilterComposer,
      $$LocalReceivingSessionsTableOrderingComposer,
      $$LocalReceivingSessionsTableAnnotationComposer,
      $$LocalReceivingSessionsTableCreateCompanionBuilder,
      $$LocalReceivingSessionsTableUpdateCompanionBuilder,
      (LocalReceivingSessionRow, $$LocalReceivingSessionsTableReferences),
      LocalReceivingSessionRow,
      PrefetchHooks Function({bool localReceivingEntriesRefs})
    >;
typedef $$LocalReceivingEntriesTableCreateCompanionBuilder =
    LocalReceivingEntriesCompanion Function({
      required String id,
      required String receivingSessionId,
      required String operationId,
      required int localSequence,
      required String productReference,
      required String bagTypeReference,
      required int bagCount,
      required String rawWeightKg,
      required String processedWeightKg,
      required String displayWeightKg,
      required int decimalPlaces,
      required String processingMethod,
      required String weightSource,
      required String entryStatus,
      Value<String?> reversalOfEntryId,
      required String capturedAtDeviceUtc,
      required String createdAtDeviceUtc,
      Value<int> rowid,
    });
typedef $$LocalReceivingEntriesTableUpdateCompanionBuilder =
    LocalReceivingEntriesCompanion Function({
      Value<String> id,
      Value<String> receivingSessionId,
      Value<String> operationId,
      Value<int> localSequence,
      Value<String> productReference,
      Value<String> bagTypeReference,
      Value<int> bagCount,
      Value<String> rawWeightKg,
      Value<String> processedWeightKg,
      Value<String> displayWeightKg,
      Value<int> decimalPlaces,
      Value<String> processingMethod,
      Value<String> weightSource,
      Value<String> entryStatus,
      Value<String?> reversalOfEntryId,
      Value<String> capturedAtDeviceUtc,
      Value<String> createdAtDeviceUtc,
      Value<int> rowid,
    });

final class $$LocalReceivingEntriesTableReferences
    extends
        BaseReferences<
          _$TraderProLocalDatabase,
          $LocalReceivingEntriesTable,
          LocalReceivingEntryRow
        > {
  $$LocalReceivingEntriesTableReferences(
    super.$_db,
    super.$_table,
    super.$_typedResult,
  );

  static $LocalReceivingSessionsTable _receivingSessionIdTable(
    _$TraderProLocalDatabase db,
  ) => db.localReceivingSessions.createAlias(
    'local_receiving_entries__receiving_session_id__local_receiving_sessions__id',
  );

  $$LocalReceivingSessionsTableProcessedTableManager get receivingSessionId {
    final $_column = $_itemColumn<String>('receiving_session_id')!;

    final manager = $$LocalReceivingSessionsTableTableManager(
      $_db,
      $_db.localReceivingSessions,
    ).filter((f) => f.id.sqlEquals($_column));
    final item = $_typedResult.readTableOrNull(_receivingSessionIdTable($_db));
    if (item == null) return manager;
    return ProcessedTableManager(
      manager.$state.copyWith(prefetchedData: [item]),
    );
  }
}

class $$LocalReceivingEntriesTableFilterComposer
    extends Composer<_$TraderProLocalDatabase, $LocalReceivingEntriesTable> {
  $$LocalReceivingEntriesTableFilterComposer({
    required super.$db,
    required super.$table,
    super.joinBuilder,
    super.$addJoinBuilderToRootComposer,
    super.$removeJoinBuilderFromRootComposer,
  });
  ColumnFilters<String> get id => $composableBuilder(
    column: $table.id,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get operationId => $composableBuilder(
    column: $table.operationId,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<int> get localSequence => $composableBuilder(
    column: $table.localSequence,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get productReference => $composableBuilder(
    column: $table.productReference,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get bagTypeReference => $composableBuilder(
    column: $table.bagTypeReference,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<int> get bagCount => $composableBuilder(
    column: $table.bagCount,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get rawWeightKg => $composableBuilder(
    column: $table.rawWeightKg,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get processedWeightKg => $composableBuilder(
    column: $table.processedWeightKg,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get displayWeightKg => $composableBuilder(
    column: $table.displayWeightKg,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<int> get decimalPlaces => $composableBuilder(
    column: $table.decimalPlaces,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get processingMethod => $composableBuilder(
    column: $table.processingMethod,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get weightSource => $composableBuilder(
    column: $table.weightSource,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get entryStatus => $composableBuilder(
    column: $table.entryStatus,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get reversalOfEntryId => $composableBuilder(
    column: $table.reversalOfEntryId,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get capturedAtDeviceUtc => $composableBuilder(
    column: $table.capturedAtDeviceUtc,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get createdAtDeviceUtc => $composableBuilder(
    column: $table.createdAtDeviceUtc,
    builder: (column) => ColumnFilters(column),
  );

  $$LocalReceivingSessionsTableFilterComposer get receivingSessionId {
    final $$LocalReceivingSessionsTableFilterComposer composer =
        $composerBuilder(
          composer: this,
          getCurrentColumn: (t) => t.receivingSessionId,
          referencedTable: $db.localReceivingSessions,
          getReferencedColumn: (t) => t.id,
          builder:
              (
                joinBuilder, {
                $addJoinBuilderToRootComposer,
                $removeJoinBuilderFromRootComposer,
              }) => $$LocalReceivingSessionsTableFilterComposer(
                $db: $db,
                $table: $db.localReceivingSessions,
                $addJoinBuilderToRootComposer: $addJoinBuilderToRootComposer,
                joinBuilder: joinBuilder,
                $removeJoinBuilderFromRootComposer:
                    $removeJoinBuilderFromRootComposer,
              ),
        );
    return composer;
  }
}

class $$LocalReceivingEntriesTableOrderingComposer
    extends Composer<_$TraderProLocalDatabase, $LocalReceivingEntriesTable> {
  $$LocalReceivingEntriesTableOrderingComposer({
    required super.$db,
    required super.$table,
    super.joinBuilder,
    super.$addJoinBuilderToRootComposer,
    super.$removeJoinBuilderFromRootComposer,
  });
  ColumnOrderings<String> get id => $composableBuilder(
    column: $table.id,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get operationId => $composableBuilder(
    column: $table.operationId,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<int> get localSequence => $composableBuilder(
    column: $table.localSequence,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get productReference => $composableBuilder(
    column: $table.productReference,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get bagTypeReference => $composableBuilder(
    column: $table.bagTypeReference,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<int> get bagCount => $composableBuilder(
    column: $table.bagCount,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get rawWeightKg => $composableBuilder(
    column: $table.rawWeightKg,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get processedWeightKg => $composableBuilder(
    column: $table.processedWeightKg,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get displayWeightKg => $composableBuilder(
    column: $table.displayWeightKg,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<int> get decimalPlaces => $composableBuilder(
    column: $table.decimalPlaces,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get processingMethod => $composableBuilder(
    column: $table.processingMethod,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get weightSource => $composableBuilder(
    column: $table.weightSource,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get entryStatus => $composableBuilder(
    column: $table.entryStatus,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get reversalOfEntryId => $composableBuilder(
    column: $table.reversalOfEntryId,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get capturedAtDeviceUtc => $composableBuilder(
    column: $table.capturedAtDeviceUtc,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get createdAtDeviceUtc => $composableBuilder(
    column: $table.createdAtDeviceUtc,
    builder: (column) => ColumnOrderings(column),
  );

  $$LocalReceivingSessionsTableOrderingComposer get receivingSessionId {
    final $$LocalReceivingSessionsTableOrderingComposer composer =
        $composerBuilder(
          composer: this,
          getCurrentColumn: (t) => t.receivingSessionId,
          referencedTable: $db.localReceivingSessions,
          getReferencedColumn: (t) => t.id,
          builder:
              (
                joinBuilder, {
                $addJoinBuilderToRootComposer,
                $removeJoinBuilderFromRootComposer,
              }) => $$LocalReceivingSessionsTableOrderingComposer(
                $db: $db,
                $table: $db.localReceivingSessions,
                $addJoinBuilderToRootComposer: $addJoinBuilderToRootComposer,
                joinBuilder: joinBuilder,
                $removeJoinBuilderFromRootComposer:
                    $removeJoinBuilderFromRootComposer,
              ),
        );
    return composer;
  }
}

class $$LocalReceivingEntriesTableAnnotationComposer
    extends Composer<_$TraderProLocalDatabase, $LocalReceivingEntriesTable> {
  $$LocalReceivingEntriesTableAnnotationComposer({
    required super.$db,
    required super.$table,
    super.joinBuilder,
    super.$addJoinBuilderToRootComposer,
    super.$removeJoinBuilderFromRootComposer,
  });
  GeneratedColumn<String> get id =>
      $composableBuilder(column: $table.id, builder: (column) => column);

  GeneratedColumn<String> get operationId => $composableBuilder(
    column: $table.operationId,
    builder: (column) => column,
  );

  GeneratedColumn<int> get localSequence => $composableBuilder(
    column: $table.localSequence,
    builder: (column) => column,
  );

  GeneratedColumn<String> get productReference => $composableBuilder(
    column: $table.productReference,
    builder: (column) => column,
  );

  GeneratedColumn<String> get bagTypeReference => $composableBuilder(
    column: $table.bagTypeReference,
    builder: (column) => column,
  );

  GeneratedColumn<int> get bagCount =>
      $composableBuilder(column: $table.bagCount, builder: (column) => column);

  GeneratedColumn<String> get rawWeightKg => $composableBuilder(
    column: $table.rawWeightKg,
    builder: (column) => column,
  );

  GeneratedColumn<String> get processedWeightKg => $composableBuilder(
    column: $table.processedWeightKg,
    builder: (column) => column,
  );

  GeneratedColumn<String> get displayWeightKg => $composableBuilder(
    column: $table.displayWeightKg,
    builder: (column) => column,
  );

  GeneratedColumn<int> get decimalPlaces => $composableBuilder(
    column: $table.decimalPlaces,
    builder: (column) => column,
  );

  GeneratedColumn<String> get processingMethod => $composableBuilder(
    column: $table.processingMethod,
    builder: (column) => column,
  );

  GeneratedColumn<String> get weightSource => $composableBuilder(
    column: $table.weightSource,
    builder: (column) => column,
  );

  GeneratedColumn<String> get entryStatus => $composableBuilder(
    column: $table.entryStatus,
    builder: (column) => column,
  );

  GeneratedColumn<String> get reversalOfEntryId => $composableBuilder(
    column: $table.reversalOfEntryId,
    builder: (column) => column,
  );

  GeneratedColumn<String> get capturedAtDeviceUtc => $composableBuilder(
    column: $table.capturedAtDeviceUtc,
    builder: (column) => column,
  );

  GeneratedColumn<String> get createdAtDeviceUtc => $composableBuilder(
    column: $table.createdAtDeviceUtc,
    builder: (column) => column,
  );

  $$LocalReceivingSessionsTableAnnotationComposer get receivingSessionId {
    final $$LocalReceivingSessionsTableAnnotationComposer composer =
        $composerBuilder(
          composer: this,
          getCurrentColumn: (t) => t.receivingSessionId,
          referencedTable: $db.localReceivingSessions,
          getReferencedColumn: (t) => t.id,
          builder:
              (
                joinBuilder, {
                $addJoinBuilderToRootComposer,
                $removeJoinBuilderFromRootComposer,
              }) => $$LocalReceivingSessionsTableAnnotationComposer(
                $db: $db,
                $table: $db.localReceivingSessions,
                $addJoinBuilderToRootComposer: $addJoinBuilderToRootComposer,
                joinBuilder: joinBuilder,
                $removeJoinBuilderFromRootComposer:
                    $removeJoinBuilderFromRootComposer,
              ),
        );
    return composer;
  }
}

class $$LocalReceivingEntriesTableTableManager
    extends
        RootTableManager<
          _$TraderProLocalDatabase,
          $LocalReceivingEntriesTable,
          LocalReceivingEntryRow,
          $$LocalReceivingEntriesTableFilterComposer,
          $$LocalReceivingEntriesTableOrderingComposer,
          $$LocalReceivingEntriesTableAnnotationComposer,
          $$LocalReceivingEntriesTableCreateCompanionBuilder,
          $$LocalReceivingEntriesTableUpdateCompanionBuilder,
          (LocalReceivingEntryRow, $$LocalReceivingEntriesTableReferences),
          LocalReceivingEntryRow,
          PrefetchHooks Function({bool receivingSessionId})
        > {
  $$LocalReceivingEntriesTableTableManager(
    _$TraderProLocalDatabase db,
    $LocalReceivingEntriesTable table,
  ) : super(
        TableManagerState(
          db: db,
          table: table,
          createFilteringComposer: () =>
              $$LocalReceivingEntriesTableFilterComposer(
                $db: db,
                $table: table,
              ),
          createOrderingComposer: () =>
              $$LocalReceivingEntriesTableOrderingComposer(
                $db: db,
                $table: table,
              ),
          createComputedFieldComposer: () =>
              $$LocalReceivingEntriesTableAnnotationComposer(
                $db: db,
                $table: table,
              ),
          updateCompanionCallback:
              ({
                Value<String> id = const Value.absent(),
                Value<String> receivingSessionId = const Value.absent(),
                Value<String> operationId = const Value.absent(),
                Value<int> localSequence = const Value.absent(),
                Value<String> productReference = const Value.absent(),
                Value<String> bagTypeReference = const Value.absent(),
                Value<int> bagCount = const Value.absent(),
                Value<String> rawWeightKg = const Value.absent(),
                Value<String> processedWeightKg = const Value.absent(),
                Value<String> displayWeightKg = const Value.absent(),
                Value<int> decimalPlaces = const Value.absent(),
                Value<String> processingMethod = const Value.absent(),
                Value<String> weightSource = const Value.absent(),
                Value<String> entryStatus = const Value.absent(),
                Value<String?> reversalOfEntryId = const Value.absent(),
                Value<String> capturedAtDeviceUtc = const Value.absent(),
                Value<String> createdAtDeviceUtc = const Value.absent(),
                Value<int> rowid = const Value.absent(),
              }) => LocalReceivingEntriesCompanion(
                id: id,
                receivingSessionId: receivingSessionId,
                operationId: operationId,
                localSequence: localSequence,
                productReference: productReference,
                bagTypeReference: bagTypeReference,
                bagCount: bagCount,
                rawWeightKg: rawWeightKg,
                processedWeightKg: processedWeightKg,
                displayWeightKg: displayWeightKg,
                decimalPlaces: decimalPlaces,
                processingMethod: processingMethod,
                weightSource: weightSource,
                entryStatus: entryStatus,
                reversalOfEntryId: reversalOfEntryId,
                capturedAtDeviceUtc: capturedAtDeviceUtc,
                createdAtDeviceUtc: createdAtDeviceUtc,
                rowid: rowid,
              ),
          createCompanionCallback:
              ({
                required String id,
                required String receivingSessionId,
                required String operationId,
                required int localSequence,
                required String productReference,
                required String bagTypeReference,
                required int bagCount,
                required String rawWeightKg,
                required String processedWeightKg,
                required String displayWeightKg,
                required int decimalPlaces,
                required String processingMethod,
                required String weightSource,
                required String entryStatus,
                Value<String?> reversalOfEntryId = const Value.absent(),
                required String capturedAtDeviceUtc,
                required String createdAtDeviceUtc,
                Value<int> rowid = const Value.absent(),
              }) => LocalReceivingEntriesCompanion.insert(
                id: id,
                receivingSessionId: receivingSessionId,
                operationId: operationId,
                localSequence: localSequence,
                productReference: productReference,
                bagTypeReference: bagTypeReference,
                bagCount: bagCount,
                rawWeightKg: rawWeightKg,
                processedWeightKg: processedWeightKg,
                displayWeightKg: displayWeightKg,
                decimalPlaces: decimalPlaces,
                processingMethod: processingMethod,
                weightSource: weightSource,
                entryStatus: entryStatus,
                reversalOfEntryId: reversalOfEntryId,
                capturedAtDeviceUtc: capturedAtDeviceUtc,
                createdAtDeviceUtc: createdAtDeviceUtc,
                rowid: rowid,
              ),
          withReferenceMapper: (p0) => p0
              .map(
                (e) => (
                  e.readTable(table),
                  $$LocalReceivingEntriesTableReferences(db, table, e),
                ),
              )
              .toList(),
          prefetchHooksCallback: ({receivingSessionId = false}) {
            return PrefetchHooks(
              db: db,
              explicitlyWatchedTables: [],
              addJoins:
                  <
                    T extends TableManagerState<
                      dynamic,
                      dynamic,
                      dynamic,
                      dynamic,
                      dynamic,
                      dynamic,
                      dynamic,
                      dynamic,
                      dynamic,
                      dynamic,
                      dynamic
                    >
                  >(state) {
                    if (receivingSessionId) {
                      state =
                          state.withJoin(
                                currentTable: table,
                                currentColumn: table.receivingSessionId,
                                referencedTable:
                                    $$LocalReceivingEntriesTableReferences
                                        ._receivingSessionIdTable(db),
                                referencedColumn:
                                    $$LocalReceivingEntriesTableReferences
                                        ._receivingSessionIdTable(db)
                                        .id,
                              )
                              as T;
                    }

                    return state;
                  },
              getPrefetchedDataCallback: (items) async {
                return [];
              },
            );
          },
        ),
      );
}

typedef $$LocalReceivingEntriesTableProcessedTableManager =
    ProcessedTableManager<
      _$TraderProLocalDatabase,
      $LocalReceivingEntriesTable,
      LocalReceivingEntryRow,
      $$LocalReceivingEntriesTableFilterComposer,
      $$LocalReceivingEntriesTableOrderingComposer,
      $$LocalReceivingEntriesTableAnnotationComposer,
      $$LocalReceivingEntriesTableCreateCompanionBuilder,
      $$LocalReceivingEntriesTableUpdateCompanionBuilder,
      (LocalReceivingEntryRow, $$LocalReceivingEntriesTableReferences),
      LocalReceivingEntryRow,
      PrefetchHooks Function({bool receivingSessionId})
    >;
typedef $$LocalOutboxOperationsTableCreateCompanionBuilder =
    LocalOutboxOperationsCompanion Function({
      required String operationId,
      required String aggregateId,
      required String aggregateType,
      required String operationType,
      required int localSequence,
      Value<int?> expectedCloudVersion,
      required String payloadJson,
      required String payloadHash,
      required String status,
      required int attemptCount,
      required String createdAtDeviceUtc,
      Value<String?> lastAttemptAtUtc,
      Value<String?> acceptedAtUtc,
      Value<String?> lastErrorCode,
      Value<int> rowid,
    });
typedef $$LocalOutboxOperationsTableUpdateCompanionBuilder =
    LocalOutboxOperationsCompanion Function({
      Value<String> operationId,
      Value<String> aggregateId,
      Value<String> aggregateType,
      Value<String> operationType,
      Value<int> localSequence,
      Value<int?> expectedCloudVersion,
      Value<String> payloadJson,
      Value<String> payloadHash,
      Value<String> status,
      Value<int> attemptCount,
      Value<String> createdAtDeviceUtc,
      Value<String?> lastAttemptAtUtc,
      Value<String?> acceptedAtUtc,
      Value<String?> lastErrorCode,
      Value<int> rowid,
    });

class $$LocalOutboxOperationsTableFilterComposer
    extends Composer<_$TraderProLocalDatabase, $LocalOutboxOperationsTable> {
  $$LocalOutboxOperationsTableFilterComposer({
    required super.$db,
    required super.$table,
    super.joinBuilder,
    super.$addJoinBuilderToRootComposer,
    super.$removeJoinBuilderFromRootComposer,
  });
  ColumnFilters<String> get operationId => $composableBuilder(
    column: $table.operationId,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get aggregateId => $composableBuilder(
    column: $table.aggregateId,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get aggregateType => $composableBuilder(
    column: $table.aggregateType,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get operationType => $composableBuilder(
    column: $table.operationType,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<int> get localSequence => $composableBuilder(
    column: $table.localSequence,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<int> get expectedCloudVersion => $composableBuilder(
    column: $table.expectedCloudVersion,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get payloadJson => $composableBuilder(
    column: $table.payloadJson,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get payloadHash => $composableBuilder(
    column: $table.payloadHash,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get status => $composableBuilder(
    column: $table.status,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<int> get attemptCount => $composableBuilder(
    column: $table.attemptCount,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get createdAtDeviceUtc => $composableBuilder(
    column: $table.createdAtDeviceUtc,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get lastAttemptAtUtc => $composableBuilder(
    column: $table.lastAttemptAtUtc,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get acceptedAtUtc => $composableBuilder(
    column: $table.acceptedAtUtc,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get lastErrorCode => $composableBuilder(
    column: $table.lastErrorCode,
    builder: (column) => ColumnFilters(column),
  );
}

class $$LocalOutboxOperationsTableOrderingComposer
    extends Composer<_$TraderProLocalDatabase, $LocalOutboxOperationsTable> {
  $$LocalOutboxOperationsTableOrderingComposer({
    required super.$db,
    required super.$table,
    super.joinBuilder,
    super.$addJoinBuilderToRootComposer,
    super.$removeJoinBuilderFromRootComposer,
  });
  ColumnOrderings<String> get operationId => $composableBuilder(
    column: $table.operationId,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get aggregateId => $composableBuilder(
    column: $table.aggregateId,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get aggregateType => $composableBuilder(
    column: $table.aggregateType,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get operationType => $composableBuilder(
    column: $table.operationType,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<int> get localSequence => $composableBuilder(
    column: $table.localSequence,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<int> get expectedCloudVersion => $composableBuilder(
    column: $table.expectedCloudVersion,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get payloadJson => $composableBuilder(
    column: $table.payloadJson,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get payloadHash => $composableBuilder(
    column: $table.payloadHash,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get status => $composableBuilder(
    column: $table.status,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<int> get attemptCount => $composableBuilder(
    column: $table.attemptCount,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get createdAtDeviceUtc => $composableBuilder(
    column: $table.createdAtDeviceUtc,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get lastAttemptAtUtc => $composableBuilder(
    column: $table.lastAttemptAtUtc,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get acceptedAtUtc => $composableBuilder(
    column: $table.acceptedAtUtc,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get lastErrorCode => $composableBuilder(
    column: $table.lastErrorCode,
    builder: (column) => ColumnOrderings(column),
  );
}

class $$LocalOutboxOperationsTableAnnotationComposer
    extends Composer<_$TraderProLocalDatabase, $LocalOutboxOperationsTable> {
  $$LocalOutboxOperationsTableAnnotationComposer({
    required super.$db,
    required super.$table,
    super.joinBuilder,
    super.$addJoinBuilderToRootComposer,
    super.$removeJoinBuilderFromRootComposer,
  });
  GeneratedColumn<String> get operationId => $composableBuilder(
    column: $table.operationId,
    builder: (column) => column,
  );

  GeneratedColumn<String> get aggregateId => $composableBuilder(
    column: $table.aggregateId,
    builder: (column) => column,
  );

  GeneratedColumn<String> get aggregateType => $composableBuilder(
    column: $table.aggregateType,
    builder: (column) => column,
  );

  GeneratedColumn<String> get operationType => $composableBuilder(
    column: $table.operationType,
    builder: (column) => column,
  );

  GeneratedColumn<int> get localSequence => $composableBuilder(
    column: $table.localSequence,
    builder: (column) => column,
  );

  GeneratedColumn<int> get expectedCloudVersion => $composableBuilder(
    column: $table.expectedCloudVersion,
    builder: (column) => column,
  );

  GeneratedColumn<String> get payloadJson => $composableBuilder(
    column: $table.payloadJson,
    builder: (column) => column,
  );

  GeneratedColumn<String> get payloadHash => $composableBuilder(
    column: $table.payloadHash,
    builder: (column) => column,
  );

  GeneratedColumn<String> get status =>
      $composableBuilder(column: $table.status, builder: (column) => column);

  GeneratedColumn<int> get attemptCount => $composableBuilder(
    column: $table.attemptCount,
    builder: (column) => column,
  );

  GeneratedColumn<String> get createdAtDeviceUtc => $composableBuilder(
    column: $table.createdAtDeviceUtc,
    builder: (column) => column,
  );

  GeneratedColumn<String> get lastAttemptAtUtc => $composableBuilder(
    column: $table.lastAttemptAtUtc,
    builder: (column) => column,
  );

  GeneratedColumn<String> get acceptedAtUtc => $composableBuilder(
    column: $table.acceptedAtUtc,
    builder: (column) => column,
  );

  GeneratedColumn<String> get lastErrorCode => $composableBuilder(
    column: $table.lastErrorCode,
    builder: (column) => column,
  );
}

class $$LocalOutboxOperationsTableTableManager
    extends
        RootTableManager<
          _$TraderProLocalDatabase,
          $LocalOutboxOperationsTable,
          LocalOutboxOperationRow,
          $$LocalOutboxOperationsTableFilterComposer,
          $$LocalOutboxOperationsTableOrderingComposer,
          $$LocalOutboxOperationsTableAnnotationComposer,
          $$LocalOutboxOperationsTableCreateCompanionBuilder,
          $$LocalOutboxOperationsTableUpdateCompanionBuilder,
          (
            LocalOutboxOperationRow,
            BaseReferences<
              _$TraderProLocalDatabase,
              $LocalOutboxOperationsTable,
              LocalOutboxOperationRow
            >,
          ),
          LocalOutboxOperationRow,
          PrefetchHooks Function()
        > {
  $$LocalOutboxOperationsTableTableManager(
    _$TraderProLocalDatabase db,
    $LocalOutboxOperationsTable table,
  ) : super(
        TableManagerState(
          db: db,
          table: table,
          createFilteringComposer: () =>
              $$LocalOutboxOperationsTableFilterComposer(
                $db: db,
                $table: table,
              ),
          createOrderingComposer: () =>
              $$LocalOutboxOperationsTableOrderingComposer(
                $db: db,
                $table: table,
              ),
          createComputedFieldComposer: () =>
              $$LocalOutboxOperationsTableAnnotationComposer(
                $db: db,
                $table: table,
              ),
          updateCompanionCallback:
              ({
                Value<String> operationId = const Value.absent(),
                Value<String> aggregateId = const Value.absent(),
                Value<String> aggregateType = const Value.absent(),
                Value<String> operationType = const Value.absent(),
                Value<int> localSequence = const Value.absent(),
                Value<int?> expectedCloudVersion = const Value.absent(),
                Value<String> payloadJson = const Value.absent(),
                Value<String> payloadHash = const Value.absent(),
                Value<String> status = const Value.absent(),
                Value<int> attemptCount = const Value.absent(),
                Value<String> createdAtDeviceUtc = const Value.absent(),
                Value<String?> lastAttemptAtUtc = const Value.absent(),
                Value<String?> acceptedAtUtc = const Value.absent(),
                Value<String?> lastErrorCode = const Value.absent(),
                Value<int> rowid = const Value.absent(),
              }) => LocalOutboxOperationsCompanion(
                operationId: operationId,
                aggregateId: aggregateId,
                aggregateType: aggregateType,
                operationType: operationType,
                localSequence: localSequence,
                expectedCloudVersion: expectedCloudVersion,
                payloadJson: payloadJson,
                payloadHash: payloadHash,
                status: status,
                attemptCount: attemptCount,
                createdAtDeviceUtc: createdAtDeviceUtc,
                lastAttemptAtUtc: lastAttemptAtUtc,
                acceptedAtUtc: acceptedAtUtc,
                lastErrorCode: lastErrorCode,
                rowid: rowid,
              ),
          createCompanionCallback:
              ({
                required String operationId,
                required String aggregateId,
                required String aggregateType,
                required String operationType,
                required int localSequence,
                Value<int?> expectedCloudVersion = const Value.absent(),
                required String payloadJson,
                required String payloadHash,
                required String status,
                required int attemptCount,
                required String createdAtDeviceUtc,
                Value<String?> lastAttemptAtUtc = const Value.absent(),
                Value<String?> acceptedAtUtc = const Value.absent(),
                Value<String?> lastErrorCode = const Value.absent(),
                Value<int> rowid = const Value.absent(),
              }) => LocalOutboxOperationsCompanion.insert(
                operationId: operationId,
                aggregateId: aggregateId,
                aggregateType: aggregateType,
                operationType: operationType,
                localSequence: localSequence,
                expectedCloudVersion: expectedCloudVersion,
                payloadJson: payloadJson,
                payloadHash: payloadHash,
                status: status,
                attemptCount: attemptCount,
                createdAtDeviceUtc: createdAtDeviceUtc,
                lastAttemptAtUtc: lastAttemptAtUtc,
                acceptedAtUtc: acceptedAtUtc,
                lastErrorCode: lastErrorCode,
                rowid: rowid,
              ),
          withReferenceMapper: (p0) => p0
              .map((e) => (e.readTable(table), BaseReferences(db, table, e)))
              .toList(),
          prefetchHooksCallback: null,
        ),
      );
}

typedef $$LocalOutboxOperationsTableProcessedTableManager =
    ProcessedTableManager<
      _$TraderProLocalDatabase,
      $LocalOutboxOperationsTable,
      LocalOutboxOperationRow,
      $$LocalOutboxOperationsTableFilterComposer,
      $$LocalOutboxOperationsTableOrderingComposer,
      $$LocalOutboxOperationsTableAnnotationComposer,
      $$LocalOutboxOperationsTableCreateCompanionBuilder,
      $$LocalOutboxOperationsTableUpdateCompanionBuilder,
      (
        LocalOutboxOperationRow,
        BaseReferences<
          _$TraderProLocalDatabase,
          $LocalOutboxOperationsTable,
          LocalOutboxOperationRow
        >,
      ),
      LocalOutboxOperationRow,
      PrefetchHooks Function()
    >;
typedef $$LocalSyncStatesTableCreateCompanionBuilder =
    LocalSyncStatesCompanion Function({
      required String key,
      Value<String?> eventCursor,
      Value<String?> lastSuccessfulSyncAtUtc,
      required String updatedAtUtc,
      Value<int> rowid,
    });
typedef $$LocalSyncStatesTableUpdateCompanionBuilder =
    LocalSyncStatesCompanion Function({
      Value<String> key,
      Value<String?> eventCursor,
      Value<String?> lastSuccessfulSyncAtUtc,
      Value<String> updatedAtUtc,
      Value<int> rowid,
    });

class $$LocalSyncStatesTableFilterComposer
    extends Composer<_$TraderProLocalDatabase, $LocalSyncStatesTable> {
  $$LocalSyncStatesTableFilterComposer({
    required super.$db,
    required super.$table,
    super.joinBuilder,
    super.$addJoinBuilderToRootComposer,
    super.$removeJoinBuilderFromRootComposer,
  });
  ColumnFilters<String> get key => $composableBuilder(
    column: $table.key,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get eventCursor => $composableBuilder(
    column: $table.eventCursor,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get lastSuccessfulSyncAtUtc => $composableBuilder(
    column: $table.lastSuccessfulSyncAtUtc,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get updatedAtUtc => $composableBuilder(
    column: $table.updatedAtUtc,
    builder: (column) => ColumnFilters(column),
  );
}

class $$LocalSyncStatesTableOrderingComposer
    extends Composer<_$TraderProLocalDatabase, $LocalSyncStatesTable> {
  $$LocalSyncStatesTableOrderingComposer({
    required super.$db,
    required super.$table,
    super.joinBuilder,
    super.$addJoinBuilderToRootComposer,
    super.$removeJoinBuilderFromRootComposer,
  });
  ColumnOrderings<String> get key => $composableBuilder(
    column: $table.key,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get eventCursor => $composableBuilder(
    column: $table.eventCursor,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get lastSuccessfulSyncAtUtc => $composableBuilder(
    column: $table.lastSuccessfulSyncAtUtc,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get updatedAtUtc => $composableBuilder(
    column: $table.updatedAtUtc,
    builder: (column) => ColumnOrderings(column),
  );
}

class $$LocalSyncStatesTableAnnotationComposer
    extends Composer<_$TraderProLocalDatabase, $LocalSyncStatesTable> {
  $$LocalSyncStatesTableAnnotationComposer({
    required super.$db,
    required super.$table,
    super.joinBuilder,
    super.$addJoinBuilderToRootComposer,
    super.$removeJoinBuilderFromRootComposer,
  });
  GeneratedColumn<String> get key =>
      $composableBuilder(column: $table.key, builder: (column) => column);

  GeneratedColumn<String> get eventCursor => $composableBuilder(
    column: $table.eventCursor,
    builder: (column) => column,
  );

  GeneratedColumn<String> get lastSuccessfulSyncAtUtc => $composableBuilder(
    column: $table.lastSuccessfulSyncAtUtc,
    builder: (column) => column,
  );

  GeneratedColumn<String> get updatedAtUtc => $composableBuilder(
    column: $table.updatedAtUtc,
    builder: (column) => column,
  );
}

class $$LocalSyncStatesTableTableManager
    extends
        RootTableManager<
          _$TraderProLocalDatabase,
          $LocalSyncStatesTable,
          LocalSyncStateRow,
          $$LocalSyncStatesTableFilterComposer,
          $$LocalSyncStatesTableOrderingComposer,
          $$LocalSyncStatesTableAnnotationComposer,
          $$LocalSyncStatesTableCreateCompanionBuilder,
          $$LocalSyncStatesTableUpdateCompanionBuilder,
          (
            LocalSyncStateRow,
            BaseReferences<
              _$TraderProLocalDatabase,
              $LocalSyncStatesTable,
              LocalSyncStateRow
            >,
          ),
          LocalSyncStateRow,
          PrefetchHooks Function()
        > {
  $$LocalSyncStatesTableTableManager(
    _$TraderProLocalDatabase db,
    $LocalSyncStatesTable table,
  ) : super(
        TableManagerState(
          db: db,
          table: table,
          createFilteringComposer: () =>
              $$LocalSyncStatesTableFilterComposer($db: db, $table: table),
          createOrderingComposer: () =>
              $$LocalSyncStatesTableOrderingComposer($db: db, $table: table),
          createComputedFieldComposer: () =>
              $$LocalSyncStatesTableAnnotationComposer($db: db, $table: table),
          updateCompanionCallback:
              ({
                Value<String> key = const Value.absent(),
                Value<String?> eventCursor = const Value.absent(),
                Value<String?> lastSuccessfulSyncAtUtc = const Value.absent(),
                Value<String> updatedAtUtc = const Value.absent(),
                Value<int> rowid = const Value.absent(),
              }) => LocalSyncStatesCompanion(
                key: key,
                eventCursor: eventCursor,
                lastSuccessfulSyncAtUtc: lastSuccessfulSyncAtUtc,
                updatedAtUtc: updatedAtUtc,
                rowid: rowid,
              ),
          createCompanionCallback:
              ({
                required String key,
                Value<String?> eventCursor = const Value.absent(),
                Value<String?> lastSuccessfulSyncAtUtc = const Value.absent(),
                required String updatedAtUtc,
                Value<int> rowid = const Value.absent(),
              }) => LocalSyncStatesCompanion.insert(
                key: key,
                eventCursor: eventCursor,
                lastSuccessfulSyncAtUtc: lastSuccessfulSyncAtUtc,
                updatedAtUtc: updatedAtUtc,
                rowid: rowid,
              ),
          withReferenceMapper: (p0) => p0
              .map((e) => (e.readTable(table), BaseReferences(db, table, e)))
              .toList(),
          prefetchHooksCallback: null,
        ),
      );
}

typedef $$LocalSyncStatesTableProcessedTableManager =
    ProcessedTableManager<
      _$TraderProLocalDatabase,
      $LocalSyncStatesTable,
      LocalSyncStateRow,
      $$LocalSyncStatesTableFilterComposer,
      $$LocalSyncStatesTableOrderingComposer,
      $$LocalSyncStatesTableAnnotationComposer,
      $$LocalSyncStatesTableCreateCompanionBuilder,
      $$LocalSyncStatesTableUpdateCompanionBuilder,
      (
        LocalSyncStateRow,
        BaseReferences<
          _$TraderProLocalDatabase,
          $LocalSyncStatesTable,
          LocalSyncStateRow
        >,
      ),
      LocalSyncStateRow,
      PrefetchHooks Function()
    >;

class $TraderProLocalDatabaseManager {
  final _$TraderProLocalDatabase _db;
  $TraderProLocalDatabaseManager(this._db);
  $$LocalReceivingSessionsTableTableManager get localReceivingSessions =>
      $$LocalReceivingSessionsTableTableManager(
        _db,
        _db.localReceivingSessions,
      );
  $$LocalReceivingEntriesTableTableManager get localReceivingEntries =>
      $$LocalReceivingEntriesTableTableManager(_db, _db.localReceivingEntries);
  $$LocalOutboxOperationsTableTableManager get localOutboxOperations =>
      $$LocalOutboxOperationsTableTableManager(_db, _db.localOutboxOperations);
  $$LocalSyncStatesTableTableManager get localSyncStates =>
      $$LocalSyncStatesTableTableManager(_db, _db.localSyncStates);
}
