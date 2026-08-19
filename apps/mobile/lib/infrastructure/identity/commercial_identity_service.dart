import 'dart:convert';
import 'dart:math';

import '../../core/identity/commercial_identity_failure.dart';
import '../../core/identity/commercial_identity_models.dart';
import '../../core/identity/commercial_identity_ports.dart';
import '../network/commercial_identity_http_client.dart';
import 'commercial_identity_contract_codec.dart';

abstract interface class ActivationIdempotencyKeyGenerator {
  String generate();
}

final class SecureActivationIdempotencyKeyGenerator
    implements ActivationIdempotencyKeyGenerator {
  SecureActivationIdempotencyKeyGenerator({Random? random})
    : _random = random ?? Random.secure();

  final Random _random;

  @override
  String generate() => base64Url
      .encode(List<int>.generate(24, (_) => _random.nextInt(256)))
      .replaceAll('=', '');
}

typedef CommercialInstallationReferenceReader = Future<String?> Function();

final class CommercialIdentityService {
  CommercialIdentityService({
    required this.transport,
    required this.credentialStore,
    required this.activationAttemptStore,
    required this.bindingPort,
    required this.normalizedApiOrigin,
    required this.installationReferenceReader,
    this._codec = const CommercialIdentityContractCodec(),
    ActivationIdempotencyKeyGenerator? activationKeyGenerator,
    DateTime Function()? clock,
  }) : _activationKeys =
           activationKeyGenerator ?? SecureActivationIdempotencyKeyGenerator(),
       _clock = clock ?? DateTime.now;

  final CommercialIdentityTransport transport;
  final CommercialIdentityCredentialStore credentialStore;
  final CommercialActivationAttemptStore activationAttemptStore;
  final CommercialIdentityBindingPort bindingPort;
  final String normalizedApiOrigin;
  final CommercialInstallationReferenceReader installationReferenceReader;
  final CommercialIdentityContractCodec _codec;
  final ActivationIdempotencyKeyGenerator _activationKeys;
  final DateTime Function() _clock;

  Future<DeviceCredential> activate({
    required String workspaceCode,
    required String activationCode,
    required String deviceLabel,
  }) async {
    final normalizedWorkspace = validateWorkspaceCode(workspaceCode);
    final cleanCode = _requiredActivationCode(activationCode);
    final cleanLabel = validateDeviceLabel(deviceLabel);
    final pending = await activationAttemptStore.readActivationAttempt();
    late final CommercialActivationAttempt attempt;
    if (pending.state == CommercialCredentialStoreState.empty) {
      attempt = CommercialActivationAttempt(
        idempotencyKey: _activationKeys.generate(),
        workspaceCode: normalizedWorkspace,
        activationCode: cleanCode,
        clientInstallationReference: await _readInstallationReference(),
        deviceLabel: cleanLabel,
        platform: 'Android',
        createdAtUtc: _clock().toUtc(),
      );
      await activationAttemptStore.writeActivationAttempt(attempt);
    } else if (pending.state == CommercialCredentialStoreState.valid &&
        pending.value != null) {
      attempt = pending.value!;
      if (attempt.workspaceCode != normalizedWorkspace ||
          attempt.activationCode != cleanCode ||
          attempt.deviceLabel != cleanLabel ||
          attempt.platform != 'Android') {
        throw const CommercialIdentityFailure(
          kind: CommercialIdentityFailureKind.activationOutcomeUnknown,
          safeCode: 'ACTIVATION_ATTEMPT_RECOVERY_REQUIRED',
        );
      }
    } else if (pending.state == CommercialCredentialStoreState.unavailable ||
        pending.state == CommercialCredentialStoreState.invalidated) {
      throw const CommercialIdentityFailure(
        kind: CommercialIdentityFailureKind.secureStoreUnavailable,
        safeCode: 'IDENTITY_SECURE_STORE_UNAVAILABLE',
      );
    } else {
      throw const CommercialIdentityFailure(
        kind: CommercialIdentityFailureKind.secureStoreMalformed,
        safeCode: 'ACTIVATION_ATTEMPT_STORE_INVALID',
      );
    }
    return _redeemAttempt(attempt);
  }

