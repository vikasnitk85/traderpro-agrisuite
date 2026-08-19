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

class $CommercialIdentityBindingsTable extends CommercialIdentityBindings
    with
        TableInfo<$CommercialIdentityBindingsTable, CommercialIdentityBinding> {
  @override
  final GeneratedDatabase attachedDatabase;
  final String? _alias;
  $CommercialIdentityBindingsTable(this.attachedDatabase, [this._alias]);
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
  static const VerificationMeta _bindingContractVersionMeta =
      const VerificationMeta('bindingContractVersion');
  @override
  late final GeneratedColumn<int> bindingContractVersion = GeneratedColumn<int>(
    'binding_contract_version',
    aliasedName,
    false,
    type: DriftSqlType.int,
    requiredDuringInsert: true,
  );
  static const VerificationMeta _apiOriginMeta = const VerificationMeta(
    'apiOrigin',
  );
  @override
  late final GeneratedColumn<String> apiOrigin = GeneratedColumn<String>(
    'api_origin',
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
  static const VerificationMeta _companyIdMeta = const VerificationMeta(
    'companyId',
  );
  @override
  late final GeneratedColumn<String> companyId = GeneratedColumn<String>(
    'company_id',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
  );
  static const VerificationMeta _defaultBranchIdMeta = const VerificationMeta(
    'defaultBranchId',
  );
  @override
  late final GeneratedColumn<String> defaultBranchId = GeneratedColumn<String>(
    'default_branch_id',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
  );
  static const VerificationMeta _userIdMeta = const VerificationMeta('userId');
  @override
  late final GeneratedColumn<String> userId = GeneratedColumn<String>(
    'user_id',
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
  static const VerificationMeta _firstBoundAtUtcMicrosMeta =
      const VerificationMeta('firstBoundAtUtcMicros');
  @override
  late final GeneratedColumn<int> firstBoundAtUtcMicros = GeneratedColumn<int>(
    'first_bound_at_utc_micros',
    aliasedName,
    false,
    type: DriftSqlType.int,
    requiredDuringInsert: true,
  );
  @override
  List<GeneratedColumn> get $columns => [
    singletonId,
    bindingContractVersion,
    apiOrigin,
    workspaceId,
    companyId,
    defaultBranchId,
    userId,
    deviceId,
    firstBoundAtUtcMicros,
  ];
  @override
  String get aliasedName => _alias ?? actualTableName;
  @override
  String get actualTableName => $name;
  static const String $name = 'commercial_identity_binding';
  @override
  VerificationContext validateIntegrity(
    Insertable<CommercialIdentityBinding> instance, {
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
    if (data.containsKey('binding_contract_version')) {
      context.handle(
        _bindingContractVersionMeta,
        bindingContractVersion.isAcceptableOrUnknown(
          data['binding_contract_version']!,
          _bindingContractVersionMeta,
        ),
      );
    } else if (isInserting) {
      context.missing(_bindingContractVersionMeta);
    }
    if (data.containsKey('api_origin')) {
      context.handle(
        _apiOriginMeta,
        apiOrigin.isAcceptableOrUnknown(data['api_origin']!, _apiOriginMeta),
      );
    } else if (isInserting) {
      context.missing(_apiOriginMeta);
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
    if (data.containsKey('company_id')) {
      context.handle(
        _companyIdMeta,
        companyId.isAcceptableOrUnknown(data['company_id']!, _companyIdMeta),
      );
    } else if (isInserting) {
      context.missing(_companyIdMeta);
    }
    if (data.containsKey('default_branch_id')) {
      context.handle(
        _defaultBranchIdMeta,
        defaultBranchId.isAcceptableOrUnknown(
          data['default_branch_id']!,
          _defaultBranchIdMeta,
        ),
      );
    } else if (isInserting) {
      context.missing(_defaultBranchIdMeta);
    }
    if (data.containsKey('user_id')) {
      context.handle(
        _userIdMeta,
        userId.isAcceptableOrUnknown(data['user_id']!, _userIdMeta),
      );
    } else if (isInserting) {
      context.missing(_userIdMeta);
    }
    if (data.containsKey('device_id')) {
      context.handle(
        _deviceIdMeta,
        deviceId.isAcceptableOrUnknown(data['device_id']!, _deviceIdMeta),
      );
    } else if (isInserting) {
      context.missing(_deviceIdMeta);
    }
    if (data.containsKey('first_bound_at_utc_micros')) {
      context.handle(
        _firstBoundAtUtcMicrosMeta,
        firstBoundAtUtcMicros.isAcceptableOrUnknown(
          data['first_bound_at_utc_micros']!,
          _firstBoundAtUtcMicrosMeta,
        ),
      );
    } else if (isInserting) {
      context.missing(_firstBoundAtUtcMicrosMeta);
    }
    return context;
  }

  @override
  Set<GeneratedColumn> get $primaryKey => {singletonId};
  @override
  CommercialIdentityBinding map(
    Map<String, dynamic> data, {
    String? tablePrefix,
  }) {
    final effectivePrefix = tablePrefix != null ? '$tablePrefix.' : '';
    return CommercialIdentityBinding(
      singletonId: attachedDatabase.typeMapping.read(
        DriftSqlType.int,
        data['${effectivePrefix}singleton_id'],
      )!,
      bindingContractVersion: attachedDatabase.typeMapping.read(
        DriftSqlType.int,
        data['${effectivePrefix}binding_contract_version'],
      )!,
      apiOrigin: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}api_origin'],
      )!,
      workspaceId: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}workspace_id'],
      )!,
      companyId: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}company_id'],
      )!,
      defaultBranchId: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}default_branch_id'],
      )!,
      userId: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}user_id'],
      )!,
      deviceId: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}device_id'],
      )!,
      firstBoundAtUtcMicros: attachedDatabase.typeMapping.read(
        DriftSqlType.int,
        data['${effectivePrefix}first_bound_at_utc_micros'],
      )!,
    );
  }

  @override
  $CommercialIdentityBindingsTable createAlias(String alias) {
    return $CommercialIdentityBindingsTable(attachedDatabase, alias);
  }
}

