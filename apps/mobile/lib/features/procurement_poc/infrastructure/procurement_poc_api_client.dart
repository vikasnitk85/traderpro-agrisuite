import 'dart:async';
import 'dart:convert';
import 'dart:io';

import 'package:uuid/uuid.dart';

import '../application/procurement_poc_error_policy.dart';
import '../application/procurement_poc_ports.dart';
import '../domain/procurement_poc_models.dart';

final class ProcurementPocApiClient implements ProcurementPocApi {
  ProcurementPocApiClient({
    required String backendBaseUrl,
    String? workspaceId,
    String? deviceId,
    HttpClient? httpClient,
    this.connectTimeout = const Duration(seconds: 10),
    this.readTimeout = const Duration(seconds: 20),
    Uuid? uuid,
  }) : _baseUrl = normalizeBackendBaseUrl(backendBaseUrl),
       _workspaceId = workspaceId == null
           ? null
           : canonicalUuid(workspaceId, 'workspaceId'),
       _deviceId = deviceId == null
           ? null
           : canonicalUuid(deviceId, 'deviceId'),
       _httpClient = httpClient ?? HttpClient(),
       _ownsClient = httpClient == null,
       _uuid = uuid ?? const Uuid() {
    _httpClient.connectionTimeout = connectTimeout;
    _httpClient.idleTimeout = readTimeout;
  }

  factory ProcurementPocApiClient.forProfile(PocDeviceProfile profile) {
    return ProcurementPocApiClient(
      backendBaseUrl: profile.backendBaseUrl,
      workspaceId: profile.workspaceId,
      deviceId: profile.deviceId,
    );
  }

  final String _baseUrl;
  final String? _workspaceId;
  final String? _deviceId;
  final HttpClient _httpClient;
  final bool _ownsClient;
  final Uuid _uuid;
  final Duration connectTimeout;
  final Duration readTimeout;
  var _disposed = false;

  @override
  Future<MobileSyncBatchResult> sendOperations(
    List<MobileSyncOperationEnvelope> operations,
  ) async {
    if (operations.isEmpty || operations.length > 50) {
      throw const ProcurementPocApiException(
        code: 'SYNC_OPERATION_BATCH_INVALID',
        message: 'A sync batch must contain between 1 and 50 operations.',
        retryable: false,
        responseAmbiguous: false,
      );
    }
    final response = await _send(
      'POST',
      '/api/v1/mobile/sync/operations',
      bodyJson: jsonEncode(<String, Object?>{
        'operations': operations
            .map((operation) => operation.toJson())
            .toList(growable: false),
      }),
    );
    try {
      final root = _map(jsonDecode(response.body), 'response');
      final result = _map(root['result'], 'result');
      final items = _list(result['operations'], 'result.operations');
      return MobileSyncBatchResult(
        operations: items
            .map((item) => _mobileSyncResult(_map(item, 'result.operations[]')))
            .toList(growable: false),
        rawResponseJson: response.body,
      );
    } on ProcurementPocApiException {
      rethrow;
    } on Object {
      throw _invalidMutationResponse(response.correlationId);
    }
  }

