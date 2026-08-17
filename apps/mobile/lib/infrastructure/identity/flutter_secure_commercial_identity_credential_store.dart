import 'dart:convert';

import 'package:crypto/crypto.dart';
import 'package:flutter/services.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

import '../../core/identity/commercial_identity_failure.dart';
import '../../core/identity/commercial_identity_models.dart';
import '../../core/identity/commercial_identity_ports.dart';

abstract interface class IdentitySecureStringStore {
  Future<String?> read(String key);

  Future<void> write(String key, String value);

  Future<void> delete(String key);
}

final class FlutterSecureIdentityStringStore
    implements IdentitySecureStringStore {
  FlutterSecureIdentityStringStore({FlutterSecureStorage? storage})
    : _storage = storage ?? _createStorage();

  static const AndroidOptions androidOptions = AndroidOptions(
    resetOnError: false,
    migrateOnAlgorithmChange: true,
    migrateWithBackup: false,
    enforceBiometrics: false,
    keyCipherAlgorithm:
        KeyCipherAlgorithm.RSA_ECB_OAEPwithSHA_256andMGF1Padding,
    storageCipherAlgorithm: StorageCipherAlgorithm.AES_GCM_NoPadding,
    storageNamespace:
        FlutterSecureCommercialIdentityCredentialStore.storageNamespace,
  );

  final FlutterSecureStorage _storage;

  static FlutterSecureStorage _createStorage() =>
      const FlutterSecureStorage(aOptions: androidOptions);

  @override
  Future<String?> read(String key) => _storage.read(key: key);

  @override
  Future<void> write(String key, String value) =>
      _storage.write(key: key, value: value);

  @override
  Future<void> delete(String key) => _storage.delete(key: key);
}

