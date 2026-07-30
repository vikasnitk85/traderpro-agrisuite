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

class $PocDeviceProfilesTable extends PocDeviceProfiles
    with TableInfo<$PocDeviceProfilesTable, PocDeviceProfileRow> {
  @override
  final GeneratedDatabase attachedDatabase;
  final String? _alias;
  $PocDeviceProfilesTable(this.attachedDatabase, [this._alias]);
  static const VerificationMeta _profileKeyMeta = const VerificationMeta(
    'profileKey',
  );
  @override
  late final GeneratedColumn<String> profileKey = GeneratedColumn<String>(
    'profile_key',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
  );
  static const VerificationMeta _backendBaseUrlMeta = const VerificationMeta(
    'backendBaseUrl',
  );
  @override
  late final GeneratedColumn<String> backendBaseUrl = GeneratedColumn<String>(
    'backend_base_url',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
  );
  static const VerificationMeta _workspaceIdMeta = const VerificationMeta(
    'workspaceId',
  );
  @override
  late final GeneratedColumn<String> workspaceId = GeneratedColumn<String>(
    'workspace_id',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
  );
  static const VerificationMeta _deviceIdMeta = const VerificationMeta(
    'deviceId',
  );
  @override
  late final GeneratedColumn<String> deviceId = GeneratedColumn<String>(
    'device_id',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
  );
  static const VerificationMeta _displayRoleMeta = const VerificationMeta(
    'displayRole',
  );
  @override
  late final GeneratedColumn<String> displayRole = GeneratedColumn<String>(
    'display_role',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
    $customConstraints:
        'NOT NULL CHECK (display_role IN (\'Operator\', \'Owner\'))',
  );
  static const VerificationMeta _displayLabelMeta = const VerificationMeta(
    'displayLabel',
  );
  @override
  late final GeneratedColumn<String> displayLabel = GeneratedColumn<String>(
    'display_label',
    aliasedName,
    true,
    type: DriftSqlType.string,
    requiredDuringInsert: false,
  );
  static const VerificationMeta _automaticSyncPausedMeta =
      const VerificationMeta('automaticSyncPaused');
  @override
  late final GeneratedColumn<bool> automaticSyncPaused = GeneratedColumn<bool>(
    'automatic_sync_paused',
    aliasedName,
    false,
    type: DriftSqlType.bool,
    requiredDuringInsert: false,
    defaultConstraints: GeneratedColumn.constraintIsAlways(
      'CHECK ("automatic_sync_paused" IN (0, 1))',
    ),
    defaultValue: const Constant(false),
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
    profileKey,
    backendBaseUrl,
    workspaceId,
    deviceId,
    displayRole,
    displayLabel,
    automaticSyncPaused,
    createdAtUtc,
    updatedAtUtc,
  ];
  @override
  String get aliasedName => _alias ?? actualTableName;
  @override
  String get actualTableName => $name;
  static const String $name = 'poc_device_profiles';
  @override
  VerificationContext validateIntegrity(
    Insertable<PocDeviceProfileRow> instance, {
    bool isInserting = false,
  }) {
    final context = VerificationContext();
    final data = instance.toColumns(true);
    if (data.containsKey('profile_key')) {
      context.handle(
        _profileKeyMeta,
        profileKey.isAcceptableOrUnknown(data['profile_key']!, _profileKeyMeta),
      );
    } else if (isInserting) {
      context.missing(_profileKeyMeta);
    }
    if (data.containsKey('backend_base_url')) {
      context.handle(
        _backendBaseUrlMeta,
        backendBaseUrl.isAcceptableOrUnknown(
          data['backend_base_url']!,
          _backendBaseUrlMeta,
        ),
      );
    } else if (isInserting) {
      context.missing(_backendBaseUrlMeta);
    }
    if (data.containsKey('workspace_id')) {
      context.handle(
        _workspaceIdMeta,
        workspaceId.isAcceptableOrUnknown(
          data['workspace_id']!,
          _workspaceIdMeta,
        ),
      );
    } else if (isInserting) {
      context.missing(_workspaceIdMeta);
    }
    if (data.containsKey('device_id')) {
      context.handle(
        _deviceIdMeta,
        deviceId.isAcceptableOrUnknown(data['device_id']!, _deviceIdMeta),
      );
    } else if (isInserting) {
      context.missing(_deviceIdMeta);
    }
    if (data.containsKey('display_role')) {
      context.handle(
        _displayRoleMeta,
        displayRole.isAcceptableOrUnknown(
          data['display_role']!,
          _displayRoleMeta,
        ),
      );
    } else if (isInserting) {
      context.missing(_displayRoleMeta);
    }
    if (data.containsKey('display_label')) {
      context.handle(
        _displayLabelMeta,
        displayLabel.isAcceptableOrUnknown(
          data['display_label']!,
          _displayLabelMeta,
        ),
      );
    }
    if (data.containsKey('automatic_sync_paused')) {
      context.handle(
        _automaticSyncPausedMeta,
        automaticSyncPaused.isAcceptableOrUnknown(
          data['automatic_sync_paused']!,
          _automaticSyncPausedMeta,
        ),
      );
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
  Set<GeneratedColumn> get $primaryKey => {profileKey};
  @override
  PocDeviceProfileRow map(Map<String, dynamic> data, {String? tablePrefix}) {
    final effectivePrefix = tablePrefix != null ? '$tablePrefix.' : '';
    return PocDeviceProfileRow(
      profileKey: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}profile_key'],
      )!,
      backendBaseUrl: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}backend_base_url'],
      )!,
      workspaceId: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}workspace_id'],
      )!,
      deviceId: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}device_id'],
      )!,
      displayRole: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}display_role'],
      )!,
      displayLabel: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}display_label'],
      ),
      automaticSyncPaused: attachedDatabase.typeMapping.read(
        DriftSqlType.bool,
        data['${effectivePrefix}automatic_sync_paused'],
      )!,
      createdAtUtc: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}created_at_utc'],
      )!,
      updatedAtUtc: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}updated_at_utc'],
      )!,
    );
  }

  @override
  $PocDeviceProfilesTable createAlias(String alias) {
    return $PocDeviceProfilesTable(attachedDatabase, alias);
  }
}

class PocDeviceProfileRow extends DataClass
    implements Insertable<PocDeviceProfileRow> {
  final String profileKey;
  final String backendBaseUrl;
  final String workspaceId;
  final String deviceId;
  final String displayRole;
  final String? displayLabel;
  final bool automaticSyncPaused;
  final String createdAtUtc;
  final String updatedAtUtc;
  const PocDeviceProfileRow({
    required this.profileKey,
    required this.backendBaseUrl,
    required this.workspaceId,
    required this.deviceId,
    required this.displayRole,
    this.displayLabel,
    required this.automaticSyncPaused,
    required this.createdAtUtc,
    required this.updatedAtUtc,
  });
  @override
  Map<String, Expression> toColumns(bool nullToAbsent) {
    final map = <String, Expression>{};
    map['profile_key'] = Variable<String>(profileKey);
    map['backend_base_url'] = Variable<String>(backendBaseUrl);
    map['workspace_id'] = Variable<String>(workspaceId);
    map['device_id'] = Variable<String>(deviceId);
    map['display_role'] = Variable<String>(displayRole);
    if (!nullToAbsent || displayLabel != null) {
      map['display_label'] = Variable<String>(displayLabel);
    }
    map['automatic_sync_paused'] = Variable<bool>(automaticSyncPaused);
    map['created_at_utc'] = Variable<String>(createdAtUtc);
    map['updated_at_utc'] = Variable<String>(updatedAtUtc);
    return map;
  }

  PocDeviceProfilesCompanion toCompanion(bool nullToAbsent) {
    return PocDeviceProfilesCompanion(
      profileKey: Value(profileKey),
      backendBaseUrl: Value(backendBaseUrl),
      workspaceId: Value(workspaceId),
      deviceId: Value(deviceId),
      displayRole: Value(displayRole),
      displayLabel: displayLabel == null && nullToAbsent
          ? const Value.absent()
          : Value(displayLabel),
      automaticSyncPaused: Value(automaticSyncPaused),
      createdAtUtc: Value(createdAtUtc),
      updatedAtUtc: Value(updatedAtUtc),
    );
  }

  factory PocDeviceProfileRow.fromJson(
    Map<String, dynamic> json, {
    ValueSerializer? serializer,
  }) {
    serializer ??= driftRuntimeOptions.defaultSerializer;
    return PocDeviceProfileRow(
      profileKey: serializer.fromJson<String>(json['profileKey']),
      backendBaseUrl: serializer.fromJson<String>(json['backendBaseUrl']),
      workspaceId: serializer.fromJson<String>(json['workspaceId']),
      deviceId: serializer.fromJson<String>(json['deviceId']),
      displayRole: serializer.fromJson<String>(json['displayRole']),
      displayLabel: serializer.fromJson<String?>(json['displayLabel']),
      automaticSyncPaused: serializer.fromJson<bool>(
        json['automaticSyncPaused'],
      ),
      createdAtUtc: serializer.fromJson<String>(json['createdAtUtc']),
      updatedAtUtc: serializer.fromJson<String>(json['updatedAtUtc']),
    );
  }
  @override
  Map<String, dynamic> toJson({ValueSerializer? serializer}) {
    serializer ??= driftRuntimeOptions.defaultSerializer;
    return <String, dynamic>{
      'profileKey': serializer.toJson<String>(profileKey),
      'backendBaseUrl': serializer.toJson<String>(backendBaseUrl),
      'workspaceId': serializer.toJson<String>(workspaceId),
      'deviceId': serializer.toJson<String>(deviceId),
      'displayRole': serializer.toJson<String>(displayRole),
      'displayLabel': serializer.toJson<String?>(displayLabel),
      'automaticSyncPaused': serializer.toJson<bool>(automaticSyncPaused),
      'createdAtUtc': serializer.toJson<String>(createdAtUtc),
      'updatedAtUtc': serializer.toJson<String>(updatedAtUtc),
    };
  }

  PocDeviceProfileRow copyWith({
    String? profileKey,
    String? backendBaseUrl,
    String? workspaceId,
    String? deviceId,
    String? displayRole,
    Value<String?> displayLabel = const Value.absent(),
    bool? automaticSyncPaused,
    String? createdAtUtc,
    String? updatedAtUtc,
  }) => PocDeviceProfileRow(
    profileKey: profileKey ?? this.profileKey,
    backendBaseUrl: backendBaseUrl ?? this.backendBaseUrl,
    workspaceId: workspaceId ?? this.workspaceId,
    deviceId: deviceId ?? this.deviceId,
    displayRole: displayRole ?? this.displayRole,
    displayLabel: displayLabel.present ? displayLabel.value : this.displayLabel,
    automaticSyncPaused: automaticSyncPaused ?? this.automaticSyncPaused,
    createdAtUtc: createdAtUtc ?? this.createdAtUtc,
    updatedAtUtc: updatedAtUtc ?? this.updatedAtUtc,
  );
  PocDeviceProfileRow copyWithCompanion(PocDeviceProfilesCompanion data) {
    return PocDeviceProfileRow(
      profileKey: data.profileKey.present
          ? data.profileKey.value
          : this.profileKey,
      backendBaseUrl: data.backendBaseUrl.present
          ? data.backendBaseUrl.value
          : this.backendBaseUrl,
      workspaceId: data.workspaceId.present
          ? data.workspaceId.value
          : this.workspaceId,
      deviceId: data.deviceId.present ? data.deviceId.value : this.deviceId,
      displayRole: data.displayRole.present
          ? data.displayRole.value
          : this.displayRole,
      displayLabel: data.displayLabel.present
          ? data.displayLabel.value
          : this.displayLabel,
      automaticSyncPaused: data.automaticSyncPaused.present
          ? data.automaticSyncPaused.value
          : this.automaticSyncPaused,
      createdAtUtc: data.createdAtUtc.present
          ? data.createdAtUtc.value
          : this.createdAtUtc,
      updatedAtUtc: data.updatedAtUtc.present
          ? data.updatedAtUtc.value
          : this.updatedAtUtc,
    );
  }

  @override
  String toString() {
    return (StringBuffer('PocDeviceProfileRow(')
          ..write('profileKey: $profileKey, ')
          ..write('backendBaseUrl: $backendBaseUrl, ')
          ..write('workspaceId: $workspaceId, ')
          ..write('deviceId: $deviceId, ')
          ..write('displayRole: $displayRole, ')
          ..write('displayLabel: $displayLabel, ')
          ..write('automaticSyncPaused: $automaticSyncPaused, ')
          ..write('createdAtUtc: $createdAtUtc, ')
          ..write('updatedAtUtc: $updatedAtUtc')
          ..write(')'))
        .toString();
  }

  @override
  int get hashCode => Object.hash(
    profileKey,
    backendBaseUrl,
    workspaceId,
    deviceId,
    displayRole,
    displayLabel,
    automaticSyncPaused,
    createdAtUtc,
    updatedAtUtc,
  );
  @override
  bool operator ==(Object other) =>
      identical(this, other) ||
      (other is PocDeviceProfileRow &&
          other.profileKey == this.profileKey &&
          other.backendBaseUrl == this.backendBaseUrl &&
          other.workspaceId == this.workspaceId &&
          other.deviceId == this.deviceId &&
          other.displayRole == this.displayRole &&
          other.displayLabel == this.displayLabel &&
          other.automaticSyncPaused == this.automaticSyncPaused &&
          other.createdAtUtc == this.createdAtUtc &&
          other.updatedAtUtc == this.updatedAtUtc);
}

class PocDeviceProfilesCompanion extends UpdateCompanion<PocDeviceProfileRow> {
  final Value<String> profileKey;
  final Value<String> backendBaseUrl;
  final Value<String> workspaceId;
  final Value<String> deviceId;
  final Value<String> displayRole;
  final Value<String?> displayLabel;
  final Value<bool> automaticSyncPaused;
  final Value<String> createdAtUtc;
  final Value<String> updatedAtUtc;
  final Value<int> rowid;
  const PocDeviceProfilesCompanion({
    this.profileKey = const Value.absent(),
    this.backendBaseUrl = const Value.absent(),
    this.workspaceId = const Value.absent(),
    this.deviceId = const Value.absent(),
    this.displayRole = const Value.absent(),
    this.displayLabel = const Value.absent(),
    this.automaticSyncPaused = const Value.absent(),
    this.createdAtUtc = const Value.absent(),
    this.updatedAtUtc = const Value.absent(),
    this.rowid = const Value.absent(),
  });
  PocDeviceProfilesCompanion.insert({
    required String profileKey,
    required String backendBaseUrl,
    required String workspaceId,
    required String deviceId,
    required String displayRole,
    this.displayLabel = const Value.absent(),
    this.automaticSyncPaused = const Value.absent(),
    required String createdAtUtc,
    required String updatedAtUtc,
    this.rowid = const Value.absent(),
  }) : profileKey = Value(profileKey),
       backendBaseUrl = Value(backendBaseUrl),
       workspaceId = Value(workspaceId),
       deviceId = Value(deviceId),
       displayRole = Value(displayRole),
       createdAtUtc = Value(createdAtUtc),
       updatedAtUtc = Value(updatedAtUtc);
  static Insertable<PocDeviceProfileRow> custom({
    Expression<String>? profileKey,
    Expression<String>? backendBaseUrl,
    Expression<String>? workspaceId,
    Expression<String>? deviceId,
    Expression<String>? displayRole,
    Expression<String>? displayLabel,
    Expression<bool>? automaticSyncPaused,
    Expression<String>? createdAtUtc,
    Expression<String>? updatedAtUtc,
    Expression<int>? rowid,
  }) {
    return RawValuesInsertable({
      if (profileKey != null) 'profile_key': profileKey,
      if (backendBaseUrl != null) 'backend_base_url': backendBaseUrl,
      if (workspaceId != null) 'workspace_id': workspaceId,
      if (deviceId != null) 'device_id': deviceId,
      if (displayRole != null) 'display_role': displayRole,
      if (displayLabel != null) 'display_label': displayLabel,
      if (automaticSyncPaused != null)
        'automatic_sync_paused': automaticSyncPaused,
      if (createdAtUtc != null) 'created_at_utc': createdAtUtc,
      if (updatedAtUtc != null) 'updated_at_utc': updatedAtUtc,
      if (rowid != null) 'rowid': rowid,
    });
  }

  PocDeviceProfilesCompanion copyWith({
    Value<String>? profileKey,
    Value<String>? backendBaseUrl,
    Value<String>? workspaceId,
    Value<String>? deviceId,
    Value<String>? displayRole,
    Value<String?>? displayLabel,
    Value<bool>? automaticSyncPaused,
    Value<String>? createdAtUtc,
    Value<String>? updatedAtUtc,
    Value<int>? rowid,
  }) {
    return PocDeviceProfilesCompanion(
      profileKey: profileKey ?? this.profileKey,
      backendBaseUrl: backendBaseUrl ?? this.backendBaseUrl,
      workspaceId: workspaceId ?? this.workspaceId,
      deviceId: deviceId ?? this.deviceId,
      displayRole: displayRole ?? this.displayRole,
      displayLabel: displayLabel ?? this.displayLabel,
      automaticSyncPaused: automaticSyncPaused ?? this.automaticSyncPaused,
      createdAtUtc: createdAtUtc ?? this.createdAtUtc,
      updatedAtUtc: updatedAtUtc ?? this.updatedAtUtc,
      rowid: rowid ?? this.rowid,
    );
  }

  @override
  Map<String, Expression> toColumns(bool nullToAbsent) {
    final map = <String, Expression>{};
    if (profileKey.present) {
      map['profile_key'] = Variable<String>(profileKey.value);
    }
    if (backendBaseUrl.present) {
      map['backend_base_url'] = Variable<String>(backendBaseUrl.value);
    }
    if (workspaceId.present) {
      map['workspace_id'] = Variable<String>(workspaceId.value);
    }
    if (deviceId.present) {
      map['device_id'] = Variable<String>(deviceId.value);
    }
    if (displayRole.present) {
      map['display_role'] = Variable<String>(displayRole.value);
    }
    if (displayLabel.present) {
      map['display_label'] = Variable<String>(displayLabel.value);
    }
    if (automaticSyncPaused.present) {
      map['automatic_sync_paused'] = Variable<bool>(automaticSyncPaused.value);
    }
    if (createdAtUtc.present) {
      map['created_at_utc'] = Variable<String>(createdAtUtc.value);
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
    return (StringBuffer('PocDeviceProfilesCompanion(')
          ..write('profileKey: $profileKey, ')
          ..write('backendBaseUrl: $backendBaseUrl, ')
          ..write('workspaceId: $workspaceId, ')
          ..write('deviceId: $deviceId, ')
          ..write('displayRole: $displayRole, ')
          ..write('displayLabel: $displayLabel, ')
          ..write('automaticSyncPaused: $automaticSyncPaused, ')
          ..write('createdAtUtc: $createdAtUtc, ')
          ..write('updatedAtUtc: $updatedAtUtc, ')
          ..write('rowid: $rowid')
          ..write(')'))
        .toString();
  }
}

class $PocSyncSourcesTable extends PocSyncSources
    with TableInfo<$PocSyncSourcesTable, PocSyncSourceRow> {
  @override
  final GeneratedDatabase attachedDatabase;
  final String? _alias;
  $PocSyncSourcesTable(this.attachedDatabase, [this._alias]);
  static const VerificationMeta _sourceKeyMeta = const VerificationMeta(
    'sourceKey',
  );
  @override
  late final GeneratedColumn<String> sourceKey = GeneratedColumn<String>(
    'source_key',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
  );
  static const VerificationMeta _backendBaseUrlMeta = const VerificationMeta(
    'backendBaseUrl',
  );
  @override
  late final GeneratedColumn<String> backendBaseUrl = GeneratedColumn<String>(
    'backend_base_url',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
  );
  static const VerificationMeta _workspaceIdMeta = const VerificationMeta(
    'workspaceId',
  );
  @override
  late final GeneratedColumn<String> workspaceId = GeneratedColumn<String>(
    'workspace_id',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
  );
  static const VerificationMeta _eventCursorMeta = const VerificationMeta(
    'eventCursor',
  );
  @override
  late final GeneratedColumn<int> eventCursor = GeneratedColumn<int>(
    'event_cursor',
    aliasedName,
    false,
    type: DriftSqlType.int,
    requiredDuringInsert: false,
    $customConstraints: 'NOT NULL DEFAULT 0 CHECK (event_cursor >= 0)',
    defaultValue: const CustomExpression('0'),
  );
  static const VerificationMeta _lastSuccessfulPollAtUtcMeta =
      const VerificationMeta('lastSuccessfulPollAtUtc');
  @override
  late final GeneratedColumn<String> lastSuccessfulPollAtUtc =
      GeneratedColumn<String>(
        'last_successful_poll_at_utc',
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
  static const VerificationMeta _lastErrorMessageMeta = const VerificationMeta(
    'lastErrorMessage',
  );
  @override
  late final GeneratedColumn<String> lastErrorMessage = GeneratedColumn<String>(
    'last_error_message',
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
    sourceKey,
    backendBaseUrl,
    workspaceId,
    eventCursor,
    lastSuccessfulPollAtUtc,
    lastErrorCode,
    lastErrorMessage,
    updatedAtUtc,
  ];
  @override
  String get aliasedName => _alias ?? actualTableName;
  @override
  String get actualTableName => $name;
  static const String $name = 'poc_sync_sources';
  @override
  VerificationContext validateIntegrity(
    Insertable<PocSyncSourceRow> instance, {
    bool isInserting = false,
  }) {
    final context = VerificationContext();
    final data = instance.toColumns(true);
    if (data.containsKey('source_key')) {
      context.handle(
        _sourceKeyMeta,
        sourceKey.isAcceptableOrUnknown(data['source_key']!, _sourceKeyMeta),
      );
    } else if (isInserting) {
      context.missing(_sourceKeyMeta);
    }
    if (data.containsKey('backend_base_url')) {
      context.handle(
        _backendBaseUrlMeta,
        backendBaseUrl.isAcceptableOrUnknown(
          data['backend_base_url']!,
          _backendBaseUrlMeta,
        ),
      );
    } else if (isInserting) {
      context.missing(_backendBaseUrlMeta);
    }
    if (data.containsKey('workspace_id')) {
      context.handle(
        _workspaceIdMeta,
        workspaceId.isAcceptableOrUnknown(
          data['workspace_id']!,
          _workspaceIdMeta,
        ),
      );
    } else if (isInserting) {
      context.missing(_workspaceIdMeta);
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
    if (data.containsKey('last_successful_poll_at_utc')) {
      context.handle(
        _lastSuccessfulPollAtUtcMeta,
        lastSuccessfulPollAtUtc.isAcceptableOrUnknown(
          data['last_successful_poll_at_utc']!,
          _lastSuccessfulPollAtUtcMeta,
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
    if (data.containsKey('last_error_message')) {
      context.handle(
        _lastErrorMessageMeta,
        lastErrorMessage.isAcceptableOrUnknown(
          data['last_error_message']!,
          _lastErrorMessageMeta,
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
  Set<GeneratedColumn> get $primaryKey => {sourceKey};
  @override
  List<Set<GeneratedColumn>> get uniqueKeys => [
    {backendBaseUrl, workspaceId},
  ];
  @override
  PocSyncSourceRow map(Map<String, dynamic> data, {String? tablePrefix}) {
    final effectivePrefix = tablePrefix != null ? '$tablePrefix.' : '';
    return PocSyncSourceRow(
      sourceKey: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}source_key'],
      )!,
      backendBaseUrl: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}backend_base_url'],
      )!,
      workspaceId: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}workspace_id'],
      )!,
      eventCursor: attachedDatabase.typeMapping.read(
        DriftSqlType.int,
        data['${effectivePrefix}event_cursor'],
      )!,
      lastSuccessfulPollAtUtc: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}last_successful_poll_at_utc'],
      ),
      lastErrorCode: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}last_error_code'],
      ),
      lastErrorMessage: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}last_error_message'],
      ),
      updatedAtUtc: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}updated_at_utc'],
      )!,
    );
  }

  @override
  $PocSyncSourcesTable createAlias(String alias) {
    return $PocSyncSourcesTable(attachedDatabase, alias);
  }
}

class PocSyncSourceRow extends DataClass
    implements Insertable<PocSyncSourceRow> {
  final String sourceKey;
  final String backendBaseUrl;
  final String workspaceId;
  final int eventCursor;
  final String? lastSuccessfulPollAtUtc;
  final String? lastErrorCode;
  final String? lastErrorMessage;
  final String updatedAtUtc;
  const PocSyncSourceRow({
    required this.sourceKey,
    required this.backendBaseUrl,
    required this.workspaceId,
    required this.eventCursor,
    this.lastSuccessfulPollAtUtc,
    this.lastErrorCode,
    this.lastErrorMessage,
    required this.updatedAtUtc,
  });
  @override
  Map<String, Expression> toColumns(bool nullToAbsent) {
    final map = <String, Expression>{};
    map['source_key'] = Variable<String>(sourceKey);
    map['backend_base_url'] = Variable<String>(backendBaseUrl);
    map['workspace_id'] = Variable<String>(workspaceId);
    map['event_cursor'] = Variable<int>(eventCursor);
    if (!nullToAbsent || lastSuccessfulPollAtUtc != null) {
      map['last_successful_poll_at_utc'] = Variable<String>(
        lastSuccessfulPollAtUtc,
      );
    }
    if (!nullToAbsent || lastErrorCode != null) {
      map['last_error_code'] = Variable<String>(lastErrorCode);
    }
    if (!nullToAbsent || lastErrorMessage != null) {
      map['last_error_message'] = Variable<String>(lastErrorMessage);
    }
    map['updated_at_utc'] = Variable<String>(updatedAtUtc);
    return map;
  }

  PocSyncSourcesCompanion toCompanion(bool nullToAbsent) {
    return PocSyncSourcesCompanion(
      sourceKey: Value(sourceKey),
      backendBaseUrl: Value(backendBaseUrl),
      workspaceId: Value(workspaceId),
      eventCursor: Value(eventCursor),
      lastSuccessfulPollAtUtc: lastSuccessfulPollAtUtc == null && nullToAbsent
          ? const Value.absent()
          : Value(lastSuccessfulPollAtUtc),
      lastErrorCode: lastErrorCode == null && nullToAbsent
          ? const Value.absent()
          : Value(lastErrorCode),
      lastErrorMessage: lastErrorMessage == null && nullToAbsent
          ? const Value.absent()
          : Value(lastErrorMessage),
      updatedAtUtc: Value(updatedAtUtc),
    );
  }

  factory PocSyncSourceRow.fromJson(
    Map<String, dynamic> json, {
    ValueSerializer? serializer,
  }) {
    serializer ??= driftRuntimeOptions.defaultSerializer;
    return PocSyncSourceRow(
      sourceKey: serializer.fromJson<String>(json['sourceKey']),
      backendBaseUrl: serializer.fromJson<String>(json['backendBaseUrl']),
      workspaceId: serializer.fromJson<String>(json['workspaceId']),
      eventCursor: serializer.fromJson<int>(json['eventCursor']),
      lastSuccessfulPollAtUtc: serializer.fromJson<String?>(
        json['lastSuccessfulPollAtUtc'],
      ),
      lastErrorCode: serializer.fromJson<String?>(json['lastErrorCode']),
      lastErrorMessage: serializer.fromJson<String?>(json['lastErrorMessage']),
      updatedAtUtc: serializer.fromJson<String>(json['updatedAtUtc']),
    );
  }
  @override
  Map<String, dynamic> toJson({ValueSerializer? serializer}) {
    serializer ??= driftRuntimeOptions.defaultSerializer;
    return <String, dynamic>{
      'sourceKey': serializer.toJson<String>(sourceKey),
      'backendBaseUrl': serializer.toJson<String>(backendBaseUrl),
      'workspaceId': serializer.toJson<String>(workspaceId),
      'eventCursor': serializer.toJson<int>(eventCursor),
      'lastSuccessfulPollAtUtc': serializer.toJson<String?>(
        lastSuccessfulPollAtUtc,
      ),
      'lastErrorCode': serializer.toJson<String?>(lastErrorCode),
      'lastErrorMessage': serializer.toJson<String?>(lastErrorMessage),
      'updatedAtUtc': serializer.toJson<String>(updatedAtUtc),
    };
  }

  PocSyncSourceRow copyWith({
    String? sourceKey,
    String? backendBaseUrl,
    String? workspaceId,
    int? eventCursor,
    Value<String?> lastSuccessfulPollAtUtc = const Value.absent(),
    Value<String?> lastErrorCode = const Value.absent(),
    Value<String?> lastErrorMessage = const Value.absent(),
    String? updatedAtUtc,
  }) => PocSyncSourceRow(
    sourceKey: sourceKey ?? this.sourceKey,
    backendBaseUrl: backendBaseUrl ?? this.backendBaseUrl,
    workspaceId: workspaceId ?? this.workspaceId,
    eventCursor: eventCursor ?? this.eventCursor,
    lastSuccessfulPollAtUtc: lastSuccessfulPollAtUtc.present
        ? lastSuccessfulPollAtUtc.value
        : this.lastSuccessfulPollAtUtc,
    lastErrorCode: lastErrorCode.present
        ? lastErrorCode.value
        : this.lastErrorCode,
    lastErrorMessage: lastErrorMessage.present
        ? lastErrorMessage.value
        : this.lastErrorMessage,
    updatedAtUtc: updatedAtUtc ?? this.updatedAtUtc,
  );
  PocSyncSourceRow copyWithCompanion(PocSyncSourcesCompanion data) {
    return PocSyncSourceRow(
      sourceKey: data.sourceKey.present ? data.sourceKey.value : this.sourceKey,
      backendBaseUrl: data.backendBaseUrl.present
          ? data.backendBaseUrl.value
          : this.backendBaseUrl,
      workspaceId: data.workspaceId.present
          ? data.workspaceId.value
          : this.workspaceId,
      eventCursor: data.eventCursor.present
          ? data.eventCursor.value
          : this.eventCursor,
      lastSuccessfulPollAtUtc: data.lastSuccessfulPollAtUtc.present
          ? data.lastSuccessfulPollAtUtc.value
          : this.lastSuccessfulPollAtUtc,
      lastErrorCode: data.lastErrorCode.present
          ? data.lastErrorCode.value
          : this.lastErrorCode,
      lastErrorMessage: data.lastErrorMessage.present
          ? data.lastErrorMessage.value
          : this.lastErrorMessage,
      updatedAtUtc: data.updatedAtUtc.present
          ? data.updatedAtUtc.value
          : this.updatedAtUtc,
    );
  }

  @override
  String toString() {
    return (StringBuffer('PocSyncSourceRow(')
          ..write('sourceKey: $sourceKey, ')
          ..write('backendBaseUrl: $backendBaseUrl, ')
          ..write('workspaceId: $workspaceId, ')
          ..write('eventCursor: $eventCursor, ')
          ..write('lastSuccessfulPollAtUtc: $lastSuccessfulPollAtUtc, ')
          ..write('lastErrorCode: $lastErrorCode, ')
          ..write('lastErrorMessage: $lastErrorMessage, ')
          ..write('updatedAtUtc: $updatedAtUtc')
          ..write(')'))
        .toString();
  }

  @override
  int get hashCode => Object.hash(
    sourceKey,
    backendBaseUrl,
    workspaceId,
    eventCursor,
    lastSuccessfulPollAtUtc,
    lastErrorCode,
    lastErrorMessage,
    updatedAtUtc,
  );
  @override
  bool operator ==(Object other) =>
      identical(this, other) ||
      (other is PocSyncSourceRow &&
          other.sourceKey == this.sourceKey &&
          other.backendBaseUrl == this.backendBaseUrl &&
          other.workspaceId == this.workspaceId &&
          other.eventCursor == this.eventCursor &&
          other.lastSuccessfulPollAtUtc == this.lastSuccessfulPollAtUtc &&
          other.lastErrorCode == this.lastErrorCode &&
          other.lastErrorMessage == this.lastErrorMessage &&
          other.updatedAtUtc == this.updatedAtUtc);
}

class PocSyncSourcesCompanion extends UpdateCompanion<PocSyncSourceRow> {
  final Value<String> sourceKey;
  final Value<String> backendBaseUrl;
  final Value<String> workspaceId;
  final Value<int> eventCursor;
  final Value<String?> lastSuccessfulPollAtUtc;
  final Value<String?> lastErrorCode;
  final Value<String?> lastErrorMessage;
  final Value<String> updatedAtUtc;
  final Value<int> rowid;
  const PocSyncSourcesCompanion({
    this.sourceKey = const Value.absent(),
    this.backendBaseUrl = const Value.absent(),
    this.workspaceId = const Value.absent(),
    this.eventCursor = const Value.absent(),
    this.lastSuccessfulPollAtUtc = const Value.absent(),
    this.lastErrorCode = const Value.absent(),
    this.lastErrorMessage = const Value.absent(),
    this.updatedAtUtc = const Value.absent(),
    this.rowid = const Value.absent(),
  });
  PocSyncSourcesCompanion.insert({
    required String sourceKey,
    required String backendBaseUrl,
    required String workspaceId,
    this.eventCursor = const Value.absent(),
    this.lastSuccessfulPollAtUtc = const Value.absent(),
    this.lastErrorCode = const Value.absent(),
    this.lastErrorMessage = const Value.absent(),
    required String updatedAtUtc,
    this.rowid = const Value.absent(),
  }) : sourceKey = Value(sourceKey),
       backendBaseUrl = Value(backendBaseUrl),
       workspaceId = Value(workspaceId),
       updatedAtUtc = Value(updatedAtUtc);
  static Insertable<PocSyncSourceRow> custom({
    Expression<String>? sourceKey,
    Expression<String>? backendBaseUrl,
    Expression<String>? workspaceId,
    Expression<int>? eventCursor,
    Expression<String>? lastSuccessfulPollAtUtc,
    Expression<String>? lastErrorCode,
    Expression<String>? lastErrorMessage,
    Expression<String>? updatedAtUtc,
    Expression<int>? rowid,
  }) {
    return RawValuesInsertable({
      if (sourceKey != null) 'source_key': sourceKey,
      if (backendBaseUrl != null) 'backend_base_url': backendBaseUrl,
      if (workspaceId != null) 'workspace_id': workspaceId,
      if (eventCursor != null) 'event_cursor': eventCursor,
      if (lastSuccessfulPollAtUtc != null)
        'last_successful_poll_at_utc': lastSuccessfulPollAtUtc,
      if (lastErrorCode != null) 'last_error_code': lastErrorCode,
      if (lastErrorMessage != null) 'last_error_message': lastErrorMessage,
      if (updatedAtUtc != null) 'updated_at_utc': updatedAtUtc,
      if (rowid != null) 'rowid': rowid,
    });
  }

  PocSyncSourcesCompanion copyWith({
    Value<String>? sourceKey,
    Value<String>? backendBaseUrl,
    Value<String>? workspaceId,
    Value<int>? eventCursor,
    Value<String?>? lastSuccessfulPollAtUtc,
    Value<String?>? lastErrorCode,
    Value<String?>? lastErrorMessage,
    Value<String>? updatedAtUtc,
    Value<int>? rowid,
  }) {
    return PocSyncSourcesCompanion(
      sourceKey: sourceKey ?? this.sourceKey,
      backendBaseUrl: backendBaseUrl ?? this.backendBaseUrl,
      workspaceId: workspaceId ?? this.workspaceId,
      eventCursor: eventCursor ?? this.eventCursor,
      lastSuccessfulPollAtUtc:
          lastSuccessfulPollAtUtc ?? this.lastSuccessfulPollAtUtc,
      lastErrorCode: lastErrorCode ?? this.lastErrorCode,
      lastErrorMessage: lastErrorMessage ?? this.lastErrorMessage,
      updatedAtUtc: updatedAtUtc ?? this.updatedAtUtc,
      rowid: rowid ?? this.rowid,
    );
  }

