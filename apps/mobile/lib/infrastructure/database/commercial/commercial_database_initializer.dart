import 'dart:io';

import 'package:uuid/uuid.dart';

import '../../../core/security/commercial_database_key_material.dart';
import '../../../core/security/commercial_database_key_store.dart';
import '../../../core/security/commercial_provisioning_marker.dart';
import '../../../core/security/commercial_storage_failure.dart';
import '../../../core/security/commercial_storage_state_machine.dart';
import '../../../core/security/storage_diagnostics.dart';
import '../../storage/commercial_provisioning_marker_store.dart';
import 'commercial_database.dart';
import 'commercial_database_opener.dart';
import 'commercial_database_paths.dart';

abstract interface class CommercialStorageUuidGenerator {
  String generate();
}

final class RandomCommercialStorageUuidGenerator
    implements CommercialStorageUuidGenerator {
  RandomCommercialStorageUuidGenerator({Uuid? uuid})
    : _uuid = uuid ?? const Uuid();

  final Uuid _uuid;

  @override
  String generate() => _uuid.v4();
}

final class CommercialDatabaseInitializer {
  CommercialDatabaseInitializer({
    required this.paths,
    required this.keyStore,
    required this.markerStore,
    this.opener = const CommercialDatabaseOpener(),
    DatabaseKeyGenerator? keyGenerator,
    CommercialStorageUuidGenerator? uuidGenerator,
    this.diagnosticSink = const NoopStorageDiagnosticSink(),
    DateTime Function()? clock,
    this.coarsePlatform = 'android',
  }) : _keyGenerator = keyGenerator ?? SecureRandomDatabaseKeyGenerator(),
       _uuidGenerator = uuidGenerator ?? RandomCommercialStorageUuidGenerator(),
       _clock = clock ?? DateTime.now;

  final CommercialDatabasePaths paths;
  final CommercialDatabaseKeyStore keyStore;
  final CommercialProvisioningMarkerStore markerStore;
  final CommercialDatabaseConnectionOpener opener;
  final DatabaseKeyGenerator _keyGenerator;
  final CommercialStorageUuidGenerator _uuidGenerator;
  final StorageDiagnosticSink diagnosticSink;
  final DateTime Function() _clock;
  final String coarsePlatform;
  final CommercialStorageStateMachine _stateMachine =
      const CommercialStorageStateMachine();

  Future<CommercialDatabase>? _initialization;
  CommercialDatabase? _openedDatabase;

  Future<CommercialDatabase> initialize({
    required CommercialProvisioningContext context,
  }) {
    final opened = _openedDatabase;
    if (opened != null) {
      return Future<CommercialDatabase>.value(opened);
    }
    final inFlight = _initialization;
    if (inFlight != null) {
      return inFlight;
    }
    final future = _initialize(context);
    _initialization = future;
    return future.whenComplete(() {
      if (identical(_initialization, future)) {
        _initialization = null;
      }
    });
  }

  Future<void> close() async {
    final inFlight = _initialization;
    if (inFlight != null) {
      try {
        await inFlight;
      } on Object {
        // The original caller receives the typed initialization failure.
      }
    }
    final database = _openedDatabase;
    _openedDatabase = null;
    if (database != null) {
      await database.close();
    }
  }

