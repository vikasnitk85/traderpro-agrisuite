import 'dart:async';

import 'package:crypto/crypto.dart';

import '../../core/identity/commercial_identity_failure.dart';
import '../../core/identity/commercial_identity_models.dart';
import '../../core/identity/commercial_identity_ports.dart';
import 'commercial_identity_service.dart';

final class CommercialRefreshCoordinator {
  CommercialRefreshCoordinator({
    required this.identityService,
    required this.credentialStore,
    DateTime Function()? clock,
    this.refreshSkew = const Duration(seconds: 60),
    this.predecessorReplayWindow = const Duration(seconds: 30),
  }) : _clock = clock ?? DateTime.now;

  final CommercialIdentityService identityService;
  final CommercialIdentityCredentialStore credentialStore;
  final DateTime Function() _clock;
  final Duration refreshSkew;
  final Duration predecessorReplayWindow;

  AccessTokenLease? _accessToken;
  Future<CommercialIdentityContext>? _inFlightRefresh;
  int _sessionEpoch = 0;
  bool _disposed = false;

  bool get hasMemoryAccessToken => _accessToken != null;
  AccessTokenLease? get currentAccessToken => _accessToken;
  int get sessionEpoch => _sessionEpoch;

  Future<CommercialIdentityContext> login({
    required String workspaceCode,
    required String login,
    required String password,
  }) async {
    _ensureActive();
    final epoch = _sessionEpoch;
    final result = await identityService.requestLogin(
      workspaceCode: workspaceCode,
      login: login,
      password: password,
    );
    return _commitAuthentication(
      result,
      expectedEpoch: epoch,
      predecessorDigest: null,
    );
  }

  Future<DeviceCredential> activateDevice({
    required String workspaceCode,
    required String activationCode,
    required String deviceLabel,
  }) async {
    await _beginDeviceCredentialReplacement();
    return identityService.activate(
      workspaceCode: workspaceCode,
      activationCode: activationCode,
      deviceLabel: deviceLabel,
    );
  }

  Future<DeviceCredential> recoverDeviceActivation() async {
    await _beginDeviceCredentialReplacement();
    return identityService.recoverActivation();
  }

  Future<CommercialIdentityContext> restoreSession() =>
      _refreshSingleFlight(force: true);

  Future<AccessTokenLease> validAccessToken({bool forceRefresh = false}) async {
    _ensureActive();
    final current = _accessToken;
    if (!forceRefresh &&
        current != null &&
        current.isFreshAt(_clock().toUtc(), refreshSkew: refreshSkew)) {
      return current;
    }
    await _refreshSingleFlight(force: true);
    final refreshed = _accessToken;
    if (refreshed == null) {
      throw const CommercialIdentityFailure(
        kind: CommercialIdentityFailureKind.accessExpiredOrInvalid,
        safeCode: 'ACCESS_TOKEN_INVALID',
      );
    }
    return refreshed;
  }

  Future<CommercialIdentityContext> _refreshSingleFlight({
    required bool force,
  }) {
    _ensureActive();
    final current = _accessToken;
    if (!force &&
        current != null &&
        current.isFreshAt(_clock().toUtc(), refreshSkew: refreshSkew)) {
      throw StateError('A context refresh was requested without a context.');
    }
    final active = _inFlightRefresh;
    if (active != null) {
      return active;
    }
    final refresh = _performRefresh(_sessionEpoch);
    _inFlightRefresh = refresh;
    return refresh.whenComplete(() {
      if (identical(_inFlightRefresh, refresh)) {
        _inFlightRefresh = null;
      }
    });
  }