  @override
  Map<String, Expression> toColumns(bool nullToAbsent) {
    final map = <String, Expression>{};
    if (sourceKey.present) {
      map['source_key'] = Variable<String>(sourceKey.value);
    }
    if (backendBaseUrl.present) {
      map['backend_base_url'] = Variable<String>(backendBaseUrl.value);
    }
    if (workspaceId.present) {
      map['workspace_id'] = Variable<String>(workspaceId.value);
    }
    if (eventCursor.present) {
      map['event_cursor'] = Variable<int>(eventCursor.value);
    }
    if (lastSuccessfulPollAtUtc.present) {
      map['last_successful_poll_at_utc'] = Variable<String>(
        lastSuccessfulPollAtUtc.value,
      );
    }
    if (lastErrorCode.present) {
      map['last_error_code'] = Variable<String>(lastErrorCode.value);
    }
    if (lastErrorMessage.present) {
      map['last_error_message'] = Variable<String>(lastErrorMessage.value);
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
    return (StringBuffer('PocSyncSourcesCompanion(')
          ..write('sourceKey: $sourceKey, ')
          ..write('backendBaseUrl: $backendBaseUrl, ')
          ..write('workspaceId: $workspaceId, ')
          ..write('eventCursor: $eventCursor, ')
          ..write('lastSuccessfulPollAtUtc: $lastSuccessfulPollAtUtc, ')
          ..write('lastErrorCode: $lastErrorCode, ')
          ..write('lastErrorMessage: $lastErrorMessage, ')
          ..write('updatedAtUtc: $updatedAtUtc, ')
          ..write('rowid: $rowid')
          ..write(')'))
        .toString();
  }
}

class $ReceivingSessionCloudStatesTable extends ReceivingSessionCloudStates
    with
        TableInfo<
          $ReceivingSessionCloudStatesTable,
          ReceivingSessionCloudStateRow
        > {
  @override
  final GeneratedDatabase attachedDatabase;
  final String? _alias;
  $ReceivingSessionCloudStatesTable(this.attachedDatabase, [this._alias]);
  static const VerificationMeta _sourceKeyMeta = const VerificationMeta(
    'sourceKey',
  );
  @override
  late final GeneratedColumn<String> sourceKey = GeneratedColumn<String>(
    'source_key',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
    defaultConstraints: GeneratedColumn.constraintIsAlways(
      'REFERENCES poc_sync_sources (source_key) ON DELETE RESTRICT',
    ),
  );
  static const VerificationMeta _boundDeviceIdMeta = const VerificationMeta(
    'boundDeviceId',
  );
  @override
  late final GeneratedColumn<String> boundDeviceId = GeneratedColumn<String>(
    'bound_device_id',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
  );
  static const VerificationMeta _localSessionIdMeta = const VerificationMeta(
    'localSessionId',
  );
  @override
  late final GeneratedColumn<String> localSessionId = GeneratedColumn<String>(
    'local_session_id',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
    defaultConstraints: GeneratedColumn.constraintIsAlways(
      'REFERENCES local_receiving_sessions (id) ON DELETE RESTRICT',
    ),
  );
  static const VerificationMeta _cloudSessionIdMeta = const VerificationMeta(
    'cloudSessionId',
  );
  @override
  late final GeneratedColumn<String> cloudSessionId = GeneratedColumn<String>(
    'cloud_session_id',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
  );
  static const VerificationMeta _cloudReferenceMeta = const VerificationMeta(
    'cloudReference',
  );
  @override
  late final GeneratedColumn<String> cloudReference = GeneratedColumn<String>(
    'cloud_reference',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
  );
  static const VerificationMeta _cloudStatusMeta = const VerificationMeta(
    'cloudStatus',
  );
  @override
  late final GeneratedColumn<String> cloudStatus = GeneratedColumn<String>(
    'cloud_status',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
  );
  static const VerificationMeta _cloudVersionMeta = const VerificationMeta(
    'cloudVersion',
  );
  @override
  late final GeneratedColumn<int> cloudVersion = GeneratedColumn<int>(
    'cloud_version',
    aliasedName,
    false,
    type: DriftSqlType.int,
    requiredDuringInsert: true,
    $customConstraints: 'NOT NULL CHECK (cloud_version > 0)',
  );
  static const VerificationMeta _leaseIdMeta = const VerificationMeta(
    'leaseId',
  );
  @override
  late final GeneratedColumn<String> leaseId = GeneratedColumn<String>(
    'lease_id',
    aliasedName,
    true,
    type: DriftSqlType.string,
    requiredDuringInsert: false,
  );
  static const VerificationMeta _leaseExpiresAtUtcMeta = const VerificationMeta(
    'leaseExpiresAtUtc',
  );
  @override
  late final GeneratedColumn<String> leaseExpiresAtUtc =
      GeneratedColumn<String>(
        'lease_expires_at_utc',
        aliasedName,
        true,
        type: DriftSqlType.string,
        requiredDuringInsert: false,
      );
  static const VerificationMeta _editorDeviceIdMeta = const VerificationMeta(
    'editorDeviceId',
  );
  @override
  late final GeneratedColumn<String> editorDeviceId = GeneratedColumn<String>(
    'editor_device_id',
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
        false,
        type: DriftSqlType.string,
        requiredDuringInsert: true,
      );
  static const VerificationMeta _lastCloudUpdateAtUtcMeta =
      const VerificationMeta('lastCloudUpdateAtUtc');
  @override
  late final GeneratedColumn<String> lastCloudUpdateAtUtc =
      GeneratedColumn<String>(
        'last_cloud_update_at_utc',
        aliasedName,
        false,
        type: DriftSqlType.string,
        requiredDuringInsert: true,
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
  static const VerificationMeta _lastErrorMessageMeta = const VerificationMeta(
    'lastErrorMessage',
  );
  @override
  late final GeneratedColumn<String> lastErrorMessage = GeneratedColumn<String>(
    'last_error_message',
    aliasedName,
    true,
    type: DriftSqlType.string,
    requiredDuringInsert: false,
  );
  @override
  List<GeneratedColumn> get $columns => [
    sourceKey,
    boundDeviceId,
    localSessionId,
    cloudSessionId,
    cloudReference,
    cloudStatus,
    cloudVersion,
    leaseId,
    leaseExpiresAtUtc,
    editorDeviceId,
    lastSuccessfulSyncAtUtc,
    lastCloudUpdateAtUtc,
    lastErrorCode,
    lastErrorMessage,
  ];
  @override
  String get aliasedName => _alias ?? actualTableName;
  @override
  String get actualTableName => $name;
  static const String $name = 'receiving_session_cloud_states';
  @override
  VerificationContext validateIntegrity(
    Insertable<ReceivingSessionCloudStateRow> instance, {
    bool isInserting = false,
  }) {
    final context = VerificationContext();
    final data = instance.toColumns(true);
    if (data.containsKey('source_key')) {
      context.handle(
        _sourceKeyMeta,
        sourceKey.isAcceptableOrUnknown(data['source_key']!, _sourceKeyMeta),
      );
    } else if (isInserting) {
      context.missing(_sourceKeyMeta);
    }
    if (data.containsKey('bound_device_id')) {
      context.handle(
        _boundDeviceIdMeta,
        boundDeviceId.isAcceptableOrUnknown(
          data['bound_device_id']!,
          _boundDeviceIdMeta,
        ),
      );
    } else if (isInserting) {
      context.missing(_boundDeviceIdMeta);
    }
    if (data.containsKey('local_session_id')) {
      context.handle(
        _localSessionIdMeta,
        localSessionId.isAcceptableOrUnknown(
          data['local_session_id']!,
          _localSessionIdMeta,
        ),
      );
    } else if (isInserting) {
      context.missing(_localSessionIdMeta);
    }
    if (data.containsKey('cloud_session_id')) {
      context.handle(
        _cloudSessionIdMeta,
        cloudSessionId.isAcceptableOrUnknown(
          data['cloud_session_id']!,
          _cloudSessionIdMeta,
        ),
      );
    } else if (isInserting) {
      context.missing(_cloudSessionIdMeta);
    }
    if (data.containsKey('cloud_reference')) {
      context.handle(
        _cloudReferenceMeta,
        cloudReference.isAcceptableOrUnknown(
          data['cloud_reference']!,
          _cloudReferenceMeta,
        ),
      );
    } else if (isInserting) {
      context.missing(_cloudReferenceMeta);
    }
    if (data.containsKey('cloud_status')) {
      context.handle(
        _cloudStatusMeta,
        cloudStatus.isAcceptableOrUnknown(
          data['cloud_status']!,
          _cloudStatusMeta,
        ),
      );
    } else if (isInserting) {
      context.missing(_cloudStatusMeta);
    }
    if (data.containsKey('cloud_version')) {
      context.handle(
        _cloudVersionMeta,
        cloudVersion.isAcceptableOrUnknown(
          data['cloud_version']!,
          _cloudVersionMeta,
        ),
      );
    } else if (isInserting) {
      context.missing(_cloudVersionMeta);
    }
    if (data.containsKey('lease_id')) {
      context.handle(
        _leaseIdMeta,
        leaseId.isAcceptableOrUnknown(data['lease_id']!, _leaseIdMeta),
      );
    }
    if (data.containsKey('lease_expires_at_utc')) {
      context.handle(
        _leaseExpiresAtUtcMeta,
        leaseExpiresAtUtc.isAcceptableOrUnknown(
          data['lease_expires_at_utc']!,
          _leaseExpiresAtUtcMeta,
        ),
      );
    }
    if (data.containsKey('editor_device_id')) {
      context.handle(
        _editorDeviceIdMeta,
        editorDeviceId.isAcceptableOrUnknown(
          data['editor_device_id']!,
          _editorDeviceIdMeta,
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
    } else if (isInserting) {
      context.missing(_lastSuccessfulSyncAtUtcMeta);
    }
    if (data.containsKey('last_cloud_update_at_utc')) {
      context.handle(
        _lastCloudUpdateAtUtcMeta,
        lastCloudUpdateAtUtc.isAcceptableOrUnknown(
          data['last_cloud_update_at_utc']!,
          _lastCloudUpdateAtUtcMeta,
        ),
      );
    } else if (isInserting) {
      context.missing(_lastCloudUpdateAtUtcMeta);
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
    if (data.containsKey('last_error_message')) {
      context.handle(
        _lastErrorMessageMeta,
        lastErrorMessage.isAcceptableOrUnknown(
          data['last_error_message']!,
          _lastErrorMessageMeta,
        ),
      );
    }
    return context;
  }

  @override
  Set<GeneratedColumn> get $primaryKey => {localSessionId};
  @override
  ReceivingSessionCloudStateRow map(
    Map<String, dynamic> data, {
    String? tablePrefix,
  }) {
    final effectivePrefix = tablePrefix != null ? '$tablePrefix.' : '';
    return ReceivingSessionCloudStateRow(
      sourceKey: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}source_key'],
      )!,
      boundDeviceId: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}bound_device_id'],
      )!,
      localSessionId: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}local_session_id'],
      )!,
      cloudSessionId: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}cloud_session_id'],
      )!,
      cloudReference: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}cloud_reference'],
      )!,
      cloudStatus: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}cloud_status'],
      )!,
      cloudVersion: attachedDatabase.typeMapping.read(
        DriftSqlType.int,
        data['${effectivePrefix}cloud_version'],
      )!,
      leaseId: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}lease_id'],
      ),
      leaseExpiresAtUtc: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}lease_expires_at_utc'],
      ),
      editorDeviceId: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}editor_device_id'],
      ),
      lastSuccessfulSyncAtUtc: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}last_successful_sync_at_utc'],
      )!,
      lastCloudUpdateAtUtc: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}last_cloud_update_at_utc'],
      )!,
      lastErrorCode: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}last_error_code'],
      ),
      lastErrorMessage: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}last_error_message'],
      ),
    );
  }

  @override
  $ReceivingSessionCloudStatesTable createAlias(String alias) {
    return $ReceivingSessionCloudStatesTable(attachedDatabase, alias);
  }
}

class ReceivingSessionCloudStateRow extends DataClass
    implements Insertable<ReceivingSessionCloudStateRow> {
  final String sourceKey;
  final String boundDeviceId;
  final String localSessionId;
  final String cloudSessionId;
  final String cloudReference;
  final String cloudStatus;
  final int cloudVersion;
  final String? leaseId;
  final String? leaseExpiresAtUtc;
  final String? editorDeviceId;
  final String lastSuccessfulSyncAtUtc;
  final String lastCloudUpdateAtUtc;
  final String? lastErrorCode;
  final String? lastErrorMessage;
  const ReceivingSessionCloudStateRow({
    required this.sourceKey,
    required this.boundDeviceId,
    required this.localSessionId,
    required this.cloudSessionId,
    required this.cloudReference,
    required this.cloudStatus,
    required this.cloudVersion,
    this.leaseId,
    this.leaseExpiresAtUtc,
    this.editorDeviceId,
    required this.lastSuccessfulSyncAtUtc,
    required this.lastCloudUpdateAtUtc,
    this.lastErrorCode,
    this.lastErrorMessage,
  });
  @override
  Map<String, Expression> toColumns(bool nullToAbsent) {
    final map = <String, Expression>{};
    map['source_key'] = Variable<String>(sourceKey);
    map['bound_device_id'] = Variable<String>(boundDeviceId);
    map['local_session_id'] = Variable<String>(localSessionId);
    map['cloud_session_id'] = Variable<String>(cloudSessionId);
    map['cloud_reference'] = Variable<String>(cloudReference);
    map['cloud_status'] = Variable<String>(cloudStatus);
    map['cloud_version'] = Variable<int>(cloudVersion);
    if (!nullToAbsent || leaseId != null) {
      map['lease_id'] = Variable<String>(leaseId);
    }
    if (!nullToAbsent || leaseExpiresAtUtc != null) {
      map['lease_expires_at_utc'] = Variable<String>(leaseExpiresAtUtc);
    }
    if (!nullToAbsent || editorDeviceId != null) {
      map['editor_device_id'] = Variable<String>(editorDeviceId);
    }
    map['last_successful_sync_at_utc'] = Variable<String>(
      lastSuccessfulSyncAtUtc,
    );
    map['last_cloud_update_at_utc'] = Variable<String>(lastCloudUpdateAtUtc);
    if (!nullToAbsent || lastErrorCode != null) {
      map['last_error_code'] = Variable<String>(lastErrorCode);
    }
    if (!nullToAbsent || lastErrorMessage != null) {
      map['last_error_message'] = Variable<String>(lastErrorMessage);
    }
    return map;
  }

  ReceivingSessionCloudStatesCompanion toCompanion(bool nullToAbsent) {
    return ReceivingSessionCloudStatesCompanion(
      sourceKey: Value(sourceKey),
      boundDeviceId: Value(boundDeviceId),
      localSessionId: Value(localSessionId),
      cloudSessionId: Value(cloudSessionId),
      cloudReference: Value(cloudReference),
      cloudStatus: Value(cloudStatus),
      cloudVersion: Value(cloudVersion),
      leaseId: leaseId == null && nullToAbsent
          ? const Value.absent()
          : Value(leaseId),
      leaseExpiresAtUtc: leaseExpiresAtUtc == null && nullToAbsent
          ? const Value.absent()
          : Value(leaseExpiresAtUtc),
      editorDeviceId: editorDeviceId == null && nullToAbsent
          ? const Value.absent()
          : Value(editorDeviceId),
      lastSuccessfulSyncAtUtc: Value(lastSuccessfulSyncAtUtc),
      lastCloudUpdateAtUtc: Value(lastCloudUpdateAtUtc),
      lastErrorCode: lastErrorCode == null && nullToAbsent
          ? const Value.absent()
          : Value(lastErrorCode),
      lastErrorMessage: lastErrorMessage == null && nullToAbsent
          ? const Value.absent()
          : Value(lastErrorMessage),
    );
  }

  factory ReceivingSessionCloudStateRow.fromJson(
    Map<String, dynamic> json, {
    ValueSerializer? serializer,
  }) {
    serializer ??= driftRuntimeOptions.defaultSerializer;
    return ReceivingSessionCloudStateRow(
      sourceKey: serializer.fromJson<String>(json['sourceKey']),
      boundDeviceId: serializer.fromJson<String>(json['boundDeviceId']),
      localSessionId: serializer.fromJson<String>(json['localSessionId']),
      cloudSessionId: serializer.fromJson<String>(json['cloudSessionId']),
      cloudReference: serializer.fromJson<String>(json['cloudReference']),
      cloudStatus: serializer.fromJson<String>(json['cloudStatus']),
      cloudVersion: serializer.fromJson<int>(json['cloudVersion']),
      leaseId: serializer.fromJson<String?>(json['leaseId']),
      leaseExpiresAtUtc: serializer.fromJson<String?>(
        json['leaseExpiresAtUtc'],
      ),
      editorDeviceId: serializer.fromJson<String?>(json['editorDeviceId']),
      lastSuccessfulSyncAtUtc: serializer.fromJson<String>(
        json['lastSuccessfulSyncAtUtc'],
      ),
      lastCloudUpdateAtUtc: serializer.fromJson<String>(
        json['lastCloudUpdateAtUtc'],
      ),
      lastErrorCode: serializer.fromJson<String?>(json['lastErrorCode']),
      lastErrorMessage: serializer.fromJson<String?>(json['lastErrorMessage']),
    );
  }
  @override
  Map<String, dynamic> toJson({ValueSerializer? serializer}) {
    serializer ??= driftRuntimeOptions.defaultSerializer;
    return <String, dynamic>{
      'sourceKey': serializer.toJson<String>(sourceKey),
      'boundDeviceId': serializer.toJson<String>(boundDeviceId),
      'localSessionId': serializer.toJson<String>(localSessionId),
      'cloudSessionId': serializer.toJson<String>(cloudSessionId),
      'cloudReference': serializer.toJson<String>(cloudReference),
      'cloudStatus': serializer.toJson<String>(cloudStatus),
      'cloudVersion': serializer.toJson<int>(cloudVersion),
      'leaseId': serializer.toJson<String?>(leaseId),
      'leaseExpiresAtUtc': serializer.toJson<String?>(leaseExpiresAtUtc),
      'editorDeviceId': serializer.toJson<String?>(editorDeviceId),
      'lastSuccessfulSyncAtUtc': serializer.toJson<String>(
        lastSuccessfulSyncAtUtc,
      ),
      'lastCloudUpdateAtUtc': serializer.toJson<String>(lastCloudUpdateAtUtc),
      'lastErrorCode': serializer.toJson<String?>(lastErrorCode),
      'lastErrorMessage': serializer.toJson<String?>(lastErrorMessage),
    };
  }

  ReceivingSessionCloudStateRow copyWith({
    String? sourceKey,
    String? boundDeviceId,
    String? localSessionId,
    String? cloudSessionId,
    String? cloudReference,
    String? cloudStatus,
    int? cloudVersion,
    Value<String?> leaseId = const Value.absent(),
    Value<String?> leaseExpiresAtUtc = const Value.absent(),
    Value<String?> editorDeviceId = const Value.absent(),
    String? lastSuccessfulSyncAtUtc,
    String? lastCloudUpdateAtUtc,
    Value<String?> lastErrorCode = const Value.absent(),
    Value<String?> lastErrorMessage = const Value.absent(),
  }) => ReceivingSessionCloudStateRow(
    sourceKey: sourceKey ?? this.sourceKey,
    boundDeviceId: boundDeviceId ?? this.boundDeviceId,
    localSessionId: localSessionId ?? this.localSessionId,
    cloudSessionId: cloudSessionId ?? this.cloudSessionId,
    cloudReference: cloudReference ?? this.cloudReference,
    cloudStatus: cloudStatus ?? this.cloudStatus,
    cloudVersion: cloudVersion ?? this.cloudVersion,
    leaseId: leaseId.present ? leaseId.value : this.leaseId,
    leaseExpiresAtUtc: leaseExpiresAtUtc.present
        ? leaseExpiresAtUtc.value
        : this.leaseExpiresAtUtc,
    editorDeviceId: editorDeviceId.present
        ? editorDeviceId.value
        : this.editorDeviceId,
    lastSuccessfulSyncAtUtc:
        lastSuccessfulSyncAtUtc ?? this.lastSuccessfulSyncAtUtc,
    lastCloudUpdateAtUtc: lastCloudUpdateAtUtc ?? this.lastCloudUpdateAtUtc,
    lastErrorCode: lastErrorCode.present
        ? lastErrorCode.value
        : this.lastErrorCode,
    lastErrorMessage: lastErrorMessage.present
        ? lastErrorMessage.value
        : this.lastErrorMessage,
  );
  ReceivingSessionCloudStateRow copyWithCompanion(
    ReceivingSessionCloudStatesCompanion data,
  ) {
    return ReceivingSessionCloudStateRow(
      sourceKey: data.sourceKey.present ? data.sourceKey.value : this.sourceKey,
      boundDeviceId: data.boundDeviceId.present
          ? data.boundDeviceId.value
          : this.boundDeviceId,
      localSessionId: data.localSessionId.present
          ? data.localSessionId.value
          : this.localSessionId,
      cloudSessionId: data.cloudSessionId.present
          ? data.cloudSessionId.value
          : this.cloudSessionId,
      cloudReference: data.cloudReference.present
          ? data.cloudReference.value
          : this.cloudReference,
      cloudStatus: data.cloudStatus.present
          ? data.cloudStatus.value
          : this.cloudStatus,
      cloudVersion: data.cloudVersion.present
          ? data.cloudVersion.value
          : this.cloudVersion,
      leaseId: data.leaseId.present ? data.leaseId.value : this.leaseId,
      leaseExpiresAtUtc: data.leaseExpiresAtUtc.present
          ? data.leaseExpiresAtUtc.value
          : this.leaseExpiresAtUtc,
      editorDeviceId: data.editorDeviceId.present
          ? data.editorDeviceId.value
          : this.editorDeviceId,
      lastSuccessfulSyncAtUtc: data.lastSuccessfulSyncAtUtc.present
          ? data.lastSuccessfulSyncAtUtc.value
          : this.lastSuccessfulSyncAtUtc,
      lastCloudUpdateAtUtc: data.lastCloudUpdateAtUtc.present
          ? data.lastCloudUpdateAtUtc.value
          : this.lastCloudUpdateAtUtc,
      lastErrorCode: data.lastErrorCode.present
          ? data.lastErrorCode.value
          : this.lastErrorCode,
      lastErrorMessage: data.lastErrorMessage.present
          ? data.lastErrorMessage.value
          : this.lastErrorMessage,
    );
  }

  @override
  String toString() {
    return (StringBuffer('ReceivingSessionCloudStateRow(')
          ..write('sourceKey: $sourceKey, ')
          ..write('boundDeviceId: $boundDeviceId, ')
          ..write('localSessionId: $localSessionId, ')
          ..write('cloudSessionId: $cloudSessionId, ')
          ..write('cloudReference: $cloudReference, ')
          ..write('cloudStatus: $cloudStatus, ')
          ..write('cloudVersion: $cloudVersion, ')
          ..write('leaseId: $leaseId, ')
          ..write('leaseExpiresAtUtc: $leaseExpiresAtUtc, ')
          ..write('editorDeviceId: $editorDeviceId, ')
          ..write('lastSuccessfulSyncAtUtc: $lastSuccessfulSyncAtUtc, ')
          ..write('lastCloudUpdateAtUtc: $lastCloudUpdateAtUtc, ')
          ..write('lastErrorCode: $lastErrorCode, ')
          ..write('lastErrorMessage: $lastErrorMessage')
          ..write(')'))
        .toString();
  }

  @override
  int get hashCode => Object.hash(
    sourceKey,
    boundDeviceId,
    localSessionId,
    cloudSessionId,
    cloudReference,
    cloudStatus,
    cloudVersion,
    leaseId,
    leaseExpiresAtUtc,
    editorDeviceId,
    lastSuccessfulSyncAtUtc,
    lastCloudUpdateAtUtc,
    lastErrorCode,
    lastErrorMessage,
  );
  @override
  bool operator ==(Object other) =>
      identical(this, other) ||
      (other is ReceivingSessionCloudStateRow &&
          other.sourceKey == this.sourceKey &&
          other.boundDeviceId == this.boundDeviceId &&
          other.localSessionId == this.localSessionId &&
          other.cloudSessionId == this.cloudSessionId &&
          other.cloudReference == this.cloudReference &&
          other.cloudStatus == this.cloudStatus &&
          other.cloudVersion == this.cloudVersion &&
          other.leaseId == this.leaseId &&
          other.leaseExpiresAtUtc == this.leaseExpiresAtUtc &&
          other.editorDeviceId == this.editorDeviceId &&
          other.lastSuccessfulSyncAtUtc == this.lastSuccessfulSyncAtUtc &&
          other.lastCloudUpdateAtUtc == this.lastCloudUpdateAtUtc &&
          other.lastErrorCode == this.lastErrorCode &&
          other.lastErrorMessage == this.lastErrorMessage);
}

class ReceivingSessionCloudStatesCompanion
    extends UpdateCompanion<ReceivingSessionCloudStateRow> {
  final Value<String> sourceKey;
  final Value<String> boundDeviceId;
  final Value<String> localSessionId;
  final Value<String> cloudSessionId;
  final Value<String> cloudReference;
  final Value<String> cloudStatus;
  final Value<int> cloudVersion;
  final Value<String?> leaseId;
  final Value<String?> leaseExpiresAtUtc;
  final Value<String?> editorDeviceId;
  final Value<String> lastSuccessfulSyncAtUtc;
  final Value<String> lastCloudUpdateAtUtc;
  final Value<String?> lastErrorCode;
  final Value<String?> lastErrorMessage;
  final Value<int> rowid;
  const ReceivingSessionCloudStatesCompanion({
    this.sourceKey = const Value.absent(),
    this.boundDeviceId = const Value.absent(),
    this.localSessionId = const Value.absent(),
    this.cloudSessionId = const Value.absent(),
    this.cloudReference = const Value.absent(),
    this.cloudStatus = const Value.absent(),
    this.cloudVersion = const Value.absent(),
    this.leaseId = const Value.absent(),
    this.leaseExpiresAtUtc = const Value.absent(),
    this.editorDeviceId = const Value.absent(),
    this.lastSuccessfulSyncAtUtc = const Value.absent(),
    this.lastCloudUpdateAtUtc = const Value.absent(),
    this.lastErrorCode = const Value.absent(),
    this.lastErrorMessage = const Value.absent(),
    this.rowid = const Value.absent(),
  });
  ReceivingSessionCloudStatesCompanion.insert({
    required String sourceKey,
    required String boundDeviceId,
    required String localSessionId,
    required String cloudSessionId,
    required String cloudReference,
    required String cloudStatus,
    required int cloudVersion,
    this.leaseId = const Value.absent(),
    this.leaseExpiresAtUtc = const Value.absent(),
    this.editorDeviceId = const Value.absent(),
    required String lastSuccessfulSyncAtUtc,
    required String lastCloudUpdateAtUtc,
    this.lastErrorCode = const Value.absent(),
    this.lastErrorMessage = const Value.absent(),
    this.rowid = const Value.absent(),
  }) : sourceKey = Value(sourceKey),
       boundDeviceId = Value(boundDeviceId),
       localSessionId = Value(localSessionId),
       cloudSessionId = Value(cloudSessionId),
       cloudReference = Value(cloudReference),
       cloudStatus = Value(cloudStatus),
       cloudVersion = Value(cloudVersion),
       lastSuccessfulSyncAtUtc = Value(lastSuccessfulSyncAtUtc),
       lastCloudUpdateAtUtc = Value(lastCloudUpdateAtUtc);
  static Insertable<ReceivingSessionCloudStateRow> custom({
    Expression<String>? sourceKey,
    Expression<String>? boundDeviceId,
    Expression<String>? localSessionId,
    Expression<String>? cloudSessionId,
    Expression<String>? cloudReference,
    Expression<String>? cloudStatus,
    Expression<int>? cloudVersion,
    Expression<String>? leaseId,
    Expression<String>? leaseExpiresAtUtc,
    Expression<String>? editorDeviceId,
    Expression<String>? lastSuccessfulSyncAtUtc,
    Expression<String>? lastCloudUpdateAtUtc,
    Expression<String>? lastErrorCode,
    Expression<String>? lastErrorMessage,
    Expression<int>? rowid,
  }) {
    return RawValuesInsertable({
      if (sourceKey != null) 'source_key': sourceKey,
      if (boundDeviceId != null) 'bound_device_id': boundDeviceId,
      if (localSessionId != null) 'local_session_id': localSessionId,
      if (cloudSessionId != null) 'cloud_session_id': cloudSessionId,
      if (cloudReference != null) 'cloud_reference': cloudReference,
      if (cloudStatus != null) 'cloud_status': cloudStatus,
      if (cloudVersion != null) 'cloud_version': cloudVersion,
      if (leaseId != null) 'lease_id': leaseId,
      if (leaseExpiresAtUtc != null) 'lease_expires_at_utc': leaseExpiresAtUtc,
      if (editorDeviceId != null) 'editor_device_id': editorDeviceId,
      if (lastSuccessfulSyncAtUtc != null)
        'last_successful_sync_at_utc': lastSuccessfulSyncAtUtc,
      if (lastCloudUpdateAtUtc != null)
        'last_cloud_update_at_utc': lastCloudUpdateAtUtc,
      if (lastErrorCode != null) 'last_error_code': lastErrorCode,
      if (lastErrorMessage != null) 'last_error_message': lastErrorMessage,
      if (rowid != null) 'rowid': rowid,
    });
  }

  ReceivingSessionCloudStatesCompanion copyWith({
    Value<String>? sourceKey,
    Value<String>? boundDeviceId,
    Value<String>? localSessionId,
    Value<String>? cloudSessionId,
    Value<String>? cloudReference,
    Value<String>? cloudStatus,
    Value<int>? cloudVersion,
    Value<String?>? leaseId,
    Value<String?>? leaseExpiresAtUtc,
    Value<String?>? editorDeviceId,
    Value<String>? lastSuccessfulSyncAtUtc,
    Value<String>? lastCloudUpdateAtUtc,
    Value<String?>? lastErrorCode,
    Value<String?>? lastErrorMessage,
    Value<int>? rowid,
  }) {
    return ReceivingSessionCloudStatesCompanion(
      sourceKey: sourceKey ?? this.sourceKey,
      boundDeviceId: boundDeviceId ?? this.boundDeviceId,
      localSessionId: localSessionId ?? this.localSessionId,
      cloudSessionId: cloudSessionId ?? this.cloudSessionId,
      cloudReference: cloudReference ?? this.cloudReference,
      cloudStatus: cloudStatus ?? this.cloudStatus,
      cloudVersion: cloudVersion ?? this.cloudVersion,
      leaseId: leaseId ?? this.leaseId,
      leaseExpiresAtUtc: leaseExpiresAtUtc ?? this.leaseExpiresAtUtc,
      editorDeviceId: editorDeviceId ?? this.editorDeviceId,
      lastSuccessfulSyncAtUtc:
          lastSuccessfulSyncAtUtc ?? this.lastSuccessfulSyncAtUtc,
      lastCloudUpdateAtUtc: lastCloudUpdateAtUtc ?? this.lastCloudUpdateAtUtc,
      lastErrorCode: lastErrorCode ?? this.lastErrorCode,
      lastErrorMessage: lastErrorMessage ?? this.lastErrorMessage,
      rowid: rowid ?? this.rowid,
    );
  }

  @override
  Map<String, Expression> toColumns(bool nullToAbsent) {
    final map = <String, Expression>{};
    if (sourceKey.present) {
      map['source_key'] = Variable<String>(sourceKey.value);
    }
    if (boundDeviceId.present) {
      map['bound_device_id'] = Variable<String>(boundDeviceId.value);
    }
    if (localSessionId.present) {
      map['local_session_id'] = Variable<String>(localSessionId.value);
    }
    if (cloudSessionId.present) {
      map['cloud_session_id'] = Variable<String>(cloudSessionId.value);
    }
    if (cloudReference.present) {
      map['cloud_reference'] = Variable<String>(cloudReference.value);
    }
    if (cloudStatus.present) {
      map['cloud_status'] = Variable<String>(cloudStatus.value);
    }
    if (cloudVersion.present) {
      map['cloud_version'] = Variable<int>(cloudVersion.value);
    }
    if (leaseId.present) {
      map['lease_id'] = Variable<String>(leaseId.value);
    }
    if (leaseExpiresAtUtc.present) {
      map['lease_expires_at_utc'] = Variable<String>(leaseExpiresAtUtc.value);
    }
    if (editorDeviceId.present) {
      map['editor_device_id'] = Variable<String>(editorDeviceId.value);
    }
    if (lastSuccessfulSyncAtUtc.present) {
      map['last_successful_sync_at_utc'] = Variable<String>(
        lastSuccessfulSyncAtUtc.value,
      );
    }
    if (lastCloudUpdateAtUtc.present) {
      map['last_cloud_update_at_utc'] = Variable<String>(
        lastCloudUpdateAtUtc.value,
      );
    }
    if (lastErrorCode.present) {
      map['last_error_code'] = Variable<String>(lastErrorCode.value);
    }
    if (lastErrorMessage.present) {
      map['last_error_message'] = Variable<String>(lastErrorMessage.value);
    }
    if (rowid.present) {
      map['rowid'] = Variable<int>(rowid.value);
    }
    return map;
  }

  @override
  String toString() {
    return (StringBuffer('ReceivingSessionCloudStatesCompanion(')
          ..write('sourceKey: $sourceKey, ')
          ..write('boundDeviceId: $boundDeviceId, ')
          ..write('localSessionId: $localSessionId, ')
          ..write('cloudSessionId: $cloudSessionId, ')
          ..write('cloudReference: $cloudReference, ')
          ..write('cloudStatus: $cloudStatus, ')
          ..write('cloudVersion: $cloudVersion, ')
          ..write('leaseId: $leaseId, ')
          ..write('leaseExpiresAtUtc: $leaseExpiresAtUtc, ')
          ..write('editorDeviceId: $editorDeviceId, ')
          ..write('lastSuccessfulSyncAtUtc: $lastSuccessfulSyncAtUtc, ')
          ..write('lastCloudUpdateAtUtc: $lastCloudUpdateAtUtc, ')
          ..write('lastErrorCode: $lastErrorCode, ')
          ..write('lastErrorMessage: $lastErrorMessage, ')
          ..write('rowid: $rowid')
          ..write(')'))
        .toString();
  }
}