  @override
  Future<MobileSyncEventPage> readEvents({
    required int after,
    required int limit,
  }) async {
    final response = await _send(
      'GET',
      '/api/v1/mobile/sync/events?after=$after&limit=$limit',
    );
    try {
      final root = _map(jsonDecode(response.body), 'response');
      final itemValues = _list(root['events'], 'events');
      final rawPayloads = _RawJsonPayloadReader.eventPayloads(response.body);
      if (rawPayloads.length != itemValues.length) {
        throw const FormatException('Event payload count mismatch.');
      }
      final events = <MobileSyncEvent>[];
      for (var index = 0; index < itemValues.length; index++) {
        final item = _map(itemValues[index], 'events[$index]');
        events.add(
          MobileSyncEvent(
            sequence: _positiveInt(item['sequence'], 'sequence'),
            eventId: canonicalUuid(
              _string(item['eventId'], 'eventId'),
              'eventId',
            ),
            eventType: _string(item['eventType'], 'eventType'),
            eventVersion: _positiveInt(item['eventVersion'], 'eventVersion'),
            aggregateType: _string(item['aggregateType'], 'aggregateType'),
            aggregateId: canonicalUuid(
              _string(item['aggregateId'], 'aggregateId'),
              'aggregateId',
            ),
            aggregateVersion: _positiveInt(
              item['aggregateVersion'],
              'aggregateVersion',
            ),
            occurredAtUtc: parseRequiredUtc(
              item['occurredAtUtc'],
              'occurredAtUtc',
            ),
            correlationId: _string(item['correlationId'], 'correlationId'),
            payloadJson: rawPayloads[index],
          ),
        );
      }
      return MobileSyncEventPage(
        events: events,
        nextCursor: _nonNegativeInt(root['nextCursor'], 'nextCursor'),
        hasMore: _bool(root['hasMore'], 'hasMore'),
      );
    } on Object {
      throw ProcurementPocApiException(
        code: 'POC_RESPONSE_INVALID',
        message: 'The event service returned an invalid response.',
        retryable: true,
        responseAmbiguous: false,
        statusCode: response.statusCode,
        correlationId: response.correlationId,
      );
    }
  }

  @override
  Future<ReceivingSessionListPage> listReceivingSessions({
    String? status,
    int? after,
    int limit = 50,
  }) async {
    final query = <String>[
      if (status != null) 'status=${Uri.encodeQueryComponent(status)}',
      if (after != null) 'after=$after',
      'limit=$limit',
    ].join('&');
    final response = await _send(
      'GET',
      '/api/v1/spikes/procurement-poc/receiving-sessions/?$query',
    );
    try {
      final root = _map(jsonDecode(response.body), 'response');
      final result = _map(root['result'], 'result');
      final sessions = _list(result['sessions'], 'result.sessions')
          .map((item) => _listProjection(_map(item, 'result.sessions[]')))
          .toList(growable: false);
      return ReceivingSessionListPage(
        sessions: sessions,
        nextCursor: _nullableInt(result['nextCursor'], 'nextCursor'),
        hasMore: _bool(result['hasMore'], 'hasMore'),
      );
    } on Object {
      throw _invalidReadResponse(response.correlationId);
    }
  }

  @override
  Future<ReceivingSessionLiveView> getReceivingSessionLiveView(
    String sessionId,
  ) async {
    final canonicalSession = canonicalUuid(sessionId, 'sessionId');
    final response = await _send(
      'GET',
      '/api/v1/spikes/procurement-poc/receiving-sessions/'
          '$canonicalSession/live-view',
    );
    try {
      final root = _map(jsonDecode(response.body), 'response');
      final result = _map(root['result'], 'result');
      final entries = _list(result['recentEntries'], 'recentEntries')
          .map((item) => _entrySummary(_map(item, 'recentEntries[]')))
          .toList(growable: false);
      return RemoteReceivingSessionProjection(
        sessionId: canonicalUuid(
          _string(result['sessionId'], 'sessionId'),
          'sessionId',
        ),
        cloudReference: _string(result['cloudReference'], 'cloudReference'),
        status: _string(result['status'], 'status'),
        editorDeviceId: canonicalUuid(
          _string(result['editorDeviceId'], 'editorDeviceId'),
          'editorDeviceId',
        ),
        leaseExpiresAtUtc: parseNullableUtc(
          result['leaseExpiresAtUtc'],
          'leaseExpiresAtUtc',
        ),
        entryCount: _nonNegativeInt(result['entryCount'], 'entryCount'),
        processedTotalWeightKg: _decimalString(
          result['processedTotalWeightKg'],
          'processedTotalWeightKg',
        ),
        cloudVersion: _positiveInt(result['version'], 'version'),
        approvedByDeviceId: null,
        finalizationId: null,
        lastCloudUpdateAtUtc: parseRequiredUtc(
          result['lastCloudUpdateAtUtc'],
          'lastCloudUpdateAtUtc',
        ),
        recentEntries: entries,
      );
    } on Object {
      throw _invalidReadResponse(response.correlationId);
    }
  }