  Future<CommercialDatabase> _initialize(
    CommercialProvisioningContext context,
  ) async {
    final correlationId = _uuidGenerator.generate();
    CommercialDatabaseKeyMaterial? keyMaterial;
    CommercialDatabase? database;
    try {
      final marker = await markerStore.read();
      final finalState = await _finalDatabaseState();
      final initializingExists = await paths.initializingDatabase.exists();
      // A zero-length final database is still a physical fact. Opening it
      // would let SQLite initialize the file and destroy evidence that must
      // instead be reconciled explicitly.
      if (finalState == CommercialDatabaseFileState.unreadable) {
        throw const CommercialStorageException(
          CommercialStorageFailureCode.databaseWrongKeyOrUnreadable,
          phase: 'final-database-unreadable',
        );
      }
      if (finalState != CommercialDatabaseFileState.absent &&
          initializingExists) {
        throw const CommercialStorageException(
          CommercialStorageFailureCode.databaseInitializationIncomplete,
          phase: 'final-and-initializing-coexist',
        );
      }
      if (finalState != CommercialDatabaseFileState.absent && marker == null) {
        throw const CommercialStorageException(
          CommercialStorageFailureCode.databaseInitializationIncomplete,
          phase: 'final-without-marker',
        );
      }
      if (initializingExists && marker == null) {
        throw const CommercialStorageException(
          CommercialStorageFailureCode.databaseInitializationIncomplete,
          phase: 'initializing-without-marker',
        );
      }

      final keyExpected =
          finalState != CommercialDatabaseFileState.absent ||
          initializingExists ||
          marker?.state == CommercialProvisioningMarkerState.complete;
      final keyState = await keyStore.read(keyExpected: keyExpected);
      final decision = _stateMachine.decide(
        CommercialStorageSnapshot(
          finalDatabaseState: finalState,
          keyStoreState: keyState.kind,
          context: context,
          hasMatchingPreparingMarker:
              marker?.state == CommercialProvisioningMarkerState.preparing,
          initializingDatabaseExists: initializingExists,
        ),
      );
      _record(
        safeCode: decision.safeCode,
        phase: StorageDiagnosticPhase.inspect,
        databaseExists: finalState != CommercialDatabaseFileState.absent,
        secureStoreState: keyState.kind.name,
        correlationId: correlationId,
      );

      switch (decision.action) {
        case CommercialStorageStartupAction.provisionNew:
          final preparing = CommercialProvisioningMarker(
            version: 1,
            generation: _uuidGenerator.generate(),
            installationId: _uuidGenerator.generate(),
            databaseInstanceId: _uuidGenerator.generate(),
            state: CommercialProvisioningMarkerState.preparing,
          );
          await markerStore.write(preparing);
          keyMaterial = CommercialDatabaseKeyMaterial.generate(_keyGenerator);
          await keyStore.writeInitial(keyMaterial);
          database = await _initializeTemporaryDatabase(
            marker: preparing,
            keyMaterial: keyMaterial,
          );
          break;
        case CommercialStorageStartupAction.resumeBeforeKeyWrite:
          final preparing = marker!;
          keyMaterial = CommercialDatabaseKeyMaterial.generate(_keyGenerator);
          await keyStore.writeInitial(keyMaterial);
          database = await _initializeTemporaryDatabase(
            marker: preparing,
            keyMaterial: keyMaterial,
          );
          break;
        case CommercialStorageStartupAction.resumeWithExistingKey:
          final preparing = marker!;
          keyMaterial =
              (keyState as CommercialDatabaseKeyStoreValid).keyMaterial;
          database = await _initializeTemporaryDatabase(
            marker: preparing,
            keyMaterial: keyMaterial,
          );
          break;
        case CommercialStorageStartupAction.openExisting:
        case CommercialStorageStartupAction.inspectUnreadable:
          final existingMarker = marker!;
          keyMaterial =
              (keyState as CommercialDatabaseKeyStoreValid).keyMaterial;
          database = await opener.open(
            file: paths.finalDatabase,
            keyMaterial: keyMaterial,
          );
          await database.verifyFoundationMetadata(existingMarker);
          if (existingMarker.state ==
              CommercialProvisioningMarkerState.preparing) {
            await markerStore.write(existingMarker.complete());
          }
          break;
        case CommercialStorageStartupAction.waitForExplicitInitialization:
        case CommercialStorageStartupAction.failClosed:
          throw CommercialStorageException(
            decision.failure ??
                CommercialStorageFailureCode.databaseInitializationIncomplete,
            phase: decision.safeCode,
          );
      }

      _openedDatabase = database;
      return database;
    } on CommercialStorageException catch (error) {
      if (database != null && !identical(database, _openedDatabase)) {
        await database.close();
      }
      _record(
        safeCode: error.code.safeCode,
        phase: StorageDiagnosticPhase.verify,
        databaseExists: await paths.finalDatabase.exists(),
        secureStoreState: 'redacted-failure',
        correlationId: correlationId,
      );
      rethrow;
    } on FileSystemException {
      if (database != null && !identical(database, _openedDatabase)) {
        await database.close();
      }
      throw const CommercialStorageException(
        CommercialStorageFailureCode.storageUnavailable,
        phase: 'filesystem',
      );
    } on Object {
      if (database != null && !identical(database, _openedDatabase)) {
        await database.close();
      }
      throw const CommercialStorageException(
        CommercialStorageFailureCode.unexpectedStorageFailure,
        phase: 'initializer',
      );
    } finally {
      keyMaterial?.dispose();
    }
  }