class CommercialIdentityBinding extends DataClass
    implements Insertable<CommercialIdentityBinding> {
  final int singletonId;
  final int bindingContractVersion;
  final String apiOrigin;
  final String workspaceId;
  final String companyId;
  final String defaultBranchId;
  final String userId;
  final String deviceId;
  final int firstBoundAtUtcMicros;
  const CommercialIdentityBinding({
    required this.singletonId,
    required this.bindingContractVersion,
    required this.apiOrigin,
    required this.workspaceId,
    required this.companyId,
    required this.defaultBranchId,
    required this.userId,
    required this.deviceId,
    required this.firstBoundAtUtcMicros,
  });
  @override
  Map<String, Expression> toColumns(bool nullToAbsent) {
    final map = <String, Expression>{};
    map['singleton_id'] = Variable<int>(singletonId);
    map['binding_contract_version'] = Variable<int>(bindingContractVersion);
    map['api_origin'] = Variable<String>(apiOrigin);
    map['workspace_id'] = Variable<String>(workspaceId);
    map['company_id'] = Variable<String>(companyId);
    map['default_branch_id'] = Variable<String>(defaultBranchId);
    map['user_id'] = Variable<String>(userId);
    map['device_id'] = Variable<String>(deviceId);
    map['first_bound_at_utc_micros'] = Variable<int>(firstBoundAtUtcMicros);
    return map;
  }

  CommercialIdentityBindingsCompanion toCompanion(bool nullToAbsent) {
    return CommercialIdentityBindingsCompanion(
      singletonId: Value(singletonId),
      bindingContractVersion: Value(bindingContractVersion),
      apiOrigin: Value(apiOrigin),
      workspaceId: Value(workspaceId),
      companyId: Value(companyId),
      defaultBranchId: Value(defaultBranchId),
      userId: Value(userId),
      deviceId: Value(deviceId),
      firstBoundAtUtcMicros: Value(firstBoundAtUtcMicros),
    );
  }

  factory CommercialIdentityBinding.fromJson(
    Map<String, dynamic> json, {
    ValueSerializer? serializer,
  }) {
    serializer ??= driftRuntimeOptions.defaultSerializer;
    return CommercialIdentityBinding(
      singletonId: serializer.fromJson<int>(json['singletonId']),
      bindingContractVersion: serializer.fromJson<int>(
        json['bindingContractVersion'],
      ),
      apiOrigin: serializer.fromJson<String>(json['apiOrigin']),
      workspaceId: serializer.fromJson<String>(json['workspaceId']),
      companyId: serializer.fromJson<String>(json['companyId']),
      defaultBranchId: serializer.fromJson<String>(json['defaultBranchId']),
      userId: serializer.fromJson<String>(json['userId']),
      deviceId: serializer.fromJson<String>(json['deviceId']),
      firstBoundAtUtcMicros: serializer.fromJson<int>(
        json['firstBoundAtUtcMicros'],
      ),
    );
  }
  @override
  Map<String, dynamic> toJson({ValueSerializer? serializer}) {
    serializer ??= driftRuntimeOptions.defaultSerializer;
    return <String, dynamic>{
      'singletonId': serializer.toJson<int>(singletonId),
      'bindingContractVersion': serializer.toJson<int>(bindingContractVersion),
      'apiOrigin': serializer.toJson<String>(apiOrigin),
      'workspaceId': serializer.toJson<String>(workspaceId),
      'companyId': serializer.toJson<String>(companyId),
      'defaultBranchId': serializer.toJson<String>(defaultBranchId),
      'userId': serializer.toJson<String>(userId),
      'deviceId': serializer.toJson<String>(deviceId),
      'firstBoundAtUtcMicros': serializer.toJson<int>(firstBoundAtUtcMicros),
    };
  }

  CommercialIdentityBinding copyWith({
    int? singletonId,
    int? bindingContractVersion,
    String? apiOrigin,
    String? workspaceId,
    String? companyId,
    String? defaultBranchId,
    String? userId,
    String? deviceId,
    int? firstBoundAtUtcMicros,
  }) => CommercialIdentityBinding(
    singletonId: singletonId ?? this.singletonId,
    bindingContractVersion:
        bindingContractVersion ?? this.bindingContractVersion,
    apiOrigin: apiOrigin ?? this.apiOrigin,
    workspaceId: workspaceId ?? this.workspaceId,
    companyId: companyId ?? this.companyId,
    defaultBranchId: defaultBranchId ?? this.defaultBranchId,
    userId: userId ?? this.userId,
    deviceId: deviceId ?? this.deviceId,
    firstBoundAtUtcMicros: firstBoundAtUtcMicros ?? this.firstBoundAtUtcMicros,
  );
  CommercialIdentityBinding copyWithCompanion(
    CommercialIdentityBindingsCompanion data,
  ) {
    return CommercialIdentityBinding(
      singletonId: data.singletonId.present
          ? data.singletonId.value
          : this.singletonId,
      bindingContractVersion: data.bindingContractVersion.present
          ? data.bindingContractVersion.value
          : this.bindingContractVersion,
      apiOrigin: data.apiOrigin.present ? data.apiOrigin.value : this.apiOrigin,
      workspaceId: data.workspaceId.present
          ? data.workspaceId.value
          : this.workspaceId,
      companyId: data.companyId.present ? data.companyId.value : this.companyId,
      defaultBranchId: data.defaultBranchId.present
          ? data.defaultBranchId.value
          : this.defaultBranchId,
      userId: data.userId.present ? data.userId.value : this.userId,
      deviceId: data.deviceId.present ? data.deviceId.value : this.deviceId,
      firstBoundAtUtcMicros: data.firstBoundAtUtcMicros.present
          ? data.firstBoundAtUtcMicros.value
          : this.firstBoundAtUtcMicros,
    );
  }

  @override
  String toString() {
    return (StringBuffer('CommercialIdentityBinding(')
          ..write('singletonId: $singletonId, ')
          ..write('bindingContractVersion: $bindingContractVersion, ')
          ..write('apiOrigin: $apiOrigin, ')
          ..write('workspaceId: $workspaceId, ')
          ..write('companyId: $companyId, ')
          ..write('defaultBranchId: $defaultBranchId, ')
          ..write('userId: $userId, ')
          ..write('deviceId: $deviceId, ')
          ..write('firstBoundAtUtcMicros: $firstBoundAtUtcMicros')
          ..write(')'))
        .toString();
  }

  @override
  int get hashCode => Object.hash(
    singletonId,
    bindingContractVersion,
    apiOrigin,
    workspaceId,
    companyId,
    defaultBranchId,
    userId,
    deviceId,
    firstBoundAtUtcMicros,
  );
  @override
  bool operator ==(Object other) =>
      identical(this, other) ||
      (other is CommercialIdentityBinding &&
          other.singletonId == this.singletonId &&
          other.bindingContractVersion == this.bindingContractVersion &&
          other.apiOrigin == this.apiOrigin &&
          other.workspaceId == this.workspaceId &&
          other.companyId == this.companyId &&
          other.defaultBranchId == this.defaultBranchId &&
          other.userId == this.userId &&
          other.deviceId == this.deviceId &&
          other.firstBoundAtUtcMicros == this.firstBoundAtUtcMicros);
}