class $MobileSyncEventInboxTable extends MobileSyncEventInbox
    with TableInfo<$MobileSyncEventInboxTable, MobileSyncEventInboxRow> {
  @override
  final GeneratedDatabase attachedDatabase;
  final String? _alias;
  $MobileSyncEventInboxTable(this.attachedDatabase, [this._alias]);
  static const VerificationMeta _sourceKeyMeta = const VerificationMeta(
    'sourceKey',
  );
  @override
  late final GeneratedColumn<String> sourceKey = GeneratedColumn<String>(
    'source_key',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
    defaultConstraints: GeneratedColumn.constraintIsAlways(
      'REFERENCES poc_sync_sources (source_key) ON DELETE RESTRICT',
    ),
  );
  static const VerificationMeta _eventSequenceMeta = const VerificationMeta(
    'eventSequence',
  );
  @override
  late final GeneratedColumn<int> eventSequence = GeneratedColumn<int>(
    'event_sequence',
    aliasedName,
    false,
    type: DriftSqlType.int,
    requiredDuringInsert: true,
    $customConstraints: 'NOT NULL CHECK (event_sequence > 0)',
  );
  static const VerificationMeta _eventIdMeta = const VerificationMeta(
    'eventId',
  );
  @override
  late final GeneratedColumn<String> eventId = GeneratedColumn<String>(
    'event_id',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
  );
  static const VerificationMeta _eventTypeMeta = const VerificationMeta(
    'eventType',
  );
  @override
  late final GeneratedColumn<String> eventType = GeneratedColumn<String>(
    'event_type',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
  );
  static const VerificationMeta _eventVersionMeta = const VerificationMeta(
    'eventVersion',
  );
  @override
  late final GeneratedColumn<int> eventVersion = GeneratedColumn<int>(
    'event_version',
    aliasedName,
    false,
    type: DriftSqlType.int,
    requiredDuringInsert: true,
    $customConstraints: 'NOT NULL CHECK (event_version > 0)',
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
  static const VerificationMeta _aggregateVersionMeta = const VerificationMeta(
    'aggregateVersion',
  );
  @override
  late final GeneratedColumn<int> aggregateVersion = GeneratedColumn<int>(
    'aggregate_version',
    aliasedName,
    false,
    type: DriftSqlType.int,
    requiredDuringInsert: true,
    $customConstraints: 'NOT NULL CHECK (aggregate_version > 0)',
  );
  static const VerificationMeta _occurredAtUtcMeta = const VerificationMeta(
    'occurredAtUtc',
  );
  @override
  late final GeneratedColumn<String> occurredAtUtc = GeneratedColumn<String>(
    'occurred_at_utc',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
  );
  static const VerificationMeta _correlationIdMeta = const VerificationMeta(
    'correlationId',
  );
  @override
  late final GeneratedColumn<String> correlationId = GeneratedColumn<String>(
    'correlation_id',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
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
  static const VerificationMeta _receivedAtUtcMeta = const VerificationMeta(
    'receivedAtUtc',
  );
  @override
  late final GeneratedColumn<String> receivedAtUtc = GeneratedColumn<String>(
    'received_at_utc',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
  );
  static const VerificationMeta _appliedAtUtcMeta = const VerificationMeta(
    'appliedAtUtc',
  );
  @override
  late final GeneratedColumn<String> appliedAtUtc = GeneratedColumn<String>(
    'applied_at_utc',
    aliasedName,
    true,
    type: DriftSqlType.string,
    requiredDuringInsert: false,
  );
  static const VerificationMeta _applyStatusMeta = const VerificationMeta(
    'applyStatus',
  );
  @override
  late final GeneratedColumn<String> applyStatus = GeneratedColumn<String>(
    'apply_status',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
    $customConstraints:
        'NOT NULL CHECK (apply_status IN (\'Pending\', \'Applied\', \'SkippedUnknown\'))',
  );
  @override
  List<GeneratedColumn> get $columns => [
    sourceKey,
    eventSequence,
    eventId,
    eventType,
    eventVersion,
    aggregateType,
    aggregateId,
    aggregateVersion,
    occurredAtUtc,
    correlationId,
    payloadJson,
    receivedAtUtc,
    appliedAtUtc,
    applyStatus,
  ];
  @override
  String get aliasedName => _alias ?? actualTableName;
  @override
  String get actualTableName => $name;
  static const String $name = 'mobile_sync_event_inbox';
  @override
  VerificationContext validateIntegrity(
    Insertable<MobileSyncEventInboxRow> instance, {
    bool isInserting = false,
  }) {
    final context = VerificationContext();
    final data = instance.toColumns(true);
    if (data.containsKey('source_key')) {
      context.handle(
        _sourceKeyMeta,
        sourceKey.isAcceptableOrUnknown(data['source_key']!, _sourceKeyMeta),
      );
    } else if (isInserting) {
      context.missing(_sourceKeyMeta);
    }
    if (data.containsKey('event_sequence')) {
      context.handle(
        _eventSequenceMeta,
        eventSequence.isAcceptableOrUnknown(
          data['event_sequence']!,
          _eventSequenceMeta,
        ),
      );
    } else if (isInserting) {
      context.missing(_eventSequenceMeta);
    }
    if (data.containsKey('event_id')) {
      context.handle(
        _eventIdMeta,
        eventId.isAcceptableOrUnknown(data['event_id']!, _eventIdMeta),
      );
    } else if (isInserting) {
      context.missing(_eventIdMeta);
    }
    if (data.containsKey('event_type')) {
      context.handle(
        _eventTypeMeta,
        eventType.isAcceptableOrUnknown(data['event_type']!, _eventTypeMeta),
      );
    } else if (isInserting) {
      context.missing(_eventTypeMeta);
    }
    if (data.containsKey('event_version')) {
      context.handle(
        _eventVersionMeta,
        eventVersion.isAcceptableOrUnknown(
          data['event_version']!,
          _eventVersionMeta,
        ),
      );
    } else if (isInserting) {
      context.missing(_eventVersionMeta);
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
    if (data.containsKey('aggregate_version')) {
      context.handle(
        _aggregateVersionMeta,
        aggregateVersion.isAcceptableOrUnknown(
          data['aggregate_version']!,
          _aggregateVersionMeta,
        ),
      );
    } else if (isInserting) {
      context.missing(_aggregateVersionMeta);
    }
    if (data.containsKey('occurred_at_utc')) {
      context.handle(
        _occurredAtUtcMeta,
        occurredAtUtc.isAcceptableOrUnknown(
          data['occurred_at_utc']!,
          _occurredAtUtcMeta,
        ),
      );
    } else if (isInserting) {
      context.missing(_occurredAtUtcMeta);
    }
    if (data.containsKey('correlation_id')) {
      context.handle(
        _correlationIdMeta,
        correlationId.isAcceptableOrUnknown(
          data['correlation_id']!,
          _correlationIdMeta,
        ),
      );
    } else if (isInserting) {
      context.missing(_correlationIdMeta);
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
    if (data.containsKey('received_at_utc')) {
      context.handle(
        _receivedAtUtcMeta,
        receivedAtUtc.isAcceptableOrUnknown(
          data['received_at_utc']!,
          _receivedAtUtcMeta,
        ),
      );
    } else if (isInserting) {
      context.missing(_receivedAtUtcMeta);
    }
    if (data.containsKey('applied_at_utc')) {
      context.handle(
        _appliedAtUtcMeta,
        appliedAtUtc.isAcceptableOrUnknown(
          data['applied_at_utc']!,
          _appliedAtUtcMeta,
        ),
      );
    }
    if (data.containsKey('apply_status')) {
      context.handle(
        _applyStatusMeta,
        applyStatus.isAcceptableOrUnknown(
          data['apply_status']!,
          _applyStatusMeta,
        ),
      );
    } else if (isInserting) {
      context.missing(_applyStatusMeta);
    }
    return context;
  }

  @override
  Set<GeneratedColumn> get $primaryKey => {sourceKey, eventSequence};
  @override
  List<Set<GeneratedColumn>> get uniqueKeys => [
    {sourceKey, eventId},
  ];
  @override
  MobileSyncEventInboxRow map(
    Map<String, dynamic> data, {
    String? tablePrefix,
  }) {
    final effectivePrefix = tablePrefix != null ? '$tablePrefix.' : '';
    return MobileSyncEventInboxRow(
      sourceKey: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}source_key'],
      )!,
      eventSequence: attachedDatabase.typeMapping.read(
        DriftSqlType.int,
        data['${effectivePrefix}event_sequence'],
      )!,
      eventId: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}event_id'],
      )!,
      eventType: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}event_type'],
      )!,
      eventVersion: attachedDatabase.typeMapping.read(
        DriftSqlType.int,
        data['${effectivePrefix}event_version'],
      )!,
      aggregateType: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}aggregate_type'],
      )!,
      aggregateId: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}aggregate_id'],
      )!,
      aggregateVersion: attachedDatabase.typeMapping.read(
        DriftSqlType.int,
        data['${effectivePrefix}aggregate_version'],
      )!,
      occurredAtUtc: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}occurred_at_utc'],
      )!,
      correlationId: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}correlation_id'],
      )!,
      payloadJson: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}payload_json'],
      )!,
      receivedAtUtc: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}received_at_utc'],
      )!,
      appliedAtUtc: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}applied_at_utc'],
      ),
      applyStatus: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}apply_status'],
      )!,
    );
  }

  @override
  $MobileSyncEventInboxTable createAlias(String alias) {
    return $MobileSyncEventInboxTable(attachedDatabase, alias);
  }
}

class MobileSyncEventInboxRow extends DataClass
    implements Insertable<MobileSyncEventInboxRow> {
  final String sourceKey;
  final int eventSequence;
  final String eventId;
  final String eventType;
  final int eventVersion;
  final String aggregateType;
  final String aggregateId;
  final int aggregateVersion;
  final String occurredAtUtc;
  final String correlationId;
  final String payloadJson;
  final String receivedAtUtc;
  final String? appliedAtUtc;
  final String applyStatus;
  const MobileSyncEventInboxRow({
    required this.sourceKey,
    required this.eventSequence,
    required this.eventId,
    required this.eventType,
    required this.eventVersion,
    required this.aggregateType,
    required this.aggregateId,
    required this.aggregateVersion,
    required this.occurredAtUtc,
    required this.correlationId,
    required this.payloadJson,
    required this.receivedAtUtc,
    this.appliedAtUtc,
    required this.applyStatus,
  });
  @override
  Map<String, Expression> toColumns(bool nullToAbsent) {
    final map = <String, Expression>{};
    map['source_key'] = Variable<String>(sourceKey);
    map['event_sequence'] = Variable<int>(eventSequence);
    map['event_id'] = Variable<String>(eventId);
    map['event_type'] = Variable<String>(eventType);
    map['event_version'] = Variable<int>(eventVersion);
    map['aggregate_type'] = Variable<String>(aggregateType);
    map['aggregate_id'] = Variable<String>(aggregateId);
    map['aggregate_version'] = Variable<int>(aggregateVersion);
    map['occurred_at_utc'] = Variable<String>(occurredAtUtc);
    map['correlation_id'] = Variable<String>(correlationId);
    map['payload_json'] = Variable<String>(payloadJson);
    map['received_at_utc'] = Variable<String>(receivedAtUtc);
    if (!nullToAbsent || appliedAtUtc != null) {
      map['applied_at_utc'] = Variable<String>(appliedAtUtc);
    }
    map['apply_status'] = Variable<String>(applyStatus);
    return map;
  }

  MobileSyncEventInboxCompanion toCompanion(bool nullToAbsent) {
    return MobileSyncEventInboxCompanion(
      sourceKey: Value(sourceKey),
      eventSequence: Value(eventSequence),
      eventId: Value(eventId),
      eventType: Value(eventType),
      eventVersion: Value(eventVersion),
      aggregateType: Value(aggregateType),
      aggregateId: Value(aggregateId),
      aggregateVersion: Value(aggregateVersion),
      occurredAtUtc: Value(occurredAtUtc),
      correlationId: Value(correlationId),
      payloadJson: Value(payloadJson),
      receivedAtUtc: Value(receivedAtUtc),
      appliedAtUtc: appliedAtUtc == null && nullToAbsent
          ? const Value.absent()
          : Value(appliedAtUtc),
      applyStatus: Value(applyStatus),
    );
  }

  factory MobileSyncEventInboxRow.fromJson(
    Map<String, dynamic> json, {
    ValueSerializer? serializer,
  }) {
    serializer ??= driftRuntimeOptions.defaultSerializer;
    return MobileSyncEventInboxRow(
      sourceKey: serializer.fromJson<String>(json['sourceKey']),
      eventSequence: serializer.fromJson<int>(json['eventSequence']),
      eventId: serializer.fromJson<String>(json['eventId']),
      eventType: serializer.fromJson<String>(json['eventType']),
      eventVersion: serializer.fromJson<int>(json['eventVersion']),
      aggregateType: serializer.fromJson<String>(json['aggregateType']),
      aggregateId: serializer.fromJson<String>(json['aggregateId']),
      aggregateVersion: serializer.fromJson<int>(json['aggregateVersion']),
      occurredAtUtc: serializer.fromJson<String>(json['occurredAtUtc']),
      correlationId: serializer.fromJson<String>(json['correlationId']),
      payloadJson: serializer.fromJson<String>(json['payloadJson']),
      receivedAtUtc: serializer.fromJson<String>(json['receivedAtUtc']),
      appliedAtUtc: serializer.fromJson<String?>(json['appliedAtUtc']),
      applyStatus: serializer.fromJson<String>(json['applyStatus']),
    );
  }
  @override
  Map<String, dynamic> toJson({ValueSerializer? serializer}) {
    serializer ??= driftRuntimeOptions.defaultSerializer;
    return <String, dynamic>{
      'sourceKey': serializer.toJson<String>(sourceKey),
      'eventSequence': serializer.toJson<int>(eventSequence),
      'eventId': serializer.toJson<String>(eventId),
      'eventType': serializer.toJson<String>(eventType),
      'eventVersion': serializer.toJson<int>(eventVersion),
      'aggregateType': serializer.toJson<String>(aggregateType),
      'aggregateId': serializer.toJson<String>(aggregateId),
      'aggregateVersion': serializer.toJson<int>(aggregateVersion),
      'occurredAtUtc': serializer.toJson<String>(occurredAtUtc),
      'correlationId': serializer.toJson<String>(correlationId),
      'payloadJson': serializer.toJson<String>(payloadJson),
      'receivedAtUtc': serializer.toJson<String>(receivedAtUtc),
      'appliedAtUtc': serializer.toJson<String?>(appliedAtUtc),
      'applyStatus': serializer.toJson<String>(applyStatus),
    };
  }

  MobileSyncEventInboxRow copyWith({
    String? sourceKey,
    int? eventSequence,
    String? eventId,
    String? eventType,
    int? eventVersion,
    String? aggregateType,
    String? aggregateId,
    int? aggregateVersion,
    String? occurredAtUtc,
    String? correlationId,
    String? payloadJson,
    String? receivedAtUtc,
    Value<String?> appliedAtUtc = const Value.absent(),
    String? applyStatus,
  }) => MobileSyncEventInboxRow(
    sourceKey: sourceKey ?? this.sourceKey,
    eventSequence: eventSequence ?? this.eventSequence,
    eventId: eventId ?? this.eventId,
    eventType: eventType ?? this.eventType,
    eventVersion: eventVersion ?? this.eventVersion,
    aggregateType: aggregateType ?? this.aggregateType,
    aggregateId: aggregateId ?? this.aggregateId,
    aggregateVersion: aggregateVersion ?? this.aggregateVersion,
    occurredAtUtc: occurredAtUtc ?? this.occurredAtUtc,
    correlationId: correlationId ?? this.correlationId,
    payloadJson: payloadJson ?? this.payloadJson,
    receivedAtUtc: receivedAtUtc ?? this.receivedAtUtc,
    appliedAtUtc: appliedAtUtc.present ? appliedAtUtc.value : this.appliedAtUtc,
    applyStatus: applyStatus ?? this.applyStatus,
  );
  MobileSyncEventInboxRow copyWithCompanion(
    MobileSyncEventInboxCompanion data,
  ) {
    return MobileSyncEventInboxRow(
      sourceKey: data.sourceKey.present ? data.sourceKey.value : this.sourceKey,
      eventSequence: data.eventSequence.present
          ? data.eventSequence.value
          : this.eventSequence,
      eventId: data.eventId.present ? data.eventId.value : this.eventId,
      eventType: data.eventType.present ? data.eventType.value : this.eventType,
      eventVersion: data.eventVersion.present
          ? data.eventVersion.value
          : this.eventVersion,
      aggregateType: data.aggregateType.present
          ? data.aggregateType.value
          : this.aggregateType,
      aggregateId: data.aggregateId.present
          ? data.aggregateId.value
          : this.aggregateId,
      aggregateVersion: data.aggregateVersion.present
          ? data.aggregateVersion.value
          : this.aggregateVersion,
      occurredAtUtc: data.occurredAtUtc.present
          ? data.occurredAtUtc.value
          : this.occurredAtUtc,
      correlationId: data.correlationId.present
          ? data.correlationId.value
          : this.correlationId,
      payloadJson: data.payloadJson.present
          ? data.payloadJson.value
          : this.payloadJson,
      receivedAtUtc: data.receivedAtUtc.present
          ? data.receivedAtUtc.value
          : this.receivedAtUtc,
      appliedAtUtc: data.appliedAtUtc.present
          ? data.appliedAtUtc.value
          : this.appliedAtUtc,
      applyStatus: data.applyStatus.present
          ? data.applyStatus.value
          : this.applyStatus,
    );
  }

  @override
  String toString() {
    return (StringBuffer('MobileSyncEventInboxRow(')
          ..write('sourceKey: $sourceKey, ')
          ..write('eventSequence: $eventSequence, ')
          ..write('eventId: $eventId, ')
          ..write('eventType: $eventType, ')
          ..write('eventVersion: $eventVersion, ')
          ..write('aggregateType: $aggregateType, ')
          ..write('aggregateId: $aggregateId, ')
          ..write('aggregateVersion: $aggregateVersion, ')
          ..write('occurredAtUtc: $occurredAtUtc, ')
          ..write('correlationId: $correlationId, ')
          ..write('payloadJson: $payloadJson, ')
          ..write('receivedAtUtc: $receivedAtUtc, ')
          ..write('appliedAtUtc: $appliedAtUtc, ')
          ..write('applyStatus: $applyStatus')
          ..write(')'))
        .toString();
  }

  @override
  int get hashCode => Object.hash(
    sourceKey,
    eventSequence,
    eventId,
    eventType,
    eventVersion,
    aggregateType,
    aggregateId,
    aggregateVersion,
    occurredAtUtc,
    correlationId,
    payloadJson,
    receivedAtUtc,
    appliedAtUtc,
    applyStatus,
  );
  @override
  bool operator ==(Object other) =>
      identical(this, other) ||
      (other is MobileSyncEventInboxRow &&
          other.sourceKey == this.sourceKey &&
          other.eventSequence == this.eventSequence &&
          other.eventId == this.eventId &&
          other.eventType == this.eventType &&
          other.eventVersion == this.eventVersion &&
          other.aggregateType == this.aggregateType &&
          other.aggregateId == this.aggregateId &&
          other.aggregateVersion == this.aggregateVersion &&
          other.occurredAtUtc == this.occurredAtUtc &&
          other.correlationId == this.correlationId &&
          other.payloadJson == this.payloadJson &&
          other.receivedAtUtc == this.receivedAtUtc &&
          other.appliedAtUtc == this.appliedAtUtc &&
          other.applyStatus == this.applyStatus);
}

class MobileSyncEventInboxCompanion
    extends UpdateCompanion<MobileSyncEventInboxRow> {
  final Value<String> sourceKey;
  final Value<int> eventSequence;
  final Value<String> eventId;
  final Value<String> eventType;
  final Value<int> eventVersion;
  final Value<String> aggregateType;
  final Value<String> aggregateId;
  final Value<int> aggregateVersion;
  final Value<String> occurredAtUtc;
  final Value<String> correlationId;
  final Value<String> payloadJson;
  final Value<String> receivedAtUtc;
  final Value<String?> appliedAtUtc;
  final Value<String> applyStatus;
  final Value<int> rowid;
  const MobileSyncEventInboxCompanion({
    this.sourceKey = const Value.absent(),
    this.eventSequence = const Value.absent(),
    this.eventId = const Value.absent(),
    this.eventType = const Value.absent(),
    this.eventVersion = const Value.absent(),
    this.aggregateType = const Value.absent(),
    this.aggregateId = const Value.absent(),
    this.aggregateVersion = const Value.absent(),
    this.occurredAtUtc = const Value.absent(),
    this.correlationId = const Value.absent(),
    this.payloadJson = const Value.absent(),
    this.receivedAtUtc = const Value.absent(),
    this.appliedAtUtc = const Value.absent(),
    this.applyStatus = const Value.absent(),
    this.rowid = const Value.absent(),
  });
  MobileSyncEventInboxCompanion.insert({
    required String sourceKey,
    required int eventSequence,
    required String eventId,
    required String eventType,
    required int eventVersion,
    required String aggregateType,
    required String aggregateId,
    required int aggregateVersion,
    required String occurredAtUtc,
    required String correlationId,
    required String payloadJson,
    required String receivedAtUtc,
    this.appliedAtUtc = const Value.absent(),
    required String applyStatus,
    this.rowid = const Value.absent(),
  }) : sourceKey = Value(sourceKey),
       eventSequence = Value(eventSequence),
       eventId = Value(eventId),
       eventType = Value(eventType),
       eventVersion = Value(eventVersion),
       aggregateType = Value(aggregateType),
       aggregateId = Value(aggregateId),
       aggregateVersion = Value(aggregateVersion),
       occurredAtUtc = Value(occurredAtUtc),
       correlationId = Value(correlationId),
       payloadJson = Value(payloadJson),
       receivedAtUtc = Value(receivedAtUtc),
       applyStatus = Value(applyStatus);
  static Insertable<MobileSyncEventInboxRow> custom({
    Expression<String>? sourceKey,
    Expression<int>? eventSequence,
    Expression<String>? eventId,
    Expression<String>? eventType,
    Expression<int>? eventVersion,
    Expression<String>? aggregateType,
    Expression<String>? aggregateId,
    Expression<int>? aggregateVersion,
    Expression<String>? occurredAtUtc,
    Expression<String>? correlationId,
    Expression<String>? payloadJson,
    Expression<String>? receivedAtUtc,
    Expression<String>? appliedAtUtc,
    Expression<String>? applyStatus,
    Expression<int>? rowid,
  }) {
    return RawValuesInsertable({
      if (sourceKey != null) 'source_key': sourceKey,
      if (eventSequence != null) 'event_sequence': eventSequence,
      if (eventId != null) 'event_id': eventId,
      if (eventType != null) 'event_type': eventType,
      if (eventVersion != null) 'event_version': eventVersion,
      if (aggregateType != null) 'aggregate_type': aggregateType,
      if (aggregateId != null) 'aggregate_id': aggregateId,
      if (aggregateVersion != null) 'aggregate_version': aggregateVersion,
      if (occurredAtUtc != null) 'occurred_at_utc': occurredAtUtc,
      if (correlationId != null) 'correlation_id': correlationId,
      if (payloadJson != null) 'payload_json': payloadJson,
      if (receivedAtUtc != null) 'received_at_utc': receivedAtUtc,
      if (appliedAtUtc != null) 'applied_at_utc': appliedAtUtc,
      if (applyStatus != null) 'apply_status': applyStatus,
      if (rowid != null) 'rowid': rowid,
    });
  }

  MobileSyncEventInboxCompanion copyWith({
    Value<String>? sourceKey,
    Value<int>? eventSequence,
    Value<String>? eventId,
    Value<String>? eventType,
    Value<int>? eventVersion,
    Value<String>? aggregateType,
    Value<String>? aggregateId,
    Value<int>? aggregateVersion,
    Value<String>? occurredAtUtc,
    Value<String>? correlationId,
    Value<String>? payloadJson,
    Value<String>? receivedAtUtc,
    Value<String?>? appliedAtUtc,
    Value<String>? applyStatus,
    Value<int>? rowid,
  }) {
    return MobileSyncEventInboxCompanion(
      sourceKey: sourceKey ?? this.sourceKey,
      eventSequence: eventSequence ?? this.eventSequence,
      eventId: eventId ?? this.eventId,
      eventType: eventType ?? this.eventType,
      eventVersion: eventVersion ?? this.eventVersion,
      aggregateType: aggregateType ?? this.aggregateType,
      aggregateId: aggregateId ?? this.aggregateId,
      aggregateVersion: aggregateVersion ?? this.aggregateVersion,
      occurredAtUtc: occurredAtUtc ?? this.occurredAtUtc,
      correlationId: correlationId ?? this.correlationId,
      payloadJson: payloadJson ?? this.payloadJson,
      receivedAtUtc: receivedAtUtc ?? this.receivedAtUtc,
      appliedAtUtc: appliedAtUtc ?? this.appliedAtUtc,
      applyStatus: applyStatus ?? this.applyStatus,
      rowid: rowid ?? this.rowid,
    );
  }

  @override
  Map<String, Expression> toColumns(bool nullToAbsent) {
    final map = <String, Expression>{};
    if (sourceKey.present) {
      map['source_key'] = Variable<String>(sourceKey.value);
    }
    if (eventSequence.present) {
      map['event_sequence'] = Variable<int>(eventSequence.value);
    }
    if (eventId.present) {
      map['event_id'] = Variable<String>(eventId.value);
    }
    if (eventType.present) {
      map['event_type'] = Variable<String>(eventType.value);
    }
    if (eventVersion.present) {
      map['event_version'] = Variable<int>(eventVersion.value);
    }
    if (aggregateType.present) {
      map['aggregate_type'] = Variable<String>(aggregateType.value);
    }
    if (aggregateId.present) {
      map['aggregate_id'] = Variable<String>(aggregateId.value);
    }
    if (aggregateVersion.present) {
      map['aggregate_version'] = Variable<int>(aggregateVersion.value);
    }
    if (occurredAtUtc.present) {
      map['occurred_at_utc'] = Variable<String>(occurredAtUtc.value);
    }
    if (correlationId.present) {
      map['correlation_id'] = Variable<String>(correlationId.value);
    }
    if (payloadJson.present) {
      map['payload_json'] = Variable<String>(payloadJson.value);
    }
    if (receivedAtUtc.present) {
      map['received_at_utc'] = Variable<String>(receivedAtUtc.value);
    }
    if (appliedAtUtc.present) {
      map['applied_at_utc'] = Variable<String>(appliedAtUtc.value);
    }
    if (applyStatus.present) {
      map['apply_status'] = Variable<String>(applyStatus.value);
    }
    if (rowid.present) {
      map['rowid'] = Variable<int>(rowid.value);
    }
    return map;
  }

  @override
  String toString() {
    return (StringBuffer('MobileSyncEventInboxCompanion(')
          ..write('sourceKey: $sourceKey, ')
          ..write('eventSequence: $eventSequence, ')
          ..write('eventId: $eventId, ')
          ..write('eventType: $eventType, ')
          ..write('eventVersion: $eventVersion, ')
          ..write('aggregateType: $aggregateType, ')
          ..write('aggregateId: $aggregateId, ')
          ..write('aggregateVersion: $aggregateVersion, ')
          ..write('occurredAtUtc: $occurredAtUtc, ')
          ..write('correlationId: $correlationId, ')
          ..write('payloadJson: $payloadJson, ')
          ..write('receivedAtUtc: $receivedAtUtc, ')
          ..write('appliedAtUtc: $appliedAtUtc, ')
          ..write('applyStatus: $applyStatus, ')
          ..write('rowid: $rowid')
          ..write(')'))
        .toString();
  }
}

class $RemoteReceivingSessionProjectionsTable
    extends RemoteReceivingSessionProjections
    with
        TableInfo<
          $RemoteReceivingSessionProjectionsTable,
          RemoteReceivingSessionProjectionRow
        > {
  @override
  final GeneratedDatabase attachedDatabase;
  final String? _alias;
  $RemoteReceivingSessionProjectionsTable(this.attachedDatabase, [this._alias]);
  static const VerificationMeta _sourceKeyMeta = const VerificationMeta(
    'sourceKey',
  );
  @override
  late final GeneratedColumn<String> sourceKey = GeneratedColumn<String>(
    'source_key',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
    defaultConstraints: GeneratedColumn.constraintIsAlways(
      'REFERENCES poc_sync_sources (source_key) ON DELETE RESTRICT',
    ),
  );
  static const VerificationMeta _sessionIdMeta = const VerificationMeta(
    'sessionId',
  );
  @override
  late final GeneratedColumn<String> sessionId = GeneratedColumn<String>(
    'session_id',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
  );
  static const VerificationMeta _cloudReferenceMeta = const VerificationMeta(
    'cloudReference',
  );
  @override
  late final GeneratedColumn<String> cloudReference = GeneratedColumn<String>(
    'cloud_reference',
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
  );
  static const VerificationMeta _editorDeviceIdMeta = const VerificationMeta(
    'editorDeviceId',
  );
  @override
  late final GeneratedColumn<String> editorDeviceId = GeneratedColumn<String>(
    'editor_device_id',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
  );
  static const VerificationMeta _leaseExpiresAtUtcMeta = const VerificationMeta(
    'leaseExpiresAtUtc',
  );
  @override
  late final GeneratedColumn<String> leaseExpiresAtUtc =
      GeneratedColumn<String>(
        'lease_expires_at_utc',
        aliasedName,
        true,
        type: DriftSqlType.string,
        requiredDuringInsert: false,
      );
  static const VerificationMeta _entryCountMeta = const VerificationMeta(
    'entryCount',
  );
  @override
  late final GeneratedColumn<int> entryCount = GeneratedColumn<int>(
    'entry_count',
    aliasedName,
    false,
    type: DriftSqlType.int,
    requiredDuringInsert: true,
    $customConstraints: 'NOT NULL CHECK (entry_count >= 0)',
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
  static const VerificationMeta _cloudVersionMeta = const VerificationMeta(
    'cloudVersion',
  );
  @override
  late final GeneratedColumn<int> cloudVersion = GeneratedColumn<int>(
    'cloud_version',
    aliasedName,
    false,
    type: DriftSqlType.int,
    requiredDuringInsert: true,
    $customConstraints: 'NOT NULL CHECK (cloud_version > 0)',
  );
  static const VerificationMeta _approvedByDeviceIdMeta =
      const VerificationMeta('approvedByDeviceId');
  @override
  late final GeneratedColumn<String> approvedByDeviceId =
      GeneratedColumn<String>(
        'approved_by_device_id',
        aliasedName,
        true,
        type: DriftSqlType.string,
        requiredDuringInsert: false,
      );
  static const VerificationMeta _finalizationIdMeta = const VerificationMeta(
    'finalizationId',
  );
  @override
  late final GeneratedColumn<String> finalizationId = GeneratedColumn<String>(
    'finalization_id',
    aliasedName,
    true,
    type: DriftSqlType.string,
    requiredDuringInsert: false,
  );
  static const VerificationMeta _lastCloudUpdateAtUtcMeta =
      const VerificationMeta('lastCloudUpdateAtUtc');
  @override
  late final GeneratedColumn<String> lastCloudUpdateAtUtc =
      GeneratedColumn<String>(
        'last_cloud_update_at_utc',
        aliasedName,
        false,
        type: DriftSqlType.string,
        requiredDuringInsert: true,
      );
  @override
  List<GeneratedColumn> get $columns => [
    sourceKey,
    sessionId,
    cloudReference,
    status,
    editorDeviceId,
    leaseExpiresAtUtc,
    entryCount,
    processedTotalWeightKg,
    cloudVersion,
    approvedByDeviceId,
    finalizationId,
    lastCloudUpdateAtUtc,
  ];
  @override
  String get aliasedName => _alias ?? actualTableName;
  @override
  String get actualTableName => $name;
  static const String $name = 'remote_receiving_session_projections';
  @override
  VerificationContext validateIntegrity(
    Insertable<RemoteReceivingSessionProjectionRow> instance, {
    bool isInserting = false,
  }) {
    final context = VerificationContext();
    final data = instance.toColumns(true);
    if (data.containsKey('source_key')) {
      context.handle(
        _sourceKeyMeta,
        sourceKey.isAcceptableOrUnknown(data['source_key']!, _sourceKeyMeta),
      );
    } else if (isInserting) {
      context.missing(_sourceKeyMeta);
    }
    if (data.containsKey('session_id')) {
      context.handle(
        _sessionIdMeta,
        sessionId.isAcceptableOrUnknown(data['session_id']!, _sessionIdMeta),
      );
    } else if (isInserting) {
      context.missing(_sessionIdMeta);
    }
    if (data.containsKey('cloud_reference')) {
      context.handle(
        _cloudReferenceMeta,
        cloudReference.isAcceptableOrUnknown(
          data['cloud_reference']!,
          _cloudReferenceMeta,
        ),
      );
    } else if (isInserting) {
      context.missing(_cloudReferenceMeta);
    }
    if (data.containsKey('status')) {
      context.handle(
        _statusMeta,
        status.isAcceptableOrUnknown(data['status']!, _statusMeta),
      );
    } else if (isInserting) {
      context.missing(_statusMeta);
    }
    if (data.containsKey('editor_device_id')) {
      context.handle(
        _editorDeviceIdMeta,
        editorDeviceId.isAcceptableOrUnknown(
          data['editor_device_id']!,
          _editorDeviceIdMeta,
        ),
      );
    } else if (isInserting) {
      context.missing(_editorDeviceIdMeta);
    }
    if (data.containsKey('lease_expires_at_utc')) {
      context.handle(
        _leaseExpiresAtUtcMeta,
        leaseExpiresAtUtc.isAcceptableOrUnknown(
          data['lease_expires_at_utc']!,
          _leaseExpiresAtUtcMeta,
        ),
      );
    }
    if (data.containsKey('entry_count')) {
      context.handle(
        _entryCountMeta,
        entryCount.isAcceptableOrUnknown(data['entry_count']!, _entryCountMeta),
      );
    } else if (isInserting) {
      context.missing(_entryCountMeta);
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
    if (data.containsKey('cloud_version')) {
      context.handle(
        _cloudVersionMeta,
        cloudVersion.isAcceptableOrUnknown(
          data['cloud_version']!,
          _cloudVersionMeta,
        ),
      );
    } else if (isInserting) {
      context.missing(_cloudVersionMeta);
    }
    if (data.containsKey('approved_by_device_id')) {
      context.handle(
        _approvedByDeviceIdMeta,
        approvedByDeviceId.isAcceptableOrUnknown(
          data['approved_by_device_id']!,
          _approvedByDeviceIdMeta,
        ),
      );
    }
    if (data.containsKey('finalization_id')) {
      context.handle(
        _finalizationIdMeta,
        finalizationId.isAcceptableOrUnknown(
          data['finalization_id']!,
          _finalizationIdMeta,
        ),
      );
    }
    if (data.containsKey('last_cloud_update_at_utc')) {
      context.handle(
        _lastCloudUpdateAtUtcMeta,
        lastCloudUpdateAtUtc.isAcceptableOrUnknown(
          data['last_cloud_update_at_utc']!,
          _lastCloudUpdateAtUtcMeta,
        ),
      );
    } else if (isInserting) {
      context.missing(_lastCloudUpdateAtUtcMeta);
    }
    return context;
  }

  @override
  Set<GeneratedColumn> get $primaryKey => {sourceKey, sessionId};
  @override
  RemoteReceivingSessionProjectionRow map(
    Map<String, dynamic> data, {
    String? tablePrefix,
  }) {
    final effectivePrefix = tablePrefix != null ? '$tablePrefix.' : '';
    return RemoteReceivingSessionProjectionRow(
      sourceKey: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}source_key'],
      )!,
      sessionId: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}session_id'],
      )!,
      cloudReference: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}cloud_reference'],
      )!,
      status: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}status'],
      )!,
      editorDeviceId: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}editor_device_id'],
      )!,
      leaseExpiresAtUtc: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}lease_expires_at_utc'],
      ),
      entryCount: attachedDatabase.typeMapping.read(
        DriftSqlType.int,
        data['${effectivePrefix}entry_count'],
      )!,
      processedTotalWeightKg: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}processed_total_weight_kg'],
      )!,
      cloudVersion: attachedDatabase.typeMapping.read(
        DriftSqlType.int,
        data['${effectivePrefix}cloud_version'],
      )!,
      approvedByDeviceId: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}approved_by_device_id'],
      ),
      finalizationId: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}finalization_id'],
      ),
      lastCloudUpdateAtUtc: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}last_cloud_update_at_utc'],
      )!,
    );
  }

  @override
  $RemoteReceivingSessionProjectionsTable createAlias(String alias) {
    return $RemoteReceivingSessionProjectionsTable(attachedDatabase, alias);
  }
}