  Future<CommercialDatabase> _initializeTemporaryDatabase({
    required CommercialProvisioningMarker marker,
    required CommercialDatabaseKeyMaterial keyMaterial,
  }) async {
    await paths.directory.create(recursive: true);
    if (await paths.finalDatabase.exists()) {
      throw const CommercialStorageException(
        CommercialStorageFailureCode.databaseInitializationIncomplete,
        phase: 'final-created-during-initialization',
      );
    }

    var database = await opener.open(
      file: paths.initializingDatabase,
      keyMaterial: keyMaterial,
    );
    try {
      await database.ensureFoundationMetadata(
        marker: marker,
        createdAtUtcMicros: _clock().toUtc().microsecondsSinceEpoch,
      );
      await database.verifyFoundationMetadata(marker);
      await database.checkpointWal();
    } finally {
      await database.close();
    }

    database = await opener.open(
      file: paths.initializingDatabase,
      keyMaterial: keyMaterial,
    );
    try {
      await database.verifyFoundationMetadata(marker);
      await database.checkpointWal();
    } finally {
      await database.close();
    }

    for (final sidecar in paths.sidecarsFor(paths.initializingDatabase)) {
      if (await sidecar.exists()) {
        throw const CommercialStorageException(
          CommercialStorageFailureCode.databaseInitializationIncomplete,
          phase: 'initializing-sidecar-remains',
        );
      }
    }
    if (await paths.finalDatabase.exists()) {
      throw const CommercialStorageException(
        CommercialStorageFailureCode.databaseInitializationIncomplete,
        phase: 'promotion-target-exists',
      );
    }
    await paths.initializingDatabase.rename(paths.finalDatabase.path);

    final finalDatabase = await opener.open(
      file: paths.finalDatabase,
      keyMaterial: keyMaterial,
    );
    try {
      await finalDatabase.verifyFoundationMetadata(marker);
      await markerStore.write(marker.complete());
      return finalDatabase;
    } on Object {
      await finalDatabase.close();
      rethrow;
    }
  }

  Future<CommercialDatabaseFileState> _finalDatabaseState() async {
    if (!await paths.finalDatabase.exists()) {
      return CommercialDatabaseFileState.absent;
    }
    return await paths.finalDatabase.length() == 0
        ? CommercialDatabaseFileState.unreadable
        : CommercialDatabaseFileState.present;
  }

  void _record({
    required String safeCode,
    required StorageDiagnosticPhase phase,
    required bool databaseExists,
    required String secureStoreState,
    required String correlationId,
  }) {
    diagnosticSink.record(
      StorageDiagnosticEvent(
        safeCode: safeCode,
        phase: phase,
        databaseExists: databaseExists,
        secureStoreState: secureStoreState,
        schemaContractVersion: CommercialDatabase.currentSchemaVersion,
        storageContractVersion: CommercialDatabase.storageContractVersion,
        engineContractVersion: 1,
        coarsePlatform: coarsePlatform,
        timestampUtc: _clock().toUtc(),
        ephemeralCorrelationId: correlationId,
      ),
    );
  }
}
