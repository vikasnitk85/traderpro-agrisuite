import 'dart:convert';
import 'dart:io';

import 'package:flutter_test/flutter_test.dart';
import 'package:traderpro_agrisuite_mobile/core/identity/commercial_identity_failure.dart';
import 'package:traderpro_agrisuite_mobile/core/identity/commercial_identity_ports.dart';
import 'package:traderpro_agrisuite_mobile/infrastructure/network/commercial_api_environment.dart';
import 'package:traderpro_agrisuite_mobile/infrastructure/network/commercial_identity_http_client.dart';

void main() {
  const correlationId = '019fc400-0000-7000-8000-000000000299';

  test(
    'sends bounded JSON with canonical correlation and bearer headers',
    () async {
      final server = await HttpServer.bind(InternetAddress.loopbackIPv4, 0);
      late Map<String, Object?> received;
      late String? authorization;
      final responseFuture = server.first.then((request) async {
        authorization = request.headers.value(HttpHeaders.authorizationHeader);
        expect(request.headers.value('X-Correlation-ID'), correlationId);
        received = Map<String, Object?>.from(
          jsonDecode(await utf8.decoder.bind(request).join()) as Map,
        );
        request.response.statusCode = 200;
        request.response.headers.contentType = ContentType.json;
        request.response.headers.set('X-Correlation-ID', correlationId);
        request.response.write('{"ok":true}');
        await request.response.close();
      });
      final client = CommercialIdentityHttpClient(
        origin: CommercialApiOrigin.parse(
          'http://127.0.0.1:${server.port}',
          releaseMode: false,
          allowDebugHttp: true,
        ),
        correlationIdGenerator: const _FixedCorrelation(correlationId),
      );

      final response = await client.send(
        const CommercialIdentityTransportRequest(
          method: 'POST',
          relativePath: 'api/v1/auth/refresh',
          jsonBody: <String, Object?>{'refreshToken': '<synthetic>'},
          bearerAccessToken: '<synthetic-access>',
        ),
      );
      await responseFuture;
      expect(received, <String, Object?>{'refreshToken': '<synthetic>'});
      expect(authorization, 'Bearer <synthetic-access>');
      expect(response.jsonBody, <String, Object?>{'ok': true});
      expect(response.correlationId, correlationId);
      client.dispose();
      await server.close(force: true);
    },
  );

  test('rejects an oversized response body', () async {
    final server = await HttpServer.bind(InternetAddress.loopbackIPv4, 0);
    final responseFuture = server.first.then((request) async {
      request.response.statusCode = 200;
      request.response.headers.contentType = ContentType.json;
      request.response.headers.set('X-Correlation-ID', correlationId);
      request.response.write('{"value":"${'x' * 200}"}');
      await request.response.close();
    });
    final client = CommercialIdentityHttpClient(
      origin: CommercialApiOrigin.parse(
        'http://127.0.0.1:${server.port}',
        releaseMode: false,
        allowDebugHttp: true,
      ),
      correlationIdGenerator: const _FixedCorrelation(correlationId),
      responseBodyLimitBytes: 64,
    );
    await expectLater(
      client.send(
        const CommercialIdentityTransportRequest(
          method: 'GET',
          relativePath: 'api/v1/auth/me',
        ),
      ),
      throwsA(
        isA<CommercialIdentityFailure>().having(
          (failure) => failure.safeCode,
          'safeCode',
          'IDENTITY_RESPONSE_TOO_LARGE',
        ),
      ),
    );
    await responseFuture;
    client.dispose();
    await server.close(force: true);
  });

  test('source installs no permissive certificate callback or logging', () {
    final source = File(
      'lib/infrastructure/network/commercial_identity_http_client.dart',
    ).readAsStringSync();
    expect(source, isNot(contains('badCertificateCallback')));
    expect(source, isNot(contains('print(')));
    expect(source, isNot(contains('developer.log')));
  });

  test('socket failure is ambiguous for POST but retryable for GET', () async {
    final probe = await HttpServer.bind(InternetAddress.loopbackIPv4, 0);
    final closedPort = probe.port;
    await probe.close(force: true);
    final client = CommercialIdentityHttpClient(
      origin: CommercialApiOrigin.parse(
        'http://127.0.0.1:$closedPort',
        releaseMode: false,
        allowDebugHttp: true,
      ),
      correlationIdGenerator: const _FixedCorrelation(correlationId),
    );

    await expectLater(
      client.send(
        const CommercialIdentityTransportRequest(
          method: 'POST',
          relativePath: 'api/v1/auth/refresh',
          jsonBody: <String, Object?>{'refreshToken': '<synthetic>'},
        ),
      ),
      throwsA(
        isA<CommercialIdentityFailure>().having(
          (failure) => failure.kind,
          'kind',
          CommercialIdentityFailureKind.networkAmbiguous,
        ),
      ),
    );
    await expectLater(
      client.send(
        const CommercialIdentityTransportRequest(
          method: 'GET',
          relativePath: 'api/v1/auth/me',
        ),
      ),
      throwsA(
        isA<CommercialIdentityFailure>()
            .having(
              (failure) => failure.kind,
              'kind',
              CommercialIdentityFailureKind.networkUnavailable,
            )
            .having((failure) => failure.retryable, 'retryable', isTrue),
      ),
    );
    client.dispose();
  });
}

final class _FixedCorrelation implements CommercialCorrelationIdGenerator {
  const _FixedCorrelation(this.value);

  final String value;

  @override
  String generate() => value;
}