class RemoteReceivingSessionProjectionRow extends DataClass
    implements Insertable<RemoteReceivingSessionProjectionRow> {
  final String sourceKey;
  final String sessionId;
  final String cloudReference;
  final String status;
  final String editorDeviceId;
  final String? leaseExpiresAtUtc;
  final int entryCount;
  final String processedTotalWeightKg;
  final int cloudVersion;
  final String? approvedByDeviceId;
  final String? finalizationId;
  final String lastCloudUpdateAtUtc;
  const RemoteReceivingSessionProjectionRow({
    required this.sourceKey,
    required this.sessionId,
    required this.cloudReference,
    required this.status,
    required this.editorDeviceId,
    this.leaseExpiresAtUtc,
    required this.entryCount,
    required this.processedTotalWeightKg,
    required this.cloudVersion,
    this.approvedByDeviceId,
    this.finalizationId,
    required this.lastCloudUpdateAtUtc,
  });
  @override
  Map<String, Expression> toColumns(bool nullToAbsent) {
    final map = <String, Expression>{};
    map['source_key'] = Variable<String>(sourceKey);
    map['session_id'] = Variable<String>(sessionId);
    map['cloud_reference'] = Variable<String>(cloudReference);
    map['status'] = Variable<String>(status);
    map['editor_device_id'] = Variable<String>(editorDeviceId);
    if (!nullToAbsent || leaseExpiresAtUtc != null) {
      map['lease_expires_at_utc'] = Variable<String>(leaseExpiresAtUtc);
    }
    map['entry_count'] = Variable<int>(entryCount);
    map['processed_total_weight_kg'] = Variable<String>(processedTotalWeightKg);
    map['cloud_version'] = Variable<int>(cloudVersion);
    if (!nullToAbsent || approvedByDeviceId != null) {
      map['approved_by_device_id'] = Variable<String>(approvedByDeviceId);
    }
    if (!nullToAbsent || finalizationId != null) {
      map['finalization_id'] = Variable<String>(finalizationId);
    }
    map['last_cloud_update_at_utc'] = Variable<String>(lastCloudUpdateAtUtc);
    return map;
  }

  RemoteReceivingSessionProjectionsCompanion toCompanion(bool nullToAbsent) {
    return RemoteReceivingSessionProjectionsCompanion(
      sourceKey: Value(sourceKey),
      sessionId: Value(sessionId),
      cloudReference: Value(cloudReference),
      status: Value(status),
      editorDeviceId: Value(editorDeviceId),
      leaseExpiresAtUtc: leaseExpiresAtUtc == null && nullToAbsent
          ? const Value.absent()
          : Value(leaseExpiresAtUtc),
      entryCount: Value(entryCount),
      processedTotalWeightKg: Value(processedTotalWeightKg),
      cloudVersion: Value(cloudVersion),
      approvedByDeviceId: approvedByDeviceId == null && nullToAbsent
          ? const Value.absent()
          : Value(approvedByDeviceId),
      finalizationId: finalizationId == null && nullToAbsent
          ? const Value.absent()
          : Value(finalizationId),
      lastCloudUpdateAtUtc: Value(lastCloudUpdateAtUtc),
    );
  }

  factory RemoteReceivingSessionProjectionRow.fromJson(
    Map<String, dynamic> json, {
    ValueSerializer? serializer,
  }) {
    serializer ??= driftRuntimeOptions.defaultSerializer;
    return RemoteReceivingSessionProjectionRow(
      sourceKey: serializer.fromJson<String>(json['sourceKey']),
      sessionId: serializer.fromJson<String>(json['sessionId']),
      cloudReference: serializer.fromJson<String>(json['cloudReference']),
      status: serializer.fromJson<String>(json['status']),
      editorDeviceId: serializer.fromJson<String>(json['editorDeviceId']),
      leaseExpiresAtUtc: serializer.fromJson<String?>(
        json['leaseExpiresAtUtc'],
      ),
      entryCount: serializer.fromJson<int>(json['entryCount']),
      processedTotalWeightKg: serializer.fromJson<String>(
        json['processedTotalWeightKg'],
      ),
      cloudVersion: serializer.fromJson<int>(json['cloudVersion']),
      approvedByDeviceId: serializer.fromJson<String?>(
        json['approvedByDeviceId'],
      ),
      finalizationId: serializer.fromJson<String?>(json['finalizationId']),
      lastCloudUpdateAtUtc: serializer.fromJson<String>(
        json['lastCloudUpdateAtUtc'],
      ),
    );
  }
  @override
  Map<String, dynamic> toJson({ValueSerializer? serializer}) {
    serializer ??= driftRuntimeOptions.defaultSerializer;
    return <String, dynamic>{
      'sourceKey': serializer.toJson<String>(sourceKey),
      'sessionId': serializer.toJson<String>(sessionId),
      'cloudReference': serializer.toJson<String>(cloudReference),
      'status': serializer.toJson<String>(status),
      'editorDeviceId': serializer.toJson<String>(editorDeviceId),
      'leaseExpiresAtUtc': serializer.toJson<String?>(leaseExpiresAtUtc),
      'entryCount': serializer.toJson<int>(entryCount),
      'processedTotalWeightKg': serializer.toJson<String>(
        processedTotalWeightKg,
      ),
      'cloudVersion': serializer.toJson<int>(cloudVersion),
      'approvedByDeviceId': serializer.toJson<String?>(approvedByDeviceId),
      'finalizationId': serializer.toJson<String?>(finalizationId),
      'lastCloudUpdateAtUtc': serializer.toJson<String>(lastCloudUpdateAtUtc),
    };
  }

  RemoteReceivingSessionProjectionRow copyWith({
    String? sourceKey,
    String? sessionId,
    String? cloudReference,
    String? status,
    String? editorDeviceId,
    Value<String?> leaseExpiresAtUtc = const Value.absent(),
    int? entryCount,
    String? processedTotalWeightKg,
    int? cloudVersion,
    Value<String?> approvedByDeviceId = const Value.absent(),
    Value<String?> finalizationId = const Value.absent(),
    String? lastCloudUpdateAtUtc,
  }) => RemoteReceivingSessionProjectionRow(
    sourceKey: sourceKey ?? this.sourceKey,
    sessionId: sessionId ?? this.sessionId,
    cloudReference: cloudReference ?? this.cloudReference,
    status: status ?? this.status,
    editorDeviceId: editorDeviceId ?? this.editorDeviceId,
    leaseExpiresAtUtc: leaseExpiresAtUtc.present
        ? leaseExpiresAtUtc.value
        : this.leaseExpiresAtUtc,
    entryCount: entryCount ?? this.entryCount,
    processedTotalWeightKg:
        processedTotalWeightKg ?? this.processedTotalWeightKg,
    cloudVersion: cloudVersion ?? this.cloudVersion,
    approvedByDeviceId: approvedByDeviceId.present
        ? approvedByDeviceId.value
        : this.approvedByDeviceId,
    finalizationId: finalizationId.present
        ? finalizationId.value
        : this.finalizationId,
    lastCloudUpdateAtUtc: lastCloudUpdateAtUtc ?? this.lastCloudUpdateAtUtc,
  );
  RemoteReceivingSessionProjectionRow copyWithCompanion(
    RemoteReceivingSessionProjectionsCompanion data,
  ) {
    return RemoteReceivingSessionProjectionRow(
      sourceKey: data.sourceKey.present ? data.sourceKey.value : this.sourceKey,
      sessionId: data.sessionId.present ? data.sessionId.value : this.sessionId,
      cloudReference: data.cloudReference.present
          ? data.cloudReference.value
          : this.cloudReference,
      status: data.status.present ? data.status.value : this.status,
      editorDeviceId: data.editorDeviceId.present
          ? data.editorDeviceId.value
          : this.editorDeviceId,
      leaseExpiresAtUtc: data.leaseExpiresAtUtc.present
          ? data.leaseExpiresAtUtc.value
          : this.leaseExpiresAtUtc,
      entryCount: data.entryCount.present
          ? data.entryCount.value
          : this.entryCount,
      processedTotalWeightKg: data.processedTotalWeightKg.present
          ? data.processedTotalWeightKg.value
          : this.processedTotalWeightKg,
      cloudVersion: data.cloudVersion.present
          ? data.cloudVersion.value
          : this.cloudVersion,
      approvedByDeviceId: data.approvedByDeviceId.present
          ? data.approvedByDeviceId.value
          : this.approvedByDeviceId,
      finalizationId: data.finalizationId.present
          ? data.finalizationId.value
          : this.finalizationId,
      lastCloudUpdateAtUtc: data.lastCloudUpdateAtUtc.present
          ? data.lastCloudUpdateAtUtc.value
          : this.lastCloudUpdateAtUtc,
    );
  }

  @override
  String toString() {
    return (StringBuffer('RemoteReceivingSessionProjectionRow(')
          ..write('sourceKey: $sourceKey, ')
          ..write('sessionId: $sessionId, ')
          ..write('cloudReference: $cloudReference, ')
          ..write('status: $status, ')
          ..write('editorDeviceId: $editorDeviceId, ')
          ..write('leaseExpiresAtUtc: $leaseExpiresAtUtc, ')
          ..write('entryCount: $entryCount, ')
          ..write('processedTotalWeightKg: $processedTotalWeightKg, ')
          ..write('cloudVersion: $cloudVersion, ')
          ..write('approvedByDeviceId: $approvedByDeviceId, ')
          ..write('finalizationId: $finalizationId, ')
          ..write('lastCloudUpdateAtUtc: $lastCloudUpdateAtUtc')
          ..write(')'))
        .toString();
  }

  @override
  int get hashCode => Object.hash(
    sourceKey,
    sessionId,
    cloudReference,
    status,
    editorDeviceId,
    leaseExpiresAtUtc,
    entryCount,
    processedTotalWeightKg,
    cloudVersion,
    approvedByDeviceId,
    finalizationId,
    lastCloudUpdateAtUtc,
  );
  @override
  bool operator ==(Object other) =>
      identical(this, other) ||
      (other is RemoteReceivingSessionProjectionRow &&
          other.sourceKey == this.sourceKey &&
          other.sessionId == this.sessionId &&
          other.cloudReference == this.cloudReference &&
          other.status == this.status &&
          other.editorDeviceId == this.editorDeviceId &&
          other.leaseExpiresAtUtc == this.leaseExpiresAtUtc &&
          other.entryCount == this.entryCount &&
          other.processedTotalWeightKg == this.processedTotalWeightKg &&
          other.cloudVersion == this.cloudVersion &&
          other.approvedByDeviceId == this.approvedByDeviceId &&
          other.finalizationId == this.finalizationId &&
          other.lastCloudUpdateAtUtc == this.lastCloudUpdateAtUtc);
}

class RemoteReceivingSessionProjectionsCompanion
    extends UpdateCompanion<RemoteReceivingSessionProjectionRow> {
  final Value<String> sourceKey;
  final Value<String> sessionId;
  final Value<String> cloudReference;
  final Value<String> status;
  final Value<String> editorDeviceId;
  final Value<String?> leaseExpiresAtUtc;
  final Value<int> entryCount;
  final Value<String> processedTotalWeightKg;
  final Value<int> cloudVersion;
  final Value<String?> approvedByDeviceId;
  final Value<String?> finalizationId;
  final Value<String> lastCloudUpdateAtUtc;
  final Value<int> rowid;
  const RemoteReceivingSessionProjectionsCompanion({
    this.sourceKey = const Value.absent(),
    this.sessionId = const Value.absent(),
    this.cloudReference = const Value.absent(),
    this.status = const Value.absent(),
    this.editorDeviceId = const Value.absent(),
    this.leaseExpiresAtUtc = const Value.absent(),
    this.entryCount = const Value.absent(),
    this.processedTotalWeightKg = const Value.absent(),
    this.cloudVersion = const Value.absent(),
    this.approvedByDeviceId = const Value.absent(),
    this.finalizationId = const Value.absent(),
    this.lastCloudUpdateAtUtc = const Value.absent(),
    this.rowid = const Value.absent(),
  });
  RemoteReceivingSessionProjectionsCompanion.insert({
    required String sourceKey,
    required String sessionId,
    required String cloudReference,
    required String status,
    required String editorDeviceId,
    this.leaseExpiresAtUtc = const Value.absent(),
    required int entryCount,
    required String processedTotalWeightKg,
    required int cloudVersion,
    this.approvedByDeviceId = const Value.absent(),
    this.finalizationId = const Value.absent(),
    required String lastCloudUpdateAtUtc,
    this.rowid = const Value.absent(),
  }) : sourceKey = Value(sourceKey),
       sessionId = Value(sessionId),
       cloudReference = Value(cloudReference),
       status = Value(status),
       editorDeviceId = Value(editorDeviceId),
       entryCount = Value(entryCount),
       processedTotalWeightKg = Value(processedTotalWeightKg),
       cloudVersion = Value(cloudVersion),
       lastCloudUpdateAtUtc = Value(lastCloudUpdateAtUtc);
  static Insertable<RemoteReceivingSessionProjectionRow> custom({
    Expression<String>? sourceKey,
    Expression<String>? sessionId,
    Expression<String>? cloudReference,
    Expression<String>? status,
    Expression<String>? editorDeviceId,
    Expression<String>? leaseExpiresAtUtc,
    Expression<int>? entryCount,
    Expression<String>? processedTotalWeightKg,
    Expression<int>? cloudVersion,
    Expression<String>? approvedByDeviceId,
    Expression<String>? finalizationId,
    Expression<String>? lastCloudUpdateAtUtc,
    Expression<int>? rowid,
  }) {
    return RawValuesInsertable({
      if (sourceKey != null) 'source_key': sourceKey,
      if (sessionId != null) 'session_id': sessionId,
      if (cloudReference != null) 'cloud_reference': cloudReference,
      if (status != null) 'status': status,
      if (editorDeviceId != null) 'editor_device_id': editorDeviceId,
      if (leaseExpiresAtUtc != null) 'lease_expires_at_utc': leaseExpiresAtUtc,
      if (entryCount != null) 'entry_count': entryCount,
      if (processedTotalWeightKg != null)
        'processed_total_weight_kg': processedTotalWeightKg,
      if (cloudVersion != null) 'cloud_version': cloudVersion,
      if (approvedByDeviceId != null)
        'approved_by_device_id': approvedByDeviceId,
      if (finalizationId != null) 'finalization_id': finalizationId,
      if (lastCloudUpdateAtUtc != null)
        'last_cloud_update_at_utc': lastCloudUpdateAtUtc,
      if (rowid != null) 'rowid': rowid,
    });
  }

  RemoteReceivingSessionProjectionsCompanion copyWith({
    Value<String>? sourceKey,
    Value<String>? sessionId,
    Value<String>? cloudReference,
    Value<String>? status,
    Value<String>? editorDeviceId,
    Value<String?>? leaseExpiresAtUtc,
    Value<int>? entryCount,
    Value<String>? processedTotalWeightKg,
    Value<int>? cloudVersion,
    Value<String?>? approvedByDeviceId,
    Value<String?>? finalizationId,
    Value<String>? lastCloudUpdateAtUtc,
    Value<int>? rowid,
  }) {
    return RemoteReceivingSessionProjectionsCompanion(
      sourceKey: sourceKey ?? this.sourceKey,
      sessionId: sessionId ?? this.sessionId,
      cloudReference: cloudReference ?? this.cloudReference,
      status: status ?? this.status,
      editorDeviceId: editorDeviceId ?? this.editorDeviceId,
      leaseExpiresAtUtc: leaseExpiresAtUtc ?? this.leaseExpiresAtUtc,
      entryCount: entryCount ?? this.entryCount,
      processedTotalWeightKg:
          processedTotalWeightKg ?? this.processedTotalWeightKg,
      cloudVersion: cloudVersion ?? this.cloudVersion,
      approvedByDeviceId: approvedByDeviceId ?? this.approvedByDeviceId,
      finalizationId: finalizationId ?? this.finalizationId,
      lastCloudUpdateAtUtc: lastCloudUpdateAtUtc ?? this.lastCloudUpdateAtUtc,
      rowid: rowid ?? this.rowid,
    );
  }

  @override
  Map<String, Expression> toColumns(bool nullToAbsent) {
    final map = <String, Expression>{};
    if (sourceKey.present) {
      map['source_key'] = Variable<String>(sourceKey.value);
    }
    if (sessionId.present) {
      map['session_id'] = Variable<String>(sessionId.value);
    }
    if (cloudReference.present) {
      map['cloud_reference'] = Variable<String>(cloudReference.value);
    }
    if (status.present) {
      map['status'] = Variable<String>(status.value);
    }
    if (editorDeviceId.present) {
      map['editor_device_id'] = Variable<String>(editorDeviceId.value);
    }
    if (leaseExpiresAtUtc.present) {
      map['lease_expires_at_utc'] = Variable<String>(leaseExpiresAtUtc.value);
    }
    if (entryCount.present) {
      map['entry_count'] = Variable<int>(entryCount.value);
    }
    if (processedTotalWeightKg.present) {
      map['processed_total_weight_kg'] = Variable<String>(
        processedTotalWeightKg.value,
      );
    }
    if (cloudVersion.present) {
      map['cloud_version'] = Variable<int>(cloudVersion.value);
    }
    if (approvedByDeviceId.present) {
      map['approved_by_device_id'] = Variable<String>(approvedByDeviceId.value);
    }
    if (finalizationId.present) {
      map['finalization_id'] = Variable<String>(finalizationId.value);
    }
    if (lastCloudUpdateAtUtc.present) {
      map['last_cloud_update_at_utc'] = Variable<String>(
        lastCloudUpdateAtUtc.value,
      );
    }
    if (rowid.present) {
      map['rowid'] = Variable<int>(rowid.value);
    }
    return map;
  }

  @override
  String toString() {
    return (StringBuffer('RemoteReceivingSessionProjectionsCompanion(')
          ..write('sourceKey: $sourceKey, ')
          ..write('sessionId: $sessionId, ')
          ..write('cloudReference: $cloudReference, ')
          ..write('status: $status, ')
          ..write('editorDeviceId: $editorDeviceId, ')
          ..write('leaseExpiresAtUtc: $leaseExpiresAtUtc, ')
          ..write('entryCount: $entryCount, ')
          ..write('processedTotalWeightKg: $processedTotalWeightKg, ')
          ..write('cloudVersion: $cloudVersion, ')
          ..write('approvedByDeviceId: $approvedByDeviceId, ')
          ..write('finalizationId: $finalizationId, ')
          ..write('lastCloudUpdateAtUtc: $lastCloudUpdateAtUtc, ')
          ..write('rowid: $rowid')
          ..write(')'))
        .toString();
  }
}

class $RemoteReceivingEntrySummariesTable extends RemoteReceivingEntrySummaries
    with
        TableInfo<
          $RemoteReceivingEntrySummariesTable,
          RemoteReceivingEntrySummaryRow
        > {
  @override
  final GeneratedDatabase attachedDatabase;
  final String? _alias;
  $RemoteReceivingEntrySummariesTable(this.attachedDatabase, [this._alias]);
  static const VerificationMeta _sourceKeyMeta = const VerificationMeta(
    'sourceKey',
  );
  @override
  late final GeneratedColumn<String> sourceKey = GeneratedColumn<String>(
    'source_key',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
  );
  static const VerificationMeta _sessionIdMeta = const VerificationMeta(
    'sessionId',
  );
  @override
  late final GeneratedColumn<String> sessionId = GeneratedColumn<String>(
    'session_id',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
  );
  static const VerificationMeta _entryIdMeta = const VerificationMeta(
    'entryId',
  );
  @override
  late final GeneratedColumn<String> entryId = GeneratedColumn<String>(
    'entry_id',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
  );
  static const VerificationMeta _eventSequenceMeta = const VerificationMeta(
    'eventSequence',
  );
  @override
  late final GeneratedColumn<int> eventSequence = GeneratedColumn<int>(
    'event_sequence',
    aliasedName,
    true,
    type: DriftSqlType.int,
    requiredDuringInsert: false,
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
    true,
    type: DriftSqlType.string,
    requiredDuringInsert: false,
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
    true,
    type: DriftSqlType.string,
    requiredDuringInsert: false,
  );
  static const VerificationMeta _decimalPlacesMeta = const VerificationMeta(
    'decimalPlaces',
  );
  @override
  late final GeneratedColumn<int> decimalPlaces = GeneratedColumn<int>(
    'decimal_places',
    aliasedName,
    true,
    type: DriftSqlType.int,
    requiredDuringInsert: false,
  );
  static const VerificationMeta _processingMethodMeta = const VerificationMeta(
    'processingMethod',
  );
  @override
  late final GeneratedColumn<String> processingMethod = GeneratedColumn<String>(
    'processing_method',
    aliasedName,
    true,
    type: DriftSqlType.string,
    requiredDuringInsert: false,
  );
  static const VerificationMeta _weightSourceMeta = const VerificationMeta(
    'weightSource',
  );
  @override
  late final GeneratedColumn<String> weightSource = GeneratedColumn<String>(
    'weight_source',
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
        true,
        type: DriftSqlType.string,
        requiredDuringInsert: false,
      );
  static const VerificationMeta _acceptedAtServerUtcMeta =
      const VerificationMeta('acceptedAtServerUtc');
  @override
  late final GeneratedColumn<String> acceptedAtServerUtc =
      GeneratedColumn<String>(
        'accepted_at_server_utc',
        aliasedName,
        true,
        type: DriftSqlType.string,
        requiredDuringInsert: false,
      );
  @override
  List<GeneratedColumn> get $columns => [
    sourceKey,
    sessionId,
    entryId,
    eventSequence,
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
    capturedAtDeviceUtc,
    acceptedAtServerUtc,
  ];
  @override
  String get aliasedName => _alias ?? actualTableName;
  @override
  String get actualTableName => $name;
  static const String $name = 'remote_receiving_entry_summaries';
  @override
  VerificationContext validateIntegrity(
    Insertable<RemoteReceivingEntrySummaryRow> instance, {
    bool isInserting = false,
  }) {
    final context = VerificationContext();
    final data = instance.toColumns(true);
    if (data.containsKey('source_key')) {
      context.handle(
        _sourceKeyMeta,
        sourceKey.isAcceptableOrUnknown(data['source_key']!, _sourceKeyMeta),
      );
    } else if (isInserting) {
      context.missing(_sourceKeyMeta);
    }
    if (data.containsKey('session_id')) {
      context.handle(
        _sessionIdMeta,
        sessionId.isAcceptableOrUnknown(data['session_id']!, _sessionIdMeta),
      );
    } else if (isInserting) {
      context.missing(_sessionIdMeta);
    }
    if (data.containsKey('entry_id')) {
      context.handle(
        _entryIdMeta,
        entryId.isAcceptableOrUnknown(data['entry_id']!, _entryIdMeta),
      );
    } else if (isInserting) {
      context.missing(_entryIdMeta);
    }
    if (data.containsKey('event_sequence')) {
      context.handle(
        _eventSequenceMeta,
        eventSequence.isAcceptableOrUnknown(
          data['event_sequence']!,
          _eventSequenceMeta,
        ),
      );
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
    }
    if (data.containsKey('decimal_places')) {
      context.handle(
        _decimalPlacesMeta,
        decimalPlaces.isAcceptableOrUnknown(
          data['decimal_places']!,
          _decimalPlacesMeta,
        ),
      );
    }
    if (data.containsKey('processing_method')) {
      context.handle(
        _processingMethodMeta,
        processingMethod.isAcceptableOrUnknown(
          data['processing_method']!,
          _processingMethodMeta,
        ),
      );
    }
    if (data.containsKey('weight_source')) {
      context.handle(
        _weightSourceMeta,
        weightSource.isAcceptableOrUnknown(
          data['weight_source']!,
          _weightSourceMeta,
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
    }
    if (data.containsKey('accepted_at_server_utc')) {
      context.handle(
        _acceptedAtServerUtcMeta,
        acceptedAtServerUtc.isAcceptableOrUnknown(
          data['accepted_at_server_utc']!,
          _acceptedAtServerUtcMeta,
        ),
      );
    }
    return context;
  }

  @override
  Set<GeneratedColumn> get $primaryKey => {sourceKey, sessionId, entryId};
  @override
  List<Set<GeneratedColumn>> get uniqueKeys => [
    {sourceKey, sessionId, localSequence},
  ];
  @override
  RemoteReceivingEntrySummaryRow map(
    Map<String, dynamic> data, {
    String? tablePrefix,
  }) {
    final effectivePrefix = tablePrefix != null ? '$tablePrefix.' : '';
    return RemoteReceivingEntrySummaryRow(
      sourceKey: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}source_key'],
      )!,
      sessionId: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}session_id'],
      )!,
      entryId: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}entry_id'],
      )!,
      eventSequence: attachedDatabase.typeMapping.read(
        DriftSqlType.int,
        data['${effectivePrefix}event_sequence'],
      ),
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
      ),
      processedWeightKg: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}processed_weight_kg'],
      )!,
      displayWeightKg: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}display_weight_kg'],
      ),
      decimalPlaces: attachedDatabase.typeMapping.read(
        DriftSqlType.int,
        data['${effectivePrefix}decimal_places'],
      ),
      processingMethod: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}processing_method'],
      ),
      weightSource: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}weight_source'],
      ),
      capturedAtDeviceUtc: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}captured_at_device_utc'],
      ),
      acceptedAtServerUtc: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}accepted_at_server_utc'],
      ),
    );
  }

  @override
  $RemoteReceivingEntrySummariesTable createAlias(String alias) {
    return $RemoteReceivingEntrySummariesTable(attachedDatabase, alias);
  }
}

class RemoteReceivingEntrySummaryRow extends DataClass
    implements Insertable<RemoteReceivingEntrySummaryRow> {
  final String sourceKey;
  final String sessionId;
  final String entryId;
  final int? eventSequence;
  final int localSequence;
  final String productReference;
  final String bagTypeReference;
  final int bagCount;
  final String? rawWeightKg;
  final String processedWeightKg;
  final String? displayWeightKg;
  final int? decimalPlaces;
  final String? processingMethod;
  final String? weightSource;
  final String? capturedAtDeviceUtc;
  final String? acceptedAtServerUtc;
  const RemoteReceivingEntrySummaryRow({
    required this.sourceKey,
    required this.sessionId,
    required this.entryId,
    this.eventSequence,
    required this.localSequence,
    required this.productReference,
    required this.bagTypeReference,
    required this.bagCount,
    this.rawWeightKg,
    required this.processedWeightKg,
    this.displayWeightKg,
    this.decimalPlaces,
    this.processingMethod,
    this.weightSource,
    this.capturedAtDeviceUtc,
    this.acceptedAtServerUtc,
  });
  @override
  Map<String, Expression> toColumns(bool nullToAbsent) {
    final map = <String, Expression>{};
    map['source_key'] = Variable<String>(sourceKey);
    map['session_id'] = Variable<String>(sessionId);
    map['entry_id'] = Variable<String>(entryId);
    if (!nullToAbsent || eventSequence != null) {
      map['event_sequence'] = Variable<int>(eventSequence);
    }
    map['local_sequence'] = Variable<int>(localSequence);
    map['product_reference'] = Variable<String>(productReference);
    map['bag_type_reference'] = Variable<String>(bagTypeReference);
    map['bag_count'] = Variable<int>(bagCount);
    if (!nullToAbsent || rawWeightKg != null) {
      map['raw_weight_kg'] = Variable<String>(rawWeightKg);
    }
    map['processed_weight_kg'] = Variable<String>(processedWeightKg);
    if (!nullToAbsent || displayWeightKg != null) {
      map['display_weight_kg'] = Variable<String>(displayWeightKg);
    }
    if (!nullToAbsent || decimalPlaces != null) {
      map['decimal_places'] = Variable<int>(decimalPlaces);
    }
    if (!nullToAbsent || processingMethod != null) {
      map['processing_method'] = Variable<String>(processingMethod);
    }
    if (!nullToAbsent || weightSource != null) {
      map['weight_source'] = Variable<String>(weightSource);
    }
    if (!nullToAbsent || capturedAtDeviceUtc != null) {
      map['captured_at_device_utc'] = Variable<String>(capturedAtDeviceUtc);
    }
    if (!nullToAbsent || acceptedAtServerUtc != null) {
      map['accepted_at_server_utc'] = Variable<String>(acceptedAtServerUtc);
    }
    return map;
  }

  RemoteReceivingEntrySummariesCompanion toCompanion(bool nullToAbsent) {
    return RemoteReceivingEntrySummariesCompanion(
      sourceKey: Value(sourceKey),
      sessionId: Value(sessionId),
      entryId: Value(entryId),
      eventSequence: eventSequence == null && nullToAbsent
          ? const Value.absent()
          : Value(eventSequence),
      localSequence: Value(localSequence),
      productReference: Value(productReference),
      bagTypeReference: Value(bagTypeReference),
      bagCount: Value(bagCount),
      rawWeightKg: rawWeightKg == null && nullToAbsent
          ? const Value.absent()
          : Value(rawWeightKg),
      processedWeightKg: Value(processedWeightKg),
      displayWeightKg: displayWeightKg == null && nullToAbsent
          ? const Value.absent()
          : Value(displayWeightKg),
      decimalPlaces: decimalPlaces == null && nullToAbsent
          ? const Value.absent()
          : Value(decimalPlaces),
      processingMethod: processingMethod == null && nullToAbsent
          ? const Value.absent()
          : Value(processingMethod),
      weightSource: weightSource == null && nullToAbsent
          ? const Value.absent()
          : Value(weightSource),
      capturedAtDeviceUtc: capturedAtDeviceUtc == null && nullToAbsent
          ? const Value.absent()
          : Value(capturedAtDeviceUtc),
      acceptedAtServerUtc: acceptedAtServerUtc == null && nullToAbsent
          ? const Value.absent()
          : Value(acceptedAtServerUtc),
    );
  }

  factory RemoteReceivingEntrySummaryRow.fromJson(
    Map<String, dynamic> json, {
    ValueSerializer? serializer,
  }) {
    serializer ??= driftRuntimeOptions.defaultSerializer;
    return RemoteReceivingEntrySummaryRow(
      sourceKey: serializer.fromJson<String>(json['sourceKey']),
      sessionId: serializer.fromJson<String>(json['sessionId']),
      entryId: serializer.fromJson<String>(json['entryId']),
      eventSequence: serializer.fromJson<int?>(json['eventSequence']),
      localSequence: serializer.fromJson<int>(json['localSequence']),
      productReference: serializer.fromJson<String>(json['productReference']),
      bagTypeReference: serializer.fromJson<String>(json['bagTypeReference']),
      bagCount: serializer.fromJson<int>(json['bagCount']),
      rawWeightKg: serializer.fromJson<String?>(json['rawWeightKg']),
      processedWeightKg: serializer.fromJson<String>(json['processedWeightKg']),
      displayWeightKg: serializer.fromJson<String?>(json['displayWeightKg']),
      decimalPlaces: serializer.fromJson<int?>(json['decimalPlaces']),
      processingMethod: serializer.fromJson<String?>(json['processingMethod']),
      weightSource: serializer.fromJson<String?>(json['weightSource']),
      capturedAtDeviceUtc: serializer.fromJson<String?>(
        json['capturedAtDeviceUtc'],
      ),
      acceptedAtServerUtc: serializer.fromJson<String?>(
        json['acceptedAtServerUtc'],
      ),
    );
  }
  @override
  Map<String, dynamic> toJson({ValueSerializer? serializer}) {
    serializer ??= driftRuntimeOptions.defaultSerializer;
    return <String, dynamic>{
      'sourceKey': serializer.toJson<String>(sourceKey),
      'sessionId': serializer.toJson<String>(sessionId),
      'entryId': serializer.toJson<String>(entryId),
      'eventSequence': serializer.toJson<int?>(eventSequence),
      'localSequence': serializer.toJson<int>(localSequence),
      'productReference': serializer.toJson<String>(productReference),
      'bagTypeReference': serializer.toJson<String>(bagTypeReference),
      'bagCount': serializer.toJson<int>(bagCount),
      'rawWeightKg': serializer.toJson<String?>(rawWeightKg),
      'processedWeightKg': serializer.toJson<String>(processedWeightKg),
      'displayWeightKg': serializer.toJson<String?>(displayWeightKg),
      'decimalPlaces': serializer.toJson<int?>(decimalPlaces),
      'processingMethod': serializer.toJson<String?>(processingMethod),
      'weightSource': serializer.toJson<String?>(weightSource),
      'capturedAtDeviceUtc': serializer.toJson<String?>(capturedAtDeviceUtc),
      'acceptedAtServerUtc': serializer.toJson<String?>(acceptedAtServerUtc),
    };
  }

  RemoteReceivingEntrySummaryRow copyWith({
    String? sourceKey,
    String? sessionId,
    String? entryId,
    Value<int?> eventSequence = const Value.absent(),
    int? localSequence,
    String? productReference,
    String? bagTypeReference,
    int? bagCount,
    Value<String?> rawWeightKg = const Value.absent(),
    String? processedWeightKg,
    Value<String?> displayWeightKg = const Value.absent(),
    Value<int?> decimalPlaces = const Value.absent(),
    Value<String?> processingMethod = const Value.absent(),
    Value<String?> weightSource = const Value.absent(),
    Value<String?> capturedAtDeviceUtc = const Value.absent(),
    Value<String?> acceptedAtServerUtc = const Value.absent(),
  }) => RemoteReceivingEntrySummaryRow(
    sourceKey: sourceKey ?? this.sourceKey,
    sessionId: sessionId ?? this.sessionId,
    entryId: entryId ?? this.entryId,
    eventSequence: eventSequence.present
        ? eventSequence.value
        : this.eventSequence,
    localSequence: localSequence ?? this.localSequence,
    productReference: productReference ?? this.productReference,
    bagTypeReference: bagTypeReference ?? this.bagTypeReference,
    bagCount: bagCount ?? this.bagCount,
    rawWeightKg: rawWeightKg.present ? rawWeightKg.value : this.rawWeightKg,
    processedWeightKg: processedWeightKg ?? this.processedWeightKg,
    displayWeightKg: displayWeightKg.present
        ? displayWeightKg.value
        : this.displayWeightKg,
    decimalPlaces: decimalPlaces.present
        ? decimalPlaces.value
        : this.decimalPlaces,
    processingMethod: processingMethod.present
        ? processingMethod.value
        : this.processingMethod,
    weightSource: weightSource.present ? weightSource.value : this.weightSource,
    capturedAtDeviceUtc: capturedAtDeviceUtc.present
        ? capturedAtDeviceUtc.value
        : this.capturedAtDeviceUtc,
    acceptedAtServerUtc: acceptedAtServerUtc.present
        ? acceptedAtServerUtc.value
        : this.acceptedAtServerUtc,
  );
  RemoteReceivingEntrySummaryRow copyWithCompanion(
    RemoteReceivingEntrySummariesCompanion data,
  ) {
    return RemoteReceivingEntrySummaryRow(
      sourceKey: data.sourceKey.present ? data.sourceKey.value : this.sourceKey,
      sessionId: data.sessionId.present ? data.sessionId.value : this.sessionId,
      entryId: data.entryId.present ? data.entryId.value : this.entryId,
      eventSequence: data.eventSequence.present
          ? data.eventSequence.value
          : this.eventSequence,
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
      capturedAtDeviceUtc: data.capturedAtDeviceUtc.present
          ? data.capturedAtDeviceUtc.value
          : this.capturedAtDeviceUtc,
      acceptedAtServerUtc: data.acceptedAtServerUtc.present
          ? data.acceptedAtServerUtc.value
          : this.acceptedAtServerUtc,
    );
  }

  @override
  String toString() {
    return (StringBuffer('RemoteReceivingEntrySummaryRow(')
          ..write('sourceKey: $sourceKey, ')
          ..write('sessionId: $sessionId, ')
          ..write('entryId: $entryId, ')
          ..write('eventSequence: $eventSequence, ')
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
          ..write('capturedAtDeviceUtc: $capturedAtDeviceUtc, ')
          ..write('acceptedAtServerUtc: $acceptedAtServerUtc')
          ..write(')'))
        .toString();
  }

  @override
  int get hashCode => Object.hash(
    sourceKey,
    sessionId,
    entryId,
    eventSequence,
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
    capturedAtDeviceUtc,
    acceptedAtServerUtc,
  );
  @override
  bool operator ==(Object other) =>
      identical(this, other) ||
      (other is RemoteReceivingEntrySummaryRow &&
          other.sourceKey == this.sourceKey &&
          other.sessionId == this.sessionId &&
          other.entryId == this.entryId &&
          other.eventSequence == this.eventSequence &&
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
          other.capturedAtDeviceUtc == this.capturedAtDeviceUtc &&
          other.acceptedAtServerUtc == this.acceptedAtServerUtc);
}

