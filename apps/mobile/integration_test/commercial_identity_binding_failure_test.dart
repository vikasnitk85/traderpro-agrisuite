import 'dart:io';
import 'dart:typed_data';

import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:integration_test/integration_test.dart';
import 'package:path_provider/path_provider.dart';
import 'package:traderpro_agrisuite_mobile/app/commercial_identity_controller.dart';
import 'package:traderpro_agrisuite_mobile/core/identity/commercial_authentication_status.dart';
import 'package:traderpro_agrisuite_mobile/core/identity/commercial_identity_failure.dart';
import 'package:traderpro_agrisuite_mobile/core/identity/commercial_identity_models.dart';
import 'package:traderpro_agrisuite_mobile/core/identity/commercial_identity_ports.dart';
import 'package:traderpro_agrisuite_mobile/core/security/commercial_database_key_store.dart';
import 'package:traderpro_agrisuite_mobile/core/security/commercial_storage_state_machine.dart';
import 'package:traderpro_agrisuite_mobile/infrastructure/database/commercial/commercial_database_initializer.dart';
import 'package:traderpro_agrisuite_mobile/infrastructure/database/commercial/commercial_database_paths.dart';
import 'package:traderpro_agrisuite_mobile/infrastructure/database/commercial/commercial_identity_binding_repository.dart';
import 'package:traderpro_agrisuite_mobile/infrastructure/identity/commercial_identity_service.dart';
import 'package:traderpro_agrisuite_mobile/infrastructure/identity/commercial_refresh_coordinator.dart';
import 'package:traderpro_agrisuite_mobile/infrastructure/identity/flutter_secure_commercial_identity_credential_store.dart';
import 'package:traderpro_agrisuite_mobile/infrastructure/security/flutter_secure_commercial_database_key_store.dart';
import 'package:traderpro_agrisuite_mobile/infrastructure/storage/commercial_provisioning_marker_store.dart';

const _deviceId = '019fc400-0000-7000-8000-000000000201';
const _workspaceId = '11111111-1111-4111-8111-111111111111';
const _companyId = '22222222-2222-4222-8222-222222222222';
const _branchId = '019fc400-0000-7000-8000-000000000202';
const _userId = '33333333-3333-4333-8333-333333333333';
const _familyId = '44444444-4444-4444-8444-444444444444';
const _origin = 'https://identity.device.test/';
const _securePrefix = 'b3_binding_failure_matrix_';

