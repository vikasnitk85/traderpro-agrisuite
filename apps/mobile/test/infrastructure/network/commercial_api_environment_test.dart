import 'package:flutter_test/flutter_test.dart';
import 'package:traderpro_agrisuite_mobile/core/identity/commercial_identity_failure.dart';
import 'package:traderpro_agrisuite_mobile/infrastructure/network/commercial_api_environment.dart';

void main() {
  test('normalizes a production HTTPS base with one trailing slash', () {
    final origin = CommercialApiOrigin.parse(
      'https://API.Example.com/mobile',
      releaseMode: true,
    );

    expect(origin.normalized, 'https://api.example.com/mobile/');
    expect(
      origin.resolve('api/v1/auth/me').toString(),
      'https://api.example.com/mobile/api/v1/auth/me',
    );
  });

  test(
    'release rejects missing, relative, HTTP, local, and private origins',
    () {
      for (final value in <String>[
        '',
        '/api',
        'http://api.example.com',
        'https://localhost',
        'https://127.0.0.1',
        'https://10.0.0.4',
        'https://192.168.1.4',
        'https://service.local',
      ]) {
        expect(
          () => CommercialApiOrigin.parse(value, releaseMode: true),
          throwsA(isA<CommercialIdentityFailure>()),
          reason: value,
        );
      }
    },
  );

  test('rejects credentials, query, and fragment', () {
    for (final value in <String>[
      'https://user:pass@api.example.com',
      'https://api.example.com?mode=prod',
      'https://api.example.com#fragment',
    ]) {
      expect(
        () => CommercialApiOrigin.parse(value, releaseMode: true),
        throwsA(isA<CommercialIdentityFailure>()),
        reason: value,
      );
    }
  });

  test('explicit debug-only path can use local HTTP', () {
    final origin = CommercialApiOrigin.parse(
      'http://127.0.0.1:5000',
      releaseMode: false,
      allowDebugHttp: true,
    );
    expect(origin.normalized, 'http://127.0.0.1:5000/');
  });

  test('debug HTTP still fails without the explicit opt-in', () {
    expect(
      () => CommercialApiOrigin.parse(
        'http://127.0.0.1:5000',
        releaseMode: false,
      ),
      throwsA(
        isA<CommercialIdentityFailure>().having(
          (failure) => failure.kind,
          'kind',
          CommercialIdentityFailureKind.protocolContractMismatch,
        ),
      ),
    );
  });
}