final class FlutterSecureCommercialIdentityCredentialStore
    implements
        CommercialIdentityCredentialStore,
        CommercialActivationAttemptStore {
  FlutterSecureCommercialIdentityCredentialStore({
    IdentitySecureStringStore? secureStringStore,
  }) : _store = secureStringStore ?? FlutterSecureIdentityStringStore();

  static const String storageNamespace = 'traderpro_commercial_identity_v1';
  static const String deviceSlotA = 'device_credential_slot_a_v1';
  static const String deviceSlotB = 'device_credential_slot_b_v1';
  static const String deviceHead = 'device_credential_head_v1';
  static const String refreshSlotA = 'refresh_credential_slot_a_v1';
  static const String refreshSlotB = 'refresh_credential_slot_b_v1';
  static const String refreshHead = 'refresh_credential_head_v1';
  static const String activationAttemptEntry = 'activation_attempt_v1';
  static const int contractVersion = 1;
  static const int maxEnvelopeCharacters = 32768;

  final IdentitySecureStringStore _store;

  _CredentialJournal get _deviceJournal => _CredentialJournal(
    store: _store,
    kind: 'device',
    slotAKey: deviceSlotA,
    slotBKey: deviceSlotB,
    headKey: deviceHead,
  );

  _CredentialJournal get _refreshJournal => _CredentialJournal(
    store: _store,
    kind: 'refresh',
    slotAKey: refreshSlotA,
    slotBKey: refreshSlotB,
    headKey: refreshHead,
  );

  @override
  Future<CommercialCredentialRead<DeviceCredential>>
  readDeviceCredential() async {
    final read = await _deviceJournal.read();
    if (read.state != CommercialCredentialStoreState.valid) {
      return CommercialCredentialRead<DeviceCredential>(read.state);
    }
    try {
      final json = read.envelope!;
      final state = _requiredString(json, 'state', max: 16);
      if (state == 'retired') {
        return const CommercialCredentialRead<DeviceCredential>(
          CommercialCredentialStoreState.retired,
        );
      }
      if (state != 'active') {
        return const CommercialCredentialRead<DeviceCredential>(
          CommercialCredentialStoreState.inconsistent,
        );
      }
      return CommercialCredentialRead<DeviceCredential>(
        CommercialCredentialStoreState.valid,
        value: DeviceCredential(
          deviceId: DeviceId(_requiredString(json, 'deviceId', max: 36)),
          secret: _requiredString(json, 'credentialValue', max: 4096),
          secretVersion: _requiredPositiveInt(json, 'secretVersion'),
        ),
      );
    } on Object {
      return const CommercialCredentialRead<DeviceCredential>(
        CommercialCredentialStoreState.malformed,
      );
    }
  }

  @override
  Future<void> replaceDeviceCredential(
    DeviceCredential credential, {
    String? bindingFingerprint,
  }) => _deviceJournal.writeActive(<String, Object?>{
    'deviceId': credential.deviceId.value,
    'credentialValue': credential.secret,
    'secretVersion': credential.secretVersion,
    'bindingFingerprint': bindingFingerprint,
  });

  @override
  Future<void> retireDeviceCredential({required DeviceId deviceId}) =>
      _deviceJournal.writeTerminal(
        state: 'retired',
        fields: <String, Object?>{'deviceId': deviceId.value},
      );

  @override
  Future<CommercialCredentialRead<RefreshCredential>>
  readRefreshCredential() async {
    final read = await _refreshJournal.read();
    if (read.state != CommercialCredentialStoreState.valid) {
      return CommercialCredentialRead<RefreshCredential>(read.state);
    }
    try {
      final json = read.envelope!;
      final state = _requiredString(json, 'state', max: 16);
      if (state == 'cleared') {
        return const CommercialCredentialRead<RefreshCredential>(
          CommercialCredentialStoreState.empty,
        );
      }
      if (state != 'active') {
        return const CommercialCredentialRead<RefreshCredential>(
          CommercialCredentialStoreState.inconsistent,
        );
      }
      return CommercialCredentialRead<RefreshCredential>(
        CommercialCredentialStoreState.valid,
        value: RefreshCredential(
          value: _requiredString(json, 'credentialValue', max: 4096),
          expiresAtUtc: _requiredUtc(json, 'refreshExpiresAtUtc'),
          bindingFingerprint: _nullableString(
            json,
            'bindingFingerprint',
            max: 64,
          ),
          predecessorDigest: _nullableString(
            json,
            'predecessorDigest',
            max: 64,
          ),
          committedAtUtc: _nullableUtc(json, 'committedAtUtc'),
        ),
      );
    } on Object {
      return const CommercialCredentialRead<RefreshCredential>(
        CommercialCredentialStoreState.malformed,
      );
    }
  }

  @override
  Future<void> replaceRefreshCredential(RefreshCredential credential) =>
      _refreshJournal.writeActive(<String, Object?>{
        'credentialValue': credential.value,
        'refreshExpiresAtUtc': credential.expiresAtUtc
            .toUtc()
            .toIso8601String(),
        'bindingFingerprint': credential.bindingFingerprint,
        'predecessorDigest': credential.predecessorDigest,
        'committedAtUtc': credential.committedAtUtc?.toUtc().toIso8601String(),
      });

  @override
  Future<void> clearRefreshCredential() =>
      _refreshJournal.writeTerminal(state: 'cleared');

  @override
  Future<CommercialCredentialRead<CommercialActivationAttempt>>
  readActivationAttempt() async {
    try {
      final serialized = await _store.read(activationAttemptEntry);
      if (serialized == null) {
        return const CommercialCredentialRead<CommercialActivationAttempt>(
          CommercialCredentialStoreState.empty,
        );
      }
      if (serialized.length > maxEnvelopeCharacters) {
        return const CommercialCredentialRead<CommercialActivationAttempt>(
          CommercialCredentialStoreState.malformed,
        );
      }
      final json = _decodeMap(serialized);
      if (_requiredPositiveInt(json, 'contractVersion') != contractVersion) {
        throw const FormatException();
      }
      return CommercialCredentialRead<CommercialActivationAttempt>(
        CommercialCredentialStoreState.valid,
        value: CommercialActivationAttempt(
          idempotencyKey: _requiredString(json, 'idempotencyKey', max: 200),
          workspaceCode: _requiredString(json, 'workspaceCode', max: 64),
          activationCode: _requiredString(json, 'activationCode', max: 4096),
          deviceLabel: _requiredString(json, 'deviceLabel', max: 256),
          platform: _requiredString(json, 'platform', max: 32),
          createdAtUtc: _requiredUtc(json, 'createdAtUtc'),
          clientInstallationReference: _nullableString(
            json,
            'clientInstallationReference',
            max: 256,
          ),
        ),
      );
    } on PlatformException catch (error) {
      return CommercialCredentialRead<CommercialActivationAttempt>(
        _platformState(error),
      );
    } on Object {
      return const CommercialCredentialRead<CommercialActivationAttempt>(
        CommercialCredentialStoreState.malformed,
      );
    }
  }

  @override
  Future<void> writeActivationAttempt(
    CommercialActivationAttempt attempt,
  ) async {
    final serialized = jsonEncode(<String, Object?>{
      'contractVersion': contractVersion,
      'idempotencyKey': attempt.idempotencyKey,
      'workspaceCode': attempt.workspaceCode,
      'activationCode': attempt.activationCode,
      'clientInstallationReference': attempt.clientInstallationReference,
      'deviceLabel': attempt.deviceLabel,
      'platform': attempt.platform,
      'createdAtUtc': attempt.createdAtUtc.toUtc().toIso8601String(),
    });
    if (serialized.length > maxEnvelopeCharacters) {
      throw _persistenceFailure();
    }
    try {
      final existing = await _store.read(activationAttemptEntry);
      if (existing != null && existing != serialized) {
        throw const CommercialIdentityFailure(
          kind: CommercialIdentityFailureKind.credentialPersistenceFailed,
          safeCode: 'ACTIVATION_ATTEMPT_ALREADY_PENDING',
        );
      }
      await _store.write(activationAttemptEntry, serialized);
      if (await _store.read(activationAttemptEntry) != serialized) {
        throw _persistenceFailure();
      }
    } on CommercialIdentityFailure {
      rethrow;
    } on PlatformException catch (error) {
      throw _platformFailure(error);
    } on Object {
      throw _persistenceFailure();
    }
  }

  @override
  Future<void> clearActivationAttempt() async {
    try {
      await _store.delete(activationAttemptEntry);
      if (await _store.read(activationAttemptEntry) != null) {
        throw _persistenceFailure();
      }
    } on CommercialIdentityFailure {
      rethrow;
    } on PlatformException catch (error) {
      throw _platformFailure(error);
    } on Object {
      throw _persistenceFailure();
    }
  }

  static CommercialCredentialStoreState _platformState(
    PlatformException error,
  ) => _isReliablyInvalidated(error.code)
      ? CommercialCredentialStoreState.invalidated
      : CommercialCredentialStoreState.unavailable;

  static CommercialIdentityFailure _platformFailure(PlatformException error) =>
      CommercialIdentityFailure(
        kind: _isReliablyInvalidated(error.code)
            ? CommercialIdentityFailureKind.secureStoreUnavailable
            : CommercialIdentityFailureKind.secureStoreUnavailable,
        safeCode: _isReliablyInvalidated(error.code)
            ? 'IDENTITY_SECURE_STORE_INVALIDATED'
            : 'IDENTITY_SECURE_STORE_UNAVAILABLE',
      );

  static CommercialIdentityFailure _persistenceFailure() =>
      const CommercialIdentityFailure(
        kind: CommercialIdentityFailureKind.credentialPersistenceFailed,
        safeCode: 'IDENTITY_CREDENTIAL_PERSISTENCE_FAILED',
      );

  static bool _isReliablyInvalidated(String code) =>
      code == 'key_permanently_invalidated' ||
      code == 'KeyPermanentlyInvalidatedException';
}

