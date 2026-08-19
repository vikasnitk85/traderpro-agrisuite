import 'package:flutter/foundation.dart';

import '../core/identity/commercial_authentication_status.dart';
import '../core/identity/commercial_identity_failure.dart';
import '../core/identity/commercial_identity_models.dart';
import '../core/identity/commercial_identity_ports.dart';
import '../core/identity/commercial_identity_state_machine.dart';
import '../infrastructure/identity/commercial_identity_service.dart';
import '../infrastructure/identity/commercial_refresh_coordinator.dart';

final class CommercialIdentityView {
  const CommercialIdentityView({
    required this.displayName,
    required this.workspaceCode,
    required this.role,
    required this.deviceLabel,
  });

  final String displayName;
  final String workspaceCode;
  final String role;
  final String deviceLabel;
}

final class CommercialIdentityController extends ChangeNotifier {
  CommercialIdentityController({
    required this.identityService,
    required this.refreshCoordinator,
    required this.credentialStore,
    required this.activationAttemptStore,
    required this.bindingPort,
    this._stateMachine = const CommercialIdentityStateMachine(),
  });

  final CommercialIdentityService identityService;
  final CommercialRefreshCoordinator refreshCoordinator;
  final CommercialIdentityCredentialStore credentialStore;
  final CommercialActivationAttemptStore activationAttemptStore;
  final CommercialIdentityBindingPort bindingPort;
  final CommercialIdentityStateMachine _stateMachine;

  CommercialAuthenticationStatus _status =
      CommercialAuthenticationStatus.identityContextLoading;
  String? _safeFailureCode;
  CommercialIdentityView? _identity;
  CommercialAuthenticationStatus _activationFallbackStatus =
      CommercialAuthenticationStatus.deviceUnregistered;
  bool _disposed = false;

  CommercialAuthenticationStatus get status => _status;
  String? get safeFailureCode => _safeFailureCode;
  CommercialIdentityView? get identity => _identity;

  Future<void> start() async {
    CommercialAccountBinding? binding;
    CommercialIdentitySnapshot? snapshot;
    try {
      binding = await bindingPort.readBinding();
      snapshot = await bindingPort.readSnapshot();
      if (snapshot != null) {
        _identity = _viewFromSnapshot(snapshot);
      }
      final activation = await activationAttemptStore.readActivationAttempt();
      if (activation.state == CommercialCredentialStoreState.valid &&
          activation.value != null) {
        _activationFallbackStatus = binding == null
            ? CommercialAuthenticationStatus.deviceUnregistered
            : CommercialAuthenticationStatus.deviceRevoked;
        _setStatus(CommercialAuthenticationStatus.activationOutcomeUnknown);
        return;
      }
      if (activation.state != CommercialCredentialStoreState.empty) {
        _safeFailureCode = 'ACTIVATION_ATTEMPT_STORE_INVALID';
        _setStatus(
          CommercialAuthenticationStatus.lockedFailClosed,
          preserveFailure: true,
        );
        return;
      }
      final device = await credentialStore.readDeviceCredential();
      final refresh = await credentialStore.readRefreshCredential();
      final next = _stateMachine.classifyStartup(
        CommercialIdentityStartupFacts(
          hasBinding: binding != null,
          hasSnapshot: snapshot != null,
          deviceState: device.state,
          refreshState: refresh.state,
        ),
      );
      if (next != CommercialAuthenticationStatus.refreshing) {
        _setStatus(next);
        return;
      }
      _setStatus(CommercialAuthenticationStatus.refreshing);
      final context = await refreshCoordinator.restoreSession();
      _setReady(context);
    } on CommercialIdentityFailure catch (failure) {
      refreshCoordinator.clearMemoryAccessToken();
      await _applyFailure(
        failure,
        hasBinding: binding != null,
        hasSnapshot: snapshot != null,
      );
    } on Object {
      refreshCoordinator.clearMemoryAccessToken();
      await _applyFailure(_unexpectedFailure());
    }
  }

  Future<void> activate({
    required String workspaceCode,
    required String activationCode,
    required String deviceLabel,
  }) async {
    _activationFallbackStatus =
        _status == CommercialAuthenticationStatus.deviceRevoked
        ? CommercialAuthenticationStatus.deviceRevoked
        : CommercialAuthenticationStatus.deviceUnregistered;
    _setStatus(CommercialAuthenticationStatus.activating);
    try {
      await refreshCoordinator.activateDevice(
        workspaceCode: workspaceCode,
        activationCode: activationCode,
        deviceLabel: deviceLabel,
      );
      _identity = null;
      _activationFallbackStatus =
          CommercialAuthenticationStatus.deviceUnregistered;
      _setStatus(CommercialAuthenticationStatus.deviceRegisteredNoSession);
    } on CommercialIdentityFailure catch (failure) {
      await _applyActivationFailure(failure);
    } on Object {
      await _applyActivationFailure(_unexpectedFailure());
    }
  }