  Future<CommercialIdentityContext> _performRefresh(int expectedEpoch) async {
    final read = await credentialStore.readRefreshCredential();
    if (read.state != CommercialCredentialStoreState.valid ||
        read.value == null) {
      throw const CommercialIdentityFailure(
        kind: CommercialIdentityFailureKind.refreshRejected,
        safeCode: 'REFRESH_CREDENTIAL_UNAVAILABLE',
      );
    }
    final predecessor = read.value!;
    final now = _clock().toUtc();
    if (!predecessor.expiresAtUtc.isAfter(now)) {
      await credentialStore.clearRefreshCredential();
      throw const CommercialIdentityFailure(
        kind: CommercialIdentityFailureKind.refreshExpired,
        safeCode: 'REFRESH_TOKEN_EXPIRED',
      );
    }
    final predecessorDigest = _credentialDigest(predecessor.value);
    final pendingRecovery =
        predecessor.predecessorDigest == predecessorDigest &&
        predecessor.committedAtUtc != null;
    if (pendingRecovery &&
        now.difference(predecessor.committedAtUtc!) >=
            predecessorReplayWindow) {
      await credentialStore.clearRefreshCredential();
      throw const CommercialIdentityFailure(
        kind: CommercialIdentityFailureKind.refreshRejected,
        safeCode: 'REFRESH_RECOVERY_WINDOW_EXPIRED',
      );
    }

    if (!pendingRecovery) {
      final attemptMarker = RefreshCredential(
        value: predecessor.value,
        expiresAtUtc: predecessor.expiresAtUtc,
        bindingFingerprint: predecessor.bindingFingerprint,
        predecessorDigest: predecessorDigest,
        committedAtUtc: now,
      );
      await credentialStore.replaceRefreshCredential(attemptMarker);
      final markerRead = await credentialStore.readRefreshCredential();
      if (markerRead.state != CommercialCredentialStoreState.valid ||
          markerRead.value?.value != predecessor.value ||
          markerRead.value?.predecessorDigest != predecessorDigest) {
        throw const CommercialIdentityFailure(
          kind: CommercialIdentityFailureKind.credentialPersistenceFailed,
          safeCode: 'REFRESH_ATTEMPT_PERSISTENCE_FAILED',
        );
      }
    }

    CommercialAuthenticationResult result;
    try {
      result = await identityService.requestRefresh(predecessor);
    } on CommercialIdentityFailure catch (failure) {
      if (!pendingRecovery &&
          failure.kind == CommercialIdentityFailureKind.networkAmbiguous &&
          _clock().toUtc().difference(now) < predecessorReplayWindow) {
        try {
          result = await identityService.requestRefresh(predecessor);
        } on CommercialIdentityFailure catch (replayFailure) {
          await _handleRefreshFailure(replayFailure);
          rethrow;
        }
      } else {
        await _handleRefreshFailure(failure);
        rethrow;
      }
    }

    if (expectedEpoch != _sessionEpoch || _disposed) {
      throw const CommercialIdentityFailure(
        kind: CommercialIdentityFailureKind.sessionRevoked,
        safeCode: 'SESSION_EPOCH_CHANGED',
      );
    }
    return _commitAuthentication(
      result,
      expectedEpoch: expectedEpoch,
      predecessorDigest: predecessorDigest,
      expectedBindingFingerprint: predecessor.bindingFingerprint,
    );
  }

  Future<CommercialIdentityContext> _commitAuthentication(
    CommercialAuthenticationResult result, {
    required int expectedEpoch,
    required String? predecessorDigest,
    String? expectedBindingFingerprint,
  }) async {
    final now = _clock().toUtc();
    final candidateBinding = result.context.toBinding(
      apiOrigin: identityService.normalizedApiOrigin,
      firstBoundAtUtc: now,
    );
    if (expectedBindingFingerprint != null &&
        expectedBindingFingerprint != candidateBinding.fingerprint) {
      await _clearLocalSession();
      throw const CommercialIdentityFailure(
        kind: CommercialIdentityFailureKind.identityContextMismatch,
        safeCode: 'IDENTITY_CONTEXT_MISMATCH',
      );
    }
    final replacement = RefreshCredential(
      value: result.refreshCredential.value,
      expiresAtUtc: result.refreshCredential.expiresAtUtc,
      bindingFingerprint: candidateBinding.fingerprint,
      predecessorDigest: predecessorDigest,
      committedAtUtc: now,
    );
    if (expectedEpoch != _sessionEpoch || _disposed) {
      throw const CommercialIdentityFailure(
        kind: CommercialIdentityFailureKind.sessionRevoked,
        safeCode: 'SESSION_EPOCH_CHANGED',
      );
    }
    await credentialStore.replaceRefreshCredential(replacement);
    final committed = await credentialStore.readRefreshCredential();
    if (committed.state != CommercialCredentialStoreState.valid ||
        committed.value?.value != replacement.value ||
        committed.value?.expiresAtUtc != replacement.expiresAtUtc ||
        committed.value?.bindingFingerprint != replacement.bindingFingerprint) {
      throw const CommercialIdentityFailure(
        kind: CommercialIdentityFailureKind.credentialPersistenceFailed,
        safeCode: 'IDENTITY_CREDENTIAL_PERSISTENCE_FAILED',
      );
    }
    if (expectedEpoch != _sessionEpoch || _disposed) {
      throw const CommercialIdentityFailure(
        kind: CommercialIdentityFailureKind.sessionRevoked,
        safeCode: 'SESSION_EPOCH_CHANGED',
      );
    }

    _accessToken = result.accessToken;
    try {
      final context = await identityService.confirmAndBind(result);
      if (expectedEpoch != _sessionEpoch || _disposed) {
        throw const CommercialIdentityFailure(
          kind: CommercialIdentityFailureKind.sessionRevoked,
          safeCode: 'SESSION_EPOCH_CHANGED',
        );
      }
      return context;
    } on CommercialIdentityFailure catch (failure) {
      _accessToken = null;
      if (failure.kind ==
          CommercialIdentityFailureKind.identityContextMismatch) {
        try {
          await identityService.bestEffortLogout(
            result.accessToken,
            allSessions: true,
          );
        } on CommercialIdentityFailure {
          // A mismatch remains fail-closed even if family revocation is
          // unavailable or its outcome is ambiguous.
        }
        await credentialStore.clearRefreshCredential();
      } else if (failure.kind == CommercialIdentityFailureKind.sessionRevoked ||
          failure.kind == CommercialIdentityFailureKind.refreshReplayDetected ||
          failure.kind == CommercialIdentityFailureKind.deviceInactive ||
          failure.kind == CommercialIdentityFailureKind.accountDisabled) {
        await credentialStore.clearRefreshCredential();
      }
      rethrow;
    }
  }

