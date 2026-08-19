import 'dart:convert';
import 'dart:io';

import 'package:flutter_test/flutter_test.dart';
import 'package:traderpro_agrisuite_mobile/core/identity/commercial_identity_failure.dart';
import 'package:traderpro_agrisuite_mobile/core/identity/commercial_identity_models.dart';
import 'package:traderpro_agrisuite_mobile/infrastructure/identity/commercial_identity_contract_codec.dart';

void main() {
  const codec = CommercialIdentityContractCodec();
  late Map<String, Object?> fixture;
  late Map<String, Object?> examples;

  setUpAll(() {
    fixture = _map(
      jsonDecode(
        File(
          '../../contracts/openapi/traderpro-commercial-mobile.v1.json',
        ).readAsStringSync(),
      ),
    );
    examples = _map(fixture['authenticationExamples']);
  });

  test(
    'identity paths, methods, and required activation header match fixture',
    () {
      final routes = (fixture['routes']! as List<Object?>)
          .map(_map)
          .where(
            (route) => <String>{
              'redeemActivation',
              'login',
              'refresh',
              'logout',
              'logoutAll',
              'me',
            }.contains(route['name']),
          )
          .toList();
      final expected = <String, (String, String)>{
        'redeemActivation': (
          CommercialIdentityContract.activationMethod,
          '/${CommercialIdentityContract.activationPath}',
        ),
        'login': (
          CommercialIdentityContract.loginMethod,
          '/${CommercialIdentityContract.loginPath}',
        ),
        'refresh': (
          CommercialIdentityContract.refreshMethod,
          '/${CommercialIdentityContract.refreshPath}',
        ),
        'logout': (
          CommercialIdentityContract.logoutMethod,
          '/${CommercialIdentityContract.logoutPath}',
        ),
        'logoutAll': (
          CommercialIdentityContract.logoutAllMethod,
          '/${CommercialIdentityContract.logoutAllPath}',
        ),
        'me': (
          CommercialIdentityContract.meMethod,
          '/${CommercialIdentityContract.mePath}',
        ),
      };
      for (final route in routes) {
        final contract = expected[route['name']]!;
        expect(route['method'], contract.$1);
        expect(route['path'], contract.$2);
      }

      final endpoints = _map(fixture['identityContract'])['endpoints']! as List;
      final activation = endpoints
          .map(_map)
          .singleWhere((endpoint) => endpoint['name'] == 'redeemActivation');
      expect(activation['requiredHeaders'], <Object?>['Idempotency-Key']);
    },
  );

  test('activation request and response have exact fixture parity', () {
    final activation = _map(examples['activation']);
    final request = _map(activation['request']);
    final encoded = codec.encodeActivation(
      ActivationRedeemRequest(
        workspaceCode: request['workspaceCode']! as String,
        activationCode: request['activationCode']! as String,
        clientInstallationReference:
            request['clientInstallationReference']! as String,
        deviceLabel: request['deviceLabel']! as String,
        platform: request['platform']! as String,
      ),
    );
    expect(encoded, request);

    final decoded = codec.decodeActivation(_map(activation['response']));
    expect(
      decoded.credential.deviceId.value,
      '019fc400-0000-7000-8000-000000000201',
    );
    expect(decoded.credential.secretVersion, 1);
    expect(decoded.activatedAtUtc.isUtc, isTrue);
  });

  test('login, refresh, and me decode exact fixture shapes', () {
    final login = codec.decodeAuthentication(
      _map(_map(examples['login'])['response']),
    );
    final refresh = codec.decodeAuthentication(
      _map(_map(examples['refresh'])['response']),
    );
    final me = codec.decodeMe(_map(_map(examples['me'])['response']));

    expect(login.context.role.wireValue, 'Operator');
    expect(login.context.tokenFamilyId, isNull);
    expect(refresh.refreshCredential.expiresAtUtc.isUtc, isTrue);
    expect(me.tokenFamilyId?.value, '019fc400-0000-7000-8000-000000000203');
    expect(login.context.sameImmutableContext(me), isTrue);
  });

  test('login and refresh requests have exact fixture parity', () {
    final loginRequest = _map(_map(examples['login'])['request']);
    expect(
      codec.encodeLogin(
        CommercialLoginRequest(
          workspaceCode: loginRequest['workspaceCode']! as String,
          login: loginRequest['login']! as String,
          password: loginRequest['password']! as String,
          deviceCredential: DeviceCredential(
            deviceId: DeviceId(loginRequest['deviceId']! as String),
            secret: loginRequest['deviceSecret']! as String,
            secretVersion: 1,
          ),
        ),
      ),
      loginRequest,
    );
    final refreshRequest = _map(_map(examples['refresh'])['request']);
    expect(
      codec.encodeRefresh(
        RefreshCredential(
          value: refreshRequest['refreshToken']! as String,
          expiresAtUtc: DateTime.utc(2026, 9, 1),
        ),
      ),
      refreshRequest,
    );
    expect(_map(examples['logout'])['responseStatus'], 204);
    expect(_map(examples['logoutAll'])['responseStatus'], 204);
  });

  test('unknown additive response fields are ignored', () {
    final response = _map(_map(examples['login'])['response']);
    response['futureField'] = <String, Object?>{'ignored': true};
    final result = codec.decodeAuthentication(response);
    expect(result.context.workspaceCode, 'DEMO-WORKSPACE');
  });

  test(
    'missing required fields, wrong role, and non-UTC dates fail closed',
    () {
      final missing = _map(_map(examples['login'])['response'])
        ..remove('refreshToken');
      final wrongRole = _map(_map(examples['login'])['response']);
      wrongRole['user'] = _map(wrongRole['user'])..['role'] = 'Administrator';
      final nonUtc = _map(_map(examples['login'])['response'])
        ..['accessTokenExpiresAtUtc'] = '2026-08-02T12:45:00+05:30';

      for (final response in <Map<String, Object?>>[
        missing,
        wrongRole,
        nonUtc,
      ]) {
        expect(
          () => codec.decodeAuthentication(response),
          throwsA(
            isA<CommercialIdentityFailure>().having(
              (failure) => failure.safeCode,
              'safeCode',
              'IDENTITY_PROTOCOL_CONTRACT_MISMATCH',
            ),
          ),
        );
      }
    },
  );

  test(
    'standard error envelope maps safe backend codes without raw details',
    () {
      final envelope = _map(_map(fixture['identityContract'])['errorEnvelope']);
      final error = _map(envelope['error']);
      error['code'] = 'REFRESH_TOKEN_REUSE_DETECTED';
      error['message'] = 'server message must not cross the failure boundary';
      error['category'] = 'Conflict';
      error['retryable'] = false;
      envelope['error'] = error;
      final failure = codec.decodeError(envelope, httpStatus: 409);
      expect(failure.kind, CommercialIdentityFailureKind.refreshReplayDetected);
      expect(failure.safeCode, 'REFRESH_TOKEN_REUSE_DETECTED');
      expect(failure.toString(), isNot(contains('server message')));
    },
  );
}

Map<String, Object?> _map(Object? value) =>
    Map<String, Object?>.from(value! as Map);