  Future<void> recoverActivation() async {
    _setStatus(CommercialAuthenticationStatus.activating);
    try {
      await refreshCoordinator.recoverDeviceActivation();
      _activationFallbackStatus =
          CommercialAuthenticationStatus.deviceUnregistered;
      _setStatus(CommercialAuthenticationStatus.deviceRegisteredNoSession);
    } on CommercialIdentityFailure catch (failure) {
      await _applyActivationFailure(failure);
    } on Object {
      await _applyActivationFailure(_unexpectedFailure());
    }
  }

  Future<void> login({
    required String workspaceCode,
    required String login,
    required String password,
  }) async {
    _setStatus(CommercialAuthenticationStatus.authenticating);
    try {
      final context = await refreshCoordinator.login(
        workspaceCode: workspaceCode,
        login: login,
        password: password,
      );
      _setReady(context);
    } on CommercialIdentityFailure catch (failure) {
      await _applyFailure(failure);
    } on Object {
      await _applyFailure(_unexpectedFailure());
    }
  }

  Future<void> revalidate() async {
    _setStatus(CommercialAuthenticationStatus.refreshing);
    try {
      final context = await refreshCoordinator.restoreSession();
      _setReady(context);
    } on CommercialIdentityFailure catch (failure) {
      refreshCoordinator.clearMemoryAccessToken();
      if (!_needsOfflineFacts(failure.kind)) {
        await _applyFailure(failure);
        return;
      }
      try {
        final binding = await bindingPort.readBinding();
        final snapshot = await bindingPort.readSnapshot();
        await _applyFailure(
          failure,
          hasBinding: binding != null,
          hasSnapshot: snapshot != null,
        );
      } on CommercialIdentityFailure catch (storageFailure) {
        await _applyFailure(storageFailure);
      } on Object {
        await _applyFailure(_bindingStorageFailure());
      }
    } on Object {
      refreshCoordinator.clearMemoryAccessToken();
      await _applyFailure(_unexpectedFailure());
    }
  }

  Future<void> logout({required bool allSessions}) async {
    _setStatus(CommercialAuthenticationStatus.loggingOut);
    try {
      await refreshCoordinator.logout(allSessions: allSessions);
      _identity = null;
      _setStatus(CommercialAuthenticationStatus.deviceRegisteredNoSession);
    } on CommercialIdentityFailure catch (failure) {
      if (failure.kind ==
          CommercialIdentityFailureKind.remoteLogoutAllUnconfirmed) {
        _safeFailureCode = failure.safeCode;
        _setStatus(
          CommercialAuthenticationStatus.logoutAllUnconfirmed,
          preserveFailure: true,
        );
        return;
      }
      await _applyFailure(failure);
    } on Object {
      _safeFailureCode = 'IDENTITY_LOGOUT_UNEXPECTED';
      _setStatus(
        CommercialAuthenticationStatus.lockedFailClosed,
        preserveFailure: true,
      );
    }
  }

  Future<void> _applyActivationFailure(
    CommercialIdentityFailure failure,
  ) async {
    _safeFailureCode = failure.safeCode;
    final next = switch (failure.kind) {
      CommercialIdentityFailureKind.activationOutcomeUnknown ||
      CommercialIdentityFailureKind.activationRecoveryUnavailable ||
      CommercialIdentityFailureKind.networkUnavailable ||
      CommercialIdentityFailureKind.networkAmbiguous ||
      CommercialIdentityFailureKind.tlsFailure =>
        CommercialAuthenticationStatus.activationOutcomeUnknown,
      CommercialIdentityFailureKind.secureStoreUnavailable ||
      CommercialIdentityFailureKind.secureStoreMalformed ||
      CommercialIdentityFailureKind.credentialPersistenceFailed ||
      CommercialIdentityFailureKind.identityBindingStorageFailure ||
      CommercialIdentityFailureKind.unexpectedIdentityFailure ||
      CommercialIdentityFailureKind.protocolContractMismatch =>
        CommercialAuthenticationStatus.lockedFailClosed,
      CommercialIdentityFailureKind.identityContextMismatch =>
        CommercialAuthenticationStatus.identityContextMismatch,
      CommercialIdentityFailureKind.deviceInactive =>
        CommercialAuthenticationStatus.deviceRevoked,
      _ => _activationFallbackStatus,
    };
    if (failure.kind == CommercialIdentityFailureKind.deviceInactive) {
      if (!await _retireInactiveDevice()) {
        return;
      }
    }
    _setStatus(next, preserveFailure: true);
  }

  Future<void> onResume() async {
    if (_disposed) {
      return;
    }
    if (_status ==
            CommercialAuthenticationStatus.boundOfflineRevalidationRequired ||
        (_status == CommercialAuthenticationStatus.identityContextReady &&
            (!refreshCoordinator.hasMemoryAccessToken ||
                !(refreshCoordinator.currentAccessToken?.isFreshAt(
                      DateTime.now().toUtc(),
                    ) ??
                    false)))) {
      await revalidate();
    }
  }

