import 'dart:convert';
import 'dart:io';

import 'package:flutter_test/flutter_test.dart';
import 'package:traderpro_agrisuite_mobile/features/procurement_poc/procurement_poc.dart';

void main() {
  test('operation envelope preserves payload bytes and headers', () async {
    late Map<String, Object?> received;
    late HttpHeaders headers;
    final server = await HttpServer.bind(InternetAddress.loopbackIPv4, 0);
    server.listen((request) async {
      headers = request.headers;
      received =
          jsonDecode(await utf8.decoder.bind(request).join())
              as Map<String, Object?>;
      request.response.headers.contentType = ContentType.json;
      request.response.write(
        jsonEncode(<String, Object?>{
          'result': <String, Object?>{
            'operations': <Object?>[
              <String, Object?>{
                'operationId': '019fad0f-2d6a-7000-8000-000000000002',
                'aggregateId': '019fad0f-2d6a-7000-8000-000000000001',
                'localSequence': 2,
                'resultStatus': 'Accepted',
                'cloudAggregateVersion': 2,
                'cloudReference': 'RS-POC-000001',
                'leaseId': '019fad0f-2d6a-7000-8000-000000000099',
                'leaseExpiresAtUtc': '2026-07-29T10:00:00.000Z',
                'errorCode': null,
                'message': null,
              },
            ],
          },
          'meta': <String, Object?>{
            'correlationId': '019fad0f-2d6a-7000-8000-000000000090',
          },
        }),
      );
      await request.response.close();
    });
    final client = ProcurementPocApiClient(
      backendBaseUrl: 'http://${server.address.host}:${server.port}/',
      workspaceId: '019fad0f-2d6a-7000-8000-000000000010',
      deviceId: '019fad0f-2d6a-7000-8000-000000000011',
    );
    const payload =
        '{"operationId":"019fad0f-2d6a-7000-8000-000000000002",'
        '"cloudSessionId":null,"rawWeightKg":"00050.2370",'
        '"processedWeightKg":"50.230000"}';
    const hash =
        'aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa';

    try {
      final result = await client.sendOperations([
        const MobileSyncOperationEnvelope(
          operationId: '019fad0f-2d6a-7000-8000-000000000002',
          operationType: 'RecordReceivingEntry',
          aggregateId: '019fad0f-2d6a-7000-8000-000000000001',
          localSequence: 2,
          expectedCloudVersion: null,
          payloadJson: payload,
          payloadHash: hash,
          leaseId: '019fad0f-2d6a-7000-8000-000000000099',
        ),
      ]);

      final envelope =
          (received['operations']! as List<Object?>).single
              as Map<String, Object?>;
      expect(envelope['payloadJson'], payload);
      expect(envelope['payloadHash'], hash);
      expect(envelope['leaseId'], '019fad0f-2d6a-7000-8000-000000000099');
      expect(
        (jsonDecode(payload) as Map<String, Object?>),
        isNot(contains('leaseId')),
      );
      expect(headers.value('X-TraderPro-Workspace-ID'), endsWith('0010'));
      expect(headers.value('X-TraderPro-Device-ID'), endsWith('0011'));
      expect(headers.contentType?.mimeType, 'application/json');
      expect(result.operations.single.resultStatus, 'Accepted');
    } finally {
      client.dispose();
      await server.close(force: true);
    }
  });

  test('Start omits lease metadata and raw event payload survives', () async {
    final requests = <HttpRequest>[];
    final server = await HttpServer.bind(InternetAddress.loopbackIPv4, 0);
    server.listen((request) async {
      requests.add(request);
      request.response.headers.contentType = ContentType.json;
      if (request.uri.path.endsWith('/operations')) {
        final body =
            jsonDecode(await utf8.decoder.bind(request).join())
                as Map<String, Object?>;
        final operation =
            (body['operations']! as List<Object?>).single
                as Map<String, Object?>;
        expect(operation, isNot(contains('leaseId')));
        request.response.write(
          '{"result":{"operations":[{"operationId":'
          '"019fad0f-2d6a-7000-8000-000000000021","aggregateId":'
          '"019fad0f-2d6a-7000-8000-000000000020","localSequence":1,'
          '"resultStatus":"Accepted","cloudAggregateVersion":1,'
          '"cloudReference":"RS-POC-000002","leaseId":'
          '"019fad0f-2d6a-7000-8000-000000000022",'
          '"leaseExpiresAtUtc":"2026-07-29T10:00:00.000Z",'
          '"errorCode":null,"message":null}]},"meta":'
          '{"correlationId":"019fad0f-2d6a-7000-8000-000000000023"}}',
        );
      } else {
        request.response.write(
          '{"events":[{"sequence":7,"eventId":'
          '"019fad0f-2d6a-7000-8000-000000000024",'
          '"eventType":"ReceivingSessionPocStarted","eventVersion":1,'
          '"aggregateType":"ReceivingSessionPoc","aggregateId":'
          '"019fad0f-2d6a-7000-8000-000000000020",'
          '"aggregateVersion":1,"occurredAtUtc":'
          '"2026-07-29T09:00:00.000Z","correlationId":"corr",'
          '"payload": { "sessionId" : '
          '"019fad0f-2d6a-7000-8000-000000000020", "version" : 1 }}],'
          '"nextCursor":7,"hasMore":false}',
        );
      }
      await request.response.close();
    });
    final client = ProcurementPocApiClient(
      backendBaseUrl: 'http://${server.address.host}:${server.port}',
      workspaceId: '019fad0f-2d6a-7000-8000-000000000010',
      deviceId: '019fad0f-2d6a-7000-8000-000000000011',
    );
    try {
      await client.sendOperations([
        const MobileSyncOperationEnvelope(
          operationId: '019fad0f-2d6a-7000-8000-000000000021',
          operationType: 'StartReceivingSession',
          aggregateId: '019fad0f-2d6a-7000-8000-000000000020',
          localSequence: 1,
          expectedCloudVersion: null,
          payloadJson: '{"start":true}',
          payloadHash:
              'bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb',
          leaseId: null,
        ),
      ]);
      final page = await client.readEvents(after: 0, limit: 50);
      expect(
        page.events.single.payloadJson,
        '{ "sessionId" : '
        '"019fad0f-2d6a-7000-8000-000000000020", "version" : 1 }',
      );
      expect(requests, hasLength(2));
    } finally {
      client.dispose();
      await server.close(force: true);
    }
  });
}