class RemoteReceivingEntrySummariesCompanion
    extends UpdateCompanion<RemoteReceivingEntrySummaryRow> {
  final Value<String> sourceKey;
  final Value<String> sessionId;
  final Value<String> entryId;
  final Value<int?> eventSequence;
  final Value<int> localSequence;
  final Value<String> productReference;
  final Value<String> bagTypeReference;
  final Value<int> bagCount;
  final Value<String?> rawWeightKg;
  final Value<String> processedWeightKg;
  final Value<String?> displayWeightKg;
  final Value<int?> decimalPlaces;
  final Value<String?> processingMethod;
  final Value<String?> weightSource;
  final Value<String?> capturedAtDeviceUtc;
  final Value<String?> acceptedAtServerUtc;
  final Value<int> rowid;
  const RemoteReceivingEntrySummariesCompanion({
    this.sourceKey = const Value.absent(),
    this.sessionId = const Value.absent(),
    this.entryId = const Value.absent(),
    this.eventSequence = const Value.absent(),
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
    this.capturedAtDeviceUtc = const Value.absent(),
    this.acceptedAtServerUtc = const Value.absent(),
    this.rowid = const Value.absent(),
  });
  RemoteReceivingEntrySummariesCompanion.insert({
    required String sourceKey,
    required String sessionId,
    required String entryId,
    this.eventSequence = const Value.absent(),
    required int localSequence,
    required String productReference,
    required String bagTypeReference,
    required int bagCount,
    this.rawWeightKg = const Value.absent(),
    required String processedWeightKg,
    this.displayWeightKg = const Value.absent(),
    this.decimalPlaces = const Value.absent(),
    this.processingMethod = const Value.absent(),
    this.weightSource = const Value.absent(),
    this.capturedAtDeviceUtc = const Value.absent(),
    this.acceptedAtServerUtc = const Value.absent(),
    this.rowid = const Value.absent(),
  }) : sourceKey = Value(sourceKey),
       sessionId = Value(sessionId),
       entryId = Value(entryId),
       localSequence = Value(localSequence),
       productReference = Value(productReference),
       bagTypeReference = Value(bagTypeReference),
       bagCount = Value(bagCount),
       processedWeightKg = Value(processedWeightKg);
  static Insertable<RemoteReceivingEntrySummaryRow> custom({
    Expression<String>? sourceKey,
    Expression<String>? sessionId,
    Expression<String>? entryId,
    Expression<int>? eventSequence,
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
    Expression<String>? capturedAtDeviceUtc,
    Expression<String>? acceptedAtServerUtc,
    Expression<int>? rowid,
  }) {
    return RawValuesInsertable({
      if (sourceKey != null) 'source_key': sourceKey,
      if (sessionId != null) 'session_id': sessionId,
      if (entryId != null) 'entry_id': entryId,
      if (eventSequence != null) 'event_sequence': eventSequence,
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
      if (capturedAtDeviceUtc != null)
        'captured_at_device_utc': capturedAtDeviceUtc,
      if (acceptedAtServerUtc != null)
        'accepted_at_server_utc': acceptedAtServerUtc,
      if (rowid != null) 'rowid': rowid,
    });
  }

  RemoteReceivingEntrySummariesCompanion copyWith({
    Value<String>? sourceKey,
    Value<String>? sessionId,
    Value<String>? entryId,
    Value<int?>? eventSequence,
    Value<int>? localSequence,
    Value<String>? productReference,
    Value<String>? bagTypeReference,
    Value<int>? bagCount,
    Value<String?>? rawWeightKg,
    Value<String>? processedWeightKg,
    Value<String?>? displayWeightKg,
    Value<int?>? decimalPlaces,
    Value<String?>? processingMethod,
    Value<String?>? weightSource,
    Value<String?>? capturedAtDeviceUtc,
    Value<String?>? acceptedAtServerUtc,
    Value<int>? rowid,
  }) {
    return RemoteReceivingEntrySummariesCompanion(
      sourceKey: sourceKey ?? this.sourceKey,
      sessionId: sessionId ?? this.sessionId,
      entryId: entryId ?? this.entryId,
      eventSequence: eventSequence ?? this.eventSequence,
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
      capturedAtDeviceUtc: capturedAtDeviceUtc ?? this.capturedAtDeviceUtc,
      acceptedAtServerUtc: acceptedAtServerUtc ?? this.acceptedAtServerUtc,
      rowid: rowid ?? this.rowid,
    );
  }

  @override
  Map<String, Expression> toColumns(bool nullToAbsent) {
    final map = <String, Expression>{};
    if (sourceKey.present) {
      map['source_key'] = Variable<String>(sourceKey.value);
    }
    if (sessionId.present) {
      map['session_id'] = Variable<String>(sessionId.value);
    }
    if (entryId.present) {
      map['entry_id'] = Variable<String>(entryId.value);
    }
    if (eventSequence.present) {
      map['event_sequence'] = Variable<int>(eventSequence.value);
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
    if (capturedAtDeviceUtc.present) {
      map['captured_at_device_utc'] = Variable<String>(
        capturedAtDeviceUtc.value,
      );
    }
    if (acceptedAtServerUtc.present) {
      map['accepted_at_server_utc'] = Variable<String>(
        acceptedAtServerUtc.value,
      );
    }
    if (rowid.present) {
      map['rowid'] = Variable<int>(rowid.value);
    }
    return map;
  }

  @override
  String toString() {
    return (StringBuffer('RemoteReceivingEntrySummariesCompanion(')
          ..write('sourceKey: $sourceKey, ')
          ..write('sessionId: $sessionId, ')
          ..write('entryId: $entryId, ')
          ..write('eventSequence: $eventSequence, ')
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
          ..write('capturedAtDeviceUtc: $capturedAtDeviceUtc, ')
          ..write('acceptedAtServerUtc: $acceptedAtServerUtc, ')
          ..write('rowid: $rowid')
          ..write(')'))
        .toString();
  }
}

class $PocControlCommandsTable extends PocControlCommands
    with TableInfo<$PocControlCommandsTable, PocControlCommandRow> {
  @override
  final GeneratedDatabase attachedDatabase;
  final String? _alias;
  $PocControlCommandsTable(this.attachedDatabase, [this._alias]);
  static const VerificationMeta _commandIdMeta = const VerificationMeta(
    'commandId',
  );
  @override
  late final GeneratedColumn<String> commandId = GeneratedColumn<String>(
    'command_id',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
  );
  static const VerificationMeta _sourceKeyMeta = const VerificationMeta(
    'sourceKey',
  );
  @override
  late final GeneratedColumn<String> sourceKey = GeneratedColumn<String>(
    'source_key',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
    defaultConstraints: GeneratedColumn.constraintIsAlways(
      'REFERENCES poc_sync_sources (source_key) ON DELETE RESTRICT',
    ),
  );
  static const VerificationMeta _actingDeviceIdMeta = const VerificationMeta(
    'actingDeviceId',
  );
  @override
  late final GeneratedColumn<String> actingDeviceId = GeneratedColumn<String>(
    'acting_device_id',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
  );
  static const VerificationMeta _commandTypeMeta = const VerificationMeta(
    'commandType',
  );
  @override
  late final GeneratedColumn<String> commandType = GeneratedColumn<String>(
    'command_type',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
    $customConstraints:
        'NOT NULL CHECK (command_type IN (\'Heartbeat\', \'Approve\', \'Finalize\'))',
  );
  static const VerificationMeta _sessionIdMeta = const VerificationMeta(
    'sessionId',
  );
  @override
  late final GeneratedColumn<String> sessionId = GeneratedColumn<String>(
    'session_id',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
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
  static const VerificationMeta _leaseIdMeta = const VerificationMeta(
    'leaseId',
  );
  @override
  late final GeneratedColumn<String> leaseId = GeneratedColumn<String>(
    'lease_id',
    aliasedName,
    true,
    type: DriftSqlType.string,
    requiredDuringInsert: false,
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
        'NOT NULL CHECK (status IN (\'Pending\', \'Sending\', \'Completed\', \'NeedsAttention\'))',
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
  static const VerificationMeta _nextAttemptAtUtcMeta = const VerificationMeta(
    'nextAttemptAtUtc',
  );
  @override
  late final GeneratedColumn<String> nextAttemptAtUtc = GeneratedColumn<String>(
    'next_attempt_at_utc',
    aliasedName,
    true,
    type: DriftSqlType.string,
    requiredDuringInsert: false,
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
  static const VerificationMeta _lastErrorMessageMeta = const VerificationMeta(
    'lastErrorMessage',
  );
  @override
  late final GeneratedColumn<String> lastErrorMessage = GeneratedColumn<String>(
    'last_error_message',
    aliasedName,
    true,
    type: DriftSqlType.string,
    requiredDuringInsert: false,
  );
  static const VerificationMeta _successfulResponseJsonMeta =
      const VerificationMeta('successfulResponseJson');
  @override
  late final GeneratedColumn<String> successfulResponseJson =
      GeneratedColumn<String>(
        'successful_response_json',
        aliasedName,
        true,
        type: DriftSqlType.string,
        requiredDuringInsert: false,
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
    commandId,
    sourceKey,
    actingDeviceId,
    commandType,
    sessionId,
    expectedCloudVersion,
    leaseId,
    status,
    attemptCount,
    nextAttemptAtUtc,
    lastAttemptAtUtc,
    lastErrorCode,
    lastErrorMessage,
    successfulResponseJson,
    createdAtUtc,
    updatedAtUtc,
  ];
  @override
  String get aliasedName => _alias ?? actualTableName;
  @override
  String get actualTableName => $name;
  static const String $name = 'poc_control_commands';
  @override
  VerificationContext validateIntegrity(
    Insertable<PocControlCommandRow> instance, {
    bool isInserting = false,
  }) {
    final context = VerificationContext();
    final data = instance.toColumns(true);
    if (data.containsKey('command_id')) {
      context.handle(
        _commandIdMeta,
        commandId.isAcceptableOrUnknown(data['command_id']!, _commandIdMeta),
      );
    } else if (isInserting) {
      context.missing(_commandIdMeta);
    }
    if (data.containsKey('source_key')) {
      context.handle(
        _sourceKeyMeta,
        sourceKey.isAcceptableOrUnknown(data['source_key']!, _sourceKeyMeta),
      );
    } else if (isInserting) {
      context.missing(_sourceKeyMeta);
    }
    if (data.containsKey('acting_device_id')) {
      context.handle(
        _actingDeviceIdMeta,
        actingDeviceId.isAcceptableOrUnknown(
          data['acting_device_id']!,
          _actingDeviceIdMeta,
        ),
      );
    } else if (isInserting) {
      context.missing(_actingDeviceIdMeta);
    }
    if (data.containsKey('command_type')) {
      context.handle(
        _commandTypeMeta,
        commandType.isAcceptableOrUnknown(
          data['command_type']!,
          _commandTypeMeta,
        ),
      );
    } else if (isInserting) {
      context.missing(_commandTypeMeta);
    }
    if (data.containsKey('session_id')) {
      context.handle(
        _sessionIdMeta,
        sessionId.isAcceptableOrUnknown(data['session_id']!, _sessionIdMeta),
      );
    } else if (isInserting) {
      context.missing(_sessionIdMeta);
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
    if (data.containsKey('lease_id')) {
      context.handle(
        _leaseIdMeta,
        leaseId.isAcceptableOrUnknown(data['lease_id']!, _leaseIdMeta),
      );
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
    if (data.containsKey('next_attempt_at_utc')) {
      context.handle(
        _nextAttemptAtUtcMeta,
        nextAttemptAtUtc.isAcceptableOrUnknown(
          data['next_attempt_at_utc']!,
          _nextAttemptAtUtcMeta,
        ),
      );
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
    if (data.containsKey('last_error_code')) {
      context.handle(
        _lastErrorCodeMeta,
        lastErrorCode.isAcceptableOrUnknown(
          data['last_error_code']!,
          _lastErrorCodeMeta,
        ),
      );
    }
    if (data.containsKey('last_error_message')) {
      context.handle(
        _lastErrorMessageMeta,
        lastErrorMessage.isAcceptableOrUnknown(
          data['last_error_message']!,
          _lastErrorMessageMeta,
        ),
      );
    }
    if (data.containsKey('successful_response_json')) {
      context.handle(
        _successfulResponseJsonMeta,
        successfulResponseJson.isAcceptableOrUnknown(
          data['successful_response_json']!,
          _successfulResponseJsonMeta,
        ),
      );
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
  Set<GeneratedColumn> get $primaryKey => {commandId};
  @override
  PocControlCommandRow map(Map<String, dynamic> data, {String? tablePrefix}) {
    final effectivePrefix = tablePrefix != null ? '$tablePrefix.' : '';
    return PocControlCommandRow(
      commandId: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}command_id'],
      )!,
      sourceKey: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}source_key'],
      )!,
      actingDeviceId: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}acting_device_id'],
      )!,
      commandType: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}command_type'],
      )!,
      sessionId: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}session_id'],
      )!,
      expectedCloudVersion: attachedDatabase.typeMapping.read(
        DriftSqlType.int,
        data['${effectivePrefix}expected_cloud_version'],
      ),
      leaseId: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}lease_id'],
      ),
      status: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}status'],
      )!,
      attemptCount: attachedDatabase.typeMapping.read(
        DriftSqlType.int,
        data['${effectivePrefix}attempt_count'],
      )!,
      nextAttemptAtUtc: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}next_attempt_at_utc'],
      ),
      lastAttemptAtUtc: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}last_attempt_at_utc'],
      ),
      lastErrorCode: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}last_error_code'],
      ),
      lastErrorMessage: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}last_error_message'],
      ),
      successfulResponseJson: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}successful_response_json'],
      ),
      createdAtUtc: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}created_at_utc'],
      )!,
      updatedAtUtc: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}updated_at_utc'],
      )!,
    );
  }

  @override
  $PocControlCommandsTable createAlias(String alias) {
    return $PocControlCommandsTable(attachedDatabase, alias);
  }
}

class PocControlCommandRow extends DataClass
    implements Insertable<PocControlCommandRow> {
  final String commandId;
  final String sourceKey;
  final String actingDeviceId;
  final String commandType;
  final String sessionId;
  final int? expectedCloudVersion;
  final String? leaseId;
  final String status;
  final int attemptCount;
  final String? nextAttemptAtUtc;
  final String? lastAttemptAtUtc;
  final String? lastErrorCode;
  final String? lastErrorMessage;
  final String? successfulResponseJson;
  final String createdAtUtc;
  final String updatedAtUtc;
  const PocControlCommandRow({
    required this.commandId,
    required this.sourceKey,
    required this.actingDeviceId,
    required this.commandType,
    required this.sessionId,
    this.expectedCloudVersion,
    this.leaseId,
    required this.status,
    required this.attemptCount,
    this.nextAttemptAtUtc,
    this.lastAttemptAtUtc,
    this.lastErrorCode,
    this.lastErrorMessage,
    this.successfulResponseJson,
    required this.createdAtUtc,
    required this.updatedAtUtc,
  });
  @override
  Map<String, Expression> toColumns(bool nullToAbsent) {
    final map = <String, Expression>{};
    map['command_id'] = Variable<String>(commandId);
    map['source_key'] = Variable<String>(sourceKey);
    map['acting_device_id'] = Variable<String>(actingDeviceId);
    map['command_type'] = Variable<String>(commandType);
    map['session_id'] = Variable<String>(sessionId);
    if (!nullToAbsent || expectedCloudVersion != null) {
      map['expected_cloud_version'] = Variable<int>(expectedCloudVersion);
    }
    if (!nullToAbsent || leaseId != null) {
      map['lease_id'] = Variable<String>(leaseId);
    }
    map['status'] = Variable<String>(status);
    map['attempt_count'] = Variable<int>(attemptCount);
    if (!nullToAbsent || nextAttemptAtUtc != null) {
      map['next_attempt_at_utc'] = Variable<String>(nextAttemptAtUtc);
    }
    if (!nullToAbsent || lastAttemptAtUtc != null) {
      map['last_attempt_at_utc'] = Variable<String>(lastAttemptAtUtc);
    }
    if (!nullToAbsent || lastErrorCode != null) {
      map['last_error_code'] = Variable<String>(lastErrorCode);
    }
    if (!nullToAbsent || lastErrorMessage != null) {
      map['last_error_message'] = Variable<String>(lastErrorMessage);
    }
    if (!nullToAbsent || successfulResponseJson != null) {
      map['successful_response_json'] = Variable<String>(
        successfulResponseJson,
      );
    }
    map['created_at_utc'] = Variable<String>(createdAtUtc);
    map['updated_at_utc'] = Variable<String>(updatedAtUtc);
    return map;
  }

  PocControlCommandsCompanion toCompanion(bool nullToAbsent) {
    return PocControlCommandsCompanion(
      commandId: Value(commandId),
      sourceKey: Value(sourceKey),
      actingDeviceId: Value(actingDeviceId),
      commandType: Value(commandType),
      sessionId: Value(sessionId),
      expectedCloudVersion: expectedCloudVersion == null && nullToAbsent
          ? const Value.absent()
          : Value(expectedCloudVersion),
      leaseId: leaseId == null && nullToAbsent
          ? const Value.absent()
          : Value(leaseId),
      status: Value(status),
      attemptCount: Value(attemptCount),
      nextAttemptAtUtc: nextAttemptAtUtc == null && nullToAbsent
          ? const Value.absent()
          : Value(nextAttemptAtUtc),
      lastAttemptAtUtc: lastAttemptAtUtc == null && nullToAbsent
          ? const Value.absent()
          : Value(lastAttemptAtUtc),
      lastErrorCode: lastErrorCode == null && nullToAbsent
          ? const Value.absent()
          : Value(lastErrorCode),
      lastErrorMessage: lastErrorMessage == null && nullToAbsent
          ? const Value.absent()
          : Value(lastErrorMessage),
      successfulResponseJson: successfulResponseJson == null && nullToAbsent
          ? const Value.absent()
          : Value(successfulResponseJson),
      createdAtUtc: Value(createdAtUtc),
      updatedAtUtc: Value(updatedAtUtc),
    );
  }

  factory PocControlCommandRow.fromJson(
    Map<String, dynamic> json, {
    ValueSerializer? serializer,
  }) {
    serializer ??= driftRuntimeOptions.defaultSerializer;
    return PocControlCommandRow(
      commandId: serializer.fromJson<String>(json['commandId']),
      sourceKey: serializer.fromJson<String>(json['sourceKey']),
      actingDeviceId: serializer.fromJson<String>(json['actingDeviceId']),
      commandType: serializer.fromJson<String>(json['commandType']),
      sessionId: serializer.fromJson<String>(json['sessionId']),
      expectedCloudVersion: serializer.fromJson<int?>(
        json['expectedCloudVersion'],
      ),
      leaseId: serializer.fromJson<String?>(json['leaseId']),
      status: serializer.fromJson<String>(json['status']),
      attemptCount: serializer.fromJson<int>(json['attemptCount']),
      nextAttemptAtUtc: serializer.fromJson<String?>(json['nextAttemptAtUtc']),
      lastAttemptAtUtc: serializer.fromJson<String?>(json['lastAttemptAtUtc']),
      lastErrorCode: serializer.fromJson<String?>(json['lastErrorCode']),
      lastErrorMessage: serializer.fromJson<String?>(json['lastErrorMessage']),
      successfulResponseJson: serializer.fromJson<String?>(
        json['successfulResponseJson'],
      ),
      createdAtUtc: serializer.fromJson<String>(json['createdAtUtc']),
      updatedAtUtc: serializer.fromJson<String>(json['updatedAtUtc']),
    );
  }
  @override
  Map<String, dynamic> toJson({ValueSerializer? serializer}) {
    serializer ??= driftRuntimeOptions.defaultSerializer;
    return <String, dynamic>{
      'commandId': serializer.toJson<String>(commandId),
      'sourceKey': serializer.toJson<String>(sourceKey),
      'actingDeviceId': serializer.toJson<String>(actingDeviceId),
      'commandType': serializer.toJson<String>(commandType),
      'sessionId': serializer.toJson<String>(sessionId),
      'expectedCloudVersion': serializer.toJson<int?>(expectedCloudVersion),
      'leaseId': serializer.toJson<String?>(leaseId),
      'status': serializer.toJson<String>(status),
      'attemptCount': serializer.toJson<int>(attemptCount),
      'nextAttemptAtUtc': serializer.toJson<String?>(nextAttemptAtUtc),
      'lastAttemptAtUtc': serializer.toJson<String?>(lastAttemptAtUtc),
      'lastErrorCode': serializer.toJson<String?>(lastErrorCode),
      'lastErrorMessage': serializer.toJson<String?>(lastErrorMessage),
      'successfulResponseJson': serializer.toJson<String?>(
        successfulResponseJson,
      ),
      'createdAtUtc': serializer.toJson<String>(createdAtUtc),
      'updatedAtUtc': serializer.toJson<String>(updatedAtUtc),
    };
  }

  PocControlCommandRow copyWith({
    String? commandId,
    String? sourceKey,
    String? actingDeviceId,
    String? commandType,
    String? sessionId,
    Value<int?> expectedCloudVersion = const Value.absent(),
    Value<String?> leaseId = const Value.absent(),
    String? status,
    int? attemptCount,
    Value<String?> nextAttemptAtUtc = const Value.absent(),
    Value<String?> lastAttemptAtUtc = const Value.absent(),
    Value<String?> lastErrorCode = const Value.absent(),
    Value<String?> lastErrorMessage = const Value.absent(),
    Value<String?> successfulResponseJson = const Value.absent(),
    String? createdAtUtc,
    String? updatedAtUtc,
  }) => PocControlCommandRow(
    commandId: commandId ?? this.commandId,
    sourceKey: sourceKey ?? this.sourceKey,
    actingDeviceId: actingDeviceId ?? this.actingDeviceId,
    commandType: commandType ?? this.commandType,
    sessionId: sessionId ?? this.sessionId,
    expectedCloudVersion: expectedCloudVersion.present
        ? expectedCloudVersion.value
        : this.expectedCloudVersion,
    leaseId: leaseId.present ? leaseId.value : this.leaseId,
    status: status ?? this.status,
    attemptCount: attemptCount ?? this.attemptCount,
    nextAttemptAtUtc: nextAttemptAtUtc.present
        ? nextAttemptAtUtc.value
        : this.nextAttemptAtUtc,
    lastAttemptAtUtc: lastAttemptAtUtc.present
        ? lastAttemptAtUtc.value
        : this.lastAttemptAtUtc,
    lastErrorCode: lastErrorCode.present
        ? lastErrorCode.value
        : this.lastErrorCode,
    lastErrorMessage: lastErrorMessage.present
        ? lastErrorMessage.value
        : this.lastErrorMessage,
    successfulResponseJson: successfulResponseJson.present
        ? successfulResponseJson.value
        : this.successfulResponseJson,
    createdAtUtc: createdAtUtc ?? this.createdAtUtc,
    updatedAtUtc: updatedAtUtc ?? this.updatedAtUtc,
  );
  PocControlCommandRow copyWithCompanion(PocControlCommandsCompanion data) {
    return PocControlCommandRow(
      commandId: data.commandId.present ? data.commandId.value : this.commandId,
      sourceKey: data.sourceKey.present ? data.sourceKey.value : this.sourceKey,
      actingDeviceId: data.actingDeviceId.present
          ? data.actingDeviceId.value
          : this.actingDeviceId,
      commandType: data.commandType.present
          ? data.commandType.value
          : this.commandType,
      sessionId: data.sessionId.present ? data.sessionId.value : this.sessionId,
      expectedCloudVersion: data.expectedCloudVersion.present
          ? data.expectedCloudVersion.value
          : this.expectedCloudVersion,
      leaseId: data.leaseId.present ? data.leaseId.value : this.leaseId,
      status: data.status.present ? data.status.value : this.status,
      attemptCount: data.attemptCount.present
          ? data.attemptCount.value
          : this.attemptCount,
      nextAttemptAtUtc: data.nextAttemptAtUtc.present
          ? data.nextAttemptAtUtc.value
          : this.nextAttemptAtUtc,
      lastAttemptAtUtc: data.lastAttemptAtUtc.present
          ? data.lastAttemptAtUtc.value
          : this.lastAttemptAtUtc,
      lastErrorCode: data.lastErrorCode.present
          ? data.lastErrorCode.value
          : this.lastErrorCode,
      lastErrorMessage: data.lastErrorMessage.present
          ? data.lastErrorMessage.value
          : this.lastErrorMessage,
      successfulResponseJson: data.successfulResponseJson.present
          ? data.successfulResponseJson.value
          : this.successfulResponseJson,
      createdAtUtc: data.createdAtUtc.present
          ? data.createdAtUtc.value
          : this.createdAtUtc,
      updatedAtUtc: data.updatedAtUtc.present
          ? data.updatedAtUtc.value
          : this.updatedAtUtc,
    );
  }

  @override
  String toString() {
    return (StringBuffer('PocControlCommandRow(')
          ..write('commandId: $commandId, ')
          ..write('sourceKey: $sourceKey, ')
          ..write('actingDeviceId: $actingDeviceId, ')
          ..write('commandType: $commandType, ')
          ..write('sessionId: $sessionId, ')
          ..write('expectedCloudVersion: $expectedCloudVersion, ')
          ..write('leaseId: $leaseId, ')
          ..write('status: $status, ')
          ..write('attemptCount: $attemptCount, ')
          ..write('nextAttemptAtUtc: $nextAttemptAtUtc, ')
          ..write('lastAttemptAtUtc: $lastAttemptAtUtc, ')
          ..write('lastErrorCode: $lastErrorCode, ')
          ..write('lastErrorMessage: $lastErrorMessage, ')
          ..write('successfulResponseJson: $successfulResponseJson, ')
          ..write('createdAtUtc: $createdAtUtc, ')
          ..write('updatedAtUtc: $updatedAtUtc')
          ..write(')'))
        .toString();
  }

  @override
  int get hashCode => Object.hash(
    commandId,
    sourceKey,
    actingDeviceId,
    commandType,
    sessionId,
    expectedCloudVersion,
    leaseId,
    status,
    attemptCount,
    nextAttemptAtUtc,
    lastAttemptAtUtc,
    lastErrorCode,
    lastErrorMessage,
    successfulResponseJson,
    createdAtUtc,
    updatedAtUtc,
  );
  @override
  bool operator ==(Object other) =>
      identical(this, other) ||
      (other is PocControlCommandRow &&
          other.commandId == this.commandId &&
          other.sourceKey == this.sourceKey &&
          other.actingDeviceId == this.actingDeviceId &&
          other.commandType == this.commandType &&
          other.sessionId == this.sessionId &&
          other.expectedCloudVersion == this.expectedCloudVersion &&
          other.leaseId == this.leaseId &&
          other.status == this.status &&
          other.attemptCount == this.attemptCount &&
          other.nextAttemptAtUtc == this.nextAttemptAtUtc &&
          other.lastAttemptAtUtc == this.lastAttemptAtUtc &&
          other.lastErrorCode == this.lastErrorCode &&
          other.lastErrorMessage == this.lastErrorMessage &&
          other.successfulResponseJson == this.successfulResponseJson &&
          other.createdAtUtc == this.createdAtUtc &&
          other.updatedAtUtc == this.updatedAtUtc);
}