  Future<void> _applyFailure(
    CommercialIdentityFailure failure, {
    bool hasBinding = false,
    bool hasSnapshot = false,
  }) async {
    _safeFailureCode = failure.safeCode;
    final next = switch (failure.kind) {
      CommercialIdentityFailureKind.activationOutcomeUnknown ||
      CommercialIdentityFailureKind.activationRecoveryUnavailable =>
        CommercialAuthenticationStatus.activationOutcomeUnknown,
      CommercialIdentityFailureKind.deviceInactive =>
        CommercialAuthenticationStatus.deviceRevoked,
      CommercialIdentityFailureKind.identityContextMismatch =>
        CommercialAuthenticationStatus.identityContextMismatch,
      CommercialIdentityFailureKind.networkUnavailable ||
      CommercialIdentityFailureKind.networkAmbiguous ||
      CommercialIdentityFailureKind.tlsFailure => _stateMachine.classifyOffline(
        hasBinding: hasBinding,
        hasSnapshot: hasSnapshot,
      ),
      CommercialIdentityFailureKind.secureStoreUnavailable ||
      CommercialIdentityFailureKind.secureStoreMalformed ||
      CommercialIdentityFailureKind.credentialPersistenceFailed ||
      CommercialIdentityFailureKind.identityBindingStorageFailure ||
      CommercialIdentityFailureKind.unexpectedIdentityFailure ||
      CommercialIdentityFailureKind.protocolContractMismatch =>
        CommercialAuthenticationStatus.lockedFailClosed,
      CommercialIdentityFailureKind.refreshRejected ||
      CommercialIdentityFailureKind.refreshExpired ||
      CommercialIdentityFailureKind.sessionRevoked ||
      CommercialIdentityFailureKind.refreshReplayDetected ||
      CommercialIdentityFailureKind.accountDisabled ||
      CommercialIdentityFailureKind.accessExpiredOrInvalid =>
        CommercialAuthenticationStatus.authRequired,
      CommercialIdentityFailureKind.remoteLogoutAllUnconfirmed =>
        CommercialAuthenticationStatus.logoutAllUnconfirmed,
      _ =>
        _status == CommercialAuthenticationStatus.activating
            ? CommercialAuthenticationStatus.deviceUnregistered
            : CommercialAuthenticationStatus.deviceRegisteredNoSession,
    };
    if (failure.kind == CommercialIdentityFailureKind.deviceInactive) {
      if (!await _retireInactiveDevice()) {
        return;
      }
    }
    _setStatus(next, preserveFailure: true);
  }

  Future<bool> _retireInactiveDevice() async {
    try {
      await refreshCoordinator.handleDefiniteDeviceInactive();
      return true;
    } on CommercialIdentityFailure catch (failure) {
      _setFailClosed(failure);
      return false;
    } on Object {
      _setFailClosed(_credentialPersistenceFailure());
      return false;
    }
  }

  void _setFailClosed(CommercialIdentityFailure failure) {
    refreshCoordinator.clearMemoryAccessToken();
    _safeFailureCode = failure.safeCode;
    _setStatus(
      CommercialAuthenticationStatus.lockedFailClosed,
      preserveFailure: true,
    );
  }

  void _setReady(CommercialIdentityContext context) {
    _identity = CommercialIdentityView(
      displayName: context.userDisplayName,
      workspaceCode: context.workspaceCode,
      role: context.role.wireValue,
      deviceLabel: context.deviceLabel,
    );
    _setStatus(CommercialAuthenticationStatus.identityContextReady);
  }

  void _setStatus(
    CommercialAuthenticationStatus status, {
    bool preserveFailure = false,
  }) {
    if (_disposed) {
      return;
    }
    _status = status;
    if (!preserveFailure) {
      _safeFailureCode = null;
    }
    notifyListeners();
  }

  static CommercialIdentityView _viewFromSnapshot(
    CommercialIdentitySnapshot snapshot,
  ) => CommercialIdentityView(
    displayName: snapshot.userDisplayName,
    workspaceCode: snapshot.workspaceCode,
    role: snapshot.role.wireValue,
    deviceLabel: snapshot.deviceLabel,
  );

  static bool _needsOfflineFacts(CommercialIdentityFailureKind kind) =>
      kind == CommercialIdentityFailureKind.networkUnavailable ||
      kind == CommercialIdentityFailureKind.networkAmbiguous ||
      kind == CommercialIdentityFailureKind.tlsFailure;

  static CommercialIdentityFailure _bindingStorageFailure() =>
      const CommercialIdentityFailure(
        kind: CommercialIdentityFailureKind.identityBindingStorageFailure,
        safeCode: 'IDENTITY_BINDING_STORAGE_FAILED',
        retryable: true,
      );

  static CommercialIdentityFailure _credentialPersistenceFailure() =>
      const CommercialIdentityFailure(
        kind: CommercialIdentityFailureKind.credentialPersistenceFailed,
        safeCode: 'IDENTITY_CREDENTIAL_PERSISTENCE_FAILED',
      );

  static CommercialIdentityFailure _unexpectedFailure() =>
      const CommercialIdentityFailure(
        kind: CommercialIdentityFailureKind.unexpectedIdentityFailure,
        safeCode: 'IDENTITY_OPERATION_FAILED',
      );

  @override
  void dispose() {
    _disposed = true;
    super.dispose();
  }
}