final class _CredentialJournal {
  const _CredentialJournal({
    required this.store,
    required this.kind,
    required this.slotAKey,
    required this.slotBKey,
    required this.headKey,
  });

  final IdentitySecureStringStore store;
  final String kind;
  final String slotAKey;
  final String slotBKey;
  final String headKey;

  Future<_JournalRead> read() async {
    try {
      final headSerialized = await store.read(headKey);
      final slotA = await store.read(slotAKey);
      final slotB = await store.read(slotBKey);
      if (headSerialized == null) {
        return slotA == null && slotB == null
            ? const _JournalRead(CommercialCredentialStoreState.empty)
            : const _JournalRead(CommercialCredentialStoreState.inconsistent);
      }
      if (headSerialized.length > 2048) {
        return const _JournalRead(CommercialCredentialStoreState.malformed);
      }
      final head = _decodeMap(headSerialized);
      if (_requiredPositiveInt(head, 'contractVersion') !=
          FlutterSecureCommercialIdentityCredentialStore.contractVersion) {
        return const _JournalRead(CommercialCredentialStoreState.malformed);
      }
      final slot = _requiredString(head, 'slot', max: 1);
      if (slot != 'a' && slot != 'b') {
        return const _JournalRead(CommercialCredentialStoreState.malformed);
      }
      final generation = _requiredPositiveInt(head, 'generation');
      final expectedDigest = _requiredString(head, 'envelopeDigest', max: 64);
      final envelopeSerialized = slot == 'a' ? slotA : slotB;
      if (envelopeSerialized == null) {
        return const _JournalRead(CommercialCredentialStoreState.missing);
      }
      if (envelopeSerialized.length >
          FlutterSecureCommercialIdentityCredentialStore
              .maxEnvelopeCharacters) {
        return const _JournalRead(CommercialCredentialStoreState.malformed);
      }
      if (_digest(envelopeSerialized) != expectedDigest) {
        return const _JournalRead(CommercialCredentialStoreState.inconsistent);
      }
      final envelope = _decodeMap(envelopeSerialized);
      if (_requiredPositiveInt(envelope, 'contractVersion') !=
              FlutterSecureCommercialIdentityCredentialStore.contractVersion ||
          _requiredString(envelope, 'kind', max: 16) != kind ||
          _requiredPositiveInt(envelope, 'generation') != generation) {
        return const _JournalRead(CommercialCredentialStoreState.inconsistent);
      }
      return _JournalRead(
        CommercialCredentialStoreState.valid,
        envelope: envelope,
      );
    } on PlatformException catch (error) {
      return _JournalRead(
        FlutterSecureCommercialIdentityCredentialStore._platformState(error),
      );
    } on Object {
      return const _JournalRead(CommercialCredentialStoreState.malformed);
    }
  }