class CommercialIdentityBindingsCompanion
    extends UpdateCompanion<CommercialIdentityBinding> {
  final Value<int> singletonId;
  final Value<int> bindingContractVersion;
  final Value<String> apiOrigin;
  final Value<String> workspaceId;
  final Value<String> companyId;
  final Value<String> defaultBranchId;
  final Value<String> userId;
  final Value<String> deviceId;
  final Value<int> firstBoundAtUtcMicros;
  const CommercialIdentityBindingsCompanion({
    this.singletonId = const Value.absent(),
    this.bindingContractVersion = const Value.absent(),
    this.apiOrigin = const Value.absent(),
    this.workspaceId = const Value.absent(),
    this.companyId = const Value.absent(),
    this.defaultBranchId = const Value.absent(),
    this.userId = const Value.absent(),
    this.deviceId = const Value.absent(),
    this.firstBoundAtUtcMicros = const Value.absent(),
  });
  CommercialIdentityBindingsCompanion.insert({
    this.singletonId = const Value.absent(),
    required int bindingContractVersion,
    required String apiOrigin,
    required String workspaceId,
    required String companyId,
    required String defaultBranchId,
    required String userId,
    required String deviceId,
    required int firstBoundAtUtcMicros,
  }) : bindingContractVersion = Value(bindingContractVersion),
       apiOrigin = Value(apiOrigin),
       workspaceId = Value(workspaceId),
       companyId = Value(companyId),
       defaultBranchId = Value(defaultBranchId),
       userId = Value(userId),
       deviceId = Value(deviceId),
       firstBoundAtUtcMicros = Value(firstBoundAtUtcMicros);
  static Insertable<CommercialIdentityBinding> custom({
    Expression<int>? singletonId,
    Expression<int>? bindingContractVersion,
    Expression<String>? apiOrigin,
    Expression<String>? workspaceId,
    Expression<String>? companyId,
    Expression<String>? defaultBranchId,
    Expression<String>? userId,
    Expression<String>? deviceId,
    Expression<int>? firstBoundAtUtcMicros,
  }) {
    return RawValuesInsertable({
      if (singletonId != null) 'singleton_id': singletonId,
      if (bindingContractVersion != null)
        'binding_contract_version': bindingContractVersion,
      if (apiOrigin != null) 'api_origin': apiOrigin,
      if (workspaceId != null) 'workspace_id': workspaceId,
      if (companyId != null) 'company_id': companyId,
      if (defaultBranchId != null) 'default_branch_id': defaultBranchId,
      if (userId != null) 'user_id': userId,
      if (deviceId != null) 'device_id': deviceId,
      if (firstBoundAtUtcMicros != null)
        'first_bound_at_utc_micros': firstBoundAtUtcMicros,
    });
  }

  CommercialIdentityBindingsCompanion copyWith({
    Value<int>? singletonId,
    Value<int>? bindingContractVersion,
    Value<String>? apiOrigin,
    Value<String>? workspaceId,
    Value<String>? companyId,
    Value<String>? defaultBranchId,
    Value<String>? userId,
    Value<String>? deviceId,
    Value<int>? firstBoundAtUtcMicros,
  }) {
    return CommercialIdentityBindingsCompanion(
      singletonId: singletonId ?? this.singletonId,
      bindingContractVersion:
          bindingContractVersion ?? this.bindingContractVersion,
      apiOrigin: apiOrigin ?? this.apiOrigin,
      workspaceId: workspaceId ?? this.workspaceId,
      companyId: companyId ?? this.companyId,
      defaultBranchId: defaultBranchId ?? this.defaultBranchId,
      userId: userId ?? this.userId,
      deviceId: deviceId ?? this.deviceId,
      firstBoundAtUtcMicros:
          firstBoundAtUtcMicros ?? this.firstBoundAtUtcMicros,
    );
  }

  @override
  Map<String, Expression> toColumns(bool nullToAbsent) {
    final map = <String, Expression>{};
    if (singletonId.present) {
      map['singleton_id'] = Variable<int>(singletonId.value);
    }
    if (bindingContractVersion.present) {
      map['binding_contract_version'] = Variable<int>(
        bindingContractVersion.value,
      );
    }
    if (apiOrigin.present) {
      map['api_origin'] = Variable<String>(apiOrigin.value);
    }
    if (workspaceId.present) {
      map['workspace_id'] = Variable<String>(workspaceId.value);
    }
    if (companyId.present) {
      map['company_id'] = Variable<String>(companyId.value);
    }
    if (defaultBranchId.present) {
      map['default_branch_id'] = Variable<String>(defaultBranchId.value);
    }
    if (userId.present) {
      map['user_id'] = Variable<String>(userId.value);
    }
    if (deviceId.present) {
      map['device_id'] = Variable<String>(deviceId.value);
    }
    if (firstBoundAtUtcMicros.present) {
      map['first_bound_at_utc_micros'] = Variable<int>(
        firstBoundAtUtcMicros.value,
      );
    }
    return map;
  }

  @override
  String toString() {
    return (StringBuffer('CommercialIdentityBindingsCompanion(')
          ..write('singletonId: $singletonId, ')
          ..write('bindingContractVersion: $bindingContractVersion, ')
          ..write('apiOrigin: $apiOrigin, ')
          ..write('workspaceId: $workspaceId, ')
          ..write('companyId: $companyId, ')
          ..write('defaultBranchId: $defaultBranchId, ')
          ..write('userId: $userId, ')
          ..write('deviceId: $deviceId, ')
          ..write('firstBoundAtUtcMicros: $firstBoundAtUtcMicros')
          ..write(')'))
        .toString();
  }
}

class $CommercialIdentitySnapshotsTable extends CommercialIdentitySnapshots
    with
        TableInfo<
          $CommercialIdentitySnapshotsTable,
          CommercialIdentitySnapshot
        > {
  @override
  final GeneratedDatabase attachedDatabase;
  final String? _alias;
  $CommercialIdentitySnapshotsTable(this.attachedDatabase, [this._alias]);
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
    defaultConstraints: GeneratedColumn.constraintIsAlways(
      'REFERENCES commercial_identity_binding (singleton_id)',
    ),
  );
  static const VerificationMeta _workspaceCodeMeta = const VerificationMeta(
    'workspaceCode',
  );
  @override
  late final GeneratedColumn<String> workspaceCode = GeneratedColumn<String>(
    'workspace_code',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
  );
  static const VerificationMeta _userDisplayNameMeta = const VerificationMeta(
    'userDisplayName',
  );
  @override
  late final GeneratedColumn<String> userDisplayName = GeneratedColumn<String>(
    'user_display_name',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
  );
  static const VerificationMeta _roleMeta = const VerificationMeta('role');
  @override
  late final GeneratedColumn<String> role = GeneratedColumn<String>(
    'role',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
  );
  static const VerificationMeta _deviceLabelMeta = const VerificationMeta(
    'deviceLabel',
  );
  @override
  late final GeneratedColumn<String> deviceLabel = GeneratedColumn<String>(
    'device_label',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
  );
  static const VerificationMeta _lastConfirmedAtUtcMicrosMeta =
      const VerificationMeta('lastConfirmedAtUtcMicros');
  @override
  late final GeneratedColumn<int> lastConfirmedAtUtcMicros =
      GeneratedColumn<int>(
        'last_confirmed_at_utc_micros',
        aliasedName,
        false,
        type: DriftSqlType.int,
        requiredDuringInsert: true,
      );
  @override
  List<GeneratedColumn> get $columns => [
    singletonId,
    workspaceCode,
    userDisplayName,
    role,
    deviceLabel,
    lastConfirmedAtUtcMicros,
  ];
  @override
  String get aliasedName => _alias ?? actualTableName;
  @override
  String get actualTableName => $name;
  static const String $name = 'commercial_identity_snapshot';
  @override
  VerificationContext validateIntegrity(
    Insertable<CommercialIdentitySnapshot> instance, {
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
    if (data.containsKey('workspace_code')) {
      context.handle(
        _workspaceCodeMeta,
        workspaceCode.isAcceptableOrUnknown(
          data['workspace_code']!,
          _workspaceCodeMeta,
        ),
      );
    } else if (isInserting) {
      context.missing(_workspaceCodeMeta);
    }
    if (data.containsKey('user_display_name')) {
      context.handle(
        _userDisplayNameMeta,
        userDisplayName.isAcceptableOrUnknown(
          data['user_display_name']!,
          _userDisplayNameMeta,
        ),
      );
    } else if (isInserting) {
      context.missing(_userDisplayNameMeta);
    }
    if (data.containsKey('role')) {
      context.handle(
        _roleMeta,
        role.isAcceptableOrUnknown(data['role']!, _roleMeta),
      );
    } else if (isInserting) {
      context.missing(_roleMeta);
    }
    if (data.containsKey('device_label')) {
      context.handle(
        _deviceLabelMeta,
        deviceLabel.isAcceptableOrUnknown(
          data['device_label']!,
          _deviceLabelMeta,
        ),
      );
    } else if (isInserting) {
      context.missing(_deviceLabelMeta);
    }
    if (data.containsKey('last_confirmed_at_utc_micros')) {
      context.handle(
        _lastConfirmedAtUtcMicrosMeta,
        lastConfirmedAtUtcMicros.isAcceptableOrUnknown(
          data['last_confirmed_at_utc_micros']!,
          _lastConfirmedAtUtcMicrosMeta,
        ),
      );
    } else if (isInserting) {
      context.missing(_lastConfirmedAtUtcMicrosMeta);
    }
    return context;
  }

  @override
  Set<GeneratedColumn> get $primaryKey => {singletonId};
  @override
  CommercialIdentitySnapshot map(
    Map<String, dynamic> data, {
    String? tablePrefix,
  }) {
    final effectivePrefix = tablePrefix != null ? '$tablePrefix.' : '';
    return CommercialIdentitySnapshot(
      singletonId: attachedDatabase.typeMapping.read(
        DriftSqlType.int,
        data['${effectivePrefix}singleton_id'],
      )!,
      workspaceCode: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}workspace_code'],
      )!,
      userDisplayName: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}user_display_name'],
      )!,
      role: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}role'],
      )!,
      deviceLabel: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}device_label'],
      )!,
      lastConfirmedAtUtcMicros: attachedDatabase.typeMapping.read(
        DriftSqlType.int,
        data['${effectivePrefix}last_confirmed_at_utc_micros'],
      )!,
    );
  }

  @override
  $CommercialIdentitySnapshotsTable createAlias(String alias) {
    return $CommercialIdentitySnapshotsTable(attachedDatabase, alias);
  }
}

