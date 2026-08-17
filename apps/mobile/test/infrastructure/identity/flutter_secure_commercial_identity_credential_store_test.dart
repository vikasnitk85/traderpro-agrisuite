import 'package:flutter/services.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:traderpro_agrisuite_mobile/core/identity/commercial_identity_failure.dart';
import 'package:traderpro_agrisuite_mobile/core/identity/commercial_identity_models.dart';
import 'package:traderpro_agrisuite_mobile/core/identity/commercial_identity_ports.dart';
import 'package:traderpro_agrisuite_mobile/infrastructure/identity/flutter_secure_commercial_identity_credential_store.dart';

void main() {
  final a64 = List<String>.filled(64, 'a').join();
  final b64 = List<String>.filled(64, 'b').join();
  late _MemorySecureStore strings;
  late FlutterSecureCommercialIdentityCredentialStore store;
  final firstDevice = DeviceCredential(
    deviceId: DeviceId('019fc400-0000-7000-8000-000000000201'),
    secret: '<synthetic-device-secret-a>',
    secretVersion: 1,
  );
  final secondDevice = DeviceCredential(
    deviceId: DeviceId('019fc400-0000-7000-8000-000000000201'),
    secret: '<synthetic-device-secret-b>',
    secretVersion: 2,
  );
  final firstRefresh = RefreshCredential(
    value: '<synthetic-refresh-a>',
    expiresAtUtc: DateTime.utc(2026, 9),
    bindingFingerprint: a64,
    committedAtUtc: DateTime.utc(2026, 8, 9),
  );
  final secondRefresh = RefreshCredential(
    value: '<synthetic-refresh-b>',
    expiresAtUtc: DateTime.utc(2026, 10),
    bindingFingerprint: a64,
    predecessorDigest: b64,
    committedAtUtc: DateTime.utc(2026, 8, 9, 0, 0, 20),
  );

  setUp(() {
    strings = _MemorySecureStore();
    store = FlutterSecureCommercialIdentityCredentialStore(
      secureStringStore: strings,
    );
  });

  test('uses the dedicated namespace and exact journal entries', () {
    expect(
      FlutterSecureCommercialIdentityCredentialStore.storageNamespace,
      'traderpro_commercial_identity_v1',
    );
    expect(
      FlutterSecureCommercialIdentityCredentialStore.deviceSlotA,
      'device_credential_slot_a_v1',
    );
    expect(
      FlutterSecureCommercialIdentityCredentialStore.deviceSlotB,
      'device_credential_slot_b_v1',
    );
    expect(
      FlutterSecureCommercialIdentityCredentialStore.deviceHead,
      'device_credential_head_v1',
    );
    expect(
      FlutterSecureCommercialIdentityCredentialStore.refreshSlotA,
      'refresh_credential_slot_a_v1',
    );
    expect(
      FlutterSecureCommercialIdentityCredentialStore.refreshSlotB,
      'refresh_credential_slot_b_v1',
    );
    expect(
      FlutterSecureCommercialIdentityCredentialStore.refreshHead,
      'refresh_credential_head_v1',
    );
    final options = FlutterSecureIdentityStringStore.androidOptions.toMap();
    expect(options['resetOnError'], 'false');
    expect(options['migrateOnAlgorithmChange'], 'true');
    expect(options['migrateWithBackup'], 'false');
  });

  test(
    'first Device write and replacement promote alternating journals',
    () async {
      expect(
        (await store.readDeviceCredential()).state,
        CommercialCredentialStoreState.empty,
      );
      await store.replaceDeviceCredential(firstDevice);
      expect(
        (await store.readDeviceCredential()).value?.secretVersion,
        firstDevice.secretVersion,
      );
      final firstHead = strings
          .values[FlutterSecureCommercialIdentityCredentialStore.deviceHead];

      await store.replaceDeviceCredential(secondDevice);
      final read = await store.readDeviceCredential();
      expect(read.state, CommercialCredentialStoreState.valid);
      expect(read.value?.secret, secondDevice.secret);
      expect(
        strings.values[FlutterSecureCommercialIdentityCredentialStore
            .deviceHead],
        isNot(firstHead),
      );
      expect(
        strings.values.containsKey(
          FlutterSecureCommercialIdentityCredentialStore.deviceSlotA,
        ),
        isFalse,
      );
      expect(
        strings.values.containsKey(
          FlutterSecureCommercialIdentityCredentialStore.deviceSlotB,
        ),
        isTrue,
      );
    },
  );

  test(
    'refresh first write and A/B rotation expose only committed head',
    () async {
      await store.replaceRefreshCredential(firstRefresh);
      await store.replaceRefreshCredential(secondRefresh);
      final read = await store.readRefreshCredential();
      expect(read.state, CommercialCredentialStoreState.valid);
      expect(read.value?.value, secondRefresh.value);
      expect(read.value?.predecessorDigest, secondRefresh.predecessorDigest);
      expect(read.value?.expiresAtUtc, secondRefresh.expiresAtUtc);
    },
  );

  test(
    'crash after first slot write remains inconsistent and is recoverable',
    () async {
      strings.failWriteKey =
          FlutterSecureCommercialIdentityCredentialStore.deviceHead;
      await expectLater(
        store.replaceDeviceCredential(firstDevice),
        throwsA(isA<CommercialIdentityFailure>()),
      );
      expect(
        (await store.readDeviceCredential()).state,
        CommercialCredentialStoreState.inconsistent,
      );

      strings.failWriteKey = null;
      await store.replaceDeviceCredential(firstDevice);
      expect(
        (await store.readDeviceCredential()).state,
        CommercialCredentialStoreState.valid,
      );
    },
  );

  test(
    'crash after head write leaves the promoted credential readable',
    () async {
      await store.replaceRefreshCredential(firstRefresh);
      strings.failDeleteKey =
          FlutterSecureCommercialIdentityCredentialStore.refreshSlotA;
      await expectLater(
        store.replaceRefreshCredential(secondRefresh),
        throwsA(isA<CommercialIdentityFailure>()),
      );
      final read = await store.readRefreshCredential();
      expect(read.state, CommercialCredentialStoreState.valid);
      expect(read.value?.value, secondRefresh.value);
    },
  );

  test('corrupt head, corrupt slot, and missing slot are classified', () async {
    await store.replaceDeviceCredential(firstDevice);
    final headKey = FlutterSecureCommercialIdentityCredentialStore.deviceHead;
    final slotKey = FlutterSecureCommercialIdentityCredentialStore.deviceSlotA;
    final head = strings.values[headKey]!;
    final slot = strings.values[slotKey]!;

    strings.values[headKey] = '{';
    expect(
      (await store.readDeviceCredential()).state,
      CommercialCredentialStoreState.malformed,
    );
    strings.values[headKey] = head;
    strings.values[slotKey] = '$slot-corrupt';
    expect(
      (await store.readDeviceCredential()).state,
      CommercialCredentialStoreState.inconsistent,
    );
    strings.values[slotKey] = slot;
    strings.values.remove(slotKey);
    expect(
      (await store.readDeviceCredential()).state,
      CommercialCredentialStoreState.missing,
    );
  });

  test(
    'unavailable and invalidated secure store states stay distinct',
    () async {
      strings.readError = PlatformException(code: 'unavailable');
      expect(
        (await store.readDeviceCredential()).state,
        CommercialCredentialStoreState.unavailable,
      );
      strings.readError = PlatformException(
        code: 'KeyPermanentlyInvalidatedException',
      );
      expect(
        (await store.readRefreshCredential()).state,
        CommercialCredentialStoreState.invalidated,
      );
    },
  );

  test(
    'logout clears refresh with a durable tombstone and preserves Device',
    () async {
      await store.replaceDeviceCredential(firstDevice);
      await store.replaceRefreshCredential(firstRefresh);
      await store.clearRefreshCredential();

      expect(
        (await store.readRefreshCredential()).state,
        CommercialCredentialStoreState.empty,
      );
      expect(
        (await store.readDeviceCredential()).value?.secretVersion,
        firstDevice.secretVersion,
      );
      expect(
        strings.values.keys,
        isNot(contains('commercial_database_key_v1')),
      );
    },
  );

  test('Device retirement keeps identity but removes usable secret', () async {
    await store.replaceDeviceCredential(firstDevice);
    await store.retireDeviceCredential(deviceId: firstDevice.deviceId);
    final read = await store.readDeviceCredential();
    expect(read.state, CommercialCredentialStoreState.retired);
    expect(read.value, isNull);
  });

  test('activation attempt is persisted before send and exact-only', () async {
    final attempt = CommercialActivationAttempt(
      idempotencyKey: 'synthetic-attempt-001',
      workspaceCode: 'DEMO-WORKSPACE',
      activationCode: '<synthetic-activation>',
      clientInstallationReference: 'install-001',
      deviceLabel: 'Receiving device',
      platform: 'Android',
      createdAtUtc: DateTime.utc(2026, 8, 9),
    );
    await store.writeActivationAttempt(attempt);
    final read = await store.readActivationAttempt();
    expect(read.state, CommercialCredentialStoreState.valid);
    expect(read.value?.idempotencyKey, attempt.idempotencyKey);
    expect(read.value?.activationCode, attempt.activationCode);
    expect(read.value.toString(), isNot(contains(attempt.activationCode)));

    await expectLater(
      store.writeActivationAttempt(
        CommercialActivationAttempt(
          idempotencyKey: 'different',
          workspaceCode: attempt.workspaceCode,
          activationCode: attempt.activationCode,
          deviceLabel: attempt.deviceLabel,
          platform: attempt.platform,
          createdAtUtc: attempt.createdAtUtc,
        ),
      ),
      throwsA(isA<CommercialIdentityFailure>()),
    );
    await store.clearActivationAttempt();
    expect(
      (await store.readActivationAttempt()).state,
      CommercialCredentialStoreState.empty,
    );
  });

  test('credential diagnostics never stringify secret values', () {
    expect(firstDevice.toString(), isNot(contains(firstDevice.secret)));
    expect(firstRefresh.toString(), isNot(contains(firstRefresh.value)));
  });
}

final class _MemorySecureStore implements IdentitySecureStringStore {
  final Map<String, String> values = <String, String>{};
  PlatformException? readError;
  String? failWriteKey;
  String? failDeleteKey;

  @override
  Future<String?> read(String key) async {
    final error = readError;
    if (error != null) {
      throw error;
    }
    return values[key];
  }

  @override
  Future<void> write(String key, String value) async {
    if (key == failWriteKey) {
      throw PlatformException(code: 'synthetic-write-failure');
    }
    values[key] = value;
  }

  @override
  Future<void> delete(String key) async {
    if (key == failDeleteKey) {
      throw PlatformException(code: 'synthetic-delete-failure');
    }
    values.remove(key);
  }
}