  Future<void> writeActive(Map<String, Object?> fields) =>
      _write(state: 'active', fields: fields);

  Future<void> writeTerminal({
    required String state,
    Map<String, Object?> fields = const <String, Object?>{},
  }) => _write(state: state, fields: fields);

  Future<void> _write({
    required String state,
    required Map<String, Object?> fields,
  }) async {
    try {
      final next = await _prepareNext();
      final envelope = <String, Object?>{
        'contractVersion':
            FlutterSecureCommercialIdentityCredentialStore.contractVersion,
        'kind': kind,
        'generation': next.generation,
        'state': state,
        ...fields,
      };
      final serialized = jsonEncode(envelope);
      if (serialized.length >
          FlutterSecureCommercialIdentityCredentialStore
              .maxEnvelopeCharacters) {
        throw FlutterSecureCommercialIdentityCredentialStore._persistenceFailure();
      }
      final destinationKey = next.slot == 'a' ? slotAKey : slotBKey;
      await store.write(destinationKey, serialized);
      if (await store.read(destinationKey) != serialized) {
        throw FlutterSecureCommercialIdentityCredentialStore._persistenceFailure();
      }
      final digest = _digest(serialized);
      final head = jsonEncode(<String, Object?>{
        'contractVersion':
            FlutterSecureCommercialIdentityCredentialStore.contractVersion,
        'slot': next.slot,
        'generation': next.generation,
        'envelopeDigest': digest,
      });
      await store.write(headKey, head);
      if (await store.read(headKey) != head) {
        throw FlutterSecureCommercialIdentityCredentialStore._persistenceFailure();
      }
      final committed = await read();
      if (committed.state != CommercialCredentialStoreState.valid ||
          committed.envelope?['generation'] != next.generation) {
        throw FlutterSecureCommercialIdentityCredentialStore._persistenceFailure();
      }
      final oldKey = next.slot == 'a' ? slotBKey : slotAKey;
      await store.delete(oldKey);
      if (await store.read(oldKey) != null) {
        throw FlutterSecureCommercialIdentityCredentialStore._persistenceFailure();
      }
    } on CommercialIdentityFailure {
      rethrow;
    } on PlatformException catch (error) {
      throw FlutterSecureCommercialIdentityCredentialStore._platformFailure(
        error,
      );
    } on Object {
      throw FlutterSecureCommercialIdentityCredentialStore._persistenceFailure();
    }
  }