class CommercialIdentitySnapshot extends DataClass
    implements Insertable<CommercialIdentitySnapshot> {
  final int singletonId;
  final String workspaceCode;
  final String userDisplayName;
  final String role;
  final String deviceLabel;
  final int lastConfirmedAtUtcMicros;
  const CommercialIdentitySnapshot({
    required this.singletonId,
    required this.workspaceCode,
    required this.userDisplayName,
    required this.role,
    required this.deviceLabel,
    required this.lastConfirmedAtUtcMicros,
  });
  @override
  Map<String, Expression> toColumns(bool nullToAbsent) {
    final map = <String, Expression>{};
    map['singleton_id'] = Variable<int>(singletonId);
    map['workspace_code'] = Variable<String>(workspaceCode);
    map['user_display_name'] = Variable<String>(userDisplayName);
    map['role'] = Variable<String>(role);
    map['device_label'] = Variable<String>(deviceLabel);
    map['last_confirmed_at_utc_micros'] = Variable<int>(
      lastConfirmedAtUtcMicros,
    );
    return map;
  }

  CommercialIdentitySnapshotsCompanion toCompanion(bool nullToAbsent) {
    return CommercialIdentitySnapshotsCompanion(
      singletonId: Value(singletonId),
      workspaceCode: Value(workspaceCode),
      userDisplayName: Value(userDisplayName),
      role: Value(role),
      deviceLabel: Value(deviceLabel),
      lastConfirmedAtUtcMicros: Value(lastConfirmedAtUtcMicros),
    );
  }

  factory CommercialIdentitySnapshot.fromJson(
    Map<String, dynamic> json, {
    ValueSerializer? serializer,
  }) {
    serializer ??= driftRuntimeOptions.defaultSerializer;
    return CommercialIdentitySnapshot(
      singletonId: serializer.fromJson<int>(json['singletonId']),
      workspaceCode: serializer.fromJson<String>(json['workspaceCode']),
      userDisplayName: serializer.fromJson<String>(json['userDisplayName']),
      role: serializer.fromJson<String>(json['role']),
      deviceLabel: serializer.fromJson<String>(json['deviceLabel']),
      lastConfirmedAtUtcMicros: serializer.fromJson<int>(
        json['lastConfirmedAtUtcMicros'],
      ),
    );
  }
  @override
  Map<String, dynamic> toJson({ValueSerializer? serializer}) {
    serializer ??= driftRuntimeOptions.defaultSerializer;
    return <String, dynamic>{
      'singletonId': serializer.toJson<int>(singletonId),
      'workspaceCode': serializer.toJson<String>(workspaceCode),
      'userDisplayName': serializer.toJson<String>(userDisplayName),
      'role': serializer.toJson<String>(role),
      'deviceLabel': serializer.toJson<String>(deviceLabel),
      'lastConfirmedAtUtcMicros': serializer.toJson<int>(
        lastConfirmedAtUtcMicros,
      ),
    };
  }

  CommercialIdentitySnapshot copyWith({
    int? singletonId,
    String? workspaceCode,
    String? userDisplayName,
    String? role,
    String? deviceLabel,
    int? lastConfirmedAtUtcMicros,
  }) => CommercialIdentitySnapshot(
    singletonId: singletonId ?? this.singletonId,
    workspaceCode: workspaceCode ?? this.workspaceCode,
    userDisplayName: userDisplayName ?? this.userDisplayName,
    role: role ?? this.role,
    deviceLabel: deviceLabel ?? this.deviceLabel,
    lastConfirmedAtUtcMicros:
        lastConfirmedAtUtcMicros ?? this.lastConfirmedAtUtcMicros,
  );
  CommercialIdentitySnapshot copyWithCompanion(
    CommercialIdentitySnapshotsCompanion data,
  ) {
    return CommercialIdentitySnapshot(
      singletonId: data.singletonId.present
          ? data.singletonId.value
          : this.singletonId,
      workspaceCode: data.workspaceCode.present
          ? data.workspaceCode.value
          : this.workspaceCode,
      userDisplayName: data.userDisplayName.present
          ? data.userDisplayName.value
          : this.userDisplayName,
      role: data.role.present ? data.role.value : this.role,
      deviceLabel: data.deviceLabel.present
          ? data.deviceLabel.value
          : this.deviceLabel,
      lastConfirmedAtUtcMicros: data.lastConfirmedAtUtcMicros.present
          ? data.lastConfirmedAtUtcMicros.value
          : this.lastConfirmedAtUtcMicros,
    );
  }

  @override
  String toString() {
    return (StringBuffer('CommercialIdentitySnapshot(')
          ..write('singletonId: $singletonId, ')
          ..write('workspaceCode: $workspaceCode, ')
          ..write('userDisplayName: $userDisplayName, ')
          ..write('role: $role, ')
          ..write('deviceLabel: $deviceLabel, ')
          ..write('lastConfirmedAtUtcMicros: $lastConfirmedAtUtcMicros')
          ..write(')'))
        .toString();
  }

  @override
  int get hashCode => Object.hash(
    singletonId,
    workspaceCode,
    userDisplayName,
    role,
    deviceLabel,
    lastConfirmedAtUtcMicros,
  );
  @override
  bool operator ==(Object other) =>
      identical(this, other) ||
      (other is CommercialIdentitySnapshot &&
          other.singletonId == this.singletonId &&
          other.workspaceCode == this.workspaceCode &&
          other.userDisplayName == this.userDisplayName &&
          other.role == this.role &&
          other.deviceLabel == this.deviceLabel &&
          other.lastConfirmedAtUtcMicros == this.lastConfirmedAtUtcMicros);
}

