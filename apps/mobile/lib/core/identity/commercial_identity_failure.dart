enum CommercialIdentityFailureKind {
  invalidWorkspaceCode,
  invalidCredentials,
  authenticationLocked,
  invalidActivation,
  activationAlreadyUsed,
  activationRecoveryExpired,
  activationRecoveryUnavailable,
  activationOutcomeUnknown,
  activationExpired,
  deviceInactive,
  refreshRejected,
  refreshExpired,
  sessionRevoked,
  refreshReplayDetected,
  accessExpiredOrInvalid,
  identityContextUnavailable,
  identityContextMismatch,
  accountDisabled,
  rateLimited,
  tlsRequired,
  networkUnavailable,
  networkAmbiguous,
  tlsFailure,
  secureStoreUnavailable,
  secureStoreMalformed,
  credentialPersistenceFailed,
  protocolContractMismatch,
  backendTemporarilyUnavailable,
  unexpectedIdentityFailure,
}

final class CommercialIdentityFailure implements Exception {
  const CommercialIdentityFailure({
    required this.kind,
    required this.safeCode,
    this.retryable = false,
    this.httpStatus,
    this.retryAfter,
  });

  final CommercialIdentityFailureKind kind;
  final String safeCode;
  final bool retryable;
  final int? httpStatus;
  final Duration? retryAfter;

  @override
  String toString() =>
      'CommercialIdentityFailure(safeCode: $safeCode, retryable: $retryable)';

  static CommercialIdentityFailure fromBackendCode(
    String code, {
    required int httpStatus,
    bool retryable = false,
    Duration? retryAfter,
  }) => CommercialIdentityFailure(
    kind: switch (code) {
      'WORKSPACE_CODE_INVALID' =>
        CommercialIdentityFailureKind.invalidWorkspaceCode,
      'AUTHENTICATION_FAILED' =>
        CommercialIdentityFailureKind.invalidCredentials,
      'AUTHENTICATION_TEMPORARILY_LOCKED' =>
        CommercialIdentityFailureKind.authenticationLocked,
      'DEVICE_ACTIVATION_INVALID' =>
        CommercialIdentityFailureKind.invalidActivation,
      'DEVICE_ACTIVATION_ALREADY_USED' =>
        CommercialIdentityFailureKind.activationAlreadyUsed,
      'DEVICE_ACTIVATION_RECOVERY_EXPIRED' =>
        CommercialIdentityFailureKind.activationRecoveryExpired,
      'DEVICE_ACTIVATION_RECOVERY_UNAVAILABLE' =>
        CommercialIdentityFailureKind.activationRecoveryUnavailable,
      'DEVICE_ACTIVATION_EXPIRED' =>
        CommercialIdentityFailureKind.activationExpired,
      'DEVICE_NOT_ACTIVE' => CommercialIdentityFailureKind.deviceInactive,
      'REFRESH_TOKEN_INVALID' => CommercialIdentityFailureKind.refreshRejected,
      'REFRESH_TOKEN_EXPIRED' => CommercialIdentityFailureKind.refreshExpired,
      'REFRESH_TOKEN_FAMILY_REVOKED' || 'AUTHENTICATED_CONTEXT_INVALID' =>
        CommercialIdentityFailureKind.sessionRevoked,
      'REFRESH_TOKEN_REUSE_DETECTED' =>
        CommercialIdentityFailureKind.refreshReplayDetected,
      'ACCESS_TOKEN_INVALID' =>
        CommercialIdentityFailureKind.accessExpiredOrInvalid,
      'USER_NOT_ACTIVE' => CommercialIdentityFailureKind.accountDisabled,
      'RATE_LIMIT_EXCEEDED' => CommercialIdentityFailureKind.rateLimited,
      'HTTPS_REQUIRED' => CommercialIdentityFailureKind.tlsRequired,
      'TEMPORARY_COMMAND_FAILURE' || 'WORKSPACE_CONFIGURATION_INVALID' =>
        CommercialIdentityFailureKind.backendTemporarilyUnavailable,
      _ => CommercialIdentityFailureKind.protocolContractMismatch,
    },
    safeCode: code,
    retryable: retryable,
    httpStatus: httpStatus,
    retryAfter: retryAfter,
  );
}