  Future<String?> _readInstallationReference() async {
    try {
      return await installationReferenceReader();
    } on CommercialIdentityFailure {
      rethrow;
    } on Object {
      throw const CommercialIdentityFailure(
        kind: CommercialIdentityFailureKind.identityBindingStorageFailure,
        safeCode: 'IDENTITY_BINDING_STORAGE_FAILED',
        retryable: true,
      );
    }
  }

  Future<DeviceCredential> recoverActivation() async {
    final pending = await activationAttemptStore.readActivationAttempt();
    if (pending.state != CommercialCredentialStoreState.valid ||
        pending.value == null) {
      throw const CommercialIdentityFailure(
        kind: CommercialIdentityFailureKind.activationOutcomeUnknown,
        safeCode: 'ACTIVATION_ATTEMPT_NOT_RECOVERABLE',
      );
    }
    return _redeemAttempt(pending.value!);
  }

  Future<DeviceCredential> _redeemAttempt(
    CommercialActivationAttempt attempt,
  ) async {
    try {
      final response = await transport.send(
        CommercialIdentityTransportRequest(
          method: CommercialIdentityContract.activationMethod,
          relativePath: CommercialIdentityContract.activationPath,
          idempotencyKey: attempt.idempotencyKey,
          jsonBody: _codec.encodeActivation(
            ActivationRedeemRequest(
              workspaceCode: attempt.workspaceCode,
              activationCode: attempt.activationCode,
              clientInstallationReference: attempt.clientInstallationReference,
              deviceLabel: attempt.deviceLabel,
              platform: attempt.platform,
            ),
          ),
        ),
      );
      final body = _requireSuccessBody(response, expectedStatus: 200);
      final result = _codec.decodeActivation(body);
      final binding = await bindingPort.readBinding();
      if (binding != null && binding.deviceId != result.credential.deviceId) {
        throw const CommercialIdentityFailure(
          kind: CommercialIdentityFailureKind.identityContextMismatch,
          safeCode: 'IDENTITY_CONTEXT_MISMATCH',
        );
      }
      await credentialStore.replaceDeviceCredential(
        result.credential,
        bindingFingerprint: binding?.fingerprint,
      );
      final committed = await credentialStore.readDeviceCredential();
      if (committed.state != CommercialCredentialStoreState.valid ||
          committed.value?.deviceId != result.credential.deviceId ||
          committed.value?.secret != result.credential.secret ||
          committed.value?.secretVersion != result.credential.secretVersion) {
        throw const CommercialIdentityFailure(
          kind: CommercialIdentityFailureKind.credentialPersistenceFailed,
          safeCode: 'IDENTITY_CREDENTIAL_PERSISTENCE_FAILED',
        );
      }
      await activationAttemptStore.clearActivationAttempt();
      return result.credential;
    } on CommercialIdentityFailure catch (failure) {
      if (failure.kind == CommercialIdentityFailureKind.networkAmbiguous) {
        throw const CommercialIdentityFailure(
          kind: CommercialIdentityFailureKind.activationOutcomeUnknown,
          safeCode: 'ACTIVATION_OUTCOME_UNKNOWN',
          retryable: true,
        );
      }
      if (_activationFailureIsDefinitive(failure.kind)) {
        await activationAttemptStore.clearActivationAttempt();
      }
      rethrow;
    }
  }

  Future<CommercialAuthenticationResult> requestLogin({
    required String workspaceCode,
    required String login,
    required String password,
  }) async {
    final deviceRead = await credentialStore.readDeviceCredential();
    if (deviceRead.state == CommercialCredentialStoreState.retired) {
      throw const CommercialIdentityFailure(
        kind: CommercialIdentityFailureKind.deviceInactive,
        safeCode: 'DEVICE_NOT_ACTIVE',
      );
    }
    if (deviceRead.state != CommercialCredentialStoreState.valid ||
        deviceRead.value == null) {
      throw const CommercialIdentityFailure(
        kind: CommercialIdentityFailureKind.secureStoreUnavailable,
        safeCode: 'DEVICE_CREDENTIAL_UNAVAILABLE',
      );
    }
    final request = CommercialLoginRequest(
      workspaceCode: validateWorkspaceCode(workspaceCode),
      login: validateLogin(login),
      password: _requiredPassword(password),
      deviceCredential: deviceRead.value!,
    );
    final response = await transport.send(
      CommercialIdentityTransportRequest(
        method: CommercialIdentityContract.loginMethod,
        relativePath: CommercialIdentityContract.loginPath,
        jsonBody: _codec.encodeLogin(request),
      ),
    );
    final result = _codec.decodeAuthentication(
      _requireSuccessBody(response, expectedStatus: 200),
    );
    if (result.context.deviceId != deviceRead.value!.deviceId) {
      throw const CommercialIdentityFailure(
        kind: CommercialIdentityFailureKind.protocolContractMismatch,
        safeCode: 'LOGIN_DEVICE_CONTEXT_MISMATCH',
      );
    }
    return result;
  }