class CommercialIdentitySnapshotsCompanion
    extends UpdateCompanion<CommercialIdentitySnapshot> {
  final Value<int> singletonId;
  final Value<String> workspaceCode;
  final Value<String> userDisplayName;
  final Value<String> role;
  final Value<String> deviceLabel;
  final Value<int> lastConfirmedAtUtcMicros;
  const CommercialIdentitySnapshotsCompanion({
    this.singletonId = const Value.absent(),
    this.workspaceCode = const Value.absent(),
    this.userDisplayName = const Value.absent(),
    this.role = const Value.absent(),
    this.deviceLabel = const Value.absent(),
    this.lastConfirmedAtUtcMicros = const Value.absent(),
  });
  CommercialIdentitySnapshotsCompanion.insert({
    this.singletonId = const Value.absent(),
    required String workspaceCode,
    required String userDisplayName,
    required String role,
    required String deviceLabel,
    required int lastConfirmedAtUtcMicros,
  }) : workspaceCode = Value(workspaceCode),
       userDisplayName = Value(userDisplayName),
       role = Value(role),
       deviceLabel = Value(deviceLabel),
       lastConfirmedAtUtcMicros = Value(lastConfirmedAtUtcMicros);
  static Insertable<CommercialIdentitySnapshot> custom({
    Expression<int>? singletonId,
    Expression<String>? workspaceCode,
    Expression<String>? userDisplayName,
    Expression<String>? role,
    Expression<String>? deviceLabel,
    Expression<int>? lastConfirmedAtUtcMicros,
  }) {
    return RawValuesInsertable({
      if (singletonId != null) 'singleton_id': singletonId,
      if (workspaceCode != null) 'workspace_code': workspaceCode,
      if (userDisplayName != null) 'user_display_name': userDisplayName,
      if (role != null) 'role': role,
      if (deviceLabel != null) 'device_label': deviceLabel,
      if (lastConfirmedAtUtcMicros != null)
        'last_confirmed_at_utc_micros': lastConfirmedAtUtcMicros,
    });
  }

  CommercialIdentitySnapshotsCompanion copyWith({
    Value<int>? singletonId,
    Value<String>? workspaceCode,
    Value<String>? userDisplayName,
    Value<String>? role,
    Value<String>? deviceLabel,
    Value<int>? lastConfirmedAtUtcMicros,
  }) {
    return CommercialIdentitySnapshotsCompanion(
      singletonId: singletonId ?? this.singletonId,
      workspaceCode: workspaceCode ?? this.workspaceCode,
      userDisplayName: userDisplayName ?? this.userDisplayName,
      role: role ?? this.role,
      deviceLabel: deviceLabel ?? this.deviceLabel,
      lastConfirmedAtUtcMicros:
          lastConfirmedAtUtcMicros ?? this.lastConfirmedAtUtcMicros,
    );
  }

  @override
  Map<String, Expression> toColumns(bool nullToAbsent) {
    final map = <String, Expression>{};
    if (singletonId.present) {
      map['singleton_id'] = Variable<int>(singletonId.value);
    }
    if (workspaceCode.present) {
      map['workspace_code'] = Variable<String>(workspaceCode.value);
    }
    if (userDisplayName.present) {
      map['user_display_name'] = Variable<String>(userDisplayName.value);
    }
    if (role.present) {
      map['role'] = Variable<String>(role.value);
    }
    if (deviceLabel.present) {
      map['device_label'] = Variable<String>(deviceLabel.value);
    }
    if (lastConfirmedAtUtcMicros.present) {
      map['last_confirmed_at_utc_micros'] = Variable<int>(
        lastConfirmedAtUtcMicros.value,
      );
    }
    return map;
  }

  @override
  String toString() {
    return (StringBuffer('CommercialIdentitySnapshotsCompanion(')
          ..write('singletonId: $singletonId, ')
          ..write('workspaceCode: $workspaceCode, ')
          ..write('userDisplayName: $userDisplayName, ')
          ..write('role: $role, ')
          ..write('deviceLabel: $deviceLabel, ')
          ..write('lastConfirmedAtUtcMicros: $lastConfirmedAtUtcMicros')
          ..write(')'))
        .toString();
  }
}