void main() {
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();

  testWidgets(
    'binding storage failures retire session authority and recover safely',
    (tester) async {
      final support = await getApplicationSupportDirectory();
      final root = Directory(
        '${support.path}${Platform.pathSeparator}b3-binding-failure-matrix',
      );
      final identityStrings = _IsolatedIdentityStringStore(_securePrefix);
      final keyStrings = _IsolatedKeyStringStore(_securePrefix);
      final durableCredentialStore =
          FlutterSecureCommercialIdentityCredentialStore(
            secureStringStore: identityStrings,
          );
      final credentialStore = _FaultInjectingCredentialStore(
        durableCredentialStore,
      );
      final keyStore = FlutterSecureCommercialDatabaseKeyStore(
        secureStringStore: keyStrings,
      );
      final paths = CommercialDatabasePaths.fromApplicationSupport(root);
      final initializer = CommercialDatabaseInitializer(
        paths: paths,
        keyStore: keyStore,
        markerStore: CommercialProvisioningMarkerStore(
          paths.provisioningMarker,
        ),
      );
      final coordinators = <CommercialRefreshCoordinator>[];

      if (await root.exists()) {
        await root.delete(recursive: true);
      }
      await identityStrings.clear();
      await keyStrings.clear();

      try {
        final database = await initializer.initialize(
          context: CommercialProvisioningContext.explicitFirstInitialization,
        );
        final bindingRepository = CommercialIdentityBindingRepository(database);
        final transport = _DeviceMatrixTransport();
        final now = DateTime.utc(2026, 8, 18, 12);
        final originalKey = await _readKeyBytes(keyStore);
        var installationReadFails = true;
        Future<String?> installationReferenceReader() async {
          if (installationReadFails) {
            await database
                .customSelect(
                  'SELECT installation_id FROM b3_missing_storage_metadata',
                )
                .getSingleOrNull();
          }
          final metadata = await database
              .select(database.commercialStorageMetadata)
              .getSingleOrNull();
          return metadata?.installationId;
        }

        final activationStorageFailure = _createController(
          credentialStore: credentialStore,
          bindingPort: bindingRepository,
          transport: transport,
          installationReferenceReader: installationReferenceReader,
          now: now,
        );
        coordinators.add(activationStorageFailure.refreshCoordinator);
        await activationStorageFailure.start();
        expect(
          activationStorageFailure.status,
          CommercialAuthenticationStatus.deviceUnregistered,
        );
        await activationStorageFailure.activate(
          workspaceCode: 'FARM-ONE',
          activationCode: 'ACT-SECRET',
          deviceLabel: 'Android failure matrix Device',
        );
        expect(
          activationStorageFailure.status,
          CommercialAuthenticationStatus.lockedFailClosed,
        );
        expect(
          activationStorageFailure.safeFailureCode,
          'IDENTITY_BINDING_STORAGE_FAILED',
        );
        expect(
          activationStorageFailure.refreshCoordinator.hasMemoryAccessToken,
          isFalse,
        );
        expect(
          (await credentialStore.readActivationAttempt()).state,
          CommercialCredentialStoreState.empty,
        );
        expect(
          (await credentialStore.readDeviceCredential()).state,
          CommercialCredentialStoreState.empty,
        );
        expect(await bindingRepository.readBinding(), isNull);
        expect(await bindingRepository.readSnapshot(), isNull);
        expect(await _readKeyBytes(keyStore), orderedEquals(originalKey));

        activationStorageFailure.dispose();
        await activationStorageFailure.refreshCoordinator.dispose();
        coordinators.remove(activationStorageFailure.refreshCoordinator);
        installationReadFails = false;

        final activationRecovery = _createController(
          credentialStore: credentialStore,
          bindingPort: bindingRepository,
          transport: transport,
          installationReferenceReader: installationReferenceReader,
          now: now,
        );
        coordinators.add(activationRecovery.refreshCoordinator);
        await activationRecovery.start();
        expect(
          activationRecovery.status,
          CommercialAuthenticationStatus.deviceUnregistered,
        );
        await activationRecovery.activate(
          workspaceCode: 'FARM-ONE',
          activationCode: 'ACT-SECRET',
          deviceLabel: 'Android failure matrix Device',
        );
        expect(
          activationRecovery.status,
          CommercialAuthenticationStatus.deviceRegisteredNoSession,
        );
        expect(
          activationRecovery.refreshCoordinator.hasMemoryAccessToken,
          isFalse,
        );
        expect(
          (await credentialStore.readActivationAttempt()).state,
          CommercialCredentialStoreState.empty,
        );
        expect(
          (await credentialStore.readDeviceCredential()).value?.deviceId.value,
          _deviceId,
        );
        expect(await _readKeyBytes(keyStore), orderedEquals(originalKey));

        activationRecovery.dispose();
        await activationRecovery.refreshCoordinator.dispose();
        coordinators.remove(activationRecovery.refreshCoordinator);

        await database.customStatement('''
          CREATE TRIGGER b3_fail_first_snapshot_insert
          BEFORE INSERT ON commercial_identity_snapshot
          BEGIN
            SELECT RAISE(ABORT, 'synthetic first binding failure');
          END
        ''');
        final first = _createController(
          credentialStore: credentialStore,
          bindingPort: bindingRepository,
          transport: transport,
          installationReferenceReader: installationReferenceReader,
          now: now,
        );
        coordinators.add(first.refreshCoordinator);
        await first.start();
        await first.login(
          workspaceCode: 'FARM-ONE',
          login: 'owner@example.test',
          password: 'correct horse battery staple',
        );

        expect(first.status, CommercialAuthenticationStatus.lockedFailClosed);
        expect(first.safeFailureCode, 'IDENTITY_BINDING_STORAGE_FAILED');
        expect(first.identity, isNull);
        expect(first.refreshCoordinator.hasMemoryAccessToken, isFalse);
        expect(
          (await credentialStore.readRefreshCredential()).state,
          CommercialCredentialStoreState.empty,
        );
        expect(
          (await credentialStore.readDeviceCredential()).value?.deviceId.value,
          _deviceId,
        );
        expect(await bindingRepository.readBinding(), isNull);
        expect(await bindingRepository.readSnapshot(), isNull);
        expect(await _readKeyBytes(keyStore), orderedEquals(originalKey));

        first.dispose();
        await first.refreshCoordinator.dispose();
        coordinators.remove(first.refreshCoordinator);
        await database.customStatement(
          'DROP TRIGGER b3_fail_first_snapshot_insert',
        );

        final recovered = _createController(
          credentialStore: credentialStore,
          bindingPort: bindingRepository,
          transport: transport,
          installationReferenceReader: installationReferenceReader,
          now: now,
        );
        coordinators.add(recovered.refreshCoordinator);
        await recovered.start();
        expect(
          recovered.status,
          CommercialAuthenticationStatus.deviceRegisteredNoSession,
        );
        await recovered.login(
          workspaceCode: 'FARM-ONE',
          login: 'owner@example.test',
          password: 'correct horse battery staple',
        );
        expect(
          recovered.status,
          CommercialAuthenticationStatus.identityContextReady,
        );
        final immutableBinding = await bindingRepository.readBinding();
        final originalSnapshot = await bindingRepository.readSnapshot();
        expect(immutableBinding, isNotNull);
        expect(originalSnapshot, isNotNull);

        recovered.dispose();
        await recovered.refreshCoordinator.dispose();
        coordinators.remove(recovered.refreshCoordinator);
        await database.customStatement('''
          CREATE TRIGGER b3_fail_snapshot_update
          BEFORE UPDATE ON commercial_identity_snapshot
          BEGIN
            SELECT RAISE(ABORT, 'synthetic snapshot update failure');
          END
        ''');

        final restoring = _createController(
          credentialStore: credentialStore,
          bindingPort: bindingRepository,
          transport: transport,
          installationReferenceReader: installationReferenceReader,
          now: now,
        );
        coordinators.add(restoring.refreshCoordinator);
        await restoring.start();

        expect(
          restoring.status,
          CommercialAuthenticationStatus.lockedFailClosed,
        );
        expect(restoring.safeFailureCode, 'IDENTITY_BINDING_STORAGE_FAILED');
        expect(restoring.refreshCoordinator.hasMemoryAccessToken, isFalse);
        expect(
          (await credentialStore.readRefreshCredential()).state,
          CommercialCredentialStoreState.empty,
        );
        expect(
          (await bindingRepository.readBinding())?.fingerprint,
          immutableBinding?.fingerprint,
        );
        expect(
          (await bindingRepository.readSnapshot())?.lastConfirmedAtUtc,
          originalSnapshot?.lastConfirmedAtUtc,
        );
        expect(
          (await credentialStore.readDeviceCredential()).value?.deviceId.value,
          _deviceId,
        );
        expect(await _readKeyBytes(keyStore), orderedEquals(originalKey));

        restoring.dispose();
        await restoring.refreshCoordinator.dispose();
        coordinators.remove(restoring.refreshCoordinator);
        await database.customStatement('DROP TRIGGER b3_fail_snapshot_update');

        final finalRecovery = _createController(
          credentialStore: credentialStore,
          bindingPort: bindingRepository,
          transport: transport,
          installationReferenceReader: installationReferenceReader,
          now: now,
        );
        coordinators.add(finalRecovery.refreshCoordinator);
        await finalRecovery.start();
        await finalRecovery.login(
          workspaceCode: 'FARM-ONE',
          login: 'owner@example.test',
          password: 'correct horse battery staple',
        );
        expect(
          finalRecovery.status,
          CommercialAuthenticationStatus.identityContextReady,
        );
        expect(
          (await bindingRepository.readBinding())?.fingerprint,
          immutableBinding?.fingerprint,
        );

        finalRecovery.dispose();
        await finalRecovery.refreshCoordinator.dispose();
        coordinators.remove(finalRecovery.refreshCoordinator);
        final stableBindingFingerprint =
            (await bindingRepository.readBinding())?.fingerprint;
        final stableSnapshot = await bindingRepository.readSnapshot();
        final faultingBinding = _FaultInjectingBindingPort(bindingRepository);

        Future<void> failDatabaseRead() async {
          try {
            await database
                .customSelect('SELECT value FROM b3_missing_offline_fact')
                .getSingleOrNull();
          } on Object {
            throw const CommercialIdentityFailure(
              kind: CommercialIdentityFailureKind.identityBindingStorageFailure,
              safeCode: 'IDENTITY_BINDING_STORAGE_FAILED',
              retryable: true,
            );
          }
        }

        for (final scenario in <(String, bool)>[
          ('binding', true),
          ('snapshot', false),
        ]) {
          final revalidating = _createController(
            credentialStore: credentialStore,
            bindingPort: faultingBinding,
            transport: transport,
            installationReferenceReader: installationReferenceReader,
            now: now,
          );
          coordinators.add(revalidating.refreshCoordinator);
          await revalidating.start();
          expect(
            revalidating.status,
            CommercialAuthenticationStatus.identityContextReady,
          );

          transport.networkUnavailable = true;
          if (scenario.$2) {
            faultingBinding.beforeReadBinding = failDatabaseRead;
          } else {
            faultingBinding.beforeReadSnapshot = failDatabaseRead;
          }
          await revalidating.revalidate();
          expect(
            revalidating.status,
            CommercialAuthenticationStatus.lockedFailClosed,
          );
          expect(
            revalidating.safeFailureCode,
            'IDENTITY_BINDING_STORAGE_FAILED',
          );
          expect(revalidating.refreshCoordinator.hasMemoryAccessToken, isFalse);
          expect(
            (await credentialStore.readRefreshCredential()).state,
            CommercialCredentialStoreState.valid,
          );
          expect(
            (await bindingRepository.readBinding())?.fingerprint,
            stableBindingFingerprint,
          );
          expect(
            (await bindingRepository.readSnapshot())?.lastConfirmedAtUtc,
            stableSnapshot?.lastConfirmedAtUtc,
          );
          expect(
            (await credentialStore.readDeviceCredential())
                .value
                ?.deviceId
                .value,
            _deviceId,
          );
          expect(await _readKeyBytes(keyStore), orderedEquals(originalKey));

          revalidating.dispose();
          await revalidating.refreshCoordinator.dispose();
          coordinators.remove(revalidating.refreshCoordinator);
          final faultedRestart = _createController(
            credentialStore: credentialStore,
            bindingPort: faultingBinding,
            transport: transport,
            installationReferenceReader: installationReferenceReader,
            now: now,
          );
          coordinators.add(faultedRestart.refreshCoordinator);
          await faultedRestart.start();
          expect(
            faultedRestart.status,
            CommercialAuthenticationStatus.lockedFailClosed,
          );
          expect(
            faultedRestart.refreshCoordinator.hasMemoryAccessToken,
            isFalse,
          );

          faultedRestart.dispose();
          await faultedRestart.refreshCoordinator.dispose();
          coordinators.remove(faultedRestart.refreshCoordinator);
          transport.networkUnavailable = false;
          faultingBinding
            ..beforeReadBinding = null
            ..beforeReadSnapshot = null;
          final readRecovery = _createController(
            credentialStore: credentialStore,
            bindingPort: faultingBinding,
            transport: transport,
            installationReferenceReader: installationReferenceReader,
            now: now,
          );
          coordinators.add(readRecovery.refreshCoordinator);
          await readRecovery.start();
          expect(
            readRecovery.status,
            CommercialAuthenticationStatus.identityContextReady,
          );
          expect(
            (await bindingRepository.readBinding())?.fingerprint,
            stableBindingFingerprint,
          );
          readRecovery.dispose();
          await readRecovery.refreshCoordinator.dispose();
          coordinators.remove(readRecovery.refreshCoordinator);
        }

        for (final scenario
            in <(String, Object, CommercialCredentialStoreState, String)>[
              (
                'secure store unavailable',
                const CommercialIdentityFailure(
                  kind: CommercialIdentityFailureKind.secureStoreUnavailable,
                  safeCode: 'IDENTITY_SECURE_STORE_UNAVAILABLE',
                ),
                CommercialCredentialStoreState.unavailable,
                'IDENTITY_SECURE_STORE_UNAVAILABLE',
              ),
              (
                'persistence verification',
                const CommercialIdentityFailure(
                  kind:
                      CommercialIdentityFailureKind.credentialPersistenceFailed,
                  safeCode: 'IDENTITY_CREDENTIAL_PERSISTENCE_FAILED',
                ),
                CommercialCredentialStoreState.inconsistent,
                'IDENTITY_CREDENTIAL_PERSISTENCE_FAILED',
              ),
              (
                'unexpected platform implementation',
                StateError('synthetic Android secure-store retirement error'),
                CommercialCredentialStoreState.malformed,
                'IDENTITY_CREDENTIAL_PERSISTENCE_FAILED',
              ),
            ]) {
          await credentialStore.clearRefreshCredential();
          credentialStore.retireDeviceFailure = scenario.$2;
          transport.deviceInactiveOnLogin = true;
          final retiring = _createController(
            credentialStore: credentialStore,
            bindingPort: bindingRepository,
            transport: transport,
            installationReferenceReader: installationReferenceReader,
            now: now,
          );
          coordinators.add(retiring.refreshCoordinator);
          await retiring.start();
          expect(
            retiring.status,
            CommercialAuthenticationStatus.deviceRegisteredNoSession,
          );
          await retiring.login(
            workspaceCode: 'FARM-ONE',
            login: 'owner@example.test',
            password: 'correct horse battery staple',
          );
          expect(
            retiring.status,
            CommercialAuthenticationStatus.lockedFailClosed,
          );
          expect(retiring.safeFailureCode, scenario.$4);
          expect(retiring.refreshCoordinator.hasMemoryAccessToken, isFalse);
          expect(
            (await credentialStore.readRefreshCredential()).state,
            CommercialCredentialStoreState.empty,
          );
          expect(
            (await durableCredentialStore.readDeviceCredential())
                .value
                ?.deviceId
                .value,
            _deviceId,
          );
          expect(
            (await bindingRepository.readBinding())?.fingerprint,
            stableBindingFingerprint,
          );
          expect(
            (await bindingRepository.readSnapshot())?.lastConfirmedAtUtc,
            isNotNull,
          );
          expect(await _readKeyBytes(keyStore), orderedEquals(originalKey));

          retiring.dispose();
          await retiring.refreshCoordinator.dispose();
          coordinators.remove(retiring.refreshCoordinator);
          credentialStore.deviceReadOverride = CommercialCredentialRead(
            scenario.$3,
          );
          final faultedRestart = _createController(
            credentialStore: credentialStore,
            bindingPort: bindingRepository,
            transport: transport,
            installationReferenceReader: installationReferenceReader,
            now: now,
          );
          coordinators.add(faultedRestart.refreshCoordinator);
          await faultedRestart.start();
          expect(
            faultedRestart.status,
            CommercialAuthenticationStatus.lockedFailClosed,
          );
          expect(
            faultedRestart.refreshCoordinator.hasMemoryAccessToken,
            isFalse,
          );

          faultedRestart.dispose();
          await faultedRestart.refreshCoordinator.dispose();
          coordinators.remove(faultedRestart.refreshCoordinator);
          credentialStore
            ..deviceReadOverride = null
            ..retireDeviceFailure = null;
          final retirementRecovery = _createController(
            credentialStore: credentialStore,
            bindingPort: bindingRepository,
            transport: transport,
            installationReferenceReader: installationReferenceReader,
            now: now,
          );
          coordinators.add(retirementRecovery.refreshCoordinator);
          await retirementRecovery.start();
          expect(
            retirementRecovery.status,
            CommercialAuthenticationStatus.deviceRegisteredNoSession,
          );
          await retirementRecovery.login(
            workspaceCode: 'FARM-ONE',
            login: 'owner@example.test',
            password: 'correct horse battery staple',
          );
          expect(
            retirementRecovery.status,
            CommercialAuthenticationStatus.deviceRevoked,
          );
          expect(
            (await durableCredentialStore.readDeviceCredential()).state,
            CommercialCredentialStoreState.retired,
          );
          transport.deviceInactiveOnLogin = false;
          await retirementRecovery.activate(
            workspaceCode: 'FARM-ONE',
            activationCode: 'ACT-SECRET',
            deviceLabel: 'Android failure matrix Device',
          );
          expect(
            retirementRecovery.status,
            CommercialAuthenticationStatus.deviceRegisteredNoSession,
          );
          expect(
            (await durableCredentialStore.readDeviceCredential())
                .value
                ?.deviceId
                .value,
            _deviceId,
          );
          expect(
            (await bindingRepository.readBinding())?.fingerprint,
            stableBindingFingerprint,
          );
          expect(await _readKeyBytes(keyStore), orderedEquals(originalKey));
          retirementRecovery.dispose();
          await retirementRecovery.refreshCoordinator.dispose();
          coordinators.remove(retirementRecovery.refreshCoordinator);
        }
      } finally {
        for (final coordinator in coordinators) {
          await coordinator.dispose();
        }
        await initializer.close();
        await identityStrings.clear();
        await keyStrings.clear();
        if (await root.exists()) {
          await root.delete(recursive: true);
        }
      }
    },
  );
}