  @override
  Future<ProcurementPocCommandResult> heartbeat({
    required String sessionId,
    required String leaseId,
    required String idempotencyKey,
  }) {
    return _command(
      path:
          '/api/v1/spikes/procurement-poc/receiving-sessions/'
          '${canonicalUuid(sessionId, 'sessionId')}/heartbeat',
      idempotencyKey: idempotencyKey,
      bodyJson: jsonEncode(<String, Object?>{
        'leaseId': canonicalUuid(leaseId, 'leaseId'),
      }),
    );
  }

  @override
  Future<ProcurementPocCommandResult> approve({
    required String sessionId,
    required int expectedVersion,
    required String idempotencyKey,
  }) {
    return _command(
      path:
          '/api/v1/spikes/procurement-poc/receiving-sessions/'
          '${canonicalUuid(sessionId, 'sessionId')}/approve',
      idempotencyKey: idempotencyKey,
      expectedVersion: expectedVersion,
    );
  }

  @override
  Future<ProcurementPocCommandResult> finalize({
    required String sessionId,
    required int expectedVersion,
    required String idempotencyKey,
  }) {
    return _command(
      path:
          '/api/v1/spikes/procurement-poc/receiving-sessions/'
          '${canonicalUuid(sessionId, 'sessionId')}/finalize',
      idempotencyKey: idempotencyKey,
      expectedVersion: expectedVersion,
    );
  }

  @override
  Future<ProcurementPocBootstrapResult> bootstrap() async {
    final response = await _send(
      'POST',
      '/api/v1/spikes/procurement-poc/bootstrap',
      requiresIdentity: false,
    );
    try {
      final root = _map(jsonDecode(response.body), 'response');
      final result = _map(root['result'], 'result');
      return ProcurementPocBootstrapResult(
        workspaceId: canonicalUuid(
          _string(result['workspaceId'], 'workspaceId'),
          'workspaceId',
        ),
        companyId: canonicalUuid(
          _string(result['companyId'], 'companyId'),
          'companyId',
        ),
        branchId: canonicalUuid(
          _string(result['branchId'], 'branchId'),
          'branchId',
        ),
        operatorDeviceId: canonicalUuid(
          _string(result['operatorDeviceId'], 'operatorDeviceId'),
          'operatorDeviceId',
        ),
        ownerDeviceId: canonicalUuid(
          _string(result['ownerDeviceId'], 'ownerDeviceId'),
          'ownerDeviceId',
        ),
        setupCode: _string(result['setupCode'], 'setupCode'),
        rawResponseJson: response.body,
      );
    } on Object {
      throw _invalidMutationResponse(response.correlationId);
    }
  }

  Future<ProcurementPocCommandResult> _command({
    required String path,
    required String idempotencyKey,
    int? expectedVersion,
    String? bodyJson,
  }) async {
    final response = await _send(
      'POST',
      path,
      bodyJson: bodyJson,
      extraHeaders: <String, String>{
        'Idempotency-Key': canonicalUuid(idempotencyKey, 'idempotencyKey'),
        if (expectedVersion != null)
          'X-Expected-Version': expectedVersion.toString(),
      },
    );
    try {
      final root = _map(jsonDecode(response.body), 'response');
      final result = _map(root['result'], 'result');
      final meta = _map(root['meta'], 'meta');
      return ProcurementPocCommandResult(
        sessionId: canonicalUuid(
          _string(result['sessionId'], 'sessionId'),
          'sessionId',
        ),
        cloudReference: _string(result['cloudReference'], 'cloudReference'),
        status: _string(result['status'], 'status'),
        editorDeviceId: canonicalUuid(
          _string(result['editorDeviceId'], 'editorDeviceId'),
          'editorDeviceId',
        ),
        leaseId: _nullableUuid(result['leaseId'], 'leaseId'),
        leaseExpiresAtUtc: parseNullableUtc(
          result['leaseExpiresAtUtc'],
          'leaseExpiresAtUtc',
        ),
        entryCount: _nonNegativeInt(result['entryCount'], 'entryCount'),
        processedTotalWeightKg: _decimalString(
          result['processedTotalWeightKg'],
          'processedTotalWeightKg',
        ),
        version: _positiveInt(result['version'], 'version'),
        finalizationId: _nullableUuid(
          result['finalizationId'],
          'finalizationId',
        ),
        idempotencyStatus: _string(
          meta['idempotencyStatus'],
          'idempotencyStatus',
        ),
        rawResponseJson: response.body,
      );
    } on Object {
      throw _invalidMutationResponse(response.correlationId);
    }
  }