class PocControlCommandsCompanion
    extends UpdateCompanion<PocControlCommandRow> {
  final Value<String> commandId;
  final Value<String> sourceKey;
  final Value<String> actingDeviceId;
  final Value<String> commandType;
  final Value<String> sessionId;
  final Value<int?> expectedCloudVersion;
  final Value<String?> leaseId;
  final Value<String> status;
  final Value<int> attemptCount;
  final Value<String?> nextAttemptAtUtc;
  final Value<String?> lastAttemptAtUtc;
  final Value<String?> lastErrorCode;
  final Value<String?> lastErrorMessage;
  final Value<String?> successfulResponseJson;
  final Value<String> createdAtUtc;
  final Value<String> updatedAtUtc;
  final Value<int> rowid;
  const PocControlCommandsCompanion({
    this.commandId = const Value.absent(),
    this.sourceKey = const Value.absent(),
    this.actingDeviceId = const Value.absent(),
    this.commandType = const Value.absent(),
    this.sessionId = const Value.absent(),
    this.expectedCloudVersion = const Value.absent(),
    this.leaseId = const Value.absent(),
    this.status = const Value.absent(),
    this.attemptCount = const Value.absent(),
    this.nextAttemptAtUtc = const Value.absent(),
    this.lastAttemptAtUtc = const Value.absent(),
    this.lastErrorCode = const Value.absent(),
    this.lastErrorMessage = const Value.absent(),
    this.successfulResponseJson = const Value.absent(),
    this.createdAtUtc = const Value.absent(),
    this.updatedAtUtc = const Value.absent(),
    this.rowid = const Value.absent(),
  });
  PocControlCommandsCompanion.insert({
    required String commandId,
    required String sourceKey,
    required String actingDeviceId,
    required String commandType,
    required String sessionId,
    this.expectedCloudVersion = const Value.absent(),
    this.leaseId = const Value.absent(),
    required String status,
    required int attemptCount,
    this.nextAttemptAtUtc = const Value.absent(),
    this.lastAttemptAtUtc = const Value.absent(),
    this.lastErrorCode = const Value.absent(),
    this.lastErrorMessage = const Value.absent(),
    this.successfulResponseJson = const Value.absent(),
    required String createdAtUtc,
    required String updatedAtUtc,
    this.rowid = const Value.absent(),
  }) : commandId = Value(commandId),
       sourceKey = Value(sourceKey),
       actingDeviceId = Value(actingDeviceId),
       commandType = Value(commandType),
       sessionId = Value(sessionId),
       status = Value(status),
       attemptCount = Value(attemptCount),
       createdAtUtc = Value(createdAtUtc),
       updatedAtUtc = Value(updatedAtUtc);
  static Insertable<PocControlCommandRow> custom({
    Expression<String>? commandId,
    Expression<String>? sourceKey,
    Expression<String>? actingDeviceId,
    Expression<String>? commandType,
    Expression<String>? sessionId,
    Expression<int>? expectedCloudVersion,
    Expression<String>? leaseId,
    Expression<String>? status,
    Expression<int>? attemptCount,
    Expression<String>? nextAttemptAtUtc,
    Expression<String>? lastAttemptAtUtc,
    Expression<String>? lastErrorCode,
    Expression<String>? lastErrorMessage,
    Expression<String>? successfulResponseJson,
    Expression<String>? createdAtUtc,
    Expression<String>? updatedAtUtc,
    Expression<int>? rowid,
  }) {
    return RawValuesInsertable({
      if (commandId != null) 'command_id': commandId,
      if (sourceKey != null) 'source_key': sourceKey,
      if (actingDeviceId != null) 'acting_device_id': actingDeviceId,
      if (commandType != null) 'command_type': commandType,
      if (sessionId != null) 'session_id': sessionId,
      if (expectedCloudVersion != null)
        'expected_cloud_version': expectedCloudVersion,
      if (leaseId != null) 'lease_id': leaseId,
      if (status != null) 'status': status,
      if (attemptCount != null) 'attempt_count': attemptCount,
      if (nextAttemptAtUtc != null) 'next_attempt_at_utc': nextAttemptAtUtc,
      if (lastAttemptAtUtc != null) 'last_attempt_at_utc': lastAttemptAtUtc,
      if (lastErrorCode != null) 'last_error_code': lastErrorCode,
      if (lastErrorMessage != null) 'last_error_message': lastErrorMessage,
      if (successfulResponseJson != null)
        'successful_response_json': successfulResponseJson,
      if (createdAtUtc != null) 'created_at_utc': createdAtUtc,
      if (updatedAtUtc != null) 'updated_at_utc': updatedAtUtc,
      if (rowid != null) 'rowid': rowid,
    });
  }

  PocControlCommandsCompanion copyWith({
    Value<String>? commandId,
    Value<String>? sourceKey,
    Value<String>? actingDeviceId,
    Value<String>? commandType,
    Value<String>? sessionId,
    Value<int?>? expectedCloudVersion,
    Value<String?>? leaseId,
    Value<String>? status,
    Value<int>? attemptCount,
    Value<String?>? nextAttemptAtUtc,
    Value<String?>? lastAttemptAtUtc,
    Value<String?>? lastErrorCode,
    Value<String?>? lastErrorMessage,
    Value<String?>? successfulResponseJson,
    Value<String>? createdAtUtc,
    Value<String>? updatedAtUtc,
    Value<int>? rowid,
  }) {
    return PocControlCommandsCompanion(
      commandId: commandId ?? this.commandId,
      sourceKey: sourceKey ?? this.sourceKey,
      actingDeviceId: actingDeviceId ?? this.actingDeviceId,
      commandType: commandType ?? this.commandType,
      sessionId: sessionId ?? this.sessionId,
      expectedCloudVersion: expectedCloudVersion ?? this.expectedCloudVersion,
      leaseId: leaseId ?? this.leaseId,
      status: status ?? this.status,
      attemptCount: attemptCount ?? this.attemptCount,
      nextAttemptAtUtc: nextAttemptAtUtc ?? this.nextAttemptAtUtc,
      lastAttemptAtUtc: lastAttemptAtUtc ?? this.lastAttemptAtUtc,
      lastErrorCode: lastErrorCode ?? this.lastErrorCode,
      lastErrorMessage: lastErrorMessage ?? this.lastErrorMessage,
      successfulResponseJson:
          successfulResponseJson ?? this.successfulResponseJson,
      createdAtUtc: createdAtUtc ?? this.createdAtUtc,
      updatedAtUtc: updatedAtUtc ?? this.updatedAtUtc,
      rowid: rowid ?? this.rowid,
    );
  }

  @override
  Map<String, Expression> toColumns(bool nullToAbsent) {
    final map = <String, Expression>{};
    if (commandId.present) {
      map['command_id'] = Variable<String>(commandId.value);
    }
    if (sourceKey.present) {
      map['source_key'] = Variable<String>(sourceKey.value);
    }
    if (actingDeviceId.present) {
      map['acting_device_id'] = Variable<String>(actingDeviceId.value);
    }
    if (commandType.present) {
      map['command_type'] = Variable<String>(commandType.value);
    }
    if (sessionId.present) {
      map['session_id'] = Variable<String>(sessionId.value);
    }
    if (expectedCloudVersion.present) {
      map['expected_cloud_version'] = Variable<int>(expectedCloudVersion.value);
    }
    if (leaseId.present) {
      map['lease_id'] = Variable<String>(leaseId.value);
    }
    if (status.present) {
      map['status'] = Variable<String>(status.value);
    }
    if (attemptCount.present) {
      map['attempt_count'] = Variable<int>(attemptCount.value);
    }
    if (nextAttemptAtUtc.present) {
      map['next_attempt_at_utc'] = Variable<String>(nextAttemptAtUtc.value);
    }
    if (lastAttemptAtUtc.present) {
      map['last_attempt_at_utc'] = Variable<String>(lastAttemptAtUtc.value);
    }
    if (lastErrorCode.present) {
      map['last_error_code'] = Variable<String>(lastErrorCode.value);
    }
    if (lastErrorMessage.present) {
      map['last_error_message'] = Variable<String>(lastErrorMessage.value);
    }
    if (successfulResponseJson.present) {
      map['successful_response_json'] = Variable<String>(
        successfulResponseJson.value,
      );
    }
    if (createdAtUtc.present) {
      map['created_at_utc'] = Variable<String>(createdAtUtc.value);
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
    return (StringBuffer('PocControlCommandsCompanion(')
          ..write('commandId: $commandId, ')
          ..write('sourceKey: $sourceKey, ')
          ..write('actingDeviceId: $actingDeviceId, ')
          ..write('commandType: $commandType, ')
          ..write('sessionId: $sessionId, ')
          ..write('expectedCloudVersion: $expectedCloudVersion, ')
          ..write('leaseId: $leaseId, ')
          ..write('status: $status, ')
          ..write('attemptCount: $attemptCount, ')
          ..write('nextAttemptAtUtc: $nextAttemptAtUtc, ')
          ..write('lastAttemptAtUtc: $lastAttemptAtUtc, ')
          ..write('lastErrorCode: $lastErrorCode, ')
          ..write('lastErrorMessage: $lastErrorMessage, ')
          ..write('successfulResponseJson: $successfulResponseJson, ')
          ..write('createdAtUtc: $createdAtUtc, ')
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
  late final $PocDeviceProfilesTable pocDeviceProfiles =
      $PocDeviceProfilesTable(this);
  late final $PocSyncSourcesTable pocSyncSources = $PocSyncSourcesTable(this);
  late final $ReceivingSessionCloudStatesTable receivingSessionCloudStates =
      $ReceivingSessionCloudStatesTable(this);
  late final $MobileSyncEventInboxTable mobileSyncEventInbox =
      $MobileSyncEventInboxTable(this);
  late final $RemoteReceivingSessionProjectionsTable
  remoteReceivingSessionProjections = $RemoteReceivingSessionProjectionsTable(
    this,
  );
  late final $RemoteReceivingEntrySummariesTable remoteReceivingEntrySummaries =
      $RemoteReceivingEntrySummariesTable(this);
  late final $PocControlCommandsTable pocControlCommands =
      $PocControlCommandsTable(this);
  @override
  Iterable<TableInfo<Table, Object?>> get allTables =>
      allSchemaEntities.whereType<TableInfo<Table, Object?>>();
  @override
  List<DatabaseSchemaEntity> get allSchemaEntities => [
    localReceivingSessions,
    localReceivingEntries,
    localOutboxOperations,
    localSyncStates,
    pocDeviceProfiles,
    pocSyncSources,
    receivingSessionCloudStates,
    mobileSyncEventInbox,
    remoteReceivingSessionProjections,
    remoteReceivingEntrySummaries,
    pocControlCommands,
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

  static MultiTypedResultKey<
    $ReceivingSessionCloudStatesTable,
    List<ReceivingSessionCloudStateRow>
  >
  _receivingSessionCloudStatesRefsTable(
    _$TraderProLocalDatabase db,
  ) => MultiTypedResultKey.fromTable(
    db.receivingSessionCloudStates,
    aliasName:
        'local_receiving_sessions__id__receiving_session_cloud_states__local_session_id',
  );

  $$ReceivingSessionCloudStatesTableProcessedTableManager
  get receivingSessionCloudStatesRefs {
    final manager = $$ReceivingSessionCloudStatesTableTableManager(
      $_db,
      $_db.receivingSessionCloudStates,
    ).filter((f) => f.localSessionId.id.sqlEquals($_itemColumn<String>('id')!));

    final cache = $_typedResult.readTableOrNull(
      _receivingSessionCloudStatesRefsTable($_db),
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

  Expression<bool> receivingSessionCloudStatesRefs(
    Expression<bool> Function(
      $$ReceivingSessionCloudStatesTableFilterComposer f,
    )
    f,
  ) {
    final $$ReceivingSessionCloudStatesTableFilterComposer composer =
        $composerBuilder(
          composer: this,
          getCurrentColumn: (t) => t.id,
          referencedTable: $db.receivingSessionCloudStates,
          getReferencedColumn: (t) => t.localSessionId,
          builder:
              (
                joinBuilder, {
                $addJoinBuilderToRootComposer,
                $removeJoinBuilderFromRootComposer,
              }) => $$ReceivingSessionCloudStatesTableFilterComposer(
                $db: $db,
                $table: $db.receivingSessionCloudStates,
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

  Expression<T> receivingSessionCloudStatesRefs<T extends Object>(
    Expression<T> Function(
      $$ReceivingSessionCloudStatesTableAnnotationComposer a,
    )
    f,
  ) {
    final $$ReceivingSessionCloudStatesTableAnnotationComposer composer =
        $composerBuilder(
          composer: this,
          getCurrentColumn: (t) => t.id,
          referencedTable: $db.receivingSessionCloudStates,
          getReferencedColumn: (t) => t.localSessionId,
          builder:
              (
                joinBuilder, {
                $addJoinBuilderToRootComposer,
                $removeJoinBuilderFromRootComposer,
              }) => $$ReceivingSessionCloudStatesTableAnnotationComposer(
                $db: $db,
                $table: $db.receivingSessionCloudStates,
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
          PrefetchHooks Function({
            bool localReceivingEntriesRefs,
            bool receivingSessionCloudStatesRefs,
          })
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
          prefetchHooksCallback:
              ({
                localReceivingEntriesRefs = false,
                receivingSessionCloudStatesRefs = false,
              }) {
                return PrefetchHooks(
                  db: db,
                  explicitlyWatchedTables: [
                    if (localReceivingEntriesRefs) db.localReceivingEntries,
                    if (receivingSessionCloudStatesRefs)
                      db.receivingSessionCloudStates,
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
                          referencedTable:
                              $$LocalReceivingSessionsTableReferences
                                  ._localReceivingEntriesRefsTable(db),
                          managerFromTypedResult: (p0) =>
                              $$LocalReceivingSessionsTableReferences(
                                db,
                                table,
                                p0,
                              ).localReceivingEntriesRefs,
                          referencedItemsForCurrentItem:
                              (item, referencedItems) => referencedItems.where(
                                (e) => e.receivingSessionId == item.id,
                              ),
                          typedResults: items,
                        ),
                      if (receivingSessionCloudStatesRefs)
                        await $_getPrefetchedData<
                          LocalReceivingSessionRow,
                          $LocalReceivingSessionsTable,
                          ReceivingSessionCloudStateRow
                        >(
                          currentTable: table,
                          referencedTable:
                              $$LocalReceivingSessionsTableReferences
                                  ._receivingSessionCloudStatesRefsTable(db),
                          managerFromTypedResult: (p0) =>
                              $$LocalReceivingSessionsTableReferences(
                                db,
                                table,
                                p0,
                              ).receivingSessionCloudStatesRefs,
                          referencedItemsForCurrentItem:
                              (item, referencedItems) => referencedItems.where(
                                (e) => e.localSessionId == item.id,
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
      PrefetchHooks Function({
        bool localReceivingEntriesRefs,
        bool receivingSessionCloudStatesRefs,
      })
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
typedef $$PocDeviceProfilesTableCreateCompanionBuilder =
    PocDeviceProfilesCompanion Function({
      required String profileKey,
      required String backendBaseUrl,
      required String workspaceId,
      required String deviceId,
      required String displayRole,
      Value<String?> displayLabel,
      Value<bool> automaticSyncPaused,
      required String createdAtUtc,
      required String updatedAtUtc,
      Value<int> rowid,
    });
typedef $$PocDeviceProfilesTableUpdateCompanionBuilder =
    PocDeviceProfilesCompanion Function({
      Value<String> profileKey,
      Value<String> backendBaseUrl,
      Value<String> workspaceId,
      Value<String> deviceId,
      Value<String> displayRole,
      Value<String?> displayLabel,
      Value<bool> automaticSyncPaused,
      Value<String> createdAtUtc,
      Value<String> updatedAtUtc,
      Value<int> rowid,
    });

class $$PocDeviceProfilesTableFilterComposer
    extends Composer<_$TraderProLocalDatabase, $PocDeviceProfilesTable> {
  $$PocDeviceProfilesTableFilterComposer({
    required super.$db,
    required super.$table,
    super.joinBuilder,
    super.$addJoinBuilderToRootComposer,
    super.$removeJoinBuilderFromRootComposer,
  });
  ColumnFilters<String> get profileKey => $composableBuilder(
    column: $table.profileKey,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get backendBaseUrl => $composableBuilder(
    column: $table.backendBaseUrl,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get workspaceId => $composableBuilder(
    column: $table.workspaceId,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get deviceId => $composableBuilder(
    column: $table.deviceId,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get displayRole => $composableBuilder(
    column: $table.displayRole,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get displayLabel => $composableBuilder(
    column: $table.displayLabel,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<bool> get automaticSyncPaused => $composableBuilder(
    column: $table.automaticSyncPaused,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get createdAtUtc => $composableBuilder(
    column: $table.createdAtUtc,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get updatedAtUtc => $composableBuilder(
    column: $table.updatedAtUtc,
    builder: (column) => ColumnFilters(column),
  );
}

class $$PocDeviceProfilesTableOrderingComposer
    extends Composer<_$TraderProLocalDatabase, $PocDeviceProfilesTable> {
  $$PocDeviceProfilesTableOrderingComposer({
    required super.$db,
    required super.$table,
    super.joinBuilder,
    super.$addJoinBuilderToRootComposer,
    super.$removeJoinBuilderFromRootComposer,
  });
  ColumnOrderings<String> get profileKey => $composableBuilder(
    column: $table.profileKey,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get backendBaseUrl => $composableBuilder(
    column: $table.backendBaseUrl,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get workspaceId => $composableBuilder(
    column: $table.workspaceId,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get deviceId => $composableBuilder(
    column: $table.deviceId,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get displayRole => $composableBuilder(
    column: $table.displayRole,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get displayLabel => $composableBuilder(
    column: $table.displayLabel,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<bool> get automaticSyncPaused => $composableBuilder(
    column: $table.automaticSyncPaused,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get createdAtUtc => $composableBuilder(
    column: $table.createdAtUtc,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get updatedAtUtc => $composableBuilder(
    column: $table.updatedAtUtc,
    builder: (column) => ColumnOrderings(column),
  );
}

class $$PocDeviceProfilesTableAnnotationComposer
    extends Composer<_$TraderProLocalDatabase, $PocDeviceProfilesTable> {
  $$PocDeviceProfilesTableAnnotationComposer({
    required super.$db,
    required super.$table,
    super.joinBuilder,
    super.$addJoinBuilderToRootComposer,
    super.$removeJoinBuilderFromRootComposer,
  });
  GeneratedColumn<String> get profileKey => $composableBuilder(
    column: $table.profileKey,
    builder: (column) => column,
  );

  GeneratedColumn<String> get backendBaseUrl => $composableBuilder(
    column: $table.backendBaseUrl,
    builder: (column) => column,
  );

  GeneratedColumn<String> get workspaceId => $composableBuilder(
    column: $table.workspaceId,
    builder: (column) => column,
  );

  GeneratedColumn<String> get deviceId =>
      $composableBuilder(column: $table.deviceId, builder: (column) => column);

  GeneratedColumn<String> get displayRole => $composableBuilder(
    column: $table.displayRole,
    builder: (column) => column,
  );

  GeneratedColumn<String> get displayLabel => $composableBuilder(
    column: $table.displayLabel,
    builder: (column) => column,
  );

  GeneratedColumn<bool> get automaticSyncPaused => $composableBuilder(
    column: $table.automaticSyncPaused,
    builder: (column) => column,
  );

  GeneratedColumn<String> get createdAtUtc => $composableBuilder(
    column: $table.createdAtUtc,
    builder: (column) => column,
  );

  GeneratedColumn<String> get updatedAtUtc => $composableBuilder(
    column: $table.updatedAtUtc,
    builder: (column) => column,
  );
}

class $$PocDeviceProfilesTableTableManager
    extends
        RootTableManager<
          _$TraderProLocalDatabase,
          $PocDeviceProfilesTable,
          PocDeviceProfileRow,
          $$PocDeviceProfilesTableFilterComposer,
          $$PocDeviceProfilesTableOrderingComposer,
          $$PocDeviceProfilesTableAnnotationComposer,
          $$PocDeviceProfilesTableCreateCompanionBuilder,
          $$PocDeviceProfilesTableUpdateCompanionBuilder,
          (
            PocDeviceProfileRow,
            BaseReferences<
              _$TraderProLocalDatabase,
              $PocDeviceProfilesTable,
              PocDeviceProfileRow
            >,
          ),
          PocDeviceProfileRow,
          PrefetchHooks Function()
        > {
  $$PocDeviceProfilesTableTableManager(
    _$TraderProLocalDatabase db,
    $PocDeviceProfilesTable table,
  ) : super(
        TableManagerState(
          db: db,
          table: table,
          createFilteringComposer: () =>
              $$PocDeviceProfilesTableFilterComposer($db: db, $table: table),
          createOrderingComposer: () =>
              $$PocDeviceProfilesTableOrderingComposer($db: db, $table: table),
          createComputedFieldComposer: () =>
              $$PocDeviceProfilesTableAnnotationComposer(
                $db: db,
                $table: table,
              ),
          updateCompanionCallback:
              ({
                Value<String> profileKey = const Value.absent(),
                Value<String> backendBaseUrl = const Value.absent(),
                Value<String> workspaceId = const Value.absent(),
                Value<String> deviceId = const Value.absent(),
                Value<String> displayRole = const Value.absent(),
                Value<String?> displayLabel = const Value.absent(),
                Value<bool> automaticSyncPaused = const Value.absent(),
                Value<String> createdAtUtc = const Value.absent(),
                Value<String> updatedAtUtc = const Value.absent(),
                Value<int> rowid = const Value.absent(),
              }) => PocDeviceProfilesCompanion(
                profileKey: profileKey,
                backendBaseUrl: backendBaseUrl,
                workspaceId: workspaceId,
                deviceId: deviceId,
                displayRole: displayRole,
                displayLabel: displayLabel,
                automaticSyncPaused: automaticSyncPaused,
                createdAtUtc: createdAtUtc,
                updatedAtUtc: updatedAtUtc,
                rowid: rowid,
              ),
          createCompanionCallback:
              ({
                required String profileKey,
                required String backendBaseUrl,
                required String workspaceId,
                required String deviceId,
                required String displayRole,
                Value<String?> displayLabel = const Value.absent(),
                Value<bool> automaticSyncPaused = const Value.absent(),
                required String createdAtUtc,
                required String updatedAtUtc,
                Value<int> rowid = const Value.absent(),
              }) => PocDeviceProfilesCompanion.insert(
                profileKey: profileKey,
                backendBaseUrl: backendBaseUrl,
                workspaceId: workspaceId,
                deviceId: deviceId,
                displayRole: displayRole,
                displayLabel: displayLabel,
                automaticSyncPaused: automaticSyncPaused,
                createdAtUtc: createdAtUtc,
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

typedef $$PocDeviceProfilesTableProcessedTableManager =
    ProcessedTableManager<
      _$TraderProLocalDatabase,
      $PocDeviceProfilesTable,
      PocDeviceProfileRow,
      $$PocDeviceProfilesTableFilterComposer,
      $$PocDeviceProfilesTableOrderingComposer,
      $$PocDeviceProfilesTableAnnotationComposer,
      $$PocDeviceProfilesTableCreateCompanionBuilder,
      $$PocDeviceProfilesTableUpdateCompanionBuilder,
      (
        PocDeviceProfileRow,
        BaseReferences<
          _$TraderProLocalDatabase,
          $PocDeviceProfilesTable,
          PocDeviceProfileRow
        >,
      ),
      PocDeviceProfileRow,
      PrefetchHooks Function()
    >;
typedef $$PocSyncSourcesTableCreateCompanionBuilder =
    PocSyncSourcesCompanion Function({
      required String sourceKey,
      required String backendBaseUrl,
      required String workspaceId,
      Value<int> eventCursor,
      Value<String?> lastSuccessfulPollAtUtc,
      Value<String?> lastErrorCode,
      Value<String?> lastErrorMessage,
      required String updatedAtUtc,
      Value<int> rowid,
    });
typedef $$PocSyncSourcesTableUpdateCompanionBuilder =
    PocSyncSourcesCompanion Function({
      Value<String> sourceKey,
      Value<String> backendBaseUrl,
      Value<String> workspaceId,
      Value<int> eventCursor,
      Value<String?> lastSuccessfulPollAtUtc,
      Value<String?> lastErrorCode,
      Value<String?> lastErrorMessage,
      Value<String> updatedAtUtc,
      Value<int> rowid,
    });

final class $$PocSyncSourcesTableReferences
    extends
        BaseReferences<
          _$TraderProLocalDatabase,
          $PocSyncSourcesTable,
          PocSyncSourceRow
        > {
  $$PocSyncSourcesTableReferences(
    super.$_db,
    super.$_table,
    super.$_typedResult,
  );

  static MultiTypedResultKey<
    $ReceivingSessionCloudStatesTable,
    List<ReceivingSessionCloudStateRow>
  >
  _receivingSessionCloudStatesRefsTable(
    _$TraderProLocalDatabase db,
  ) => MultiTypedResultKey.fromTable(
    db.receivingSessionCloudStates,
    aliasName:
        'poc_sync_sources__source_key__receiving_session_cloud_states__source_key',
  );

  $$ReceivingSessionCloudStatesTableProcessedTableManager
  get receivingSessionCloudStatesRefs {
    final manager =
        $$ReceivingSessionCloudStatesTableTableManager(
          $_db,
          $_db.receivingSessionCloudStates,
        ).filter(
          (f) => f.sourceKey.sourceKey.sqlEquals(
            $_itemColumn<String>('source_key')!,
          ),
        );

    final cache = $_typedResult.readTableOrNull(
      _receivingSessionCloudStatesRefsTable($_db),
    );
    return ProcessedTableManager(
      manager.$state.copyWith(prefetchedData: cache),
    );
  }

  static MultiTypedResultKey<
    $MobileSyncEventInboxTable,
    List<MobileSyncEventInboxRow>
  >
  _mobileSyncEventInboxRefsTable(_$TraderProLocalDatabase db) =>
      MultiTypedResultKey.fromTable(
        db.mobileSyncEventInbox,
        aliasName:
            'poc_sync_sources__source_key__mobile_sync_event_inbox__source_key',
      );

  $$MobileSyncEventInboxTableProcessedTableManager
  get mobileSyncEventInboxRefs {
    final manager =
        $$MobileSyncEventInboxTableTableManager(
          $_db,
          $_db.mobileSyncEventInbox,
        ).filter(
          (f) => f.sourceKey.sourceKey.sqlEquals(
            $_itemColumn<String>('source_key')!,
          ),
        );

    final cache = $_typedResult.readTableOrNull(
      _mobileSyncEventInboxRefsTable($_db),
    );
    return ProcessedTableManager(
      manager.$state.copyWith(prefetchedData: cache),
    );
  }

  static MultiTypedResultKey<
    $RemoteReceivingSessionProjectionsTable,
    List<RemoteReceivingSessionProjectionRow>
  >
  _remoteReceivingSessionProjectionsRefsTable(
    _$TraderProLocalDatabase db,
  ) => MultiTypedResultKey.fromTable(
    db.remoteReceivingSessionProjections,
    aliasName:
        'poc_sync_sources__source_key__remote_receiving_session_projections__source_key',
  );

  $$RemoteReceivingSessionProjectionsTableProcessedTableManager
  get remoteReceivingSessionProjectionsRefs {
    final manager =
        $$RemoteReceivingSessionProjectionsTableTableManager(
          $_db,
          $_db.remoteReceivingSessionProjections,
        ).filter(
          (f) => f.sourceKey.sourceKey.sqlEquals(
            $_itemColumn<String>('source_key')!,
          ),
        );

    final cache = $_typedResult.readTableOrNull(
      _remoteReceivingSessionProjectionsRefsTable($_db),
    );
    return ProcessedTableManager(
      manager.$state.copyWith(prefetchedData: cache),
    );
  }

  static MultiTypedResultKey<
    $PocControlCommandsTable,
    List<PocControlCommandRow>
  >
  _pocControlCommandsRefsTable(_$TraderProLocalDatabase db) =>
      MultiTypedResultKey.fromTable(
        db.pocControlCommands,
        aliasName:
            'poc_sync_sources__source_key__poc_control_commands__source_key',
      );

  $$PocControlCommandsTableProcessedTableManager get pocControlCommandsRefs {
    final manager =
        $$PocControlCommandsTableTableManager(
          $_db,
          $_db.pocControlCommands,
        ).filter(
          (f) => f.sourceKey.sourceKey.sqlEquals(
            $_itemColumn<String>('source_key')!,
          ),
        );

    final cache = $_typedResult.readTableOrNull(
      _pocControlCommandsRefsTable($_db),
    );
    return ProcessedTableManager(
      manager.$state.copyWith(prefetchedData: cache),
    );
  }
}

class $$PocSyncSourcesTableFilterComposer
    extends Composer<_$TraderProLocalDatabase, $PocSyncSourcesTable> {
  $$PocSyncSourcesTableFilterComposer({
    required super.$db,
    required super.$table,
    super.joinBuilder,
    super.$addJoinBuilderToRootComposer,
    super.$removeJoinBuilderFromRootComposer,
  });
  ColumnFilters<String> get sourceKey => $composableBuilder(
    column: $table.sourceKey,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get backendBaseUrl => $composableBuilder(
    column: $table.backendBaseUrl,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get workspaceId => $composableBuilder(
    column: $table.workspaceId,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<int> get eventCursor => $composableBuilder(
    column: $table.eventCursor,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get lastSuccessfulPollAtUtc => $composableBuilder(
    column: $table.lastSuccessfulPollAtUtc,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get lastErrorCode => $composableBuilder(
    column: $table.lastErrorCode,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get lastErrorMessage => $composableBuilder(
    column: $table.lastErrorMessage,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get updatedAtUtc => $composableBuilder(
    column: $table.updatedAtUtc,
    builder: (column) => ColumnFilters(column),
  );

  Expression<bool> receivingSessionCloudStatesRefs(
    Expression<bool> Function(
      $$ReceivingSessionCloudStatesTableFilterComposer f,
    )
    f,
  ) {
    final $$ReceivingSessionCloudStatesTableFilterComposer composer =
        $composerBuilder(
          composer: this,
          getCurrentColumn: (t) => t.sourceKey,
          referencedTable: $db.receivingSessionCloudStates,
          getReferencedColumn: (t) => t.sourceKey,
          builder:
              (
                joinBuilder, {
                $addJoinBuilderToRootComposer,
                $removeJoinBuilderFromRootComposer,
              }) => $$ReceivingSessionCloudStatesTableFilterComposer(
                $db: $db,
                $table: $db.receivingSessionCloudStates,
                $addJoinBuilderToRootComposer: $addJoinBuilderToRootComposer,
                joinBuilder: joinBuilder,
                $removeJoinBuilderFromRootComposer:
                    $removeJoinBuilderFromRootComposer,
              ),
        );
    return f(composer);
  }

  Expression<bool> mobileSyncEventInboxRefs(
    Expression<bool> Function($$MobileSyncEventInboxTableFilterComposer f) f,
  ) {
    final $$MobileSyncEventInboxTableFilterComposer composer = $composerBuilder(
      composer: this,
      getCurrentColumn: (t) => t.sourceKey,
      referencedTable: $db.mobileSyncEventInbox,
      getReferencedColumn: (t) => t.sourceKey,
      builder:
          (
            joinBuilder, {
            $addJoinBuilderToRootComposer,
            $removeJoinBuilderFromRootComposer,
          }) => $$MobileSyncEventInboxTableFilterComposer(
            $db: $db,
            $table: $db.mobileSyncEventInbox,
            $addJoinBuilderToRootComposer: $addJoinBuilderToRootComposer,
            joinBuilder: joinBuilder,
            $removeJoinBuilderFromRootComposer:
                $removeJoinBuilderFromRootComposer,
          ),
    );
    return f(composer);
  }

  Expression<bool> remoteReceivingSessionProjectionsRefs(
    Expression<bool> Function(
      $$RemoteReceivingSessionProjectionsTableFilterComposer f,
    )
    f,
  ) {
    final $$RemoteReceivingSessionProjectionsTableFilterComposer composer =
        $composerBuilder(
          composer: this,
          getCurrentColumn: (t) => t.sourceKey,
          referencedTable: $db.remoteReceivingSessionProjections,
          getReferencedColumn: (t) => t.sourceKey,
          builder:
              (
                joinBuilder, {
                $addJoinBuilderToRootComposer,
                $removeJoinBuilderFromRootComposer,
              }) => $$RemoteReceivingSessionProjectionsTableFilterComposer(
                $db: $db,
                $table: $db.remoteReceivingSessionProjections,
                $addJoinBuilderToRootComposer: $addJoinBuilderToRootComposer,
                joinBuilder: joinBuilder,
                $removeJoinBuilderFromRootComposer:
                    $removeJoinBuilderFromRootComposer,
              ),
        );
    return f(composer);
  }

  Expression<bool> pocControlCommandsRefs(
    Expression<bool> Function($$PocControlCommandsTableFilterComposer f) f,
  ) {
    final $$PocControlCommandsTableFilterComposer composer = $composerBuilder(
      composer: this,
      getCurrentColumn: (t) => t.sourceKey,
      referencedTable: $db.pocControlCommands,
      getReferencedColumn: (t) => t.sourceKey,
      builder:
          (
            joinBuilder, {
            $addJoinBuilderToRootComposer,
            $removeJoinBuilderFromRootComposer,
          }) => $$PocControlCommandsTableFilterComposer(
            $db: $db,
            $table: $db.pocControlCommands,
            $addJoinBuilderToRootComposer: $addJoinBuilderToRootComposer,
            joinBuilder: joinBuilder,
            $removeJoinBuilderFromRootComposer:
                $removeJoinBuilderFromRootComposer,
          ),
    );
    return f(composer);
  }
}

class $$PocSyncSourcesTableOrderingComposer
    extends Composer<_$TraderProLocalDatabase, $PocSyncSourcesTable> {
  $$PocSyncSourcesTableOrderingComposer({
    required super.$db,
    required super.$table,
    super.joinBuilder,
    super.$addJoinBuilderToRootComposer,
    super.$removeJoinBuilderFromRootComposer,
  });
  ColumnOrderings<String> get sourceKey => $composableBuilder(
    column: $table.sourceKey,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get backendBaseUrl => $composableBuilder(
    column: $table.backendBaseUrl,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get workspaceId => $composableBuilder(
    column: $table.workspaceId,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<int> get eventCursor => $composableBuilder(
    column: $table.eventCursor,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get lastSuccessfulPollAtUtc => $composableBuilder(
    column: $table.lastSuccessfulPollAtUtc,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get lastErrorCode => $composableBuilder(
    column: $table.lastErrorCode,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get lastErrorMessage => $composableBuilder(
    column: $table.lastErrorMessage,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get updatedAtUtc => $composableBuilder(
    column: $table.updatedAtUtc,
    builder: (column) => ColumnOrderings(column),
  );
}

class $$PocSyncSourcesTableAnnotationComposer
    extends Composer<_$TraderProLocalDatabase, $PocSyncSourcesTable> {
  $$PocSyncSourcesTableAnnotationComposer({
    required super.$db,
    required super.$table,
    super.joinBuilder,
    super.$addJoinBuilderToRootComposer,
    super.$removeJoinBuilderFromRootComposer,
  });
  GeneratedColumn<String> get sourceKey =>
      $composableBuilder(column: $table.sourceKey, builder: (column) => column);

  GeneratedColumn<String> get backendBaseUrl => $composableBuilder(
    column: $table.backendBaseUrl,
    builder: (column) => column,
  );

  GeneratedColumn<String> get workspaceId => $composableBuilder(
    column: $table.workspaceId,
    builder: (column) => column,
  );

  GeneratedColumn<int> get eventCursor => $composableBuilder(
    column: $table.eventCursor,
    builder: (column) => column,
  );

  GeneratedColumn<String> get lastSuccessfulPollAtUtc => $composableBuilder(
    column: $table.lastSuccessfulPollAtUtc,
    builder: (column) => column,
  );

  GeneratedColumn<String> get lastErrorCode => $composableBuilder(
    column: $table.lastErrorCode,
    builder: (column) => column,
  );

  GeneratedColumn<String> get lastErrorMessage => $composableBuilder(
    column: $table.lastErrorMessage,
    builder: (column) => column,
  );

  GeneratedColumn<String> get updatedAtUtc => $composableBuilder(
    column: $table.updatedAtUtc,
    builder: (column) => column,
  );

  Expression<T> receivingSessionCloudStatesRefs<T extends Object>(
    Expression<T> Function(
      $$ReceivingSessionCloudStatesTableAnnotationComposer a,
    )
    f,
  ) {
    final $$ReceivingSessionCloudStatesTableAnnotationComposer composer =
        $composerBuilder(
          composer: this,
          getCurrentColumn: (t) => t.sourceKey,
          referencedTable: $db.receivingSessionCloudStates,
          getReferencedColumn: (t) => t.sourceKey,
          builder:
              (
                joinBuilder, {
                $addJoinBuilderToRootComposer,
                $removeJoinBuilderFromRootComposer,
              }) => $$ReceivingSessionCloudStatesTableAnnotationComposer(
                $db: $db,
                $table: $db.receivingSessionCloudStates,
                $addJoinBuilderToRootComposer: $addJoinBuilderToRootComposer,
                joinBuilder: joinBuilder,
                $removeJoinBuilderFromRootComposer:
                    $removeJoinBuilderFromRootComposer,
              ),
        );
    return f(composer);
  }

  Expression<T> mobileSyncEventInboxRefs<T extends Object>(
    Expression<T> Function($$MobileSyncEventInboxTableAnnotationComposer a) f,
  ) {
    final $$MobileSyncEventInboxTableAnnotationComposer composer =
        $composerBuilder(
          composer: this,
          getCurrentColumn: (t) => t.sourceKey,
          referencedTable: $db.mobileSyncEventInbox,
          getReferencedColumn: (t) => t.sourceKey,
          builder:
              (
                joinBuilder, {
                $addJoinBuilderToRootComposer,
                $removeJoinBuilderFromRootComposer,
              }) => $$MobileSyncEventInboxTableAnnotationComposer(
                $db: $db,
                $table: $db.mobileSyncEventInbox,
                $addJoinBuilderToRootComposer: $addJoinBuilderToRootComposer,
                joinBuilder: joinBuilder,
                $removeJoinBuilderFromRootComposer:
                    $removeJoinBuilderFromRootComposer,
              ),
        );
    return f(composer);
  }

  Expression<T> remoteReceivingSessionProjectionsRefs<T extends Object>(
    Expression<T> Function(
      $$RemoteReceivingSessionProjectionsTableAnnotationComposer a,
    )
    f,
  ) {
    final $$RemoteReceivingSessionProjectionsTableAnnotationComposer composer =
        $composerBuilder(
          composer: this,
          getCurrentColumn: (t) => t.sourceKey,
          referencedTable: $db.remoteReceivingSessionProjections,
          getReferencedColumn: (t) => t.sourceKey,
          builder:
              (
                joinBuilder, {
                $addJoinBuilderToRootComposer,
                $removeJoinBuilderFromRootComposer,
              }) => $$RemoteReceivingSessionProjectionsTableAnnotationComposer(
                $db: $db,
                $table: $db.remoteReceivingSessionProjections,
                $addJoinBuilderToRootComposer: $addJoinBuilderToRootComposer,
                joinBuilder: joinBuilder,
                $removeJoinBuilderFromRootComposer:
                    $removeJoinBuilderFromRootComposer,
              ),
        );
    return f(composer);
  }

  Expression<T> pocControlCommandsRefs<T extends Object>(
    Expression<T> Function($$PocControlCommandsTableAnnotationComposer a) f,
  ) {
    final $$PocControlCommandsTableAnnotationComposer composer =
        $composerBuilder(
          composer: this,
          getCurrentColumn: (t) => t.sourceKey,
          referencedTable: $db.pocControlCommands,
          getReferencedColumn: (t) => t.sourceKey,
          builder:
              (
                joinBuilder, {
                $addJoinBuilderToRootComposer,
                $removeJoinBuilderFromRootComposer,
              }) => $$PocControlCommandsTableAnnotationComposer(
                $db: $db,
                $table: $db.pocControlCommands,
                $addJoinBuilderToRootComposer: $addJoinBuilderToRootComposer,
                joinBuilder: joinBuilder,
                $removeJoinBuilderFromRootComposer:
                    $removeJoinBuilderFromRootComposer,
              ),
        );
    return f(composer);
  }
}

class $$PocSyncSourcesTableTableManager
    extends
        RootTableManager<
          _$TraderProLocalDatabase,
          $PocSyncSourcesTable,
          PocSyncSourceRow,
          $$PocSyncSourcesTableFilterComposer,
          $$PocSyncSourcesTableOrderingComposer,
          $$PocSyncSourcesTableAnnotationComposer,
          $$PocSyncSourcesTableCreateCompanionBuilder,
          $$PocSyncSourcesTableUpdateCompanionBuilder,
          (PocSyncSourceRow, $$PocSyncSourcesTableReferences),
          PocSyncSourceRow,
          PrefetchHooks Function({
            bool receivingSessionCloudStatesRefs,
            bool mobileSyncEventInboxRefs,
            bool remoteReceivingSessionProjectionsRefs,
            bool pocControlCommandsRefs,
          })
        > {
  $$PocSyncSourcesTableTableManager(
    _$TraderProLocalDatabase db,
    $PocSyncSourcesTable table,
  ) : super(
        TableManagerState(
          db: db,
          table: table,
          createFilteringComposer: () =>
              $$PocSyncSourcesTableFilterComposer($db: db, $table: table),
          createOrderingComposer: () =>
              $$PocSyncSourcesTableOrderingComposer($db: db, $table: table),
          createComputedFieldComposer: () =>
              $$PocSyncSourcesTableAnnotationComposer($db: db, $table: table),
          updateCompanionCallback:
              ({
                Value<String> sourceKey = const Value.absent(),
                Value<String> backendBaseUrl = const Value.absent(),
                Value<String> workspaceId = const Value.absent(),
                Value<int> eventCursor = const Value.absent(),
                Value<String?> lastSuccessfulPollAtUtc = const Value.absent(),
                Value<String?> lastErrorCode = const Value.absent(),
                Value<String?> lastErrorMessage = const Value.absent(),
                Value<String> updatedAtUtc = const Value.absent(),
                Value<int> rowid = const Value.absent(),
              }) => PocSyncSourcesCompanion(
                sourceKey: sourceKey,
                backendBaseUrl: backendBaseUrl,
                workspaceId: workspaceId,
                eventCursor: eventCursor,
                lastSuccessfulPollAtUtc: lastSuccessfulPollAtUtc,
                lastErrorCode: lastErrorCode,
                lastErrorMessage: lastErrorMessage,
                updatedAtUtc: updatedAtUtc,
                rowid: rowid,
              ),
          createCompanionCallback:
              ({
                required String sourceKey,
                required String backendBaseUrl,
                required String workspaceId,
                Value<int> eventCursor = const Value.absent(),
                Value<String?> lastSuccessfulPollAtUtc = const Value.absent(),
                Value<String?> lastErrorCode = const Value.absent(),
                Value<String?> lastErrorMessage = const Value.absent(),
                required String updatedAtUtc,
                Value<int> rowid = const Value.absent(),
              }) => PocSyncSourcesCompanion.insert(
                sourceKey: sourceKey,
                backendBaseUrl: backendBaseUrl,
                workspaceId: workspaceId,
                eventCursor: eventCursor,
                lastSuccessfulPollAtUtc: lastSuccessfulPollAtUtc,
                lastErrorCode: lastErrorCode,
                lastErrorMessage: lastErrorMessage,
                updatedAtUtc: updatedAtUtc,
                rowid: rowid,
              ),
          withReferenceMapper: (p0) => p0
              .map(
                (e) => (
                  e.readTable(table),
                  $$PocSyncSourcesTableReferences(db, table, e),
                ),
              )
              .toList(),
          prefetchHooksCallback:
              ({
                receivingSessionCloudStatesRefs = false,
                mobileSyncEventInboxRefs = false,
                remoteReceivingSessionProjectionsRefs = false,
                pocControlCommandsRefs = false,
              }) {
                return PrefetchHooks(
                  db: db,
                  explicitlyWatchedTables: [
                    if (receivingSessionCloudStatesRefs)
                      db.receivingSessionCloudStates,
                    if (mobileSyncEventInboxRefs) db.mobileSyncEventInbox,
                    if (remoteReceivingSessionProjectionsRefs)
                      db.remoteReceivingSessionProjections,
                    if (pocControlCommandsRefs) db.pocControlCommands,
                  ],
                  addJoins: null,
                  getPrefetchedDataCallback: (items) async {
                    return [
                      if (receivingSessionCloudStatesRefs)
                        await $_getPrefetchedData<
                          PocSyncSourceRow,
                          $PocSyncSourcesTable,
                          ReceivingSessionCloudStateRow
                        >(
                          currentTable: table,
                          referencedTable: $$PocSyncSourcesTableReferences
                              ._receivingSessionCloudStatesRefsTable(db),
                          managerFromTypedResult: (p0) =>
                              $$PocSyncSourcesTableReferences(
                                db,
                                table,
                                p0,
                              ).receivingSessionCloudStatesRefs,
                          referencedItemsForCurrentItem:
                              (item, referencedItems) => referencedItems.where(
                                (e) => e.sourceKey == item.sourceKey,
                              ),
                          typedResults: items,
                        ),
                      if (mobileSyncEventInboxRefs)
                        await $_getPrefetchedData<
                          PocSyncSourceRow,
                          $PocSyncSourcesTable,
                          MobileSyncEventInboxRow
                        >(
                          currentTable: table,
                          referencedTable: $$PocSyncSourcesTableReferences
                              ._mobileSyncEventInboxRefsTable(db),
                          managerFromTypedResult: (p0) =>
                              $$PocSyncSourcesTableReferences(
                                db,
                                table,
                                p0,
                              ).mobileSyncEventInboxRefs,
                          referencedItemsForCurrentItem:
                              (item, referencedItems) => referencedItems.where(
                                (e) => e.sourceKey == item.sourceKey,
                              ),
                          typedResults: items,
                        ),
                      if (remoteReceivingSessionProjectionsRefs)
                        await $_getPrefetchedData<
                          PocSyncSourceRow,
                          $PocSyncSourcesTable,
                          RemoteReceivingSessionProjectionRow
                        >(
                          currentTable: table,
                          referencedTable: $$PocSyncSourcesTableReferences
                              ._remoteReceivingSessionProjectionsRefsTable(db),
                          managerFromTypedResult: (p0) =>
                              $$PocSyncSourcesTableReferences(
                                db,
                                table,
                                p0,
                              ).remoteReceivingSessionProjectionsRefs,
                          referencedItemsForCurrentItem:
                              (item, referencedItems) => referencedItems.where(
                                (e) => e.sourceKey == item.sourceKey,
                              ),
                          typedResults: items,
                        ),
                      if (pocControlCommandsRefs)
                        await $_getPrefetchedData<
                          PocSyncSourceRow,
                          $PocSyncSourcesTable,
                          PocControlCommandRow
                        >(
                          currentTable: table,
                          referencedTable: $$PocSyncSourcesTableReferences
                              ._pocControlCommandsRefsTable(db),
                          managerFromTypedResult: (p0) =>
                              $$PocSyncSourcesTableReferences(
                                db,
                                table,
                                p0,
                              ).pocControlCommandsRefs,
                          referencedItemsForCurrentItem:
                              (item, referencedItems) => referencedItems.where(
                                (e) => e.sourceKey == item.sourceKey,
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

typedef $$PocSyncSourcesTableProcessedTableManager =
    ProcessedTableManager<
      _$TraderProLocalDatabase,
      $PocSyncSourcesTable,
      PocSyncSourceRow,
      $$PocSyncSourcesTableFilterComposer,
      $$PocSyncSourcesTableOrderingComposer,
      $$PocSyncSourcesTableAnnotationComposer,
      $$PocSyncSourcesTableCreateCompanionBuilder,
      $$PocSyncSourcesTableUpdateCompanionBuilder,
      (PocSyncSourceRow, $$PocSyncSourcesTableReferences),
      PocSyncSourceRow,
      PrefetchHooks Function({
        bool receivingSessionCloudStatesRefs,
        bool mobileSyncEventInboxRefs,
        bool remoteReceivingSessionProjectionsRefs,
        bool pocControlCommandsRefs,
      })
    >;
typedef $$ReceivingSessionCloudStatesTableCreateCompanionBuilder =
    ReceivingSessionCloudStatesCompanion Function({
      required String sourceKey,
      required String boundDeviceId,
      required String localSessionId,
      required String cloudSessionId,
      required String cloudReference,
      required String cloudStatus,
      required int cloudVersion,
      Value<String?> leaseId,
      Value<String?> leaseExpiresAtUtc,
      Value<String?> editorDeviceId,
      required String lastSuccessfulSyncAtUtc,
      required String lastCloudUpdateAtUtc,
      Value<String?> lastErrorCode,
      Value<String?> lastErrorMessage,
      Value<int> rowid,
    });
typedef $$ReceivingSessionCloudStatesTableUpdateCompanionBuilder =
    ReceivingSessionCloudStatesCompanion Function({
      Value<String> sourceKey,
      Value<String> boundDeviceId,
      Value<String> localSessionId,
      Value<String> cloudSessionId,
      Value<String> cloudReference,
      Value<String> cloudStatus,
      Value<int> cloudVersion,
      Value<String?> leaseId,
      Value<String?> leaseExpiresAtUtc,
      Value<String?> editorDeviceId,
      Value<String> lastSuccessfulSyncAtUtc,
      Value<String> lastCloudUpdateAtUtc,
      Value<String?> lastErrorCode,
      Value<String?> lastErrorMessage,
      Value<int> rowid,
    });

final class $$ReceivingSessionCloudStatesTableReferences
    extends
        BaseReferences<
          _$TraderProLocalDatabase,
          $ReceivingSessionCloudStatesTable,
          ReceivingSessionCloudStateRow
        > {
  $$ReceivingSessionCloudStatesTableReferences(
    super.$_db,
    super.$_table,
    super.$_typedResult,
  );

  static $PocSyncSourcesTable _sourceKeyTable(
    _$TraderProLocalDatabase db,
  ) => db.pocSyncSources.createAlias(
    'receiving_session_cloud_states__source_key__poc_sync_sources__source_key',
  );

  $$PocSyncSourcesTableProcessedTableManager get sourceKey {
    final $_column = $_itemColumn<String>('source_key')!;

    final manager = $$PocSyncSourcesTableTableManager(
      $_db,
      $_db.pocSyncSources,
    ).filter((f) => f.sourceKey.sqlEquals($_column));
    final item = $_typedResult.readTableOrNull(_sourceKeyTable($_db));
    if (item == null) return manager;
    return ProcessedTableManager(
      manager.$state.copyWith(prefetchedData: [item]),
    );
  }

  static $LocalReceivingSessionsTable _localSessionIdTable(
    _$TraderProLocalDatabase db,
  ) => db.localReceivingSessions.createAlias(
    'receiving_session_cloud_states__local_session_id__local_receiving_sessions__id',
  );

  $$LocalReceivingSessionsTableProcessedTableManager get localSessionId {
    final $_column = $_itemColumn<String>('local_session_id')!;

    final manager = $$LocalReceivingSessionsTableTableManager(
      $_db,
      $_db.localReceivingSessions,
    ).filter((f) => f.id.sqlEquals($_column));
    final item = $_typedResult.readTableOrNull(_localSessionIdTable($_db));
    if (item == null) return manager;
    return ProcessedTableManager(
      manager.$state.copyWith(prefetchedData: [item]),
    );
  }
}

class $$ReceivingSessionCloudStatesTableFilterComposer
    extends
        Composer<_$TraderProLocalDatabase, $ReceivingSessionCloudStatesTable> {
  $$ReceivingSessionCloudStatesTableFilterComposer({
    required super.$db,
    required super.$table,
    super.joinBuilder,
    super.$addJoinBuilderToRootComposer,
    super.$removeJoinBuilderFromRootComposer,
  });
  ColumnFilters<String> get boundDeviceId => $composableBuilder(
    column: $table.boundDeviceId,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get cloudSessionId => $composableBuilder(
    column: $table.cloudSessionId,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get cloudReference => $composableBuilder(
    column: $table.cloudReference,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get cloudStatus => $composableBuilder(
    column: $table.cloudStatus,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<int> get cloudVersion => $composableBuilder(
    column: $table.cloudVersion,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get leaseId => $composableBuilder(
    column: $table.leaseId,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get leaseExpiresAtUtc => $composableBuilder(
    column: $table.leaseExpiresAtUtc,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get editorDeviceId => $composableBuilder(
    column: $table.editorDeviceId,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get lastSuccessfulSyncAtUtc => $composableBuilder(
    column: $table.lastSuccessfulSyncAtUtc,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get lastCloudUpdateAtUtc => $composableBuilder(
    column: $table.lastCloudUpdateAtUtc,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get lastErrorCode => $composableBuilder(
    column: $table.lastErrorCode,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get lastErrorMessage => $composableBuilder(
    column: $table.lastErrorMessage,
    builder: (column) => ColumnFilters(column),
  );

  $$PocSyncSourcesTableFilterComposer get sourceKey {
    final $$PocSyncSourcesTableFilterComposer composer = $composerBuilder(
      composer: this,
      getCurrentColumn: (t) => t.sourceKey,
      referencedTable: $db.pocSyncSources,
      getReferencedColumn: (t) => t.sourceKey,
      builder:
          (
            joinBuilder, {
            $addJoinBuilderToRootComposer,
            $removeJoinBuilderFromRootComposer,
          }) => $$PocSyncSourcesTableFilterComposer(
            $db: $db,
            $table: $db.pocSyncSources,
            $addJoinBuilderToRootComposer: $addJoinBuilderToRootComposer,
            joinBuilder: joinBuilder,
            $removeJoinBuilderFromRootComposer:
                $removeJoinBuilderFromRootComposer,
          ),
    );
    return composer;
  }

  $$LocalReceivingSessionsTableFilterComposer get localSessionId {
    final $$LocalReceivingSessionsTableFilterComposer composer =
        $composerBuilder(
          composer: this,
          getCurrentColumn: (t) => t.localSessionId,
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

class $$ReceivingSessionCloudStatesTableOrderingComposer
    extends
        Composer<_$TraderProLocalDatabase, $ReceivingSessionCloudStatesTable> {
  $$ReceivingSessionCloudStatesTableOrderingComposer({
    required super.$db,
    required super.$table,
    super.joinBuilder,
    super.$addJoinBuilderToRootComposer,
    super.$removeJoinBuilderFromRootComposer,
  });
  ColumnOrderings<String> get boundDeviceId => $composableBuilder(
    column: $table.boundDeviceId,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get cloudSessionId => $composableBuilder(
    column: $table.cloudSessionId,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get cloudReference => $composableBuilder(
    column: $table.cloudReference,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get cloudStatus => $composableBuilder(
    column: $table.cloudStatus,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<int> get cloudVersion => $composableBuilder(
    column: $table.cloudVersion,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get leaseId => $composableBuilder(
    column: $table.leaseId,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get leaseExpiresAtUtc => $composableBuilder(
    column: $table.leaseExpiresAtUtc,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get editorDeviceId => $composableBuilder(
    column: $table.editorDeviceId,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get lastSuccessfulSyncAtUtc => $composableBuilder(
    column: $table.lastSuccessfulSyncAtUtc,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get lastCloudUpdateAtUtc => $composableBuilder(
    column: $table.lastCloudUpdateAtUtc,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get lastErrorCode => $composableBuilder(
    column: $table.lastErrorCode,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get lastErrorMessage => $composableBuilder(
    column: $table.lastErrorMessage,
    builder: (column) => ColumnOrderings(column),
  );

  $$PocSyncSourcesTableOrderingComposer get sourceKey {
    final $$PocSyncSourcesTableOrderingComposer composer = $composerBuilder(
      composer: this,
      getCurrentColumn: (t) => t.sourceKey,
      referencedTable: $db.pocSyncSources,
      getReferencedColumn: (t) => t.sourceKey,
      builder:
          (
            joinBuilder, {
            $addJoinBuilderToRootComposer,
            $removeJoinBuilderFromRootComposer,
          }) => $$PocSyncSourcesTableOrderingComposer(
            $db: $db,
            $table: $db.pocSyncSources,
            $addJoinBuilderToRootComposer: $addJoinBuilderToRootComposer,
            joinBuilder: joinBuilder,
            $removeJoinBuilderFromRootComposer:
                $removeJoinBuilderFromRootComposer,
          ),
    );
    return composer;
  }

  $$LocalReceivingSessionsTableOrderingComposer get localSessionId {
    final $$LocalReceivingSessionsTableOrderingComposer composer =
        $composerBuilder(
          composer: this,
          getCurrentColumn: (t) => t.localSessionId,
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

class $$ReceivingSessionCloudStatesTableAnnotationComposer
    extends
        Composer<_$TraderProLocalDatabase, $ReceivingSessionCloudStatesTable> {
  $$ReceivingSessionCloudStatesTableAnnotationComposer({
    required super.$db,
    required super.$table,
    super.joinBuilder,
    super.$addJoinBuilderToRootComposer,
    super.$removeJoinBuilderFromRootComposer,
  });
  GeneratedColumn<String> get boundDeviceId => $composableBuilder(
    column: $table.boundDeviceId,
    builder: (column) => column,
  );

  GeneratedColumn<String> get cloudSessionId => $composableBuilder(
    column: $table.cloudSessionId,
    builder: (column) => column,
  );

  GeneratedColumn<String> get cloudReference => $composableBuilder(
    column: $table.cloudReference,
    builder: (column) => column,
  );

  GeneratedColumn<String> get cloudStatus => $composableBuilder(
    column: $table.cloudStatus,
    builder: (column) => column,
  );

  GeneratedColumn<int> get cloudVersion => $composableBuilder(
    column: $table.cloudVersion,
    builder: (column) => column,
  );

  GeneratedColumn<String> get leaseId =>
      $composableBuilder(column: $table.leaseId, builder: (column) => column);

  GeneratedColumn<String> get leaseExpiresAtUtc => $composableBuilder(
    column: $table.leaseExpiresAtUtc,
    builder: (column) => column,
  );

  GeneratedColumn<String> get editorDeviceId => $composableBuilder(
    column: $table.editorDeviceId,
    builder: (column) => column,
  );

  GeneratedColumn<String> get lastSuccessfulSyncAtUtc => $composableBuilder(
    column: $table.lastSuccessfulSyncAtUtc,
    builder: (column) => column,
  );

  GeneratedColumn<String> get lastCloudUpdateAtUtc => $composableBuilder(
    column: $table.lastCloudUpdateAtUtc,
    builder: (column) => column,
  );

  GeneratedColumn<String> get lastErrorCode => $composableBuilder(
    column: $table.lastErrorCode,
    builder: (column) => column,
  );

  GeneratedColumn<String> get lastErrorMessage => $composableBuilder(
    column: $table.lastErrorMessage,
    builder: (column) => column,
  );

  $$PocSyncSourcesTableAnnotationComposer get sourceKey {
    final $$PocSyncSourcesTableAnnotationComposer composer = $composerBuilder(
      composer: this,
      getCurrentColumn: (t) => t.sourceKey,
      referencedTable: $db.pocSyncSources,
      getReferencedColumn: (t) => t.sourceKey,
      builder:
          (
            joinBuilder, {
            $addJoinBuilderToRootComposer,
            $removeJoinBuilderFromRootComposer,
          }) => $$PocSyncSourcesTableAnnotationComposer(
            $db: $db,
            $table: $db.pocSyncSources,
            $addJoinBuilderToRootComposer: $addJoinBuilderToRootComposer,
            joinBuilder: joinBuilder,
            $removeJoinBuilderFromRootComposer:
                $removeJoinBuilderFromRootComposer,
          ),
    );
    return composer;
  }

  $$LocalReceivingSessionsTableAnnotationComposer get localSessionId {
    final $$LocalReceivingSessionsTableAnnotationComposer composer =
        $composerBuilder(
          composer: this,
          getCurrentColumn: (t) => t.localSessionId,
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

class $$ReceivingSessionCloudStatesTableTableManager
    extends
        RootTableManager<
          _$TraderProLocalDatabase,
          $ReceivingSessionCloudStatesTable,
          ReceivingSessionCloudStateRow,
          $$ReceivingSessionCloudStatesTableFilterComposer,
          $$ReceivingSessionCloudStatesTableOrderingComposer,
          $$ReceivingSessionCloudStatesTableAnnotationComposer,
          $$ReceivingSessionCloudStatesTableCreateCompanionBuilder,
          $$ReceivingSessionCloudStatesTableUpdateCompanionBuilder,
          (
            ReceivingSessionCloudStateRow,
            $$ReceivingSessionCloudStatesTableReferences,
          ),
          ReceivingSessionCloudStateRow,
          PrefetchHooks Function({bool sourceKey, bool localSessionId})
        > {
  $$ReceivingSessionCloudStatesTableTableManager(
    _$TraderProLocalDatabase db,
    $ReceivingSessionCloudStatesTable table,
  ) : super(
        TableManagerState(
          db: db,
          table: table,
          createFilteringComposer: () =>
              $$ReceivingSessionCloudStatesTableFilterComposer(
                $db: db,
                $table: table,
              ),
          createOrderingComposer: () =>
              $$ReceivingSessionCloudStatesTableOrderingComposer(
                $db: db,
                $table: table,
              ),
          createComputedFieldComposer: () =>
              $$ReceivingSessionCloudStatesTableAnnotationComposer(
                $db: db,
                $table: table,
              ),
          updateCompanionCallback:
              ({
                Value<String> sourceKey = const Value.absent(),
                Value<String> boundDeviceId = const Value.absent(),
                Value<String> localSessionId = const Value.absent(),
                Value<String> cloudSessionId = const Value.absent(),
                Value<String> cloudReference = const Value.absent(),
                Value<String> cloudStatus = const Value.absent(),
                Value<int> cloudVersion = const Value.absent(),
                Value<String?> leaseId = const Value.absent(),
                Value<String?> leaseExpiresAtUtc = const Value.absent(),
                Value<String?> editorDeviceId = const Value.absent(),
                Value<String> lastSuccessfulSyncAtUtc = const Value.absent(),
                Value<String> lastCloudUpdateAtUtc = const Value.absent(),
                Value<String?> lastErrorCode = const Value.absent(),
                Value<String?> lastErrorMessage = const Value.absent(),
                Value<int> rowid = const Value.absent(),
              }) => ReceivingSessionCloudStatesCompanion(
                sourceKey: sourceKey,
                boundDeviceId: boundDeviceId,
                localSessionId: localSessionId,
                cloudSessionId: cloudSessionId,
                cloudReference: cloudReference,
                cloudStatus: cloudStatus,
                cloudVersion: cloudVersion,
                leaseId: leaseId,
                leaseExpiresAtUtc: leaseExpiresAtUtc,
                editorDeviceId: editorDeviceId,
                lastSuccessfulSyncAtUtc: lastSuccessfulSyncAtUtc,
                lastCloudUpdateAtUtc: lastCloudUpdateAtUtc,
                lastErrorCode: lastErrorCode,
                lastErrorMessage: lastErrorMessage,
                rowid: rowid,
              ),
          createCompanionCallback:
              ({
                required String sourceKey,
                required String boundDeviceId,
                required String localSessionId,
                required String cloudSessionId,
                required String cloudReference,
                required String cloudStatus,
                required int cloudVersion,
                Value<String?> leaseId = const Value.absent(),
                Value<String?> leaseExpiresAtUtc = const Value.absent(),
                Value<String?> editorDeviceId = const Value.absent(),
                required String lastSuccessfulSyncAtUtc,
                required String lastCloudUpdateAtUtc,
                Value<String?> lastErrorCode = const Value.absent(),
                Value<String?> lastErrorMessage = const Value.absent(),
                Value<int> rowid = const Value.absent(),
              }) => ReceivingSessionCloudStatesCompanion.insert(
                sourceKey: sourceKey,
                boundDeviceId: boundDeviceId,
                localSessionId: localSessionId,
                cloudSessionId: cloudSessionId,
                cloudReference: cloudReference,
                cloudStatus: cloudStatus,
                cloudVersion: cloudVersion,
                leaseId: leaseId,
                leaseExpiresAtUtc: leaseExpiresAtUtc,
                editorDeviceId: editorDeviceId,
                lastSuccessfulSyncAtUtc: lastSuccessfulSyncAtUtc,
                lastCloudUpdateAtUtc: lastCloudUpdateAtUtc,
                lastErrorCode: lastErrorCode,
                lastErrorMessage: lastErrorMessage,
                rowid: rowid,
              ),
          withReferenceMapper: (p0) => p0
              .map(
                (e) => (
                  e.readTable(table),
                  $$ReceivingSessionCloudStatesTableReferences(db, table, e),
                ),
              )
              .toList(),
          prefetchHooksCallback: ({sourceKey = false, localSessionId = false}) {
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
                    if (sourceKey) {
                      state =
                          state.withJoin(
                                currentTable: table,
                                currentColumn: table.sourceKey,
                                referencedTable:
                                    $$ReceivingSessionCloudStatesTableReferences
                                        ._sourceKeyTable(db),
                                referencedColumn:
                                    $$ReceivingSessionCloudStatesTableReferences
                                        ._sourceKeyTable(db)
                                        .sourceKey,
                              )
                              as T;
                    }
                    if (localSessionId) {
                      state =
                          state.withJoin(
                                currentTable: table,
                                currentColumn: table.localSessionId,
                                referencedTable:
                                    $$ReceivingSessionCloudStatesTableReferences
                                        ._localSessionIdTable(db),
                                referencedColumn:
                                    $$ReceivingSessionCloudStatesTableReferences
                                        ._localSessionIdTable(db)
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

typedef $$ReceivingSessionCloudStatesTableProcessedTableManager =
    ProcessedTableManager<
      _$TraderProLocalDatabase,
      $ReceivingSessionCloudStatesTable,
      ReceivingSessionCloudStateRow,
      $$ReceivingSessionCloudStatesTableFilterComposer,
      $$ReceivingSessionCloudStatesTableOrderingComposer,
      $$ReceivingSessionCloudStatesTableAnnotationComposer,
      $$ReceivingSessionCloudStatesTableCreateCompanionBuilder,
      $$ReceivingSessionCloudStatesTableUpdateCompanionBuilder,
      (
        ReceivingSessionCloudStateRow,
        $$ReceivingSessionCloudStatesTableReferences,
      ),
      ReceivingSessionCloudStateRow,
      PrefetchHooks Function({bool sourceKey, bool localSessionId})
    >;
typedef $$MobileSyncEventInboxTableCreateCompanionBuilder =
    MobileSyncEventInboxCompanion Function({
      required String sourceKey,
      required int eventSequence,
      required String eventId,
      required String eventType,
      required int eventVersion,
      required String aggregateType,
      required String aggregateId,
      required int aggregateVersion,
      required String occurredAtUtc,
      required String correlationId,
      required String payloadJson,
      required String receivedAtUtc,
      Value<String?> appliedAtUtc,
      required String applyStatus,
      Value<int> rowid,
    });
typedef $$MobileSyncEventInboxTableUpdateCompanionBuilder =
    MobileSyncEventInboxCompanion Function({
      Value<String> sourceKey,
      Value<int> eventSequence,
      Value<String> eventId,
      Value<String> eventType,
      Value<int> eventVersion,
      Value<String> aggregateType,
      Value<String> aggregateId,
      Value<int> aggregateVersion,
      Value<String> occurredAtUtc,
      Value<String> correlationId,
      Value<String> payloadJson,
      Value<String> receivedAtUtc,
      Value<String?> appliedAtUtc,
      Value<String> applyStatus,
      Value<int> rowid,
    });

final class $$MobileSyncEventInboxTableReferences
    extends
        BaseReferences<
          _$TraderProLocalDatabase,
          $MobileSyncEventInboxTable,
          MobileSyncEventInboxRow
        > {
  $$MobileSyncEventInboxTableReferences(
    super.$_db,
    super.$_table,
    super.$_typedResult,
  );

  static $PocSyncSourcesTable _sourceKeyTable(_$TraderProLocalDatabase db) =>
      db.pocSyncSources.createAlias(
        'mobile_sync_event_inbox__source_key__poc_sync_sources__source_key',
      );

  $$PocSyncSourcesTableProcessedTableManager get sourceKey {
    final $_column = $_itemColumn<String>('source_key')!;

    final manager = $$PocSyncSourcesTableTableManager(
      $_db,
      $_db.pocSyncSources,
    ).filter((f) => f.sourceKey.sqlEquals($_column));
    final item = $_typedResult.readTableOrNull(_sourceKeyTable($_db));
    if (item == null) return manager;
    return ProcessedTableManager(
      manager.$state.copyWith(prefetchedData: [item]),
    );
  }
}

class $$MobileSyncEventInboxTableFilterComposer
    extends Composer<_$TraderProLocalDatabase, $MobileSyncEventInboxTable> {
  $$MobileSyncEventInboxTableFilterComposer({
    required super.$db,
    required super.$table,
    super.joinBuilder,
    super.$addJoinBuilderToRootComposer,
    super.$removeJoinBuilderFromRootComposer,
  });
  ColumnFilters<int> get eventSequence => $composableBuilder(
    column: $table.eventSequence,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get eventId => $composableBuilder(
    column: $table.eventId,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get eventType => $composableBuilder(
    column: $table.eventType,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<int> get eventVersion => $composableBuilder(
    column: $table.eventVersion,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get aggregateType => $composableBuilder(
    column: $table.aggregateType,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get aggregateId => $composableBuilder(
    column: $table.aggregateId,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<int> get aggregateVersion => $composableBuilder(
    column: $table.aggregateVersion,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get occurredAtUtc => $composableBuilder(
    column: $table.occurredAtUtc,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get correlationId => $composableBuilder(
    column: $table.correlationId,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get payloadJson => $composableBuilder(
    column: $table.payloadJson,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get receivedAtUtc => $composableBuilder(
    column: $table.receivedAtUtc,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get appliedAtUtc => $composableBuilder(
    column: $table.appliedAtUtc,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get applyStatus => $composableBuilder(
    column: $table.applyStatus,
    builder: (column) => ColumnFilters(column),
  );

  $$PocSyncSourcesTableFilterComposer get sourceKey {
    final $$PocSyncSourcesTableFilterComposer composer = $composerBuilder(
      composer: this,
      getCurrentColumn: (t) => t.sourceKey,
      referencedTable: $db.pocSyncSources,
      getReferencedColumn: (t) => t.sourceKey,
      builder:
          (
            joinBuilder, {
            $addJoinBuilderToRootComposer,
            $removeJoinBuilderFromRootComposer,
          }) => $$PocSyncSourcesTableFilterComposer(
            $db: $db,
            $table: $db.pocSyncSources,
            $addJoinBuilderToRootComposer: $addJoinBuilderToRootComposer,
            joinBuilder: joinBuilder,
            $removeJoinBuilderFromRootComposer:
                $removeJoinBuilderFromRootComposer,
          ),
    );
    return composer;
  }
}

class $$MobileSyncEventInboxTableOrderingComposer
    extends Composer<_$TraderProLocalDatabase, $MobileSyncEventInboxTable> {
  $$MobileSyncEventInboxTableOrderingComposer({
    required super.$db,
    required super.$table,
    super.joinBuilder,
    super.$addJoinBuilderToRootComposer,
    super.$removeJoinBuilderFromRootComposer,
  });
  ColumnOrderings<int> get eventSequence => $composableBuilder(
    column: $table.eventSequence,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get eventId => $composableBuilder(
    column: $table.eventId,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get eventType => $composableBuilder(
    column: $table.eventType,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<int> get eventVersion => $composableBuilder(
    column: $table.eventVersion,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get aggregateType => $composableBuilder(
    column: $table.aggregateType,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get aggregateId => $composableBuilder(
    column: $table.aggregateId,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<int> get aggregateVersion => $composableBuilder(
    column: $table.aggregateVersion,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get occurredAtUtc => $composableBuilder(
    column: $table.occurredAtUtc,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get correlationId => $composableBuilder(
    column: $table.correlationId,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get payloadJson => $composableBuilder(
    column: $table.payloadJson,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get receivedAtUtc => $composableBuilder(
    column: $table.receivedAtUtc,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get appliedAtUtc => $composableBuilder(
    column: $table.appliedAtUtc,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get applyStatus => $composableBuilder(
    column: $table.applyStatus,
    builder: (column) => ColumnOrderings(column),
  );

  $$PocSyncSourcesTableOrderingComposer get sourceKey {
    final $$PocSyncSourcesTableOrderingComposer composer = $composerBuilder(
      composer: this,
      getCurrentColumn: (t) => t.sourceKey,
      referencedTable: $db.pocSyncSources,
      getReferencedColumn: (t) => t.sourceKey,
      builder:
          (
            joinBuilder, {
            $addJoinBuilderToRootComposer,
            $removeJoinBuilderFromRootComposer,
          }) => $$PocSyncSourcesTableOrderingComposer(
            $db: $db,
            $table: $db.pocSyncSources,
            $addJoinBuilderToRootComposer: $addJoinBuilderToRootComposer,
            joinBuilder: joinBuilder,
            $removeJoinBuilderFromRootComposer:
                $removeJoinBuilderFromRootComposer,
          ),
    );
    return composer;
  }
}

class $$MobileSyncEventInboxTableAnnotationComposer
    extends Composer<_$TraderProLocalDatabase, $MobileSyncEventInboxTable> {
  $$MobileSyncEventInboxTableAnnotationComposer({
    required super.$db,
    required super.$table,
    super.joinBuilder,
    super.$addJoinBuilderToRootComposer,
    super.$removeJoinBuilderFromRootComposer,
  });
  GeneratedColumn<int> get eventSequence => $composableBuilder(
    column: $table.eventSequence,
    builder: (column) => column,
  );

  GeneratedColumn<String> get eventId =>
      $composableBuilder(column: $table.eventId, builder: (column) => column);

  GeneratedColumn<String> get eventType =>
      $composableBuilder(column: $table.eventType, builder: (column) => column);

  GeneratedColumn<int> get eventVersion => $composableBuilder(
    column: $table.eventVersion,
    builder: (column) => column,
  );

  GeneratedColumn<String> get aggregateType => $composableBuilder(
    column: $table.aggregateType,
    builder: (column) => column,
  );

  GeneratedColumn<String> get aggregateId => $composableBuilder(
    column: $table.aggregateId,
    builder: (column) => column,
  );

  GeneratedColumn<int> get aggregateVersion => $composableBuilder(
    column: $table.aggregateVersion,
    builder: (column) => column,
  );

  GeneratedColumn<String> get occurredAtUtc => $composableBuilder(
    column: $table.occurredAtUtc,
    builder: (column) => column,
  );

  GeneratedColumn<String> get correlationId => $composableBuilder(
    column: $table.correlationId,
    builder: (column) => column,
  );

  GeneratedColumn<String> get payloadJson => $composableBuilder(
    column: $table.payloadJson,
    builder: (column) => column,
  );

  GeneratedColumn<String> get receivedAtUtc => $composableBuilder(
    column: $table.receivedAtUtc,
    builder: (column) => column,
  );

  GeneratedColumn<String> get appliedAtUtc => $composableBuilder(
    column: $table.appliedAtUtc,
    builder: (column) => column,
  );

  GeneratedColumn<String> get applyStatus => $composableBuilder(
    column: $table.applyStatus,
    builder: (column) => column,
  );

  $$PocSyncSourcesTableAnnotationComposer get sourceKey {
    final $$PocSyncSourcesTableAnnotationComposer composer = $composerBuilder(
      composer: this,
      getCurrentColumn: (t) => t.sourceKey,
      referencedTable: $db.pocSyncSources,
      getReferencedColumn: (t) => t.sourceKey,
      builder:
          (
            joinBuilder, {
            $addJoinBuilderToRootComposer,
            $removeJoinBuilderFromRootComposer,
          }) => $$PocSyncSourcesTableAnnotationComposer(
            $db: $db,
            $table: $db.pocSyncSources,
            $addJoinBuilderToRootComposer: $addJoinBuilderToRootComposer,
            joinBuilder: joinBuilder,
            $removeJoinBuilderFromRootComposer:
                $removeJoinBuilderFromRootComposer,
          ),
    );
    return composer;
  }
}

class $$MobileSyncEventInboxTableTableManager
    extends
        RootTableManager<
          _$TraderProLocalDatabase,
          $MobileSyncEventInboxTable,
          MobileSyncEventInboxRow,
          $$MobileSyncEventInboxTableFilterComposer,
          $$MobileSyncEventInboxTableOrderingComposer,
          $$MobileSyncEventInboxTableAnnotationComposer,
          $$MobileSyncEventInboxTableCreateCompanionBuilder,
          $$MobileSyncEventInboxTableUpdateCompanionBuilder,
          (MobileSyncEventInboxRow, $$MobileSyncEventInboxTableReferences),
          MobileSyncEventInboxRow,
          PrefetchHooks Function({bool sourceKey})
        > {
  $$MobileSyncEventInboxTableTableManager(
    _$TraderProLocalDatabase db,
    $MobileSyncEventInboxTable table,
  ) : super(
        TableManagerState(
          db: db,
          table: table,
          createFilteringComposer: () =>
              $$MobileSyncEventInboxTableFilterComposer($db: db, $table: table),
          createOrderingComposer: () =>
              $$MobileSyncEventInboxTableOrderingComposer(
                $db: db,
                $table: table,
              ),
          createComputedFieldComposer: () =>
              $$MobileSyncEventInboxTableAnnotationComposer(
                $db: db,
                $table: table,
              ),
          updateCompanionCallback:
              ({
                Value<String> sourceKey = const Value.absent(),
                Value<int> eventSequence = const Value.absent(),
                Value<String> eventId = const Value.absent(),
                Value<String> eventType = const Value.absent(),
                Value<int> eventVersion = const Value.absent(),
                Value<String> aggregateType = const Value.absent(),
                Value<String> aggregateId = const Value.absent(),
                Value<int> aggregateVersion = const Value.absent(),
                Value<String> occurredAtUtc = const Value.absent(),
                Value<String> correlationId = const Value.absent(),
                Value<String> payloadJson = const Value.absent(),
                Value<String> receivedAtUtc = const Value.absent(),
                Value<String?> appliedAtUtc = const Value.absent(),
                Value<String> applyStatus = const Value.absent(),
                Value<int> rowid = const Value.absent(),
              }) => MobileSyncEventInboxCompanion(
                sourceKey: sourceKey,
                eventSequence: eventSequence,
                eventId: eventId,
                eventType: eventType,
                eventVersion: eventVersion,
                aggregateType: aggregateType,
                aggregateId: aggregateId,
                aggregateVersion: aggregateVersion,
                occurredAtUtc: occurredAtUtc,
                correlationId: correlationId,
                payloadJson: payloadJson,
                receivedAtUtc: receivedAtUtc,
                appliedAtUtc: appliedAtUtc,
                applyStatus: applyStatus,
                rowid: rowid,
              ),
          createCompanionCallback:
              ({
                required String sourceKey,
                required int eventSequence,
                required String eventId,
                required String eventType,
                required int eventVersion,
                required String aggregateType,
                required String aggregateId,
                required int aggregateVersion,
                required String occurredAtUtc,
                required String correlationId,
                required String payloadJson,
                required String receivedAtUtc,
                Value<String?> appliedAtUtc = const Value.absent(),
                required String applyStatus,
                Value<int> rowid = const Value.absent(),
              }) => MobileSyncEventInboxCompanion.insert(
                sourceKey: sourceKey,
                eventSequence: eventSequence,
                eventId: eventId,
                eventType: eventType,
                eventVersion: eventVersion,
                aggregateType: aggregateType,
                aggregateId: aggregateId,
                aggregateVersion: aggregateVersion,
                occurredAtUtc: occurredAtUtc,
                correlationId: correlationId,
                payloadJson: payloadJson,
                receivedAtUtc: receivedAtUtc,
                appliedAtUtc: appliedAtUtc,
                applyStatus: applyStatus,
                rowid: rowid,
              ),
          withReferenceMapper: (p0) => p0
              .map(
                (e) => (
                  e.readTable(table),
                  $$MobileSyncEventInboxTableReferences(db, table, e),
                ),
              )
              .toList(),
          prefetchHooksCallback: ({sourceKey = false}) {
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
                    if (sourceKey) {
                      state =
                          state.withJoin(
                                currentTable: table,
                                currentColumn: table.sourceKey,
                                referencedTable:
                                    $$MobileSyncEventInboxTableReferences
                                        ._sourceKeyTable(db),
                                referencedColumn:
                                    $$MobileSyncEventInboxTableReferences
                                        ._sourceKeyTable(db)
                                        .sourceKey,
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

typedef $$MobileSyncEventInboxTableProcessedTableManager =
    ProcessedTableManager<
      _$TraderProLocalDatabase,
      $MobileSyncEventInboxTable,
      MobileSyncEventInboxRow,
      $$MobileSyncEventInboxTableFilterComposer,
      $$MobileSyncEventInboxTableOrderingComposer,
      $$MobileSyncEventInboxTableAnnotationComposer,
      $$MobileSyncEventInboxTableCreateCompanionBuilder,
      $$MobileSyncEventInboxTableUpdateCompanionBuilder,
      (MobileSyncEventInboxRow, $$MobileSyncEventInboxTableReferences),
      MobileSyncEventInboxRow,
      PrefetchHooks Function({bool sourceKey})
    >;
typedef $$RemoteReceivingSessionProjectionsTableCreateCompanionBuilder =
    RemoteReceivingSessionProjectionsCompanion Function({
      required String sourceKey,
      required String sessionId,
      required String cloudReference,
      required String status,
      required String editorDeviceId,
      Value<String?> leaseExpiresAtUtc,
      required int entryCount,
      required String processedTotalWeightKg,
      required int cloudVersion,
      Value<String?> approvedByDeviceId,
      Value<String?> finalizationId,
      required String lastCloudUpdateAtUtc,
      Value<int> rowid,
    });
typedef $$RemoteReceivingSessionProjectionsTableUpdateCompanionBuilder =
    RemoteReceivingSessionProjectionsCompanion Function({
      Value<String> sourceKey,
      Value<String> sessionId,
      Value<String> cloudReference,
      Value<String> status,
      Value<String> editorDeviceId,
      Value<String?> leaseExpiresAtUtc,
      Value<int> entryCount,
      Value<String> processedTotalWeightKg,
      Value<int> cloudVersion,
      Value<String?> approvedByDeviceId,
      Value<String?> finalizationId,
      Value<String> lastCloudUpdateAtUtc,
      Value<int> rowid,
    });

final class $$RemoteReceivingSessionProjectionsTableReferences
    extends
        BaseReferences<
          _$TraderProLocalDatabase,
          $RemoteReceivingSessionProjectionsTable,
          RemoteReceivingSessionProjectionRow
        > {
  $$RemoteReceivingSessionProjectionsTableReferences(
    super.$_db,
    super.$_table,
    super.$_typedResult,
  );

  static $PocSyncSourcesTable _sourceKeyTable(
    _$TraderProLocalDatabase db,
  ) => db.pocSyncSources.createAlias(
    'remote_receiving_session_projections__source_key__poc_sync_sources__source_key',
  );

  $$PocSyncSourcesTableProcessedTableManager get sourceKey {
    final $_column = $_itemColumn<String>('source_key')!;

    final manager = $$PocSyncSourcesTableTableManager(
      $_db,
      $_db.pocSyncSources,
    ).filter((f) => f.sourceKey.sqlEquals($_column));
    final item = $_typedResult.readTableOrNull(_sourceKeyTable($_db));
    if (item == null) return manager;
    return ProcessedTableManager(
      manager.$state.copyWith(prefetchedData: [item]),
    );
  }
}

class $$RemoteReceivingSessionProjectionsTableFilterComposer
    extends
        Composer<
          _$TraderProLocalDatabase,
          $RemoteReceivingSessionProjectionsTable
        > {
  $$RemoteReceivingSessionProjectionsTableFilterComposer({
    required super.$db,
    required super.$table,
    super.joinBuilder,
    super.$addJoinBuilderToRootComposer,
    super.$removeJoinBuilderFromRootComposer,
  });
  ColumnFilters<String> get sessionId => $composableBuilder(
    column: $table.sessionId,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get cloudReference => $composableBuilder(
    column: $table.cloudReference,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get status => $composableBuilder(
    column: $table.status,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get editorDeviceId => $composableBuilder(
    column: $table.editorDeviceId,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get leaseExpiresAtUtc => $composableBuilder(
    column: $table.leaseExpiresAtUtc,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<int> get entryCount => $composableBuilder(
    column: $table.entryCount,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get processedTotalWeightKg => $composableBuilder(
    column: $table.processedTotalWeightKg,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<int> get cloudVersion => $composableBuilder(
    column: $table.cloudVersion,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get approvedByDeviceId => $composableBuilder(
    column: $table.approvedByDeviceId,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get finalizationId => $composableBuilder(
    column: $table.finalizationId,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get lastCloudUpdateAtUtc => $composableBuilder(
    column: $table.lastCloudUpdateAtUtc,
    builder: (column) => ColumnFilters(column),
  );

  $$PocSyncSourcesTableFilterComposer get sourceKey {
    final $$PocSyncSourcesTableFilterComposer composer = $composerBuilder(
      composer: this,
      getCurrentColumn: (t) => t.sourceKey,
      referencedTable: $db.pocSyncSources,
      getReferencedColumn: (t) => t.sourceKey,
      builder:
          (
            joinBuilder, {
            $addJoinBuilderToRootComposer,
            $removeJoinBuilderFromRootComposer,
          }) => $$PocSyncSourcesTableFilterComposer(
            $db: $db,
            $table: $db.pocSyncSources,
            $addJoinBuilderToRootComposer: $addJoinBuilderToRootComposer,
            joinBuilder: joinBuilder,
            $removeJoinBuilderFromRootComposer:
                $removeJoinBuilderFromRootComposer,
          ),
    );
    return composer;
  }
}

class $$RemoteReceivingSessionProjectionsTableOrderingComposer
    extends
        Composer<
          _$TraderProLocalDatabase,
          $RemoteReceivingSessionProjectionsTable
        > {
  $$RemoteReceivingSessionProjectionsTableOrderingComposer({
    required super.$db,
    required super.$table,
    super.joinBuilder,
    super.$addJoinBuilderToRootComposer,
    super.$removeJoinBuilderFromRootComposer,
  });
  ColumnOrderings<String> get sessionId => $composableBuilder(
    column: $table.sessionId,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get cloudReference => $composableBuilder(
    column: $table.cloudReference,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get status => $composableBuilder(
    column: $table.status,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get editorDeviceId => $composableBuilder(
    column: $table.editorDeviceId,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get leaseExpiresAtUtc => $composableBuilder(
    column: $table.leaseExpiresAtUtc,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<int> get entryCount => $composableBuilder(
    column: $table.entryCount,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get processedTotalWeightKg => $composableBuilder(
    column: $table.processedTotalWeightKg,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<int> get cloudVersion => $composableBuilder(
    column: $table.cloudVersion,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get approvedByDeviceId => $composableBuilder(
    column: $table.approvedByDeviceId,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get finalizationId => $composableBuilder(
    column: $table.finalizationId,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get lastCloudUpdateAtUtc => $composableBuilder(
    column: $table.lastCloudUpdateAtUtc,
    builder: (column) => ColumnOrderings(column),
  );

  $$PocSyncSourcesTableOrderingComposer get sourceKey {
    final $$PocSyncSourcesTableOrderingComposer composer = $composerBuilder(
      composer: this,
      getCurrentColumn: (t) => t.sourceKey,
      referencedTable: $db.pocSyncSources,
      getReferencedColumn: (t) => t.sourceKey,
      builder:
          (
            joinBuilder, {
            $addJoinBuilderToRootComposer,
            $removeJoinBuilderFromRootComposer,
          }) => $$PocSyncSourcesTableOrderingComposer(
            $db: $db,
            $table: $db.pocSyncSources,
            $addJoinBuilderToRootComposer: $addJoinBuilderToRootComposer,
            joinBuilder: joinBuilder,
            $removeJoinBuilderFromRootComposer:
                $removeJoinBuilderFromRootComposer,
          ),
    );
    return composer;
  }
}

class $$RemoteReceivingSessionProjectionsTableAnnotationComposer
    extends
        Composer<
          _$TraderProLocalDatabase,
          $RemoteReceivingSessionProjectionsTable
        > {
  $$RemoteReceivingSessionProjectionsTableAnnotationComposer({
    required super.$db,
    required super.$table,
    super.joinBuilder,
    super.$addJoinBuilderToRootComposer,
    super.$removeJoinBuilderFromRootComposer,
  });
  GeneratedColumn<String> get sessionId =>
      $composableBuilder(column: $table.sessionId, builder: (column) => column);

  GeneratedColumn<String> get cloudReference => $composableBuilder(
    column: $table.cloudReference,
    builder: (column) => column,
  );

  GeneratedColumn<String> get status =>
      $composableBuilder(column: $table.status, builder: (column) => column);

  GeneratedColumn<String> get editorDeviceId => $composableBuilder(
    column: $table.editorDeviceId,
    builder: (column) => column,
  );

  GeneratedColumn<String> get leaseExpiresAtUtc => $composableBuilder(
    column: $table.leaseExpiresAtUtc,
    builder: (column) => column,
  );

  GeneratedColumn<int> get entryCount => $composableBuilder(
    column: $table.entryCount,
    builder: (column) => column,
  );

  GeneratedColumn<String> get processedTotalWeightKg => $composableBuilder(
    column: $table.processedTotalWeightKg,
    builder: (column) => column,
  );

  GeneratedColumn<int> get cloudVersion => $composableBuilder(
    column: $table.cloudVersion,
    builder: (column) => column,
  );

  GeneratedColumn<String> get approvedByDeviceId => $composableBuilder(
    column: $table.approvedByDeviceId,
    builder: (column) => column,
  );

  GeneratedColumn<String> get finalizationId => $composableBuilder(
    column: $table.finalizationId,
    builder: (column) => column,
  );

  GeneratedColumn<String> get lastCloudUpdateAtUtc => $composableBuilder(
    column: $table.lastCloudUpdateAtUtc,
    builder: (column) => column,
  );

  $$PocSyncSourcesTableAnnotationComposer get sourceKey {
    final $$PocSyncSourcesTableAnnotationComposer composer = $composerBuilder(
      composer: this,
      getCurrentColumn: (t) => t.sourceKey,
      referencedTable: $db.pocSyncSources,
      getReferencedColumn: (t) => t.sourceKey,
      builder:
          (
            joinBuilder, {
            $addJoinBuilderToRootComposer,
            $removeJoinBuilderFromRootComposer,
          }) => $$PocSyncSourcesTableAnnotationComposer(
            $db: $db,
            $table: $db.pocSyncSources,
            $addJoinBuilderToRootComposer: $addJoinBuilderToRootComposer,
            joinBuilder: joinBuilder,
            $removeJoinBuilderFromRootComposer:
                $removeJoinBuilderFromRootComposer,
          ),
    );
    return composer;
  }
}

class $$RemoteReceivingSessionProjectionsTableTableManager
    extends
        RootTableManager<
          _$TraderProLocalDatabase,
          $RemoteReceivingSessionProjectionsTable,
          RemoteReceivingSessionProjectionRow,
          $$RemoteReceivingSessionProjectionsTableFilterComposer,
          $$RemoteReceivingSessionProjectionsTableOrderingComposer,
          $$RemoteReceivingSessionProjectionsTableAnnotationComposer,
          $$RemoteReceivingSessionProjectionsTableCreateCompanionBuilder,
          $$RemoteReceivingSessionProjectionsTableUpdateCompanionBuilder,
          (
            RemoteReceivingSessionProjectionRow,
            $$RemoteReceivingSessionProjectionsTableReferences,
          ),
          RemoteReceivingSessionProjectionRow,
          PrefetchHooks Function({bool sourceKey})
        > {
  $$RemoteReceivingSessionProjectionsTableTableManager(
    _$TraderProLocalDatabase db,
    $RemoteReceivingSessionProjectionsTable table,
  ) : super(
        TableManagerState(
          db: db,
          table: table,
          createFilteringComposer: () =>
              $$RemoteReceivingSessionProjectionsTableFilterComposer(
                $db: db,
                $table: table,
              ),
          createOrderingComposer: () =>
              $$RemoteReceivingSessionProjectionsTableOrderingComposer(
                $db: db,
                $table: table,
              ),
          createComputedFieldComposer: () =>
              $$RemoteReceivingSessionProjectionsTableAnnotationComposer(
                $db: db,
                $table: table,
              ),
          updateCompanionCallback:
              ({
                Value<String> sourceKey = const Value.absent(),
                Value<String> sessionId = const Value.absent(),
                Value<String> cloudReference = const Value.absent(),
                Value<String> status = const Value.absent(),
                Value<String> editorDeviceId = const Value.absent(),
                Value<String?> leaseExpiresAtUtc = const Value.absent(),
                Value<int> entryCount = const Value.absent(),
                Value<String> processedTotalWeightKg = const Value.absent(),
                Value<int> cloudVersion = const Value.absent(),
                Value<String?> approvedByDeviceId = const Value.absent(),
                Value<String?> finalizationId = const Value.absent(),
                Value<String> lastCloudUpdateAtUtc = const Value.absent(),
                Value<int> rowid = const Value.absent(),
              }) => RemoteReceivingSessionProjectionsCompanion(
                sourceKey: sourceKey,
                sessionId: sessionId,
                cloudReference: cloudReference,
                status: status,
                editorDeviceId: editorDeviceId,
                leaseExpiresAtUtc: leaseExpiresAtUtc,
                entryCount: entryCount,
                processedTotalWeightKg: processedTotalWeightKg,
                cloudVersion: cloudVersion,
                approvedByDeviceId: approvedByDeviceId,
                finalizationId: finalizationId,
                lastCloudUpdateAtUtc: lastCloudUpdateAtUtc,
                rowid: rowid,
              ),
          createCompanionCallback:
              ({
                required String sourceKey,
                required String sessionId,
                required String cloudReference,
                required String status,
                required String editorDeviceId,
                Value<String?> leaseExpiresAtUtc = const Value.absent(),
                required int entryCount,
                required String processedTotalWeightKg,
                required int cloudVersion,
                Value<String?> approvedByDeviceId = const Value.absent(),
                Value<String?> finalizationId = const Value.absent(),
                required String lastCloudUpdateAtUtc,
                Value<int> rowid = const Value.absent(),
              }) => RemoteReceivingSessionProjectionsCompanion.insert(
                sourceKey: sourceKey,
                sessionId: sessionId,
                cloudReference: cloudReference,
                status: status,
                editorDeviceId: editorDeviceId,
                leaseExpiresAtUtc: leaseExpiresAtUtc,
                entryCount: entryCount,
                processedTotalWeightKg: processedTotalWeightKg,
                cloudVersion: cloudVersion,
                approvedByDeviceId: approvedByDeviceId,
                finalizationId: finalizationId,
                lastCloudUpdateAtUtc: lastCloudUpdateAtUtc,
                rowid: rowid,
              ),
          withReferenceMapper: (p0) => p0
              .map(
                (e) => (
                  e.readTable(table),
                  $$RemoteReceivingSessionProjectionsTableReferences(
                    db,
                    table,
                    e,
                  ),
                ),
              )
              .toList(),
          prefetchHooksCallback: ({sourceKey = false}) {
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
                    if (sourceKey) {
                      state =
                          state.withJoin(
                                currentTable: table,
                                currentColumn: table.sourceKey,
                                referencedTable:
                                    $$RemoteReceivingSessionProjectionsTableReferences
                                        ._sourceKeyTable(db),
                                referencedColumn:
                                    $$RemoteReceivingSessionProjectionsTableReferences
                                        ._sourceKeyTable(db)
                                        .sourceKey,
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

typedef $$RemoteReceivingSessionProjectionsTableProcessedTableManager =
    ProcessedTableManager<
      _$TraderProLocalDatabase,
      $RemoteReceivingSessionProjectionsTable,
      RemoteReceivingSessionProjectionRow,
      $$RemoteReceivingSessionProjectionsTableFilterComposer,
      $$RemoteReceivingSessionProjectionsTableOrderingComposer,
      $$RemoteReceivingSessionProjectionsTableAnnotationComposer,
      $$RemoteReceivingSessionProjectionsTableCreateCompanionBuilder,
      $$RemoteReceivingSessionProjectionsTableUpdateCompanionBuilder,
      (
        RemoteReceivingSessionProjectionRow,
        $$RemoteReceivingSessionProjectionsTableReferences,
      ),
      RemoteReceivingSessionProjectionRow,
      PrefetchHooks Function({bool sourceKey})
    >;
typedef $$RemoteReceivingEntrySummariesTableCreateCompanionBuilder =
    RemoteReceivingEntrySummariesCompanion Function({
      required String sourceKey,
      required String sessionId,
      required String entryId,
      Value<int?> eventSequence,
      required int localSequence,
      required String productReference,
      required String bagTypeReference,
      required int bagCount,
      Value<String?> rawWeightKg,
      required String processedWeightKg,
      Value<String?> displayWeightKg,
      Value<int?> decimalPlaces,
      Value<String?> processingMethod,
      Value<String?> weightSource,
      Value<String?> capturedAtDeviceUtc,
      Value<String?> acceptedAtServerUtc,
      Value<int> rowid,
    });
typedef $$RemoteReceivingEntrySummariesTableUpdateCompanionBuilder =
    RemoteReceivingEntrySummariesCompanion Function({
      Value<String> sourceKey,
      Value<String> sessionId,
      Value<String> entryId,
      Value<int?> eventSequence,
      Value<int> localSequence,
      Value<String> productReference,
      Value<String> bagTypeReference,
      Value<int> bagCount,
      Value<String?> rawWeightKg,
      Value<String> processedWeightKg,
      Value<String?> displayWeightKg,
      Value<int?> decimalPlaces,
      Value<String?> processingMethod,
      Value<String?> weightSource,
      Value<String?> capturedAtDeviceUtc,
      Value<String?> acceptedAtServerUtc,
      Value<int> rowid,
    });

class $$RemoteReceivingEntrySummariesTableFilterComposer
    extends
        Composer<
          _$TraderProLocalDatabase,
          $RemoteReceivingEntrySummariesTable
        > {
  $$RemoteReceivingEntrySummariesTableFilterComposer({
    required super.$db,
    required super.$table,
    super.joinBuilder,
    super.$addJoinBuilderToRootComposer,
    super.$removeJoinBuilderFromRootComposer,
  });
  ColumnFilters<String> get sourceKey => $composableBuilder(
    column: $table.sourceKey,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get sessionId => $composableBuilder(
    column: $table.sessionId,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get entryId => $composableBuilder(
    column: $table.entryId,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<int> get eventSequence => $composableBuilder(
    column: $table.eventSequence,
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

  ColumnFilters<String> get capturedAtDeviceUtc => $composableBuilder(
    column: $table.capturedAtDeviceUtc,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get acceptedAtServerUtc => $composableBuilder(
    column: $table.acceptedAtServerUtc,
    builder: (column) => ColumnFilters(column),
  );
}

class $$RemoteReceivingEntrySummariesTableOrderingComposer
    extends
        Composer<
          _$TraderProLocalDatabase,
          $RemoteReceivingEntrySummariesTable
        > {
  $$RemoteReceivingEntrySummariesTableOrderingComposer({
    required super.$db,
    required super.$table,
    super.joinBuilder,
    super.$addJoinBuilderToRootComposer,
    super.$removeJoinBuilderFromRootComposer,
  });
  ColumnOrderings<String> get sourceKey => $composableBuilder(
    column: $table.sourceKey,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get sessionId => $composableBuilder(
    column: $table.sessionId,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get entryId => $composableBuilder(
    column: $table.entryId,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<int> get eventSequence => $composableBuilder(
    column: $table.eventSequence,
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

  ColumnOrderings<String> get capturedAtDeviceUtc => $composableBuilder(
    column: $table.capturedAtDeviceUtc,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get acceptedAtServerUtc => $composableBuilder(
    column: $table.acceptedAtServerUtc,
    builder: (column) => ColumnOrderings(column),
  );
}

class $$RemoteReceivingEntrySummariesTableAnnotationComposer
    extends
        Composer<
          _$TraderProLocalDatabase,
          $RemoteReceivingEntrySummariesTable
        > {
  $$RemoteReceivingEntrySummariesTableAnnotationComposer({
    required super.$db,
    required super.$table,
    super.joinBuilder,
    super.$addJoinBuilderToRootComposer,
    super.$removeJoinBuilderFromRootComposer,
  });
  GeneratedColumn<String> get sourceKey =>
      $composableBuilder(column: $table.sourceKey, builder: (column) => column);

  GeneratedColumn<String> get sessionId =>
      $composableBuilder(column: $table.sessionId, builder: (column) => column);

  GeneratedColumn<String> get entryId =>
      $composableBuilder(column: $table.entryId, builder: (column) => column);

  GeneratedColumn<int> get eventSequence => $composableBuilder(
    column: $table.eventSequence,
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

  GeneratedColumn<String> get capturedAtDeviceUtc => $composableBuilder(
    column: $table.capturedAtDeviceUtc,
    builder: (column) => column,
  );

  GeneratedColumn<String> get acceptedAtServerUtc => $composableBuilder(
    column: $table.acceptedAtServerUtc,
    builder: (column) => column,
  );
}

class $$RemoteReceivingEntrySummariesTableTableManager
    extends
        RootTableManager<
          _$TraderProLocalDatabase,
          $RemoteReceivingEntrySummariesTable,
          RemoteReceivingEntrySummaryRow,
          $$RemoteReceivingEntrySummariesTableFilterComposer,
          $$RemoteReceivingEntrySummariesTableOrderingComposer,
          $$RemoteReceivingEntrySummariesTableAnnotationComposer,
          $$RemoteReceivingEntrySummariesTableCreateCompanionBuilder,
          $$RemoteReceivingEntrySummariesTableUpdateCompanionBuilder,
          (
            RemoteReceivingEntrySummaryRow,
            BaseReferences<
              _$TraderProLocalDatabase,
              $RemoteReceivingEntrySummariesTable,
              RemoteReceivingEntrySummaryRow
            >,
          ),
          RemoteReceivingEntrySummaryRow,
          PrefetchHooks Function()
        > {
  $$RemoteReceivingEntrySummariesTableTableManager(
    _$TraderProLocalDatabase db,
    $RemoteReceivingEntrySummariesTable table,
  ) : super(
        TableManagerState(
          db: db,
          table: table,
          createFilteringComposer: () =>
              $$RemoteReceivingEntrySummariesTableFilterComposer(
                $db: db,
                $table: table,
              ),
          createOrderingComposer: () =>
              $$RemoteReceivingEntrySummariesTableOrderingComposer(
                $db: db,
                $table: table,
              ),
          createComputedFieldComposer: () =>
              $$RemoteReceivingEntrySummariesTableAnnotationComposer(
                $db: db,
                $table: table,
              ),
          updateCompanionCallback:
              ({
                Value<String> sourceKey = const Value.absent(),
                Value<String> sessionId = const Value.absent(),
                Value<String> entryId = const Value.absent(),
                Value<int?> eventSequence = const Value.absent(),
                Value<int> localSequence = const Value.absent(),
                Value<String> productReference = const Value.absent(),
                Value<String> bagTypeReference = const Value.absent(),
                Value<int> bagCount = const Value.absent(),
                Value<String?> rawWeightKg = const Value.absent(),
                Value<String> processedWeightKg = const Value.absent(),
                Value<String?> displayWeightKg = const Value.absent(),
                Value<int?> decimalPlaces = const Value.absent(),
                Value<String?> processingMethod = const Value.absent(),
                Value<String?> weightSource = const Value.absent(),
                Value<String?> capturedAtDeviceUtc = const Value.absent(),
                Value<String?> acceptedAtServerUtc = const Value.absent(),
                Value<int> rowid = const Value.absent(),
              }) => RemoteReceivingEntrySummariesCompanion(
                sourceKey: sourceKey,
                sessionId: sessionId,
                entryId: entryId,
                eventSequence: eventSequence,
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
                capturedAtDeviceUtc: capturedAtDeviceUtc,
                acceptedAtServerUtc: acceptedAtServerUtc,
                rowid: rowid,
              ),
          createCompanionCallback:
              ({
                required String sourceKey,
                required String sessionId,
                required String entryId,
                Value<int?> eventSequence = const Value.absent(),
                required int localSequence,
                required String productReference,
                required String bagTypeReference,
                required int bagCount,
                Value<String?> rawWeightKg = const Value.absent(),
                required String processedWeightKg,
                Value<String?> displayWeightKg = const Value.absent(),
                Value<int?> decimalPlaces = const Value.absent(),
                Value<String?> processingMethod = const Value.absent(),
                Value<String?> weightSource = const Value.absent(),
                Value<String?> capturedAtDeviceUtc = const Value.absent(),
                Value<String?> acceptedAtServerUtc = const Value.absent(),
                Value<int> rowid = const Value.absent(),
              }) => RemoteReceivingEntrySummariesCompanion.insert(
                sourceKey: sourceKey,
                sessionId: sessionId,
                entryId: entryId,
                eventSequence: eventSequence,
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
                capturedAtDeviceUtc: capturedAtDeviceUtc,
                acceptedAtServerUtc: acceptedAtServerUtc,
                rowid: rowid,
              ),
          withReferenceMapper: (p0) => p0
              .map((e) => (e.readTable(table), BaseReferences(db, table, e)))
              .toList(),
          prefetchHooksCallback: null,
        ),
      );
}

typedef $$RemoteReceivingEntrySummariesTableProcessedTableManager =
    ProcessedTableManager<
      _$TraderProLocalDatabase,
      $RemoteReceivingEntrySummariesTable,
      RemoteReceivingEntrySummaryRow,
      $$RemoteReceivingEntrySummariesTableFilterComposer,
      $$RemoteReceivingEntrySummariesTableOrderingComposer,
      $$RemoteReceivingEntrySummariesTableAnnotationComposer,
      $$RemoteReceivingEntrySummariesTableCreateCompanionBuilder,
      $$RemoteReceivingEntrySummariesTableUpdateCompanionBuilder,
      (
        RemoteReceivingEntrySummaryRow,
        BaseReferences<
          _$TraderProLocalDatabase,
          $RemoteReceivingEntrySummariesTable,
          RemoteReceivingEntrySummaryRow
        >,
      ),
      RemoteReceivingEntrySummaryRow,
      PrefetchHooks Function()
    >;
typedef $$PocControlCommandsTableCreateCompanionBuilder =
    PocControlCommandsCompanion Function({
      required String commandId,
      required String sourceKey,
      required String actingDeviceId,
      required String commandType,
      required String sessionId,
      Value<int?> expectedCloudVersion,
      Value<String?> leaseId,
      required String status,
      required int attemptCount,
      Value<String?> nextAttemptAtUtc,
      Value<String?> lastAttemptAtUtc,
      Value<String?> lastErrorCode,
      Value<String?> lastErrorMessage,
      Value<String?> successfulResponseJson,
      required String createdAtUtc,
      required String updatedAtUtc,
      Value<int> rowid,
    });
typedef $$PocControlCommandsTableUpdateCompanionBuilder =
    PocControlCommandsCompanion Function({
      Value<String> commandId,
      Value<String> sourceKey,
      Value<String> actingDeviceId,
      Value<String> commandType,
      Value<String> sessionId,
      Value<int?> expectedCloudVersion,
      Value<String?> leaseId,
      Value<String> status,
      Value<int> attemptCount,
      Value<String?> nextAttemptAtUtc,
      Value<String?> lastAttemptAtUtc,
      Value<String?> lastErrorCode,
      Value<String?> lastErrorMessage,
      Value<String?> successfulResponseJson,
      Value<String> createdAtUtc,
      Value<String> updatedAtUtc,
      Value<int> rowid,
    });

final class $$PocControlCommandsTableReferences
    extends
        BaseReferences<
          _$TraderProLocalDatabase,
          $PocControlCommandsTable,
          PocControlCommandRow
        > {
  $$PocControlCommandsTableReferences(
    super.$_db,
    super.$_table,
    super.$_typedResult,
  );

  static $PocSyncSourcesTable _sourceKeyTable(_$TraderProLocalDatabase db) =>
      db.pocSyncSources.createAlias(
        'poc_control_commands__source_key__poc_sync_sources__source_key',
      );

  $$PocSyncSourcesTableProcessedTableManager get sourceKey {
    final $_column = $_itemColumn<String>('source_key')!;

    final manager = $$PocSyncSourcesTableTableManager(
      $_db,
      $_db.pocSyncSources,
    ).filter((f) => f.sourceKey.sqlEquals($_column));
    final item = $_typedResult.readTableOrNull(_sourceKeyTable($_db));
    if (item == null) return manager;
    return ProcessedTableManager(
      manager.$state.copyWith(prefetchedData: [item]),
    );
  }
}

class $$PocControlCommandsTableFilterComposer
    extends Composer<_$TraderProLocalDatabase, $PocControlCommandsTable> {
  $$PocControlCommandsTableFilterComposer({
    required super.$db,
    required super.$table,
    super.joinBuilder,
    super.$addJoinBuilderToRootComposer,
    super.$removeJoinBuilderFromRootComposer,
  });
  ColumnFilters<String> get commandId => $composableBuilder(
    column: $table.commandId,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get actingDeviceId => $composableBuilder(
    column: $table.actingDeviceId,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get commandType => $composableBuilder(
    column: $table.commandType,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get sessionId => $composableBuilder(
    column: $table.sessionId,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<int> get expectedCloudVersion => $composableBuilder(
    column: $table.expectedCloudVersion,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get leaseId => $composableBuilder(
    column: $table.leaseId,
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

  ColumnFilters<String> get nextAttemptAtUtc => $composableBuilder(
    column: $table.nextAttemptAtUtc,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get lastAttemptAtUtc => $composableBuilder(
    column: $table.lastAttemptAtUtc,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get lastErrorCode => $composableBuilder(
    column: $table.lastErrorCode,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get lastErrorMessage => $composableBuilder(
    column: $table.lastErrorMessage,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get successfulResponseJson => $composableBuilder(
    column: $table.successfulResponseJson,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get createdAtUtc => $composableBuilder(
    column: $table.createdAtUtc,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get updatedAtUtc => $composableBuilder(
    column: $table.updatedAtUtc,
    builder: (column) => ColumnFilters(column),
  );

  $$PocSyncSourcesTableFilterComposer get sourceKey {
    final $$PocSyncSourcesTableFilterComposer composer = $composerBuilder(
      composer: this,
      getCurrentColumn: (t) => t.sourceKey,
      referencedTable: $db.pocSyncSources,
      getReferencedColumn: (t) => t.sourceKey,
      builder:
          (
            joinBuilder, {
            $addJoinBuilderToRootComposer,
            $removeJoinBuilderFromRootComposer,
          }) => $$PocSyncSourcesTableFilterComposer(
            $db: $db,
            $table: $db.pocSyncSources,
            $addJoinBuilderToRootComposer: $addJoinBuilderToRootComposer,
            joinBuilder: joinBuilder,
            $removeJoinBuilderFromRootComposer:
                $removeJoinBuilderFromRootComposer,
          ),
    );
    return composer;
  }
}

class $$PocControlCommandsTableOrderingComposer
    extends Composer<_$TraderProLocalDatabase, $PocControlCommandsTable> {
  $$PocControlCommandsTableOrderingComposer({
    required super.$db,
    required super.$table,
    super.joinBuilder,
    super.$addJoinBuilderToRootComposer,
    super.$removeJoinBuilderFromRootComposer,
  });
  ColumnOrderings<String> get commandId => $composableBuilder(
    column: $table.commandId,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get actingDeviceId => $composableBuilder(
    column: $table.actingDeviceId,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get commandType => $composableBuilder(
    column: $table.commandType,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get sessionId => $composableBuilder(
    column: $table.sessionId,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<int> get expectedCloudVersion => $composableBuilder(
    column: $table.expectedCloudVersion,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get leaseId => $composableBuilder(
    column: $table.leaseId,
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

  ColumnOrderings<String> get nextAttemptAtUtc => $composableBuilder(
    column: $table.nextAttemptAtUtc,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get lastAttemptAtUtc => $composableBuilder(
    column: $table.lastAttemptAtUtc,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get lastErrorCode => $composableBuilder(
    column: $table.lastErrorCode,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get lastErrorMessage => $composableBuilder(
    column: $table.lastErrorMessage,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get successfulResponseJson => $composableBuilder(
    column: $table.successfulResponseJson,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get createdAtUtc => $composableBuilder(
    column: $table.createdAtUtc,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get updatedAtUtc => $composableBuilder(
    column: $table.updatedAtUtc,
    builder: (column) => ColumnOrderings(column),
  );

  $$PocSyncSourcesTableOrderingComposer get sourceKey {
    final $$PocSyncSourcesTableOrderingComposer composer = $composerBuilder(
      composer: this,
      getCurrentColumn: (t) => t.sourceKey,
      referencedTable: $db.pocSyncSources,
      getReferencedColumn: (t) => t.sourceKey,
      builder:
          (
            joinBuilder, {
            $addJoinBuilderToRootComposer,
            $removeJoinBuilderFromRootComposer,
          }) => $$PocSyncSourcesTableOrderingComposer(
            $db: $db,
            $table: $db.pocSyncSources,
            $addJoinBuilderToRootComposer: $addJoinBuilderToRootComposer,
            joinBuilder: joinBuilder,
            $removeJoinBuilderFromRootComposer:
                $removeJoinBuilderFromRootComposer,
          ),
    );
    return composer;
  }
}

class $$PocControlCommandsTableAnnotationComposer
    extends Composer<_$TraderProLocalDatabase, $PocControlCommandsTable> {
  $$PocControlCommandsTableAnnotationComposer({
    required super.$db,
    required super.$table,
    super.joinBuilder,
    super.$addJoinBuilderToRootComposer,
    super.$removeJoinBuilderFromRootComposer,
  });
  GeneratedColumn<String> get commandId =>
      $composableBuilder(column: $table.commandId, builder: (column) => column);

  GeneratedColumn<String> get actingDeviceId => $composableBuilder(
    column: $table.actingDeviceId,
    builder: (column) => column,
  );

  GeneratedColumn<String> get commandType => $composableBuilder(
    column: $table.commandType,
    builder: (column) => column,
  );

  GeneratedColumn<String> get sessionId =>
      $composableBuilder(column: $table.sessionId, builder: (column) => column);

  GeneratedColumn<int> get expectedCloudVersion => $composableBuilder(
    column: $table.expectedCloudVersion,
    builder: (column) => column,
  );

  GeneratedColumn<String> get leaseId =>
      $composableBuilder(column: $table.leaseId, builder: (column) => column);

  GeneratedColumn<String> get status =>
      $composableBuilder(column: $table.status, builder: (column) => column);

  GeneratedColumn<int> get attemptCount => $composableBuilder(
    column: $table.attemptCount,
    builder: (column) => column,
  );

  GeneratedColumn<String> get nextAttemptAtUtc => $composableBuilder(
    column: $table.nextAttemptAtUtc,
    builder: (column) => column,
  );

  GeneratedColumn<String> get lastAttemptAtUtc => $composableBuilder(
    column: $table.lastAttemptAtUtc,
    builder: (column) => column,
  );

  GeneratedColumn<String> get lastErrorCode => $composableBuilder(
    column: $table.lastErrorCode,
    builder: (column) => column,
  );

  GeneratedColumn<String> get lastErrorMessage => $composableBuilder(
    column: $table.lastErrorMessage,
    builder: (column) => column,
  );

  GeneratedColumn<String> get successfulResponseJson => $composableBuilder(
    column: $table.successfulResponseJson,
    builder: (column) => column,
  );

  GeneratedColumn<String> get createdAtUtc => $composableBuilder(
    column: $table.createdAtUtc,
    builder: (column) => column,
  );

  GeneratedColumn<String> get updatedAtUtc => $composableBuilder(
    column: $table.updatedAtUtc,
    builder: (column) => column,
  );

  $$PocSyncSourcesTableAnnotationComposer get sourceKey {
    final $$PocSyncSourcesTableAnnotationComposer composer = $composerBuilder(
      composer: this,
      getCurrentColumn: (t) => t.sourceKey,
      referencedTable: $db.pocSyncSources,
      getReferencedColumn: (t) => t.sourceKey,
      builder:
          (
            joinBuilder, {
            $addJoinBuilderToRootComposer,
            $removeJoinBuilderFromRootComposer,
          }) => $$PocSyncSourcesTableAnnotationComposer(
            $db: $db,
            $table: $db.pocSyncSources,
            $addJoinBuilderToRootComposer: $addJoinBuilderToRootComposer,
            joinBuilder: joinBuilder,
            $removeJoinBuilderFromRootComposer:
                $removeJoinBuilderFromRootComposer,
          ),
    );
    return composer;
  }
}

class $$PocControlCommandsTableTableManager
    extends
        RootTableManager<
          _$TraderProLocalDatabase,
          $PocControlCommandsTable,
          PocControlCommandRow,
          $$PocControlCommandsTableFilterComposer,
          $$PocControlCommandsTableOrderingComposer,
          $$PocControlCommandsTableAnnotationComposer,
          $$PocControlCommandsTableCreateCompanionBuilder,
          $$PocControlCommandsTableUpdateCompanionBuilder,
          (PocControlCommandRow, $$PocControlCommandsTableReferences),
          PocControlCommandRow,
          PrefetchHooks Function({bool sourceKey})
        > {
  $$PocControlCommandsTableTableManager(
    _$TraderProLocalDatabase db,
    $PocControlCommandsTable table,
  ) : super(
        TableManagerState(
          db: db,
          table: table,
          createFilteringComposer: () =>
              $$PocControlCommandsTableFilterComposer($db: db, $table: table),
          createOrderingComposer: () =>
              $$PocControlCommandsTableOrderingComposer($db: db, $table: table),
          createComputedFieldComposer: () =>
              $$PocControlCommandsTableAnnotationComposer(
                $db: db,
                $table: table,
              ),
          updateCompanionCallback:
              ({
                Value<String> commandId = const Value.absent(),
                Value<String> sourceKey = const Value.absent(),
                Value<String> actingDeviceId = const Value.absent(),
                Value<String> commandType = const Value.absent(),
                Value<String> sessionId = const Value.absent(),
                Value<int?> expectedCloudVersion = const Value.absent(),
                Value<String?> leaseId = const Value.absent(),
                Value<String> status = const Value.absent(),
                Value<int> attemptCount = const Value.absent(),
                Value<String?> nextAttemptAtUtc = const Value.absent(),
                Value<String?> lastAttemptAtUtc = const Value.absent(),
                Value<String?> lastErrorCode = const Value.absent(),
                Value<String?> lastErrorMessage = const Value.absent(),
                Value<String?> successfulResponseJson = const Value.absent(),
                Value<String> createdAtUtc = const Value.absent(),
                Value<String> updatedAtUtc = const Value.absent(),
                Value<int> rowid = const Value.absent(),
              }) => PocControlCommandsCompanion(
                commandId: commandId,
                sourceKey: sourceKey,
                actingDeviceId: actingDeviceId,
                commandType: commandType,
                sessionId: sessionId,
                expectedCloudVersion: expectedCloudVersion,
                leaseId: leaseId,
                status: status,
                attemptCount: attemptCount,
                nextAttemptAtUtc: nextAttemptAtUtc,
                lastAttemptAtUtc: lastAttemptAtUtc,
                lastErrorCode: lastErrorCode,
                lastErrorMessage: lastErrorMessage,
                successfulResponseJson: successfulResponseJson,
                createdAtUtc: createdAtUtc,
                updatedAtUtc: updatedAtUtc,
                rowid: rowid,
              ),
          createCompanionCallback:
              ({
                required String commandId,
                required String sourceKey,
                required String actingDeviceId,
                required String commandType,
                required String sessionId,
                Value<int?> expectedCloudVersion = const Value.absent(),
                Value<String?> leaseId = const Value.absent(),
                required String status,
                required int attemptCount,
                Value<String?> nextAttemptAtUtc = const Value.absent(),
                Value<String?> lastAttemptAtUtc = const Value.absent(),
                Value<String?> lastErrorCode = const Value.absent(),
                Value<String?> lastErrorMessage = const Value.absent(),
                Value<String?> successfulResponseJson = const Value.absent(),
                required String createdAtUtc,
                required String updatedAtUtc,
                Value<int> rowid = const Value.absent(),
              }) => PocControlCommandsCompanion.insert(
                commandId: commandId,
                sourceKey: sourceKey,
                actingDeviceId: actingDeviceId,
                commandType: commandType,
                sessionId: sessionId,
                expectedCloudVersion: expectedCloudVersion,
                leaseId: leaseId,
                status: status,
                attemptCount: attemptCount,
                nextAttemptAtUtc: nextAttemptAtUtc,
                lastAttemptAtUtc: lastAttemptAtUtc,
                lastErrorCode: lastErrorCode,
                lastErrorMessage: lastErrorMessage,
                successfulResponseJson: successfulResponseJson,
                createdAtUtc: createdAtUtc,
                updatedAtUtc: updatedAtUtc,
                rowid: rowid,
              ),
          withReferenceMapper: (p0) => p0
              .map(
                (e) => (
                  e.readTable(table),
                  $$PocControlCommandsTableReferences(db, table, e),
                ),
              )
              .toList(),
          prefetchHooksCallback: ({sourceKey = false}) {
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
                    if (sourceKey) {
                      state =
                          state.withJoin(
                                currentTable: table,
                                currentColumn: table.sourceKey,
                                referencedTable:
                                    $$PocControlCommandsTableReferences
                                        ._sourceKeyTable(db),
                                referencedColumn:
                                    $$PocControlCommandsTableReferences
                                        ._sourceKeyTable(db)
                                        .sourceKey,
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

typedef $$PocControlCommandsTableProcessedTableManager =
    ProcessedTableManager<
      _$TraderProLocalDatabase,
      $PocControlCommandsTable,
      PocControlCommandRow,
      $$PocControlCommandsTableFilterComposer,
      $$PocControlCommandsTableOrderingComposer,
      $$PocControlCommandsTableAnnotationComposer,
      $$PocControlCommandsTableCreateCompanionBuilder,
      $$PocControlCommandsTableUpdateCompanionBuilder,
      (PocControlCommandRow, $$PocControlCommandsTableReferences),
      PocControlCommandRow,
      PrefetchHooks Function({bool sourceKey})
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
  $$PocDeviceProfilesTableTableManager get pocDeviceProfiles =>
      $$PocDeviceProfilesTableTableManager(_db, _db.pocDeviceProfiles);
  $$PocSyncSourcesTableTableManager get pocSyncSources =>
      $$PocSyncSourcesTableTableManager(_db, _db.pocSyncSources);
  $$ReceivingSessionCloudStatesTableTableManager
  get receivingSessionCloudStates =>
      $$ReceivingSessionCloudStatesTableTableManager(
        _db,
        _db.receivingSessionCloudStates,
      );
  $$MobileSyncEventInboxTableTableManager get mobileSyncEventInbox =>
      $$MobileSyncEventInboxTableTableManager(_db, _db.mobileSyncEventInbox);
  $$RemoteReceivingSessionProjectionsTableTableManager
  get remoteReceivingSessionProjections =>
      $$RemoteReceivingSessionProjectionsTableTableManager(
        _db,
        _db.remoteReceivingSessionProjections,
      );
  $$RemoteReceivingEntrySummariesTableTableManager
  get remoteReceivingEntrySummaries =>
      $$RemoteReceivingEntrySummariesTableTableManager(
        _db,
        _db.remoteReceivingEntrySummaries,
      );
  $$PocControlCommandsTableTableManager get pocControlCommands =>
      $$PocControlCommandsTableTableManager(_db, _db.pocControlCommands);
}