CommercialIdentityController _createController({
  required _FaultInjectingCredentialStore credentialStore,
  required CommercialIdentityBindingPort bindingPort,
  required _DeviceMatrixTransport transport,
  required CommercialInstallationReferenceReader installationReferenceReader,
  required DateTime now,
}) {
  final service = CommercialIdentityService(
    transport: transport,
    credentialStore: credentialStore,
    activationAttemptStore: credentialStore,
    bindingPort: bindingPort,
    normalizedApiOrigin: _origin,
    installationReferenceReader: installationReferenceReader,
    clock: () => now,
  );
  final coordinator = CommercialRefreshCoordinator(
    identityService: service,
    credentialStore: credentialStore,
    clock: () => now,
  );
  return CommercialIdentityController(
    identityService: service,
    refreshCoordinator: coordinator,
    credentialStore: credentialStore,
    activationAttemptStore: credentialStore,
    bindingPort: bindingPort,
  );
}

Future<Uint8List> _readKeyBytes(
  FlutterSecureCommercialDatabaseKeyStore keyStore,
) async {
  final state = await keyStore.read(keyExpected: true);
  expect(state, isA<CommercialDatabaseKeyStoreValid>());
  final key = (state as CommercialDatabaseKeyStoreValid).keyMaterial;
  try {
    return key.useBytes(Uint8List.fromList);
  } finally {
    key.dispose();
  }
}