  Future<void> logout({required bool allSessions}) async {
    if (allSessions) {
      await _logoutAllConfirmed();
      return;
    }
    await _logoutLocal();
  }

  Future<void> _logoutLocal() async {
    if (_disposed) {
      return;
    }
    _sessionEpoch += 1;
    final access = _accessToken;
    _accessToken = null;
    try {
      await identityService.bestEffortLogout(access, allSessions: false);
    } on CommercialIdentityFailure {
      // Local logout is authoritative for local credential removal.
    }
    final inFlight = _inFlightRefresh;
    if (inFlight != null) {
      try {
        await inFlight;
      } on Object {
        // The epoch prevents a late result from publishing a session.
      }
    }
    await credentialStore.clearRefreshCredential();
  }

  Future<void> _logoutAllConfirmed() async {
    _ensureActive();
    late final AccessTokenLease access;
    try {
      access = await validAccessToken();
      await identityService.bestEffortLogout(access, allSessions: true);
    } on CommercialIdentityFailure catch (failure) {
      if (_isSecureStoreFailure(failure.kind)) {
        rethrow;
      }
      throw CommercialIdentityFailure(
        kind: CommercialIdentityFailureKind.remoteLogoutAllUnconfirmed,
        safeCode: 'LOGOUT_ALL_NOT_CONFIRMED',
        retryable: failure.retryable,
        httpStatus: failure.httpStatus,
        retryAfter: failure.retryAfter,
      );
    }

    _sessionEpoch += 1;
    _accessToken = null;
    final inFlight = _inFlightRefresh;
    if (inFlight != null) {
      try {
        await inFlight;
      } on Object {
        // The epoch prevents a late result from publishing a session.
      }
    }
    await credentialStore.clearRefreshCredential();
  }

  Future<void> handleDefiniteDeviceInactive() async {
    _sessionEpoch += 1;
    _accessToken = null;
    await credentialStore.clearRefreshCredential();
    final device = await credentialStore.readDeviceCredential();
    if (device.state == CommercialCredentialStoreState.valid &&
        device.value != null) {
      await credentialStore.retireDeviceCredential(
        deviceId: device.value!.deviceId,
      );
    }
  }

  Future<void> _beginDeviceCredentialReplacement() async {
    _ensureActive();
    _sessionEpoch += 1;
    _accessToken = null;
    final inFlight = _inFlightRefresh;
    if (inFlight != null) {
      try {
        await inFlight;
      } on Object {
        // The epoch prevents a late refresh from publishing a session.
      }
    }
    await credentialStore.clearRefreshCredential();
  }

  Future<void> _handleRefreshFailure(CommercialIdentityFailure failure) async {
    if (failure.kind == CommercialIdentityFailureKind.refreshExpired ||
        failure.kind == CommercialIdentityFailureKind.refreshRejected ||
        failure.kind == CommercialIdentityFailureKind.sessionRevoked ||
        failure.kind == CommercialIdentityFailureKind.refreshReplayDetected ||
        failure.kind == CommercialIdentityFailureKind.accountDisabled) {
      await _clearLocalSession();
    } else if (failure.kind == CommercialIdentityFailureKind.deviceInactive) {
      await handleDefiniteDeviceInactive();
    }
  }

  Future<void> _clearLocalSession() async {
    _accessToken = null;
    await credentialStore.clearRefreshCredential();
  }

  void clearMemoryAccessToken() {
    _accessToken = null;
  }

  Future<void> dispose() async {
    if (_disposed) {
      return;
    }
    _disposed = true;
    _sessionEpoch += 1;
    _accessToken = null;
    final inFlight = _inFlightRefresh;
    if (inFlight != null) {
      try {
        await inFlight;
      } on Object {
        // Disposal intentionally suppresses late session work.
      }
    }
  }

  void _ensureActive() {
    if (_disposed) {
      throw const CommercialIdentityFailure(
        kind: CommercialIdentityFailureKind.sessionRevoked,
        safeCode: 'IDENTITY_SESSION_DISPOSED',
      );
    }
  }

  static String _credentialDigest(String value) =>
      sha256.convert(value.codeUnits).toString();

  static bool _isSecureStoreFailure(CommercialIdentityFailureKind kind) =>
      kind == CommercialIdentityFailureKind.secureStoreUnavailable ||
      kind == CommercialIdentityFailureKind.secureStoreMalformed ||
      kind == CommercialIdentityFailureKind.credentialPersistenceFailed;
}