  Future<_PocHttpResponse> _send(
    String method,
    String path, {
    String? bodyJson,
    Map<String, String> extraHeaders = const {},
    bool requiresIdentity = true,
  }) async {
    if (_disposed) {
      throw const ProcurementPocApiException(
        code: 'POC_CLIENT_DISPOSED',
        message: 'The development API client is no longer available.',
        retryable: true,
        responseAmbiguous: false,
      );
    }
    if (requiresIdentity && (_workspaceId == null || _deviceId == null)) {
      throw const ProcurementPocApiException(
        code: 'POC_PROFILE_REQUIRED',
        message: 'A development workspace and device profile is required.',
        retryable: false,
        responseAmbiguous: false,
      );
    }

    final correlationId = _uuid.v7();
    try {
      final request = await _httpClient
          .openUrl(method, Uri.parse('$_baseUrl$path'))
          .timeout(connectTimeout);
      request.headers.set(HttpHeaders.acceptHeader, 'application/json');
      request.headers.set('X-Correlation-ID', correlationId);
      if (requiresIdentity) {
        request.headers.set('X-TraderPro-Workspace-ID', _workspaceId!);
        request.headers.set('X-TraderPro-Device-ID', _deviceId!);
      }
      for (final entry in extraHeaders.entries) {
        request.headers.set(entry.key, entry.value);
      }
      if (bodyJson != null) {
        final bytes = utf8.encode(bodyJson);
        request.headers.contentType = ContentType.json;
        request.contentLength = bytes.length;
        request.add(bytes);
      } else if (method == 'POST') {
        request.contentLength = 0;
      }

      final response = await request.close().timeout(readTimeout);
      final bytes = await response
          .fold<List<int>>(<int>[], (buffer, value) => buffer..addAll(value))
          .timeout(readTimeout);
      final body = utf8.decode(bytes, allowMalformed: false);
      final responseCorrelation =
          response.headers.value('X-Correlation-ID') ?? correlationId;
      if (response.statusCode < 200 || response.statusCode >= 300) {
        throw _parseError(response.statusCode, body, responseCorrelation);
      }
      return _PocHttpResponse(
        statusCode: response.statusCode,
        body: body,
        correlationId: responseCorrelation,
      );
    } on ProcurementPocApiException {
      rethrow;
    } on TimeoutException {
      throw ProcurementPocApiException(
        code: 'POC_NETWORK_AMBIGUOUS',
        message:
            'The server response was not received. The same operation will '
            'be retried safely.',
        retryable: true,
        responseAmbiguous: method != 'GET',
        correlationId: correlationId,
      );
    } on SocketException {
      throw ProcurementPocApiException(
        code: 'POC_NETWORK_AMBIGUOUS',
        message:
            'The development backend could not be reached. Pending work was '
            'kept on this device.',
        retryable: true,
        responseAmbiguous: method != 'GET',
        correlationId: correlationId,
      );
    } on HttpException {
      throw ProcurementPocApiException(
        code: 'POC_NETWORK_AMBIGUOUS',
        message:
            'The development HTTP connection failed. Pending work was kept.',
        retryable: true,
        responseAmbiguous: method != 'GET',
        correlationId: correlationId,
      );
    } on FormatException {
      throw ProcurementPocApiException(
        code: 'POC_RESPONSE_INVALID',
        message: 'The development backend returned an invalid response.',
        retryable: true,
        responseAmbiguous: method != 'GET',
        correlationId: correlationId,
      );
    }
  }