final class _DeviceMatrixTransport implements CommercialIdentityTransport {
  int _authenticationCount = 0;
  int _activationCount = 0;
  bool networkUnavailable = false;
  bool deviceInactiveOnLogin = false;

  @override
  Future<CommercialIdentityTransportResponse> send(
    CommercialIdentityTransportRequest request,
  ) async {
    if (request.relativePath.endsWith('/device-activations/redeem')) {
      _activationCount += 1;
      return _ok(_activationResponse(_activationCount));
    }
    if (networkUnavailable && request.relativePath.endsWith('/refresh')) {
      throw const CommercialIdentityFailure(
        kind: CommercialIdentityFailureKind.networkAmbiguous,
        safeCode: 'IDENTITY_NETWORK_OUTCOME_UNKNOWN',
        retryable: true,
      );
    }
    if (deviceInactiveOnLogin && request.relativePath.endsWith('/login')) {
      return _error(401, 'DEVICE_NOT_ACTIVE');
    }
    if (request.relativePath.endsWith('/login') ||
        request.relativePath.endsWith('/refresh')) {
      _authenticationCount += 1;
      return _ok(_authenticationResponse(_authenticationCount));
    }
    if (request.relativePath.endsWith('/me')) {
      return _ok(_meResponse());
    }
    throw StateError('Unexpected identity Device-matrix request.');
  }

