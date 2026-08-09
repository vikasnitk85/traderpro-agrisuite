import 'commercial_database_key_store.dart';
import 'commercial_storage_failure.dart';

enum CommercialDatabaseFileState { absent, present, unreadable }

enum CommercialProvisioningContext {
  explicitFirstInitialization,
  normalStartup,
}

enum CommercialStorageStartupAction {
  provisionNew,
  resumeBeforeKeyWrite,
  resumeWithExistingKey,
  openExisting,
  inspectUnreadable,
  waitForExplicitInitialization,
  failClosed,
}

final class CommercialStorageSnapshot {
  const CommercialStorageSnapshot({
    required this.finalDatabaseState,
    required this.keyStoreState,
    required this.context,
    required this.hasMatchingPreparingMarker,
    required this.initializingDatabaseExists,
  });

  final CommercialDatabaseFileState finalDatabaseState;
  final CommercialDatabaseKeyStoreStateKind keyStoreState;
  final CommercialProvisioningContext context;
  final bool hasMatchingPreparingMarker;
  final bool initializingDatabaseExists;
}

final class CommercialStorageStartupDecision {
  const CommercialStorageStartupDecision({
    required this.action,
    required this.safeCode,
    required this.mayProvisionKey,
    required this.mayOpenDatabase,
    required this.retryMayHelp,
    this.failure,
  });

  final CommercialStorageStartupAction action;
  final String safeCode;
  final bool mayProvisionKey;
  final bool mayOpenDatabase;
  final bool retryMayHelp;
  final CommercialStorageFailureCode? failure;
}

final class CommercialStorageStateMachine {
  const CommercialStorageStateMachine();

  CommercialStorageStartupDecision decide(CommercialStorageSnapshot snapshot) {
    if (snapshot.finalDatabaseState != CommercialDatabaseFileState.absent) {
      return _forExistingDatabase(snapshot);
    }
    return _forAbsentFinalDatabase(snapshot);
  }

  CommercialStorageStartupDecision _forExistingDatabase(
    CommercialStorageSnapshot snapshot,
  ) {
    if (snapshot.keyStoreState == CommercialDatabaseKeyStoreStateKind.valid) {
      final unreadable =
          snapshot.finalDatabaseState == CommercialDatabaseFileState.unreadable;
      return CommercialStorageStartupDecision(
        action: unreadable
            ? CommercialStorageStartupAction.inspectUnreadable
            : CommercialStorageStartupAction.openExisting,
        safeCode: unreadable
            ? 'COMMERCIAL_DATABASE_INSPECT'
            : 'COMMERCIAL_DATABASE_OPEN',
        mayProvisionKey: false,
        mayOpenDatabase: true,
        retryMayHelp: unreadable,
      );
    }
    return _keyFailure(snapshot.keyStoreState);
  }

  CommercialStorageStartupDecision _forAbsentFinalDatabase(
    CommercialStorageSnapshot snapshot,
  ) {
    switch (snapshot.keyStoreState) {
      case CommercialDatabaseKeyStoreStateKind.valid:
        if (snapshot.hasMatchingPreparingMarker) {
          return const CommercialStorageStartupDecision(
            action: CommercialStorageStartupAction.resumeWithExistingKey,
            safeCode: 'COMMERCIAL_DATABASE_INITIALIZATION_RESUME',
            mayProvisionKey: false,
            mayOpenDatabase: true,
            retryMayHelp: true,
          );
        }
        return _initializationMismatch();
      case CommercialDatabaseKeyStoreStateKind.uninitialized:
        if (snapshot.hasMatchingPreparingMarker &&
            !snapshot.initializingDatabaseExists) {
          return const CommercialStorageStartupDecision(
            action: CommercialStorageStartupAction.resumeBeforeKeyWrite,
            safeCode: 'COMMERCIAL_DATABASE_PROVISIONING_RESUME_BEFORE_KEY',
            mayProvisionKey: true,
            mayOpenDatabase: true,
            retryMayHelp: true,
          );
        }
        if (snapshot.initializingDatabaseExists) {
          return _keyFailure(CommercialDatabaseKeyStoreStateKind.missing);
        }
        if (snapshot.context ==
            CommercialProvisioningContext.explicitFirstInitialization) {
          return const CommercialStorageStartupDecision(
            action: CommercialStorageStartupAction.provisionNew,
            safeCode: 'COMMERCIAL_DATABASE_PROVISION',
            mayProvisionKey: true,
            mayOpenDatabase: true,
            retryMayHelp: true,
          );
        }
        return const CommercialStorageStartupDecision(
          action: CommercialStorageStartupAction.waitForExplicitInitialization,
          safeCode: 'COMMERCIAL_DATABASE_UNINITIALIZED',
          mayProvisionKey: false,
          mayOpenDatabase: false,
          retryMayHelp: true,
        );
      case CommercialDatabaseKeyStoreStateKind.missing:
      case CommercialDatabaseKeyStoreStateKind.malformed:
      case CommercialDatabaseKeyStoreStateKind.unavailable:
      case CommercialDatabaseKeyStoreStateKind.invalidated:
      case CommercialDatabaseKeyStoreStateKind.unexpected:
        return _keyFailure(snapshot.keyStoreState);
    }
  }

  CommercialStorageStartupDecision _initializationMismatch() =>
      const CommercialStorageStartupDecision(
        action: CommercialStorageStartupAction.failClosed,
        safeCode: 'COMMERCIAL_DATABASE_INITIALIZATION_MISMATCH',
        mayProvisionKey: false,
        mayOpenDatabase: false,
        retryMayHelp: false,
        failure: CommercialStorageFailureCode.databaseInitializationIncomplete,
      );

  CommercialStorageStartupDecision _keyFailure(
    CommercialDatabaseKeyStoreStateKind state,
  ) {
    final failure = switch (state) {
      CommercialDatabaseKeyStoreStateKind.uninitialized ||
      CommercialDatabaseKeyStoreStateKind.missing =>
        CommercialStorageFailureCode.secureStoreMissing,
      CommercialDatabaseKeyStoreStateKind.malformed =>
        CommercialStorageFailureCode.secureStoreMalformed,
      CommercialDatabaseKeyStoreStateKind.unavailable =>
        CommercialStorageFailureCode.secureStoreUnavailable,
      CommercialDatabaseKeyStoreStateKind.invalidated =>
        CommercialStorageFailureCode.secureStoreInvalidated,
      CommercialDatabaseKeyStoreStateKind.unexpected =>
        CommercialStorageFailureCode.unexpectedStorageFailure,
      CommercialDatabaseKeyStoreStateKind.valid =>
        CommercialStorageFailureCode.unexpectedStorageFailure,
    };
    return CommercialStorageStartupDecision(
      action: CommercialStorageStartupAction.failClosed,
      safeCode: failure.safeCode,
      mayProvisionKey: false,
      mayOpenDatabase: false,
      retryMayHelp:
          state == CommercialDatabaseKeyStoreStateKind.unavailable ||
          state == CommercialDatabaseKeyStoreStateKind.unexpected,
      failure: failure,
    );
  }
}
