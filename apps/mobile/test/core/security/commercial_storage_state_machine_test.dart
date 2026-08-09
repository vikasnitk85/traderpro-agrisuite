import 'package:flutter_test/flutter_test.dart';
import 'package:traderpro_agrisuite_mobile/core/security/commercial_database_key_store.dart';
import 'package:traderpro_agrisuite_mobile/core/security/commercial_storage_state_machine.dart';

void main() {
  const machine = CommercialStorageStateMachine();

  test(
    'all thirty approved final-db/key/context combinations are classified',
    () {
      var combinations = 0;
      for (final databaseState in CommercialDatabaseFileState.values) {
        for (final keyState in <CommercialDatabaseKeyStoreStateKind>[
          CommercialDatabaseKeyStoreStateKind.uninitialized,
          CommercialDatabaseKeyStoreStateKind.valid,
          CommercialDatabaseKeyStoreStateKind.malformed,
          CommercialDatabaseKeyStoreStateKind.unavailable,
          CommercialDatabaseKeyStoreStateKind.invalidated,
        ]) {
          for (final context in CommercialProvisioningContext.values) {
            combinations++;
            final decision = machine.decide(
              CommercialStorageSnapshot(
                finalDatabaseState: databaseState,
                keyStoreState: keyState,
                context: context,
                hasMatchingPreparingMarker: false,
                initializingDatabaseExists: false,
              ),
            );

            if (databaseState != CommercialDatabaseFileState.absent &&
                keyState != CommercialDatabaseKeyStoreStateKind.valid) {
              expect(
                decision.action,
                CommercialStorageStartupAction.failClosed,
              );
              expect(decision.mayProvisionKey, isFalse);
              expect(decision.mayOpenDatabase, isFalse);
            }
            if (databaseState != CommercialDatabaseFileState.absent &&
                keyState == CommercialDatabaseKeyStoreStateKind.valid) {
              expect(decision.mayOpenDatabase, isTrue);
              expect(decision.mayProvisionKey, isFalse);
            }
            if (databaseState == CommercialDatabaseFileState.absent &&
                keyState == CommercialDatabaseKeyStoreStateKind.valid) {
              expect(
                decision.action,
                CommercialStorageStartupAction.failClosed,
              );
            }
            if (databaseState == CommercialDatabaseFileState.absent &&
                keyState == CommercialDatabaseKeyStoreStateKind.uninitialized &&
                context ==
                    CommercialProvisioningContext.explicitFirstInitialization) {
              expect(
                decision.action,
                CommercialStorageStartupAction.provisionNew,
              );
              expect(decision.mayProvisionKey, isTrue);
            }
          }
        }
      }
      expect(combinations, 30);
    },
  );

  test('valid key resumes only with matching preparing marker', () {
    final decision = machine.decide(
      const CommercialStorageSnapshot(
        finalDatabaseState: CommercialDatabaseFileState.absent,
        keyStoreState: CommercialDatabaseKeyStoreStateKind.valid,
        context: CommercialProvisioningContext.normalStartup,
        hasMatchingPreparingMarker: true,
        initializingDatabaseExists: true,
      ),
    );
    expect(
      decision.action,
      CommercialStorageStartupAction.resumeWithExistingKey,
    );
    expect(decision.mayProvisionKey, isFalse);
    expect(decision.mayOpenDatabase, isTrue);
  });

  test('pre-key crash can resume only before an initializing DB exists', () {
    final safe = machine.decide(
      const CommercialStorageSnapshot(
        finalDatabaseState: CommercialDatabaseFileState.absent,
        keyStoreState: CommercialDatabaseKeyStoreStateKind.uninitialized,
        context: CommercialProvisioningContext.normalStartup,
        hasMatchingPreparingMarker: true,
        initializingDatabaseExists: false,
      ),
    );
    final unsafe = machine.decide(
      const CommercialStorageSnapshot(
        finalDatabaseState: CommercialDatabaseFileState.absent,
        keyStoreState: CommercialDatabaseKeyStoreStateKind.uninitialized,
        context: CommercialProvisioningContext.normalStartup,
        hasMatchingPreparingMarker: true,
        initializingDatabaseExists: true,
      ),
    );
    expect(safe.action, CommercialStorageStartupAction.resumeBeforeKeyWrite);
    expect(safe.mayProvisionKey, isTrue);
    expect(unsafe.action, CommercialStorageStartupAction.failClosed);
    expect(unsafe.mayProvisionKey, isFalse);
  });

  test(
    'unexpected secure-store failure is retryable but remains fail closed',
    () {
      final decision = machine.decide(
        const CommercialStorageSnapshot(
          finalDatabaseState: CommercialDatabaseFileState.present,
          keyStoreState: CommercialDatabaseKeyStoreStateKind.unexpected,
          context: CommercialProvisioningContext.normalStartup,
          hasMatchingPreparingMarker: false,
          initializingDatabaseExists: false,
        ),
      );
      expect(decision.action, CommercialStorageStartupAction.failClosed);
      expect(decision.retryMayHelp, isTrue);
      expect(decision.mayProvisionKey, isFalse);
    },
  );
}