  @override
  void dispose() {}
}

Map<String, Object?> _activationResponse(int sequence) => <String, Object?>{
  'deviceId': _deviceId,
  'deviceSecret': 'synthetic-device-secret-$sequence',
  'secretVersion': sequence,
  'activatedAtUtc': '2026-08-18T12:00:00Z',
};

Map<String, Object?> _authenticationResponse(int sequence) => <String, Object?>{
  'accessToken': 'synthetic-access-$sequence',
  'accessTokenExpiresAtUtc': '2026-08-18T13:00:00Z',
  'refreshToken': 'synthetic-refresh-$sequence',
  'refreshTokenExpiresAtUtc': '2026-08-20T12:00:00Z',
  ..._contextResponse(),
};

Map<String, Object?> _meResponse() => <String, Object?>{
  ..._contextResponse(),
  'tokenFamilyId': _familyId,
};

Map<String, Object?> _contextResponse() => <String, Object?>{
  'user': <String, Object?>{
    'userId': _userId,
    'displayName': 'Synthetic Owner',
    'role': 'Owner',
  },
  'workspace': <String, Object?>{
    'workspaceId': _workspaceId,
    'workspaceCode': 'FARM-ONE',
  },
  'company': <String, Object?>{
    'companyId': _companyId,
    'defaultBranchId': _branchId,
  },
  'device': <String, Object?>{
    'deviceId': _deviceId,
    'label': 'Binding failure matrix Device',
  },
};