  Future<CommercialAuthenticationResult> requestRefresh(
    RefreshCredential credential,
  ) async {
    final response = await transport.send(
      CommercialIdentityTransportRequest(
        method: CommercialIdentityContract.refreshMethod,
        relativePath: CommercialIdentityContract.refreshPath,
        jsonBody: _codec.encodeRefresh(credential),
      ),
    );
    return _codec.decodeAuthentication(
      _requireSuccessBody(response, expectedStatus: 200),
    );
  }

  Future<CommercialIdentityContext> confirmAndBind(
    CommercialAuthenticationResult authentication,
  ) async {
    final me = await _requestMeWithOneSafeRetry(authentication.accessToken);
    if (!_contextsAgree(authentication.context, me)) {
      throw const CommercialIdentityFailure(
        kind: CommercialIdentityFailureKind.identityContextMismatch,
        safeCode: 'IDENTITY_CONTEXT_MISMATCH',
      );
    }
    final now = _clock().toUtc();
    try {
      await bindingPort.bindOrMatch(
        me.toBinding(apiOrigin: normalizedApiOrigin, firstBoundAtUtc: now),
        me.toSnapshot(now),
      );
    } on CommercialIdentityFailure {
      rethrow;
    } on Object {
      throw const CommercialIdentityFailure(
        kind: CommercialIdentityFailureKind.identityBindingStorageFailure,
        safeCode: 'IDENTITY_BINDING_STORAGE_FAILED',
        retryable: true,
      );
    }
    return me;
  }

  Future<CommercialIdentityContext> _requestMeWithOneSafeRetry(
    AccessTokenLease accessToken,
  ) async {
    for (var attempt = 0; attempt < 2; attempt++) {
      try {
        final response = await transport.send(
          CommercialIdentityTransportRequest(
            method: CommercialIdentityContract.meMethod,
            relativePath: CommercialIdentityContract.mePath,
            bearerAccessToken: accessToken.value,
          ),
        );
        return _codec.decodeMe(
          _requireSuccessBody(response, expectedStatus: 200),
        );
      } on CommercialIdentityFailure catch (failure) {
        final safeToRetry =
            failure.kind == CommercialIdentityFailureKind.networkUnavailable ||
            (failure.kind ==
                    CommercialIdentityFailureKind
                        .backendTemporarilyUnavailable &&
                failure.httpStatus == 503);
        if (!safeToRetry || attempt == 1) {
          rethrow;
        }
      }
    }
    throw const CommercialIdentityFailure(
      kind: CommercialIdentityFailureKind.identityContextUnavailable,
      safeCode: 'IDENTITY_CONTEXT_UNAVAILABLE',
    );
  }

  Future<void> bestEffortLogout(
    AccessTokenLease? accessToken, {
    required bool allSessions,
  }) async {
    if (accessToken == null) {
      return;
    }
    final request = CommercialIdentityTransportRequest(
      method: allSessions
          ? CommercialIdentityContract.logoutAllMethod
          : CommercialIdentityContract.logoutMethod,
      relativePath: allSessions
          ? CommercialIdentityContract.logoutAllPath
          : CommercialIdentityContract.logoutPath,
      bearerAccessToken: accessToken.value,
    );
    final attempts = allSessions ? 1 : 2;
    for (var attempt = 0; attempt < attempts; attempt++) {
      try {
        final response = await transport.send(request);
        if (response.statusCode == 204 && response.jsonBody == null) {
          return;
        }
        _throwResponseFailure(response);
      } on CommercialIdentityFailure catch (failure) {
        final safeToRetry =
            !allSessions &&
            (failure.kind == CommercialIdentityFailureKind.networkUnavailable ||
                failure.kind ==
                    CommercialIdentityFailureKind
                        .backendTemporarilyUnavailable);
        if (!safeToRetry || attempt + 1 >= attempts) {
          rethrow;
        }
      }
    }
  }

