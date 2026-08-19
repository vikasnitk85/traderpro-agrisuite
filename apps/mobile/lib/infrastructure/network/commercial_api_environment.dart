import 'dart:io';

import 'package:flutter/foundation.dart';

import '../../core/identity/commercial_identity_failure.dart';

final class CommercialApiOrigin {
  const CommercialApiOrigin._(this.uri);

  final Uri uri;

  String get normalized => uri.toString();

  Uri resolve(String relativePath) {
    if (relativePath.startsWith('/')) {
      throw ArgumentError.value(
        relativePath,
        'relativePath',
        'Identity endpoint paths must be relative to the configured base.',
      );
    }
    return uri.resolve(relativePath);
  }

  static CommercialApiOrigin parse(
    String value, {
    required bool releaseMode,
    bool allowDebugHttp = false,
  }) {
    final trimmed = value.trim();
    Uri parsed;
    try {
      parsed = Uri.parse(trimmed);
    } on FormatException {
      throw _configurationFailure();
    }
    if (trimmed.isEmpty ||
        !parsed.isAbsolute ||
        !parsed.hasAuthority ||
        parsed.host.isEmpty ||
        parsed.userInfo.isNotEmpty ||
        parsed.hasQuery ||
        parsed.hasFragment ||
        ((releaseMode || !allowDebugHttp) &&
            _isLocalOrPrivateHost(parsed.host))) {
      throw _configurationFailure();
    }

    final scheme = parsed.scheme.toLowerCase();
    final https = scheme == 'https';
    final explicitlyAllowedDebugHttp =
        !releaseMode && allowDebugHttp && scheme == 'http';
    if (!https && !explicitlyAllowedDebugHttp) {
      throw const CommercialIdentityFailure(
        kind: CommercialIdentityFailureKind.tlsRequired,
        safeCode: 'COMMERCIAL_API_HTTPS_REQUIRED',
      );
    }

    var path = parsed.path;
    if (path.isEmpty) {
      path = '/';
    } else if (!path.endsWith('/')) {
      path = '$path/';
    }
    final normalized = parsed.replace(
      scheme: scheme,
      host: parsed.host.toLowerCase(),
      path: path,
      query: null,
      fragment: null,
    );
    return CommercialApiOrigin._(normalized);
  }

  static CommercialIdentityFailure _configurationFailure() =>
      const CommercialIdentityFailure(
        kind: CommercialIdentityFailureKind.protocolContractMismatch,
        safeCode: 'COMMERCIAL_API_ORIGIN_INVALID',
      );

  static bool _isLocalOrPrivateHost(String host) {
    final normalized = host.toLowerCase();
    if (normalized == 'localhost' ||
        normalized.endsWith('.localhost') ||
        normalized.endsWith('.local')) {
      return true;
    }
    final address = InternetAddress.tryParse(normalized);
    if (address == null) {
      return false;
    }
    if (address.isLoopback || address.isLinkLocal) {
      return true;
    }
    if (address.type == InternetAddressType.IPv6) {
      final mappedIpv4 = _mappedIpv4Octets(address.rawAddress);
      if (mappedIpv4 != null) {
        return _isLocalOrPrivateIpv4(mappedIpv4);
      }
      final compact = normalized.replaceAll(':', '');
      return normalized == '::' ||
          normalized.startsWith('fc') ||
          normalized.startsWith('fd') ||
          normalized.startsWith('fe8') ||
          normalized.startsWith('fe9') ||
          normalized.startsWith('fea') ||
          normalized.startsWith('feb') ||
          compact == '0';
    }
    return _isLocalOrPrivateIpv4(address.rawAddress);
  }

  static List<int>? _mappedIpv4Octets(List<int> octets) {
    if (octets.length != 16 ||
        octets.take(10).any((octet) => octet != 0) ||
        octets[10] != 0xff ||
        octets[11] != 0xff) {
      return null;
    }
    return octets.sublist(12);
  }

  static bool _isLocalOrPrivateIpv4(List<int> octets) {
    if (octets.length != 4) {
      return false;
    }
    return octets[0] == 0 ||
        octets[0] == 10 ||
        octets[0] == 127 ||
        (octets[0] == 100 && octets[1] >= 64 && octets[1] <= 127) ||
        (octets[0] == 169 && octets[1] == 254) ||
        (octets[0] == 172 && octets[1] >= 16 && octets[1] <= 31) ||
        (octets[0] == 192 && octets[1] == 168);
  }

  @override
  bool operator ==(Object other) =>
      other is CommercialApiOrigin && other.normalized == normalized;

  @override
  int get hashCode => normalized.hashCode;

  @override
  String toString() => normalized;
}

final class CommercialApiEnvironment {
  const CommercialApiEnvironment(this.origin);

  final CommercialApiOrigin origin;

  factory CommercialApiEnvironment.fromBuildDefine({
    bool releaseMode = kReleaseMode,
    bool allowDebugHttp = false,
  }) {
    const value = String.fromEnvironment('TRADERPRO_API_BASE_URL');
    return CommercialApiEnvironment(
      CommercialApiOrigin.parse(
        value,
        releaseMode: releaseMode,
        allowDebugHttp: allowDebugHttp,
      ),
    );
  }
}
