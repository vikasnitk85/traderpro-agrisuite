import 'dart:io';
import 'dart:typed_data';

import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:integration_test/integration_test.dart';
import 'package:path_provider/path_provider.dart';
import 'package:traderpro_agrisuite_mobile/app/commercial_identity_controller.dart';
import 'package:traderpro_agrisuite_mobile/core/identity/commercial_authentication_status.dart';
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
      final credentialStore = FlutterSecureCommercialIdentityCredentialStore(
        secureStringStore: identityStrings,
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
        final device = DeviceCredential(
          deviceId: DeviceId(_deviceId),
          secret: 'synthetic-device-secret',
          secretVersion: 1,
        );
        await credentialStore.replaceDeviceCredential(device);
        final originalKey = await _readKeyBytes(keyStore);

        await database.customStatement('''
          CREATE TRIGGER b3_fail_first_snapshot_insert
          BEFORE INSERT ON commercial_identity_snapshot
          BEGIN
            SELECT RAISE(ABORT, 'synthetic first binding failure');
          END
        ''');
        final first = _createController(
          credentialStore: credentialStore,
          bindingRepository: bindingRepository,
          transport: transport,
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
          bindingRepository: bindingRepository,
          transport: transport,
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
          bindingRepository: bindingRepository,
          transport: transport,
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
          bindingRepository: bindingRepository,
          transport: transport,
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
  required FlutterSecureCommercialIdentityCredentialStore credentialStore,
  required CommercialIdentityBindingRepository bindingRepository,
  required _DeviceMatrixTransport transport,
  required DateTime now,
}) {
  final service = CommercialIdentityService(
    transport: transport,
    credentialStore: credentialStore,
    activationAttemptStore: credentialStore,
    bindingPort: bindingRepository,
    normalizedApiOrigin: _origin,
    installationReferenceReader: () async => 'device-matrix-installation',
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
    bindingPort: bindingRepository,
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

  @override
  Future<CommercialIdentityTransportResponse> send(
    CommercialIdentityTransportRequest request,
  ) async {
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