CommercialIdentityTransportResponse _ok(Map<String, Object?> body) =>
    CommercialIdentityTransportResponse(
      statusCode: 200,
      correlationId: '55555555-5555-4555-8555-555555555555',
      jsonBody: body,
      headers: const <String, String>{},
    );

CommercialIdentityTransportResponse _error(int status, String code) =>
    CommercialIdentityTransportResponse(
      statusCode: status,
      correlationId: '55555555-5555-4555-8555-555555555555',
      jsonBody: <String, Object?>{
        'error': <String, Object?>{
          'code': code,
          'message': 'Safe Device-matrix failure',
          'category': 'Conflict',
          'retryable': false,
        },
        'meta': <String, Object?>{
          'correlationId': '55555555-5555-4555-8555-555555555555',
        },
      },
      headers: const <String, String>{},
    );

final class _FaultInjectingCredentialStore
    implements
        CommercialIdentityCredentialStore,
        CommercialActivationAttemptStore {
  _FaultInjectingCredentialStore(this.delegate);

  final FlutterSecureCommercialIdentityCredentialStore delegate;
  CommercialCredentialRead<DeviceCredential>? deviceReadOverride;
  Object? retireDeviceFailure;

  @override
  Future<CommercialCredentialRead<DeviceCredential>>
  readDeviceCredential() async =>
      deviceReadOverride ?? await delegate.readDeviceCredential();

  @override
  Future<void> replaceDeviceCredential(
    DeviceCredential credential, {
    String? bindingFingerprint,
  }) => delegate.replaceDeviceCredential(
    credential,
    bindingFingerprint: bindingFingerprint,
  );

  @override
  Future<void> retireDeviceCredential({required DeviceId deviceId}) async {
    final failure = retireDeviceFailure;
    if (failure != null) {
      throw failure;
    }
    await delegate.retireDeviceCredential(deviceId: deviceId);
  }

  @override
  Future<CommercialCredentialRead<RefreshCredential>> readRefreshCredential() =>
      delegate.readRefreshCredential();

  @override
  Future<void> replaceRefreshCredential(RefreshCredential credential) =>
      delegate.replaceRefreshCredential(credential);

  @override
  Future<void> clearRefreshCredential() => delegate.clearRefreshCredential();

  @override
  Future<CommercialCredentialRead<CommercialActivationAttempt>>
  readActivationAttempt() => delegate.readActivationAttempt();

  @override
  Future<void> writeActivationAttempt(CommercialActivationAttempt attempt) =>
      delegate.writeActivationAttempt(attempt);

  @override
  Future<void> clearActivationAttempt() => delegate.clearActivationAttempt();
}

