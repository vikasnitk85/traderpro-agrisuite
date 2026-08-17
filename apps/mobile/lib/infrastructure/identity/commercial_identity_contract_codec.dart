import '../../core/identity/commercial_identity_failure.dart';
import '../../core/identity/commercial_identity_models.dart';

abstract final class CommercialIdentityContract {
  static const String activationMethod = 'POST';
  static const String activationPath = 'api/v1/auth/device-activations/redeem';
  static const String loginMethod = 'POST';
  static const String loginPath = 'api/v1/auth/login';
  static const String refreshMethod = 'POST';
  static const String refreshPath = 'api/v1/auth/refresh';
  static const String logoutMethod = 'POST';
  static const String logoutPath = 'api/v1/auth/logout';
  static const String logoutAllMethod = 'POST';
  static const String logoutAllPath = 'api/v1/auth/logout-all';
  static const String meMethod = 'GET';
  static const String mePath = 'api/v1/auth/me';
}

final class ActivationRedeemRequest {
  const ActivationRedeemRequest({
    required this.workspaceCode,
    required this.activationCode,
    required this.deviceLabel,
    this.clientInstallationReference,
    this.platform = 'Android',
  });

  final String workspaceCode;
  final String activationCode;
  final String? clientInstallationReference;
  final String deviceLabel;
  final String platform;
}

final class ActivationRedeemResult {
  const ActivationRedeemResult({
    required this.credential,
    required this.activatedAtUtc,
  });

  final DeviceCredential credential;
  final DateTime activatedAtUtc;
}

final class CommercialLoginRequest {
  const CommercialLoginRequest({
    required this.workspaceCode,
    required this.login,
    required this.password,
    required this.deviceCredential,
  });

  final String workspaceCode;
  final String login;
  final String password;
  final DeviceCredential deviceCredential;
}

final class CommercialIdentityContractCodec {
  const CommercialIdentityContractCodec();

  static const int maxAccessTokenCharacters = 16384;
  static const int maxCredentialCharacters = 4096;

  Map<String, Object?> encodeActivation(ActivationRedeemRequest request) =>
      <String, Object?>{
        'workspaceCode': request.workspaceCode,
        'activationCode': request.activationCode,
        'clientInstallationReference': request.clientInstallationReference,
        'deviceLabel': request.deviceLabel,
        'platform': request.platform,
      };

  ActivationRedeemResult decodeActivation(Map<String, Object?> json) {
    try {
      final deviceId = DeviceId(_requiredString(json, 'deviceId', max: 36));
      final secret = _requiredString(
        json,
        'deviceSecret',
        max: maxCredentialCharacters,
      );
      final activatedAt = _requiredUtc(json, 'activatedAtUtc');
      final secretVersion = _requiredPositiveInt(json, 'secretVersion');
      return ActivationRedeemResult(
        credential: DeviceCredential(
          deviceId: deviceId,
          secret: secret,
          secretVersion: secretVersion,
        ),
        activatedAtUtc: activatedAt,
      );
    } on CommercialIdentityFailure {
      rethrow;
    } on Object {
      throw _contractMismatch();
    }
  }

  Map<String, Object?> encodeLogin(CommercialLoginRequest request) =>
      <String, Object?>{
        'workspaceCode': request.workspaceCode,
        'login': request.login,
        'password': request.password,
        'deviceId': request.deviceCredential.deviceId.value,
        'deviceSecret': request.deviceCredential.secret,
      };

  Map<String, Object?> encodeRefresh(RefreshCredential credential) =>
      <String, Object?>{'refreshToken': credential.value};

  CommercialAuthenticationResult decodeAuthentication(
    Map<String, Object?> json,
  ) {
    try {
      final accessToken = _requiredString(
        json,
        'accessToken',
        max: maxAccessTokenCharacters,
      );
      final accessExpiry = _requiredUtc(json, 'accessTokenExpiresAtUtc');
      final refreshToken = _requiredString(
        json,
        'refreshToken',
        max: maxCredentialCharacters,
      );
      final refreshExpiry = _requiredUtc(json, 'refreshTokenExpiresAtUtc');
      final context = _decodeContext(json, tokenFamilyRequired: false);
      return CommercialAuthenticationResult(
        accessToken: AccessTokenLease(
          value: accessToken,
          expiresAtUtc: accessExpiry,
        ),
        refreshCredential: RefreshCredential(
          value: refreshToken,
          expiresAtUtc: refreshExpiry,
        ),
        context: context,
      );
    } on CommercialIdentityFailure {
      rethrow;
    } on Object {
      throw _contractMismatch();
    }
  }