  ProcurementPocApiException _parseError(
    int statusCode,
    String body,
    String correlationId,
  ) {
    try {
      final root = _map(jsonDecode(body), 'response');
      final error = _map(root['error'], 'error');
      final metaValue = root['meta'];
      final meta = metaValue is Map<String, Object?>
          ? metaValue
          : metaValue is Map
          ? Map<String, Object?>.from(metaValue)
          : const <String, Object?>{};
      final code = _string(error['code'], 'error.code');
      final serverMessage = _string(error['message'], 'error.message');
      return ProcurementPocApiException(
        code: code,
        message: ProcurementPocErrorPolicy.safeMessage(code, serverMessage),
        retryable: error['retryable'] is bool
            ? error['retryable']! as bool
            : statusCode >= 500,
        responseAmbiguous: false,
        statusCode: statusCode,
        correlationId: meta['correlationId'] is String
            ? meta['correlationId']! as String
            : correlationId,
      );
    } on Object {
      return ProcurementPocApiException(
        code: 'POC_HTTP_ERROR',
        message:
            'The development backend rejected the request without a valid '
            'TraderPro error envelope.',
        retryable: statusCode >= 500,
        responseAmbiguous: false,
        statusCode: statusCode,
        correlationId: correlationId,
      );
    }
  }

  @override
  void dispose() {
    if (_disposed) {
      return;
    }
    _disposed = true;
    if (_ownsClient) {
      _httpClient.close(force: true);
    }
  }

  static MobileSyncOperationResult _mobileSyncResult(
    Map<String, Object?> value,
  ) {
    return MobileSyncOperationResult(
      operationId: canonicalUuid(
        _string(value['operationId'], 'operationId'),
        'operationId',
      ),
      aggregateId: canonicalUuid(
        _string(value['aggregateId'], 'aggregateId'),
        'aggregateId',
      ),
      localSequence: _positiveInt(value['localSequence'], 'localSequence'),
      resultStatus: _string(value['resultStatus'], 'resultStatus'),
      cloudAggregateVersion: _nullableInt(
        value['cloudAggregateVersion'],
        'cloudAggregateVersion',
      ),
      cloudReference: _nullableString(
        value['cloudReference'],
        'cloudReference',
      ),
      leaseId: _nullableUuid(value['leaseId'], 'leaseId'),
      leaseExpiresAtUtc: parseNullableUtc(
        value['leaseExpiresAtUtc'],
        'leaseExpiresAtUtc',
      ),
      errorCode: _nullableString(value['errorCode'], 'errorCode'),
      message: _nullableString(value['message'], 'message'),
    );
  }

  static RemoteReceivingSessionProjection _listProjection(
    Map<String, Object?> value,
  ) {
    return RemoteReceivingSessionProjection(
      sessionId: canonicalUuid(
        _string(value['sessionId'], 'sessionId'),
        'sessionId',
      ),
      cloudReference: _string(value['cloudReference'], 'cloudReference'),
      status: _string(value['status'], 'status'),
      editorDeviceId: canonicalUuid(
        _string(value['editorDeviceId'], 'editorDeviceId'),
        'editorDeviceId',
      ),
      leaseExpiresAtUtc: parseNullableUtc(
        value['leaseExpiresAtUtc'],
        'leaseExpiresAtUtc',
      ),
      entryCount: _nonNegativeInt(value['entryCount'], 'entryCount'),
      processedTotalWeightKg: _decimalString(
        value['processedTotalWeightKg'],
        'processedTotalWeightKg',
      ),
      cloudVersion: _positiveInt(value['version'], 'version'),
      approvedByDeviceId: null,
      finalizationId: null,
      lastCloudUpdateAtUtc: parseRequiredUtc(
        value['updatedAtUtc'],
        'updatedAtUtc',
      ),
    );
  }

  static RemoteReceivingEntrySummary _entrySummary(Map<String, Object?> value) {
    return RemoteReceivingEntrySummary(
      entryId: canonicalUuid(_string(value['entryId'], 'entryId'), 'entryId'),
      localSequence: _positiveInt(value['localSequence'], 'localSequence'),
      productReference: _string(value['productReference'], 'productReference'),
      bagTypeReference: _string(value['bagTypeReference'], 'bagTypeReference'),
      bagCount: _positiveInt(value['bagCount'], 'bagCount'),
      rawWeightKg: _decimalString(value['rawWeightKg'], 'rawWeightKg'),
      processedWeightKg: _decimalString(
        value['processedWeightKg'],
        'processedWeightKg',
      ),
      displayWeightKg: _decimalString(
        value['displayWeightKg'],
        'displayWeightKg',
      ),
      decimalPlaces: _positiveInt(value['decimalPlaces'], 'decimalPlaces'),
      processingMethod: _string(value['processingMethod'], 'processingMethod'),
      weightSource: _string(value['weightSource'], 'weightSource'),
      capturedAtDeviceUtc: parseRequiredUtc(
        value['capturedAtDeviceUtc'],
        'capturedAtDeviceUtc',
      ),
      acceptedAtServerUtc: parseRequiredUtc(
        value['acceptedAtServerUtc'],
        'acceptedAtServerUtc',
      ),
    );
  }