final class _FaultInjectingBindingPort
    implements CommercialIdentityBindingPort {
  _FaultInjectingBindingPort(this.delegate);

  final CommercialIdentityBindingPort delegate;
  Future<void> Function()? beforeReadBinding;
  Future<void> Function()? beforeReadSnapshot;

  @override
  Future<CommercialAccountBinding?> readBinding() async {
    await beforeReadBinding?.call();
    return delegate.readBinding();
  }

  @override
  Future<CommercialIdentitySnapshot?> readSnapshot() async {
    await beforeReadSnapshot?.call();
    return delegate.readSnapshot();
  }

  @override
  Future<void> bindOrMatch(
    CommercialAccountBinding binding,
    CommercialIdentitySnapshot snapshot,
  ) => delegate.bindOrMatch(binding, snapshot);
}

final class _IsolatedIdentityStringStore implements IdentitySecureStringStore {
  _IsolatedIdentityStringStore(this.prefix)
    : _storage = const FlutterSecureStorage(
        aOptions: FlutterSecureIdentityStringStore.androidOptions,
      );

  final String prefix;
  final FlutterSecureStorage _storage;

  @override
  Future<String?> read(String key) => _storage.read(key: '$prefix$key');

  @override
  Future<void> write(String key, String value) =>
      _storage.write(key: '$prefix$key', value: value);