  CommercialIdentityContext decodeMe(Map<String, Object?> json) {
    try {
      return _decodeContext(json, tokenFamilyRequired: true);
    } on CommercialIdentityFailure {
      rethrow;
    } on Object {
      throw _contractMismatch();
    }
  }

  CommercialIdentityFailure decodeError(
    Map<String, Object?> json, {
    required int httpStatus,
    Duration? retryAfter,
  }) {
    try {
      final error = _requiredMap(json, 'error');
      final code = _requiredString(error, 'code', max: 128);
      _requiredString(error, 'message', max: 1024);
      _requiredString(error, 'category', max: 64);
      final retryable = _requiredBool(error, 'retryable');
      final meta = _requiredMap(json, 'meta');
      _canonicalUuidValue(_requiredString(meta, 'correlationId', max: 36));
      return CommercialIdentityFailure.fromBackendCode(
        code,
        httpStatus: httpStatus,
        retryable: retryable,
        retryAfter: retryAfter,
      );
    } on CommercialIdentityFailure {
      rethrow;
    } on Object {
      throw _contractMismatch();
    }
  }

  CommercialIdentityContext _decodeContext(
    Map<String, Object?> json, {
    required bool tokenFamilyRequired,
  }) {
    final user = _requiredMap(json, 'user');
    final workspace = _requiredMap(json, 'workspace');
    final company = _requiredMap(json, 'company');
    final device = _requiredMap(json, 'device');
    return CommercialIdentityContext(
      userId: UserId(_requiredString(user, 'userId', max: 36)),
      userDisplayName: _requiredString(user, 'displayName', max: 256),
      role: CommercialRole.parse(_requiredString(user, 'role', max: 16)),
      workspaceId: WorkspaceId(
        _requiredString(workspace, 'workspaceId', max: 36),
      ),
      workspaceCode: _requiredString(workspace, 'workspaceCode', max: 64),
      companyId: CompanyId(_requiredString(company, 'companyId', max: 36)),
      defaultBranchId: BranchId(
        _requiredString(company, 'defaultBranchId', max: 36),
      ),
      deviceId: DeviceId(_requiredString(device, 'deviceId', max: 36)),
      deviceLabel: _requiredString(device, 'label', max: 256),
      tokenFamilyId: tokenFamilyRequired
          ? TokenFamilyId(_requiredString(json, 'tokenFamilyId', max: 36))
          : null,
    );
  }

  static Map<String, Object?> _requiredMap(
    Map<String, Object?> json,
    String field,
  ) {
    final value = json[field];
    if (value is! Map<String, Object?>) {
      throw _contractMismatch();
    }
    return value;
  }

  static String _requiredString(
    Map<String, Object?> json,
    String field, {
    required int max,
  }) {
    final value = json[field];
    if (value is! String ||
        value.isEmpty ||
        value.trim().isEmpty ||
        value.length > max) {
      throw _contractMismatch();
    }
    return value;
  }

  static int _requiredPositiveInt(Map<String, Object?> json, String field) {
    final value = json[field];
    if (value is! int || value <= 0) {
      throw _contractMismatch();
    }
    return value;
  }

  static bool _requiredBool(Map<String, Object?> json, String field) {
    final value = json[field];
    if (value is! bool) {
      throw _contractMismatch();
    }
    return value;
  }

  static DateTime _requiredUtc(Map<String, Object?> json, String field) {
    final raw = _requiredString(json, field, max: 64);
    if (!RegExp(r'(?:Z|[+-]00:00)$').hasMatch(raw)) {
      throw _contractMismatch();
    }
    final parsed = DateTime.tryParse(raw);
    if (parsed == null || !parsed.isUtc) {
      throw _contractMismatch();
    }
    return parsed;
  }

  static void _canonicalUuidValue(String value) {
    if (!RegExp(
      r'^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$',
      caseSensitive: false,
    ).hasMatch(value)) {
      throw _contractMismatch();
    }
  }

  static CommercialIdentityFailure _contractMismatch() =>
      const CommercialIdentityFailure(
        kind: CommercialIdentityFailureKind.protocolContractMismatch,
        safeCode: 'IDENTITY_PROTOCOL_CONTRACT_MISMATCH',
      );
}