  static ProcurementPocApiException _invalidMutationResponse(
    String? correlationId,
  ) {
    return ProcurementPocApiException(
      code: 'POC_RESPONSE_INVALID',
      message:
          'The server may have committed the request, but its response was '
          'invalid. The same idempotency key will be reused.',
      retryable: true,
      responseAmbiguous: true,
      correlationId: correlationId,
    );
  }

  static ProcurementPocApiException _invalidReadResponse(
    String? correlationId,
  ) {
    return ProcurementPocApiException(
      code: 'POC_RESPONSE_INVALID',
      message: 'The development backend returned an invalid read response.',
      retryable: true,
      responseAmbiguous: false,
      correlationId: correlationId,
    );
  }
}

final class _PocHttpResponse {
  const _PocHttpResponse({
    required this.statusCode,
    required this.body,
    required this.correlationId,
  });

  final int statusCode;
  final String body;
  final String correlationId;
}

Map<String, Object?> _map(Object? value, String fieldName) {
  if (value is Map<String, Object?>) {
    return value;
  }
  if (value is Map) {
    return Map<String, Object?>.from(value);
  }
  throw FormatException('$fieldName must be a JSON object.');
}

List<Object?> _list(Object? value, String fieldName) {
  if (value is List<Object?>) {
    return value;
  }
  if (value is List) {
    return List<Object?>.from(value);
  }
  throw FormatException('$fieldName must be a JSON array.');
}

String _string(Object? value, String fieldName) {
  if (value is! String || value.isEmpty) {
    throw FormatException('$fieldName must be a non-empty string.');
  }
  return value;
}

String? _nullableString(Object? value, String fieldName) {
  return value == null ? null : _string(value, fieldName);
}

String? _nullableUuid(Object? value, String fieldName) {
  return value == null
      ? null
      : canonicalUuid(_string(value, fieldName), fieldName);
}

int _positiveInt(Object? value, String fieldName) {
  if (value is! int || value <= 0) {
    throw FormatException('$fieldName must be a positive integer.');
  }
  return value;
}

int _nonNegativeInt(Object? value, String fieldName) {
  if (value is! int || value < 0) {
    throw FormatException('$fieldName must be a non-negative integer.');
  }
  return value;
}

int? _nullableInt(Object? value, String fieldName) {
  if (value == null) {
    return null;
  }
  if (value is! int) {
    throw FormatException('$fieldName must be an integer.');
  }
  return value;
}

bool _bool(Object? value, String fieldName) {
  if (value is! bool) {
    throw FormatException('$fieldName must be true or false.');
  }
  return value;
}

String _decimalString(Object? value, String fieldName) {
  if (value is! String || !RegExp(r'^\d+(?:\.\d+)?$').hasMatch(value)) {
    throw FormatException('$fieldName must be a decimal string.');
  }
  return value;
}