  Map<String, Object?> _requireSuccessBody(
    CommercialIdentityTransportResponse response, {
    required int expectedStatus,
  }) {
    if (response.statusCode != expectedStatus) {
      _throwResponseFailure(response);
    }
    final body = response.jsonBody;
    if (body == null) {
      throw const CommercialIdentityFailure(
        kind: CommercialIdentityFailureKind.protocolContractMismatch,
        safeCode: 'IDENTITY_RESPONSE_BODY_MISSING',
      );
    }
    return body;
  }

  Never _throwResponseFailure(CommercialIdentityTransportResponse response) {
    final body = response.jsonBody;
    if (body == null) {
      throw const CommercialIdentityFailure(
        kind: CommercialIdentityFailureKind.protocolContractMismatch,
        safeCode: 'IDENTITY_ERROR_ENVELOPE_MISSING',
      );
    }
    throw _codec.decodeError(
      body,
      httpStatus: response.statusCode,
      retryAfter: CommercialIdentityHttpClient.parseRetryAfter(
        response.headers['retry-after'],
        _clock().toUtc(),
      ),
    );
  }

  static bool _contextsAgree(
    CommercialIdentityContext first,
    CommercialIdentityContext second,
  ) =>
      first.sameImmutableContext(second) &&
      first.workspaceCode == second.workspaceCode &&
      first.userDisplayName == second.userDisplayName &&
      first.role == second.role &&
      first.deviceLabel == second.deviceLabel;

  static bool _activationFailureIsDefinitive(
    CommercialIdentityFailureKind kind,
  ) =>
      kind == CommercialIdentityFailureKind.invalidActivation ||
      kind == CommercialIdentityFailureKind.activationAlreadyUsed ||
      kind == CommercialIdentityFailureKind.activationRecoveryExpired ||
      kind == CommercialIdentityFailureKind.activationExpired;

  static String validateWorkspaceCode(String input) {
    if (input != input.trim()) {
      throw const CommercialIdentityFailure(
        kind: CommercialIdentityFailureKind.invalidWorkspaceCode,
        safeCode: 'WORKSPACE_CODE_INVALID',
      );
    }
    final normalized = input.toUpperCase();
    if (normalized.length < 3 ||
        normalized.length > 64 ||
        !RegExp(r'^[A-Z0-9]+(?:-[A-Z0-9]+)*$').hasMatch(normalized)) {
      throw const CommercialIdentityFailure(
        kind: CommercialIdentityFailureKind.invalidWorkspaceCode,
        safeCode: 'WORKSPACE_CODE_INVALID',
      );
    }
    return normalized;
  }

  static String validateDeviceLabel(String value) {
    final trimmed = value.trim();
    if (trimmed.isEmpty || trimmed.length > 200) {
      throw const CommercialIdentityFailure(
        kind: CommercialIdentityFailureKind.invalidDeviceLabel,
        safeCode: 'DEVICE_LABEL_INVALID',
      );
    }
    return trimmed;
  }

  static String validateLogin(String value) {
    final trimmed = value.trim();
    if (trimmed.isEmpty || trimmed.length > 100) {
      throw const CommercialIdentityFailure(
        kind: CommercialIdentityFailureKind.invalidCredentials,
        safeCode: 'AUTHENTICATION_FAILED',
      );
    }
    return trimmed;
  }

  static String _requiredActivationCode(String value) {
    if (value.isEmpty || value.trim().isEmpty || value.length > 4096) {
      throw const CommercialIdentityFailure(
        kind: CommercialIdentityFailureKind.invalidActivation,
        safeCode: 'DEVICE_ACTIVATION_INVALID',
      );
    }
    return value;
  }

  static String _requiredPassword(String value) {
    if (value.length < 10 || value.length > 256 || value.trim().isEmpty) {
      throw const CommercialIdentityFailure(
        kind: CommercialIdentityFailureKind.invalidCredentials,
        safeCode: 'AUTHENTICATION_FAILED',
      );
    }
    return value;
  }
}
