import 'dart:async';
import 'dart:convert';
import 'dart:io';
import 'dart:typed_data';

import 'package:uuid/uuid.dart';

import '../../core/identity/commercial_identity_failure.dart';
import '../../core/identity/commercial_identity_ports.dart';
import 'commercial_api_environment.dart';

abstract interface class CommercialCorrelationIdGenerator {
  String generate();
}

final class RandomCommercialCorrelationIdGenerator
    implements CommercialCorrelationIdGenerator {
  RandomCommercialCorrelationIdGenerator({Uuid? uuid})
    : _uuid = uuid ?? const Uuid();

  final Uuid _uuid;

  @override
  String generate() => _uuid.v4();
}

final class CommercialIdentityHttpClient
    implements CommercialIdentityTransport {
  CommercialIdentityHttpClient({
    required this.origin,
    HttpClient? httpClient,
    CommercialCorrelationIdGenerator? correlationIdGenerator,
    this.responseBodyLimitBytes = 1024 * 1024,
    this.responseTimeout = const Duration(seconds: 20),
  }) : _client = httpClient ?? HttpClient(),
       _correlationIds =
           correlationIdGenerator ?? RandomCommercialCorrelationIdGenerator() {
    _client.connectionTimeout = const Duration(seconds: 10);
  }

  final CommercialApiOrigin origin;
  final HttpClient _client;
  final CommercialCorrelationIdGenerator _correlationIds;
  final int responseBodyLimitBytes;
  final Duration responseTimeout;
  bool _disposed = false;

  @override
  Future<CommercialIdentityTransportResponse> send(
    CommercialIdentityTransportRequest request,
  ) async {
    if (_disposed) {
      throw const CommercialIdentityFailure(
        kind: CommercialIdentityFailureKind.networkUnavailable,
        safeCode: 'IDENTITY_TRANSPORT_DISPOSED',
      );
    }
    final correlationId = _correlationIds.generate().toLowerCase();
    if (!_isCanonicalUuid(correlationId)) {
      throw const CommercialIdentityFailure(
        kind: CommercialIdentityFailureKind.unexpectedIdentityFailure,
        safeCode: 'IDENTITY_CORRELATION_GENERATION_FAILED',
      );
    }

    HttpClientRequest? httpRequest;
    try {
      final uri = origin.resolve(request.relativePath);
      if (uri.scheme != origin.uri.scheme || uri.host != origin.uri.host) {
        throw const CommercialIdentityFailure(
          kind: CommercialIdentityFailureKind.protocolContractMismatch,
          safeCode: 'IDENTITY_ENDPOINT_PATH_INVALID',
        );
      }
      httpRequest = await _client.openUrl(request.method, uri);
      httpRequest.followRedirects = false;
      httpRequest.headers.set(HttpHeaders.acceptHeader, 'application/json');
      httpRequest.headers.set('X-Correlation-ID', correlationId);
      final bearer = request.bearerAccessToken;
      if (bearer != null) {
        httpRequest.headers.set(
          HttpHeaders.authorizationHeader,
          'Bearer $bearer',
        );
      }
      final idempotencyKey = request.idempotencyKey;
      if (idempotencyKey != null) {
        httpRequest.headers.set('Idempotency-Key', idempotencyKey);
      }
      final body = request.jsonBody;
      if (body != null) {
        final encoded = utf8.encode(jsonEncode(body));
        httpRequest.headers.contentType = ContentType.json;
        httpRequest.contentLength = encoded.length;
        httpRequest.add(encoded);
      } else {
        httpRequest.contentLength = 0;
      }

      final response = await httpRequest.close().timeout(responseTimeout);
      final bytes = await _readBounded(response);
      final responseCorrelation = response.headers
          .value('X-Correlation-ID')
          ?.toLowerCase();
      if (responseCorrelation == null ||
          !_isCanonicalUuid(responseCorrelation)) {
        throw const CommercialIdentityFailure(
          kind: CommercialIdentityFailureKind.protocolContractMismatch,
          safeCode: 'IDENTITY_CORRELATION_RESPONSE_INVALID',
        );
      }
      final jsonBody = _decodeBody(response, bytes);
      return CommercialIdentityTransportResponse(
        statusCode: response.statusCode,
        correlationId: responseCorrelation,
        jsonBody: jsonBody,
        headers: _safeResponseHeaders(response.headers),
      );
    } on CommercialIdentityFailure {
      rethrow;
    } on HandshakeException {
      throw const CommercialIdentityFailure(
        kind: CommercialIdentityFailureKind.tlsFailure,
        safeCode: 'IDENTITY_TLS_FAILURE',
      );
    } on TimeoutException {
      httpRequest?.abort();
      throw CommercialIdentityFailure(
        kind: request.method == 'GET'
            ? CommercialIdentityFailureKind.networkUnavailable
            : CommercialIdentityFailureKind.networkAmbiguous,
        safeCode: request.method == 'GET'
            ? 'IDENTITY_NETWORK_TIMEOUT'
            : 'IDENTITY_NETWORK_OUTCOME_UNKNOWN',
        retryable: request.method == 'GET',
      );
    } on SocketException {
      throw CommercialIdentityFailure(
        kind: request.method == 'GET'
            ? CommercialIdentityFailureKind.networkUnavailable
            : CommercialIdentityFailureKind.networkAmbiguous,
        safeCode: request.method == 'GET'
            ? 'IDENTITY_NETWORK_UNAVAILABLE'
            : 'IDENTITY_NETWORK_OUTCOME_UNKNOWN',
        retryable: request.method == 'GET',
      );
    } on TlsException {
      throw const CommercialIdentityFailure(
        kind: CommercialIdentityFailureKind.tlsFailure,
        safeCode: 'IDENTITY_TLS_FAILURE',
      );
    } on HttpException {
      throw const CommercialIdentityFailure(
        kind: CommercialIdentityFailureKind.protocolContractMismatch,
        safeCode: 'IDENTITY_HTTP_PROTOCOL_INVALID',
      );
    } on FormatException {
      throw const CommercialIdentityFailure(
        kind: CommercialIdentityFailureKind.protocolContractMismatch,
        safeCode: 'IDENTITY_RESPONSE_ENCODING_INVALID',
      );
    } on Object {
      throw const CommercialIdentityFailure(
        kind: CommercialIdentityFailureKind.unexpectedIdentityFailure,
        safeCode: 'IDENTITY_TRANSPORT_UNEXPECTED',
      );
    }
  }

  Future<Uint8List> _readBounded(HttpClientResponse response) async {
    final builder = BytesBuilder(copy: false);
    await for (final chunk in response.timeout(responseTimeout)) {
      if (builder.length + chunk.length > responseBodyLimitBytes) {
        throw const CommercialIdentityFailure(
          kind: CommercialIdentityFailureKind.protocolContractMismatch,
          safeCode: 'IDENTITY_RESPONSE_TOO_LARGE',
        );
      }
      builder.add(chunk);
    }
    return builder.takeBytes();
  }

  Map<String, Object?>? _decodeBody(
    HttpClientResponse response,
    Uint8List bytes,
  ) {
    if (bytes.isEmpty) {
      if (response.statusCode == HttpStatus.noContent) {
        return null;
      }
      throw const CommercialIdentityFailure(
        kind: CommercialIdentityFailureKind.protocolContractMismatch,
        safeCode: 'IDENTITY_RESPONSE_BODY_MISSING',
      );
    }
    final contentType = response.headers.contentType;
    final mimeType = contentType?.mimeType.toLowerCase();
    if (mimeType != 'application/json' &&
        mimeType != 'application/problem+json') {
      throw const CommercialIdentityFailure(
        kind: CommercialIdentityFailureKind.protocolContractMismatch,
        safeCode: 'IDENTITY_RESPONSE_CONTENT_TYPE_INVALID',
      );
    }
    final decodedText = const Utf8Decoder(allowMalformed: false).convert(bytes);
    final decoded = jsonDecode(decodedText);
    if (decoded is! Map) {
      throw const CommercialIdentityFailure(
        kind: CommercialIdentityFailureKind.protocolContractMismatch,
        safeCode: 'IDENTITY_RESPONSE_JSON_INVALID',
      );
    }
    return decoded.map<String, Object?>(
      (key, value) => MapEntry(key.toString(), value),
    );
  }

  Map<String, String> _safeResponseHeaders(HttpHeaders headers) {
    final safe = <String, String>{};
    for (final name in <String>[
      'cache-control',
      'pragma',
      'retry-after',
      'x-correlation-id',
    ]) {
      final value = headers.value(name);
      if (value != null) {
        safe[name] = value;
      }
    }
    return safe;
  }

  static Duration? parseRetryAfter(String? value, DateTime nowUtc) {
    if (value == null) {
      return null;
    }
    final seconds = int.tryParse(value.trim());
    if (seconds != null && seconds >= 0) {
      return Duration(seconds: seconds);
    }
    try {
      final deadline = HttpDate.parse(value).toUtc();
      final duration = deadline.difference(nowUtc.toUtc());
      return duration.isNegative ? Duration.zero : duration;
    } on FormatException {
      return null;
    }
  }

  static bool _isCanonicalUuid(String value) => RegExp(
    r'^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$',
  ).hasMatch(value);

  @override
  void dispose() {
    if (_disposed) {
      return;
    }
    _disposed = true;
    _client.close(force: true);
  }
}