  Future<_NextJournalWrite> _prepareNext() async {
    final headSerialized = await store.read(headKey);
    final slotA = await store.read(slotAKey);
    final slotB = await store.read(slotBKey);
    if (headSerialized != null) {
      final head = _decodeMap(headSerialized);
      final slot = _requiredString(head, 'slot', max: 1);
      final generation = _requiredPositiveInt(head, 'generation');
      final readback = await read();
      if (readback.state != CommercialCredentialStoreState.valid) {
        throw FlutterSecureCommercialIdentityCredentialStore._persistenceFailure();
      }
      return _NextJournalWrite(slot == 'a' ? 'b' : 'a', generation + 1);
    }

    final orphanA = _safeEnvelopeGeneration(slotA);
    final orphanB = _safeEnvelopeGeneration(slotB);
    if ((slotA != null && orphanA == null) ||
        (slotB != null && orphanB == null)) {
      throw FlutterSecureCommercialIdentityCredentialStore._persistenceFailure();
    }
    final maxGeneration = <int>[
      ?orphanA,
      ?orphanB,
    ].fold<int>(0, (maximum, value) => value > maximum ? value : maximum);
    final slot = orphanA != null && orphanA >= (orphanB ?? -1) ? 'b' : 'a';
    return _NextJournalWrite(slot, maxGeneration + 1);
  }

  int? _safeEnvelopeGeneration(String? serialized) {
    if (serialized == null) {
      return null;
    }
    try {
      final envelope = _decodeMap(serialized);
      if (_requiredString(envelope, 'kind', max: 16) != kind) {
        return null;
      }
      return _requiredPositiveInt(envelope, 'generation');
    } on Object {
      return null;
    }
  }
}

final class _JournalRead {
  const _JournalRead(this.state, {this.envelope});

  final CommercialCredentialStoreState state;
  final Map<String, Object?>? envelope;
}

final class _NextJournalWrite {
  const _NextJournalWrite(this.slot, this.generation);

  final String slot;
  final int generation;
}

Map<String, Object?> _decodeMap(String serialized) {
  final decoded = jsonDecode(serialized);
  if (decoded is! Map) {
    throw const FormatException();
  }
  return Map<String, Object?>.from(decoded);
}

String _digest(String value) => sha256.convert(utf8.encode(value)).toString();

String _requiredString(
  Map<String, Object?> json,
  String field, {
  required int max,
}) {
  final value = json[field];
  if (value is! String ||
      value.isEmpty ||
      value.trim().isEmpty ||
      value.length > max) {
    throw const FormatException();
  }
  return value;
}

String? _nullableString(
  Map<String, Object?> json,
  String field, {
  required int max,
}) {
  final value = json[field];
  if (value == null) {
    return null;
  }
  if (value is! String || value.isEmpty || value.length > max) {
    throw const FormatException();
  }
  return value;
}

int _requiredPositiveInt(Map<String, Object?> json, String field) {
  final value = json[field];
  if (value is! int || value <= 0) {
    throw const FormatException();
  }
  return value;
}

DateTime _requiredUtc(Map<String, Object?> json, String field) {
  final value = _requiredString(json, field, max: 64);
  final parsed = DateTime.tryParse(value);
  if (parsed == null || !parsed.isUtc) {
    throw const FormatException();
  }
  return parsed;
}

DateTime? _nullableUtc(Map<String, Object?> json, String field) {
  if (json[field] == null) {
    return null;
  }
  return _requiredUtc(json, field);
}
