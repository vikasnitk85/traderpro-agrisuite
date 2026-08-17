import 'package:crypto/crypto.dart';

final RegExp _canonicalUuid = RegExp(
  r'^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$',
  caseSensitive: false,
);

abstract base class CommercialUuidValue {
  const CommercialUuidValue(this.value);

  final String value;

  @override
  bool operator ==(Object other) =>
      other.runtimeType == runtimeType &&
      other is CommercialUuidValue &&
      other.value == value;

  @override
  int get hashCode => Object.hash(runtimeType, value);

  @override
  String toString() => value;
}

String _normalizeUuid(String value, String field) {
  if (!_canonicalUuid.hasMatch(value)) {
    throw FormatException('$field must be a canonical UUID.');
  }
  return value.toLowerCase();
}

final class WorkspaceId extends CommercialUuidValue {
  WorkspaceId(String value) : super(_normalizeUuid(value, 'workspaceId'));
}

final class CompanyId extends CommercialUuidValue {
  CompanyId(String value) : super(_normalizeUuid(value, 'companyId'));
}

final class BranchId extends CommercialUuidValue {
  BranchId(String value) : super(_normalizeUuid(value, 'defaultBranchId'));
}

final class UserId extends CommercialUuidValue {
  UserId(String value) : super(_normalizeUuid(value, 'userId'));
}

final class DeviceId extends CommercialUuidValue {
  DeviceId(String value) : super(_normalizeUuid(value, 'deviceId'));
}

final class TokenFamilyId extends CommercialUuidValue {
  TokenFamilyId(String value) : super(_normalizeUuid(value, 'tokenFamilyId'));
}

enum CommercialRole {
  owner('Owner'),
  operator('Operator');

  const CommercialRole(this.wireValue);

  final String wireValue;

  static CommercialRole parse(String value) => switch (value) {
    'Owner' => CommercialRole.owner,
    'Operator' => CommercialRole.operator,
    _ => throw FormatException('Unknown commercial role.'),
  };
}

final class DeviceCredential {
  const DeviceCredential({
    required this.deviceId,
    required this.secret,
    required this.secretVersion,
  });

  final DeviceId deviceId;
  final String secret;
  final int secretVersion;

  @override
  String toString() =>
      'DeviceCredential(deviceIdPresent: true, secretPresent: true, '
      'secretVersion: $secretVersion)';
}

final class RefreshCredential {
  const RefreshCredential({
    required this.value,
    required this.expiresAtUtc,
    this.bindingFingerprint,
    this.predecessorDigest,
    this.committedAtUtc,
  });

  final String value;
  final DateTime expiresAtUtc;
  final String? bindingFingerprint;
  final String? predecessorDigest;
  final DateTime? committedAtUtc;

  bool get isExpired => !expiresAtUtc.isAfter(DateTime.now().toUtc());

  @override
  String toString() =>
      'RefreshCredential(valuePresent: true, expiresAtPresent: true, '
      'bindingFingerprintPresent: ${bindingFingerprint != null})';
}

final class AccessTokenLease {
  const AccessTokenLease({required this.value, required this.expiresAtUtc});

  final String value;
  final DateTime expiresAtUtc;

  bool isFreshAt(
    DateTime nowUtc, {
    Duration refreshSkew = const Duration(seconds: 60),
  }) => expiresAtUtc.isAfter(nowUtc.toUtc().add(refreshSkew));

  @override
  String toString() =>
      'AccessTokenLease(valuePresent: true, expiresAtPresent: true)';
}

final class CommercialAccountBinding {
  const CommercialAccountBinding({
    required this.apiOrigin,
    required this.workspaceId,
    required this.companyId,
    required this.defaultBranchId,
    required this.userId,
    required this.deviceId,
    required this.firstBoundAtUtc,
  });

  final String apiOrigin;
  final WorkspaceId workspaceId;
  final CompanyId companyId;
  final BranchId defaultBranchId;
  final UserId userId;
  final DeviceId deviceId;
  final DateTime firstBoundAtUtc;

  bool matches(CommercialAccountBinding other) =>
      apiOrigin == other.apiOrigin &&
      workspaceId == other.workspaceId &&
      companyId == other.companyId &&
      defaultBranchId == other.defaultBranchId &&
      userId == other.userId &&
      deviceId == other.deviceId;

  String get fingerprint {
    final canonical = <String>[
      apiOrigin,
      workspaceId.value,
      companyId.value,
      defaultBranchId.value,
      userId.value,
      deviceId.value,
    ].join('\u001f');
    return sha256.convert(canonical.codeUnits).toString();
  }
}

final class CommercialIdentitySnapshot {
  const CommercialIdentitySnapshot({
    required this.workspaceCode,
    required this.userDisplayName,
    required this.role,
    required this.deviceLabel,
    required this.lastConfirmedAtUtc,
  });

  final String workspaceCode;
  final String userDisplayName;
  final CommercialRole role;
  final String deviceLabel;
  final DateTime lastConfirmedAtUtc;
}

final class CommercialIdentityContext {
  const CommercialIdentityContext({
    required this.workspaceId,
    required this.workspaceCode,
    required this.companyId,
    required this.defaultBranchId,
    required this.userId,
    required this.userDisplayName,
    required this.role,
    required this.deviceId,
    required this.deviceLabel,
    this.tokenFamilyId,
  });

  final WorkspaceId workspaceId;
  final String workspaceCode;
  final CompanyId companyId;
  final BranchId defaultBranchId;
  final UserId userId;
  final String userDisplayName;
  final CommercialRole role;
  final DeviceId deviceId;
  final String deviceLabel;
  final TokenFamilyId? tokenFamilyId;

  CommercialAccountBinding toBinding({
    required String apiOrigin,
    required DateTime firstBoundAtUtc,
  }) => CommercialAccountBinding(
    apiOrigin: apiOrigin,
    workspaceId: workspaceId,
    companyId: companyId,
    defaultBranchId: defaultBranchId,
    userId: userId,
    deviceId: deviceId,
    firstBoundAtUtc: firstBoundAtUtc.toUtc(),
  );

  CommercialIdentitySnapshot toSnapshot(DateTime confirmedAtUtc) =>
      CommercialIdentitySnapshot(
        workspaceCode: workspaceCode,
        userDisplayName: userDisplayName,
        role: role,
        deviceLabel: deviceLabel,
        lastConfirmedAtUtc: confirmedAtUtc.toUtc(),
      );

  bool sameImmutableContext(CommercialIdentityContext other) =>
      workspaceId == other.workspaceId &&
      companyId == other.companyId &&
      defaultBranchId == other.defaultBranchId &&
      userId == other.userId &&
      deviceId == other.deviceId;
}

final class CommercialAuthenticationResult {
  const CommercialAuthenticationResult({
    required this.accessToken,
    required this.refreshCredential,
    required this.context,
  });

  final AccessTokenLease accessToken;
  final RefreshCredential refreshCredential;
  final CommercialIdentityContext context;
}

final class CommercialActivationAttempt {
  const CommercialActivationAttempt({
    required this.idempotencyKey,
    required this.workspaceCode,
    required this.activationCode,
    required this.deviceLabel,
    required this.platform,
    required this.createdAtUtc,
    this.clientInstallationReference,
  });

  final String idempotencyKey;
  final String workspaceCode;
  final String activationCode;
  final String deviceLabel;
  final String platform;
  final DateTime createdAtUtc;
  final String? clientInstallationReference;

  @override
  String toString() =>
      'CommercialActivationAttempt(idempotencyKeyPresent: true, '
      'activationCodePresent: true, createdAtPresent: true)';
}