abstract final class _RawJsonPayloadReader {
  static List<String> eventPayloads(String source) {
    final rootStart = _skipWhitespace(source, 0);
    if (rootStart >= source.length || source.codeUnitAt(rootStart) != 0x7b) {
      throw const FormatException('Expected a JSON object.');
    }
    final eventsSpan = _objectProperty(source, rootStart, 'events');
    var cursor = _skipWhitespace(source, eventsSpan.$1);
    if (source.codeUnitAt(cursor) != 0x5b) {
      throw const FormatException('events must be an array.');
    }
    cursor++;
    final values = <String>[];
    while (true) {
      cursor = _skipWhitespace(source, cursor);
      if (cursor >= source.length) {
        throw const FormatException('Unterminated events array.');
      }
      if (source.codeUnitAt(cursor) == 0x5d) {
        return values;
      }
      final eventEnd = _valueEnd(source, cursor);
      final payloadSpan = _objectProperty(
        source,
        cursor,
        'payload',
        objectEnd: eventEnd,
      );
      values.add(source.substring(payloadSpan.$1, payloadSpan.$2));
      cursor = _skipWhitespace(source, eventEnd);
      if (source.codeUnitAt(cursor) == 0x2c) {
        cursor++;
        continue;
      }
      if (source.codeUnitAt(cursor) == 0x5d) {
        return values;
      }
      throw const FormatException('Invalid events array separator.');
    }
  }

  static (int, int) _objectProperty(
    String source,
    int objectStart,
    String name, {
    int? objectEnd,
  }) {
    var cursor = _skipWhitespace(source, objectStart);
    if (cursor >= source.length || source.codeUnitAt(cursor) != 0x7b) {
      throw const FormatException('Expected a JSON object.');
    }
    final end = objectEnd ?? _valueEnd(source, cursor);
    cursor++;
    while (cursor < end) {
      cursor = _skipWhitespace(source, cursor);
      if (source.codeUnitAt(cursor) == 0x7d) {
        break;
      }
      if (source.codeUnitAt(cursor) != 0x22) {
        throw const FormatException('Expected a JSON property name.');
      }
      final keyEnd = _stringEnd(source, cursor);
      final key = jsonDecode(source.substring(cursor, keyEnd)) as String;
      cursor = _skipWhitespace(source, keyEnd);
      if (source.codeUnitAt(cursor) != 0x3a) {
        throw const FormatException('Expected a JSON property separator.');
      }
      cursor = _skipWhitespace(source, cursor + 1);
      final valueEnd = _valueEnd(source, cursor);
      if (key == name) {
        return (cursor, valueEnd);
      }
      cursor = _skipWhitespace(source, valueEnd);
      if (source.codeUnitAt(cursor) == 0x2c) {
        cursor++;
      }
    }
    throw FormatException('Missing JSON property $name.');
  }

  static int _valueEnd(String source, int start) {
    final cursor = _skipWhitespace(source, start);
    final first = source.codeUnitAt(cursor);
    if (first == 0x22) {
      return _stringEnd(source, cursor);
    }
    if (first == 0x7b || first == 0x5b) {
      final stack = <int>[first];
      var index = cursor + 1;
      while (index < source.length && stack.isNotEmpty) {
        final code = source.codeUnitAt(index);
        if (code == 0x22) {
          index = _stringEnd(source, index);
          continue;
        }
        if (code == 0x7b || code == 0x5b) {
          stack.add(code);
        } else if (code == 0x7d) {
          if (stack.removeLast() != 0x7b) {
            throw const FormatException('Mismatched JSON object.');
          }
        } else if (code == 0x5d) {
          if (stack.removeLast() != 0x5b) {
            throw const FormatException('Mismatched JSON array.');
          }
        }
        index++;
      }
      if (stack.isNotEmpty) {
        throw const FormatException('Unterminated JSON value.');
      }
      return index;
    }
    var index = cursor;
    while (index < source.length) {
      final code = source.codeUnitAt(index);
      if (code == 0x2c || code == 0x7d || code == 0x5d || code <= 0x20) {
        break;
      }
      index++;
    }
    if (index == cursor) {
      throw const FormatException('Invalid JSON value.');
    }
    return index;
  }

  static int _stringEnd(String source, int start) {
    var escaped = false;
    for (var index = start + 1; index < source.length; index++) {
      final code = source.codeUnitAt(index);
      if (escaped) {
        escaped = false;
      } else if (code == 0x5c) {
        escaped = true;
      } else if (code == 0x22) {
        return index + 1;
      }
    }
    throw const FormatException('Unterminated JSON string.');
  }

  static int _skipWhitespace(String source, int start) {
    var index = start;
    while (index < source.length && source.codeUnitAt(index) <= 0x20) {
      index++;
    }
    return index;
  }
}