  @override
  Future<void> delete(String key) => _storage.delete(key: '$prefix$key');

  Future<void> clear() async {
    for (final key in <String>[
      FlutterSecureCommercialIdentityCredentialStore.deviceSlotA,
      FlutterSecureCommercialIdentityCredentialStore.deviceSlotB,
      FlutterSecureCommercialIdentityCredentialStore.deviceHead,
      FlutterSecureCommercialIdentityCredentialStore.refreshSlotA,
      FlutterSecureCommercialIdentityCredentialStore.refreshSlotB,
      FlutterSecureCommercialIdentityCredentialStore.refreshHead,
      FlutterSecureCommercialIdentityCredentialStore.activationAttemptEntry,
    ]) {
      await delete(key);
    }
  }
}

final class _IsolatedKeyStringStore implements SecureStringStore {
  _IsolatedKeyStringStore(this.prefix)
    : _storage = const FlutterSecureStorage(
        aOptions: FlutterSecureStringStore.androidOptions,
      );

  final String prefix;
  final FlutterSecureStorage _storage;

  @override
  Future<String?> read(String key) => _storage.read(key: '$prefix$key');

  @override
  Future<void> write(String key, String value) =>
      _storage.write(key: '$prefix$key', value: value);

  Future<void> clear() => _storage.delete(
    key: '$prefix${FlutterSecureCommercialDatabaseKeyStore.databaseKeyEntry}',
  );
}