abstract class _$CommercialDatabase extends GeneratedDatabase {
  _$CommercialDatabase(QueryExecutor e) : super(e);
  $CommercialDatabaseManager get managers => $CommercialDatabaseManager(this);
  late final $CommercialStorageMetadataTable commercialStorageMetadata =
      $CommercialStorageMetadataTable(this);
  late final $CommercialIdentityBindingsTable commercialIdentityBindings =
      $CommercialIdentityBindingsTable(this);
  late final $CommercialIdentitySnapshotsTable commercialIdentitySnapshots =
      $CommercialIdentitySnapshotsTable(this);
  @override
  Iterable<TableInfo<Table, Object?>> get allTables =>
      allSchemaEntities.whereType<TableInfo<Table, Object?>>();
  @override
  List<DatabaseSchemaEntity> get allSchemaEntities => [
    commercialStorageMetadata,
    commercialIdentityBindings,
    commercialIdentitySnapshots,
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
typedef $$CommercialIdentityBindingsTableCreateCompanionBuilder =
    CommercialIdentityBindingsCompanion Function({
      Value<int> singletonId,
      required int bindingContractVersion,
      required String apiOrigin,
      required String workspaceId,
      required String companyId,
      required String defaultBranchId,
      required String userId,
      required String deviceId,
      required int firstBoundAtUtcMicros,
    });
typedef $$CommercialIdentityBindingsTableUpdateCompanionBuilder =
    CommercialIdentityBindingsCompanion Function({
      Value<int> singletonId,
      Value<int> bindingContractVersion,
      Value<String> apiOrigin,
      Value<String> workspaceId,
      Value<String> companyId,
      Value<String> defaultBranchId,
      Value<String> userId,
      Value<String> deviceId,
      Value<int> firstBoundAtUtcMicros,
    });

final class $$CommercialIdentityBindingsTableReferences
    extends
        BaseReferences<
          _$CommercialDatabase,
          $CommercialIdentityBindingsTable,
          CommercialIdentityBinding
        > {
  $$CommercialIdentityBindingsTableReferences(
    super.$_db,
    super.$_table,
    super.$_typedResult,
  );

  static MultiTypedResultKey<
    $CommercialIdentitySnapshotsTable,
    List<CommercialIdentitySnapshot>
  >
  _commercialIdentitySnapshotsRefsTable(
    _$CommercialDatabase db,
  ) => MultiTypedResultKey.fromTable(
    db.commercialIdentitySnapshots,
    aliasName:
        'commercial_identity_binding__singleton_id__commercial_identity_snapshot__singleton_id',
  );

  $$CommercialIdentitySnapshotsTableProcessedTableManager
  get commercialIdentitySnapshotsRefs {
    final manager =
        $$CommercialIdentitySnapshotsTableTableManager(
          $_db,
          $_db.commercialIdentitySnapshots,
        ).filter(
          (f) => f.singletonId.singletonId.sqlEquals(
            $_itemColumn<int>('singleton_id')!,
          ),
        );

    final cache = $_typedResult.readTableOrNull(
      _commercialIdentitySnapshotsRefsTable($_db),
    );
    return ProcessedTableManager(
      manager.$state.copyWith(prefetchedData: cache),
    );
  }
}

class $$CommercialIdentityBindingsTableFilterComposer
    extends Composer<_$CommercialDatabase, $CommercialIdentityBindingsTable> {
  $$CommercialIdentityBindingsTableFilterComposer({
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

  ColumnFilters<int> get bindingContractVersion => $composableBuilder(
    column: $table.bindingContractVersion,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get apiOrigin => $composableBuilder(
    column: $table.apiOrigin,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get workspaceId => $composableBuilder(
    column: $table.workspaceId,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get companyId => $composableBuilder(
    column: $table.companyId,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get defaultBranchId => $composableBuilder(
    column: $table.defaultBranchId,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get userId => $composableBuilder(
    column: $table.userId,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get deviceId => $composableBuilder(
    column: $table.deviceId,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<int> get firstBoundAtUtcMicros => $composableBuilder(
    column: $table.firstBoundAtUtcMicros,
    builder: (column) => ColumnFilters(column),
  );

  Expression<bool> commercialIdentitySnapshotsRefs(
    Expression<bool> Function(
      $$CommercialIdentitySnapshotsTableFilterComposer f,
    )
    f,
  ) {
    final $$CommercialIdentitySnapshotsTableFilterComposer composer =
        $composerBuilder(
          composer: this,
          getCurrentColumn: (t) => t.singletonId,
          referencedTable: $db.commercialIdentitySnapshots,
          getReferencedColumn: (t) => t.singletonId,
          builder:
              (
                joinBuilder, {
                $addJoinBuilderToRootComposer,
                $removeJoinBuilderFromRootComposer,
              }) => $$CommercialIdentitySnapshotsTableFilterComposer(
                $db: $db,
                $table: $db.commercialIdentitySnapshots,
                $addJoinBuilderToRootComposer: $addJoinBuilderToRootComposer,
                joinBuilder: joinBuilder,
                $removeJoinBuilderFromRootComposer:
                    $removeJoinBuilderFromRootComposer,
              ),
        );
    return f(composer);
  }
}

class $$CommercialIdentityBindingsTableOrderingComposer
    extends Composer<_$CommercialDatabase, $CommercialIdentityBindingsTable> {
  $$CommercialIdentityBindingsTableOrderingComposer({
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

  ColumnOrderings<int> get bindingContractVersion => $composableBuilder(
    column: $table.bindingContractVersion,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get apiOrigin => $composableBuilder(
    column: $table.apiOrigin,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get workspaceId => $composableBuilder(
    column: $table.workspaceId,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get companyId => $composableBuilder(
    column: $table.companyId,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get defaultBranchId => $composableBuilder(
    column: $table.defaultBranchId,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get userId => $composableBuilder(
    column: $table.userId,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get deviceId => $composableBuilder(
    column: $table.deviceId,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<int> get firstBoundAtUtcMicros => $composableBuilder(
    column: $table.firstBoundAtUtcMicros,
    builder: (column) => ColumnOrderings(column),
  );
}

class $$CommercialIdentityBindingsTableAnnotationComposer
    extends Composer<_$CommercialDatabase, $CommercialIdentityBindingsTable> {
  $$CommercialIdentityBindingsTableAnnotationComposer({
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

  GeneratedColumn<int> get bindingContractVersion => $composableBuilder(
    column: $table.bindingContractVersion,
    builder: (column) => column,
  );

  GeneratedColumn<String> get apiOrigin =>
      $composableBuilder(column: $table.apiOrigin, builder: (column) => column);

  GeneratedColumn<String> get workspaceId => $composableBuilder(
    column: $table.workspaceId,
    builder: (column) => column,
  );

  GeneratedColumn<String> get companyId =>
      $composableBuilder(column: $table.companyId, builder: (column) => column);

  GeneratedColumn<String> get defaultBranchId => $composableBuilder(
    column: $table.defaultBranchId,
    builder: (column) => column,
  );

  GeneratedColumn<String> get userId =>
      $composableBuilder(column: $table.userId, builder: (column) => column);

  GeneratedColumn<String> get deviceId =>
      $composableBuilder(column: $table.deviceId, builder: (column) => column);

  GeneratedColumn<int> get firstBoundAtUtcMicros => $composableBuilder(
    column: $table.firstBoundAtUtcMicros,
    builder: (column) => column,
  );

  Expression<T> commercialIdentitySnapshotsRefs<T extends Object>(
    Expression<T> Function(
      $$CommercialIdentitySnapshotsTableAnnotationComposer a,
    )
    f,
  ) {
    final $$CommercialIdentitySnapshotsTableAnnotationComposer composer =
        $composerBuilder(
          composer: this,
          getCurrentColumn: (t) => t.singletonId,
          referencedTable: $db.commercialIdentitySnapshots,
          getReferencedColumn: (t) => t.singletonId,
          builder:
              (
                joinBuilder, {
                $addJoinBuilderToRootComposer,
                $removeJoinBuilderFromRootComposer,
              }) => $$CommercialIdentitySnapshotsTableAnnotationComposer(
                $db: $db,
                $table: $db.commercialIdentitySnapshots,
                $addJoinBuilderToRootComposer: $addJoinBuilderToRootComposer,
                joinBuilder: joinBuilder,
                $removeJoinBuilderFromRootComposer:
                    $removeJoinBuilderFromRootComposer,
              ),
        );
    return f(composer);
  }
}

class $$CommercialIdentityBindingsTableTableManager
    extends
        RootTableManager<
          _$CommercialDatabase,
          $CommercialIdentityBindingsTable,
          CommercialIdentityBinding,
          $$CommercialIdentityBindingsTableFilterComposer,
          $$CommercialIdentityBindingsTableOrderingComposer,
          $$CommercialIdentityBindingsTableAnnotationComposer,
          $$CommercialIdentityBindingsTableCreateCompanionBuilder,
          $$CommercialIdentityBindingsTableUpdateCompanionBuilder,
          (
            CommercialIdentityBinding,
            $$CommercialIdentityBindingsTableReferences,
          ),
          CommercialIdentityBinding,
          PrefetchHooks Function({bool commercialIdentitySnapshotsRefs})
        > {
  $$CommercialIdentityBindingsTableTableManager(
    _$CommercialDatabase db,
    $CommercialIdentityBindingsTable table,
  ) : super(
        TableManagerState(
          db: db,
          table: table,
          createFilteringComposer: () =>
              $$CommercialIdentityBindingsTableFilterComposer(
                $db: db,
                $table: table,
              ),
          createOrderingComposer: () =>
              $$CommercialIdentityBindingsTableOrderingComposer(
                $db: db,
                $table: table,
              ),
          createComputedFieldComposer: () =>
              $$CommercialIdentityBindingsTableAnnotationComposer(
                $db: db,
                $table: table,
              ),
          updateCompanionCallback:
              ({
                Value<int> singletonId = const Value.absent(),
                Value<int> bindingContractVersion = const Value.absent(),
                Value<String> apiOrigin = const Value.absent(),
                Value<String> workspaceId = const Value.absent(),
                Value<String> companyId = const Value.absent(),
                Value<String> defaultBranchId = const Value.absent(),
                Value<String> userId = const Value.absent(),
                Value<String> deviceId = const Value.absent(),
                Value<int> firstBoundAtUtcMicros = const Value.absent(),
              }) => CommercialIdentityBindingsCompanion(
                singletonId: singletonId,
                bindingContractVersion: bindingContractVersion,
                apiOrigin: apiOrigin,
                workspaceId: workspaceId,
                companyId: companyId,
                defaultBranchId: defaultBranchId,
                userId: userId,
                deviceId: deviceId,
                firstBoundAtUtcMicros: firstBoundAtUtcMicros,
              ),
          createCompanionCallback:
              ({
                Value<int> singletonId = const Value.absent(),
                required int bindingContractVersion,
                required String apiOrigin,
                required String workspaceId,
                required String companyId,
                required String defaultBranchId,
                required String userId,
                required String deviceId,
                required int firstBoundAtUtcMicros,
              }) => CommercialIdentityBindingsCompanion.insert(
                singletonId: singletonId,
                bindingContractVersion: bindingContractVersion,
                apiOrigin: apiOrigin,
                workspaceId: workspaceId,
                companyId: companyId,
                defaultBranchId: defaultBranchId,
                userId: userId,
                deviceId: deviceId,
                firstBoundAtUtcMicros: firstBoundAtUtcMicros,
              ),
          withReferenceMapper: (p0) => p0
              .map(
                (e) => (
                  e.readTable(table),
                  $$CommercialIdentityBindingsTableReferences(db, table, e),
                ),
              )
              .toList(),
          prefetchHooksCallback: ({commercialIdentitySnapshotsRefs = false}) {
            return PrefetchHooks(
              db: db,
              explicitlyWatchedTables: [
                if (commercialIdentitySnapshotsRefs)
                  db.commercialIdentitySnapshots,
              ],
              addJoins: null,
              getPrefetchedDataCallback: (items) async {
                return [
                  if (commercialIdentitySnapshotsRefs)
                    await $_getPrefetchedData<
                      CommercialIdentityBinding,
                      $CommercialIdentityBindingsTable,
                      CommercialIdentitySnapshot
                    >(
                      currentTable: table,
                      referencedTable:
                          $$CommercialIdentityBindingsTableReferences
                              ._commercialIdentitySnapshotsRefsTable(db),
                      managerFromTypedResult: (p0) =>
                          $$CommercialIdentityBindingsTableReferences(
                            db,
                            table,
                            p0,
                          ).commercialIdentitySnapshotsRefs,
                      referencedItemsForCurrentItem: (item, referencedItems) =>
                          referencedItems.where(
                            (e) => e.singletonId == item.singletonId,
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

typedef $$CommercialIdentityBindingsTableProcessedTableManager =
    ProcessedTableManager<
      _$CommercialDatabase,
      $CommercialIdentityBindingsTable,
      CommercialIdentityBinding,
      $$CommercialIdentityBindingsTableFilterComposer,
      $$CommercialIdentityBindingsTableOrderingComposer,
      $$CommercialIdentityBindingsTableAnnotationComposer,
      $$CommercialIdentityBindingsTableCreateCompanionBuilder,
      $$CommercialIdentityBindingsTableUpdateCompanionBuilder,
      (CommercialIdentityBinding, $$CommercialIdentityBindingsTableReferences),
      CommercialIdentityBinding,
      PrefetchHooks Function({bool commercialIdentitySnapshotsRefs})
    >;
typedef $$CommercialIdentitySnapshotsTableCreateCompanionBuilder =
    CommercialIdentitySnapshotsCompanion Function({
      Value<int> singletonId,
      required String workspaceCode,
      required String userDisplayName,
      required String role,
      required String deviceLabel,
      required int lastConfirmedAtUtcMicros,
    });
typedef $$CommercialIdentitySnapshotsTableUpdateCompanionBuilder =
    CommercialIdentitySnapshotsCompanion Function({
      Value<int> singletonId,
      Value<String> workspaceCode,
      Value<String> userDisplayName,
      Value<String> role,
      Value<String> deviceLabel,
      Value<int> lastConfirmedAtUtcMicros,
    });

final class $$CommercialIdentitySnapshotsTableReferences
    extends
        BaseReferences<
          _$CommercialDatabase,
          $CommercialIdentitySnapshotsTable,
          CommercialIdentitySnapshot
        > {
  $$CommercialIdentitySnapshotsTableReferences(
    super.$_db,
    super.$_table,
    super.$_typedResult,
  );

  static $CommercialIdentityBindingsTable _singletonIdTable(
    _$CommercialDatabase db,
  ) => db.commercialIdentityBindings.createAlias(
    'commercial_identity_snapshot__singleton_id__commercial_identity_binding__singleton_id',
  );

  $$CommercialIdentityBindingsTableProcessedTableManager get singletonId {
    final $_column = $_itemColumn<int>('singleton_id')!;

    final manager = $$CommercialIdentityBindingsTableTableManager(
      $_db,
      $_db.commercialIdentityBindings,
    ).filter((f) => f.singletonId.sqlEquals($_column));
    final item = $_typedResult.readTableOrNull(_singletonIdTable($_db));
    if (item == null) return manager;
    return ProcessedTableManager(
      manager.$state.copyWith(prefetchedData: [item]),
    );
  }
}

class $$CommercialIdentitySnapshotsTableFilterComposer
    extends Composer<_$CommercialDatabase, $CommercialIdentitySnapshotsTable> {
  $$CommercialIdentitySnapshotsTableFilterComposer({
    required super.$db,
    required super.$table,
    super.joinBuilder,
    super.$addJoinBuilderToRootComposer,
    super.$removeJoinBuilderFromRootComposer,
  });
  ColumnFilters<String> get workspaceCode => $composableBuilder(
    column: $table.workspaceCode,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get userDisplayName => $composableBuilder(
    column: $table.userDisplayName,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get role => $composableBuilder(
    column: $table.role,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get deviceLabel => $composableBuilder(
    column: $table.deviceLabel,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<int> get lastConfirmedAtUtcMicros => $composableBuilder(
    column: $table.lastConfirmedAtUtcMicros,
    builder: (column) => ColumnFilters(column),
  );

  $$CommercialIdentityBindingsTableFilterComposer get singletonId {
    final $$CommercialIdentityBindingsTableFilterComposer composer =
        $composerBuilder(
          composer: this,
          getCurrentColumn: (t) => t.singletonId,
          referencedTable: $db.commercialIdentityBindings,
          getReferencedColumn: (t) => t.singletonId,
          builder:
              (
                joinBuilder, {
                $addJoinBuilderToRootComposer,
                $removeJoinBuilderFromRootComposer,
              }) => $$CommercialIdentityBindingsTableFilterComposer(
                $db: $db,
                $table: $db.commercialIdentityBindings,
                $addJoinBuilderToRootComposer: $addJoinBuilderToRootComposer,
                joinBuilder: joinBuilder,
                $removeJoinBuilderFromRootComposer:
                    $removeJoinBuilderFromRootComposer,
              ),
        );
    return composer;
  }
}

class $$CommercialIdentitySnapshotsTableOrderingComposer
    extends Composer<_$CommercialDatabase, $CommercialIdentitySnapshotsTable> {
  $$CommercialIdentitySnapshotsTableOrderingComposer({
    required super.$db,
    required super.$table,
    super.joinBuilder,
    super.$addJoinBuilderToRootComposer,
    super.$removeJoinBuilderFromRootComposer,
  });
  ColumnOrderings<String> get workspaceCode => $composableBuilder(
    column: $table.workspaceCode,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get userDisplayName => $composableBuilder(
    column: $table.userDisplayName,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get role => $composableBuilder(
    column: $table.role,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get deviceLabel => $composableBuilder(
    column: $table.deviceLabel,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<int> get lastConfirmedAtUtcMicros => $composableBuilder(
    column: $table.lastConfirmedAtUtcMicros,
    builder: (column) => ColumnOrderings(column),
  );

  $$CommercialIdentityBindingsTableOrderingComposer get singletonId {
    final $$CommercialIdentityBindingsTableOrderingComposer composer =
        $composerBuilder(
          composer: this,
          getCurrentColumn: (t) => t.singletonId,
          referencedTable: $db.commercialIdentityBindings,
          getReferencedColumn: (t) => t.singletonId,
          builder:
              (
                joinBuilder, {
                $addJoinBuilderToRootComposer,
                $removeJoinBuilderFromRootComposer,
              }) => $$CommercialIdentityBindingsTableOrderingComposer(
                $db: $db,
                $table: $db.commercialIdentityBindings,
                $addJoinBuilderToRootComposer: $addJoinBuilderToRootComposer,
                joinBuilder: joinBuilder,
                $removeJoinBuilderFromRootComposer:
                    $removeJoinBuilderFromRootComposer,
              ),
        );
    return composer;
  }
}

class $$CommercialIdentitySnapshotsTableAnnotationComposer
    extends Composer<_$CommercialDatabase, $CommercialIdentitySnapshotsTable> {
  $$CommercialIdentitySnapshotsTableAnnotationComposer({
    required super.$db,
    required super.$table,
    super.joinBuilder,
    super.$addJoinBuilderToRootComposer,
    super.$removeJoinBuilderFromRootComposer,
  });
  GeneratedColumn<String> get workspaceCode => $composableBuilder(
    column: $table.workspaceCode,
    builder: (column) => column,
  );

  GeneratedColumn<String> get userDisplayName => $composableBuilder(
    column: $table.userDisplayName,
    builder: (column) => column,
  );

  GeneratedColumn<String> get role =>
      $composableBuilder(column: $table.role, builder: (column) => column);

  GeneratedColumn<String> get deviceLabel => $composableBuilder(
    column: $table.deviceLabel,
    builder: (column) => column,
  );

  GeneratedColumn<int> get lastConfirmedAtUtcMicros => $composableBuilder(
    column: $table.lastConfirmedAtUtcMicros,
    builder: (column) => column,
  );

  $$CommercialIdentityBindingsTableAnnotationComposer get singletonId {
    final $$CommercialIdentityBindingsTableAnnotationComposer composer =
        $composerBuilder(
          composer: this,
          getCurrentColumn: (t) => t.singletonId,
          referencedTable: $db.commercialIdentityBindings,
          getReferencedColumn: (t) => t.singletonId,
          builder:
              (
                joinBuilder, {
                $addJoinBuilderToRootComposer,
                $removeJoinBuilderFromRootComposer,
              }) => $$CommercialIdentityBindingsTableAnnotationComposer(
                $db: $db,
                $table: $db.commercialIdentityBindings,
                $addJoinBuilderToRootComposer: $addJoinBuilderToRootComposer,
                joinBuilder: joinBuilder,
                $removeJoinBuilderFromRootComposer:
                    $removeJoinBuilderFromRootComposer,
              ),
        );
    return composer;
  }
}

class $$CommercialIdentitySnapshotsTableTableManager
    extends
        RootTableManager<
          _$CommercialDatabase,
          $CommercialIdentitySnapshotsTable,
          CommercialIdentitySnapshot,
          $$CommercialIdentitySnapshotsTableFilterComposer,
          $$CommercialIdentitySnapshotsTableOrderingComposer,
          $$CommercialIdentitySnapshotsTableAnnotationComposer,
          $$CommercialIdentitySnapshotsTableCreateCompanionBuilder,
          $$CommercialIdentitySnapshotsTableUpdateCompanionBuilder,
          (
            CommercialIdentitySnapshot,
            $$CommercialIdentitySnapshotsTableReferences,
          ),
          CommercialIdentitySnapshot,
          PrefetchHooks Function({bool singletonId})
        > {
  $$CommercialIdentitySnapshotsTableTableManager(
    _$CommercialDatabase db,
    $CommercialIdentitySnapshotsTable table,
  ) : super(
        TableManagerState(
          db: db,
          table: table,
          createFilteringComposer: () =>
              $$CommercialIdentitySnapshotsTableFilterComposer(
                $db: db,
                $table: table,
              ),
          createOrderingComposer: () =>
              $$CommercialIdentitySnapshotsTableOrderingComposer(
                $db: db,
                $table: table,
              ),
          createComputedFieldComposer: () =>
              $$CommercialIdentitySnapshotsTableAnnotationComposer(
                $db: db,
                $table: table,
              ),
          updateCompanionCallback:
              ({
                Value<int> singletonId = const Value.absent(),
                Value<String> workspaceCode = const Value.absent(),
                Value<String> userDisplayName = const Value.absent(),
                Value<String> role = const Value.absent(),
                Value<String> deviceLabel = const Value.absent(),
                Value<int> lastConfirmedAtUtcMicros = const Value.absent(),
              }) => CommercialIdentitySnapshotsCompanion(
                singletonId: singletonId,
                workspaceCode: workspaceCode,
                userDisplayName: userDisplayName,
                role: role,
                deviceLabel: deviceLabel,
                lastConfirmedAtUtcMicros: lastConfirmedAtUtcMicros,
              ),
          createCompanionCallback:
              ({
                Value<int> singletonId = const Value.absent(),
                required String workspaceCode,
                required String userDisplayName,
                required String role,
                required String deviceLabel,
                required int lastConfirmedAtUtcMicros,
              }) => CommercialIdentitySnapshotsCompanion.insert(
                singletonId: singletonId,
                workspaceCode: workspaceCode,
                userDisplayName: userDisplayName,
                role: role,
                deviceLabel: deviceLabel,
                lastConfirmedAtUtcMicros: lastConfirmedAtUtcMicros,
              ),
          withReferenceMapper: (p0) => p0
              .map(
                (e) => (
                  e.readTable(table),
                  $$CommercialIdentitySnapshotsTableReferences(db, table, e),
                ),
              )
              .toList(),
          prefetchHooksCallback: ({singletonId = false}) {
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
                    if (singletonId) {
                      state =
                          state.withJoin(
                                currentTable: table,
                                currentColumn: table.singletonId,
                                referencedTable:
                                    $$CommercialIdentitySnapshotsTableReferences
                                        ._singletonIdTable(db),
                                referencedColumn:
                                    $$CommercialIdentitySnapshotsTableReferences
                                        ._singletonIdTable(db)
                                        .singletonId,
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

typedef $$CommercialIdentitySnapshotsTableProcessedTableManager =
    ProcessedTableManager<
      _$CommercialDatabase,
      $CommercialIdentitySnapshotsTable,
      CommercialIdentitySnapshot,
      $$CommercialIdentitySnapshotsTableFilterComposer,
      $$CommercialIdentitySnapshotsTableOrderingComposer,
      $$CommercialIdentitySnapshotsTableAnnotationComposer,
      $$CommercialIdentitySnapshotsTableCreateCompanionBuilder,
      $$CommercialIdentitySnapshotsTableUpdateCompanionBuilder,
      (
        CommercialIdentitySnapshot,
        $$CommercialIdentitySnapshotsTableReferences,
      ),
      CommercialIdentitySnapshot,
      PrefetchHooks Function({bool singletonId})
    >;

class $CommercialDatabaseManager {
  final _$CommercialDatabase _db;
  $CommercialDatabaseManager(this._db);
  $$CommercialStorageMetadataTableTableManager get commercialStorageMetadata =>
      $$CommercialStorageMetadataTableTableManager(
        _db,
        _db.commercialStorageMetadata,
      );
  $$CommercialIdentityBindingsTableTableManager
  get commercialIdentityBindings =>
      $$CommercialIdentityBindingsTableTableManager(
        _db,
        _db.commercialIdentityBindings,
      );
  $$CommercialIdentitySnapshotsTableTableManager
  get commercialIdentitySnapshots =>
      $$CommercialIdentitySnapshotsTableTableManager(
        _db,
        _db.commercialIdentitySnapshots,
      );
}
