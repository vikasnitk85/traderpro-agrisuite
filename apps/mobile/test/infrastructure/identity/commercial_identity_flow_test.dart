import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:traderpro_agrisuite_mobile/app/commercial_identity_controller.dart';
import 'package:traderpro_agrisuite_mobile/app/commercial_identity_screens.dart';
import 'package:traderpro_agrisuite_mobile/core/identity/commercial_authentication_status.dart';
import 'package:traderpro_agrisuite_mobile/core/identity/commercial_identity_failure.dart';
import 'package:traderpro_agrisuite_mobile/core/identity/commercial_identity_models.dart';
import 'package:traderpro_agrisuite_mobile/core/identity/commercial_identity_ports.dart';
import 'package:traderpro_agrisuite_mobile/infrastructure/identity/commercial_identity_contract_codec.dart';
import 'package:traderpro_agrisuite_mobile/infrastructure/identity/commercial_identity_service.dart';
import 'package:traderpro_agrisuite_mobile/infrastructure/identity/commercial_refresh_coordinator.dart';

const _deviceId = '55555555-5555-4555-8555-555555555555';
const _workspaceId = '11111111-1111-4111-8111-111111111111';
const _companyId = '22222222-2222-4222-8222-222222222222';
const _branchId = '33333333-3333-4333-8333-333333333333';
const _userId = '44444444-4444-4444-8444-444444444444';
const _familyId = '66666666-6666-4666-8666-666666666666';
const _correlationId = '77777777-7777-4777-8777-777777777777';

void main() {
  group('activation and login orchestration', () {
    test(
      'activation attempt is durable before send and commits Device',
      () async {
        final store = _MemoryCredentialStore();
        late _FakeTransport transport;
        transport = _FakeTransport((request) async {
          expect(store.activationAttempt?.idempotencyKey, 'stable-key');
          expect(request.idempotencyKey, 'stable-key');
          expect(request.jsonBody?['activationCode'], 'ACT-SECRET');
          return _ok(_activationResponse());
        });
        final service = _service(
          transport: transport,
          store: store,
          activationKeys: _FixedActivationKeys(),
        );

        final result = await service.activate(
          workspaceCode: 'FARM-ONE',
          activationCode: 'ACT-SECRET',
          deviceLabel: 'Yard tablet',
        );

        expect(result.deviceId.value, _deviceId);
        expect(store.device?.secret, 'device-secret');
        expect(store.activationAttempt, isNull);
        expect(transport.requests, hasLength(1));
      },
    );

    test(
      'response loss recovers with the exact activation transaction',
      () async {
        final store = _MemoryCredentialStore();
        var call = 0;
        final transport = _FakeTransport((request) async {
          call += 1;
          if (call == 1) {
            throw const CommercialIdentityFailure(
              kind: CommercialIdentityFailureKind.networkAmbiguous,
              safeCode: 'IDENTITY_NETWORK_OUTCOME_UNKNOWN',
            );
          }
          return _ok(_activationResponse());
        });
        final service = _service(
          transport: transport,
          store: store,
          activationKeys: _FixedActivationKeys(),
        );

        await expectLater(
          service.activate(
            workspaceCode: 'FARM-ONE',
            activationCode: 'ACT-SECRET',
            deviceLabel: 'Yard tablet',
          ),
          throwsA(
            isA<CommercialIdentityFailure>().having(
              (failure) => failure.kind,
              'kind',
              CommercialIdentityFailureKind.activationOutcomeUnknown,
            ),
          ),
        );
        expect(store.activationAttempt, isNotNull);

        await service.recoverActivation();

        expect(
          transport.requests.map((request) => request.idempotencyKey),
          everyElement('stable-key'),
        );
        expect(transport.requests[0].jsonBody, transport.requests[1].jsonBody);
        expect(store.activationAttempt, isNull);
      },
    );

    test(
      'definitive activation rejection clears the pending attempt',
      () async {
        final store = _MemoryCredentialStore();
        final transport = _FakeTransport(
          (_) async => _error(409, 'DEVICE_ACTIVATION_ALREADY_USED'),
        );
        final service = _service(
          transport: transport,
          store: store,
          activationKeys: _FixedActivationKeys(),
        );

        await expectLater(
          service.activate(
            workspaceCode: 'FARM-ONE',
            activationCode: 'ACT-SECRET',
            deviceLabel: 'Yard tablet',
          ),
          throwsA(isA<CommercialIdentityFailure>()),
        );

        expect(store.activationAttempt, isNull);
      },
    );

    test(
      'Device persistence failure never marks activation complete',
      () async {
        final store = _MemoryCredentialStore()..failDeviceReplacement = true;
        final service = _service(
          transport: _FakeTransport((_) async => _ok(_activationResponse())),
          store: store,
          activationKeys: _FixedActivationKeys(),
        );

        await expectLater(
          service.activate(
            workspaceCode: 'FARM-ONE',
            activationCode: 'ACT-SECRET',
            deviceLabel: 'Yard tablet',
          ),
          throwsA(
            isA<CommercialIdentityFailure>().having(
              (failure) => failure.kind,
              'kind',
              CommercialIdentityFailureKind.credentialPersistenceFailed,
            ),
          ),
        );
        expect(store.activationAttempt, isNotNull);
      },
    );

    test(
      'Device label accepts 200 characters and trims before persistence',
      () async {
        final store = _MemoryCredentialStore();
        final transport = _FakeTransport(
          (_) async => _ok(_activationResponse()),
        );
        final service = _service(
          transport: transport,
          store: store,
          activationKeys: _FixedActivationKeys(),
        );
        final label = List.filled(200, 'D').join();

        await service.activate(
          workspaceCode: 'FARM-ONE',
          activationCode: 'ACT-SECRET',
          deviceLabel: ' $label ',
        );

        expect(transport.requests, hasLength(1));
        expect(transport.requests.single.jsonBody?['deviceLabel'], label);
      },
    );

    test('invalid Device labels fail before persistence and HTTP', () async {
      for (final label in <String>[List.filled(201, 'D').join(), ' \t\n ']) {
        final store = _MemoryCredentialStore();
        final transport = _FakeTransport(
          (_) async => throw StateError('No request expected'),
        );
        final service = _service(transport: transport, store: store);

        await expectLater(
          service.activate(
            workspaceCode: 'FARM-ONE',
            activationCode: 'ACT-SECRET',
            deviceLabel: label,
          ),
          throwsA(
            isA<CommercialIdentityFailure>().having(
              (failure) => failure.kind,
              'kind',
              CommercialIdentityFailureKind.invalidDeviceLabel,
            ),
          ),
        );

        expect(store.activationAttempt, isNull);
        expect(transport.requests, isEmpty);
      }
    });

    test('empty login and password are correctable and send no HTTP', () async {
      for (final credentials in <(String, String)>[
        ('', 'correct horse battery staple'),
        (' \t\n ', 'correct horse battery staple'),
        ('owner@example.test', ''),
      ]) {
        final store = _MemoryCredentialStore()..device = _device();
        final transport = _FakeTransport(
          (_) async => throw StateError('No request expected'),
        );
        final service = _service(transport: transport, store: store);

        await expectLater(
          service.requestLogin(
            workspaceCode: 'FARM-ONE',
            login: credentials.$1,
            password: credentials.$2,
          ),
          throwsA(
            isA<CommercialIdentityFailure>().having(
              (failure) => failure.kind,
              'kind',
              CommercialIdentityFailureKind.invalidCredentials,
            ),
          ),
        );
        expect(transport.requests, isEmpty);
      }
    });

    test('login is not automatically retried', () async {
      final store = _MemoryCredentialStore()..device = _device();
      final transport = _FakeTransport((_) async {
        throw const CommercialIdentityFailure(
          kind: CommercialIdentityFailureKind.networkAmbiguous,
          safeCode: 'IDENTITY_NETWORK_OUTCOME_UNKNOWN',
        );
      });
      final service = _service(transport: transport, store: store);

      await expectLater(
        service.requestLogin(
          workspaceCode: 'FARM-ONE',
          login: 'owner@example.test',
          password: 'correct horse battery staple',
        ),
        throwsA(isA<CommercialIdentityFailure>()),
      );

      expect(transport.requests, hasLength(1));
    });

    test('/me is authoritative and retries one safe GET failure', () async {
      final store = _MemoryCredentialStore();
      var calls = 0;
      final transport = _FakeTransport((request) async {
        calls += 1;
        if (calls == 1) {
          throw const CommercialIdentityFailure(
            kind: CommercialIdentityFailureKind.networkUnavailable,
            safeCode: 'IDENTITY_NETWORK_UNAVAILABLE',
          );
        }
        return _ok(_meResponse());
      });
      final binding = _MemoryBindingPort();
      final service = _service(
        transport: transport,
        store: store,
        binding: binding,
      );

      final context = await service.confirmAndBind(
        _authentication('refresh-2'),
      );

      expect(context.workspaceCode, 'FARM-ONE');
      expect(transport.requests, hasLength(2));
      expect(binding.binding, isNotNull);
      expect(binding.snapshot?.userDisplayName, 'Owner One');
    });
  });

  group('refresh coordinator crash and race matrix', () {
    test(
      'A: predecessor marker recovers response-before-persistence crash',
      () async {
        final now = DateTime.utc(2026, 8, 9, 10);
        final store = _MemoryCredentialStore()
          ..refresh = _pendingPredecessor('refresh-1', now);
        final transport = _refreshAndMeTransport();
        final coordinator = _coordinator(store, transport, now);

        await coordinator.restoreSession();

        expect(store.refresh?.value, 'refresh-2');
        expect(coordinator.currentAccessToken?.value, 'access-2');
        expect(transport.requests.first.jsonBody, {
          'refreshToken': 'refresh-1',
        });
      },
    );

    test('B: durable replacement restores after access-memory loss', () async {
      final now = DateTime.utc(2026, 8, 9, 10);
      final store = _MemoryCredentialStore()
        ..refresh = RefreshCredential(
          value: 'refresh-2',
          expiresAtUtc: now.add(const Duration(days: 1)),
          bindingFingerprint: _binding(now).fingerprint,
          predecessorDigest: 'old-predecessor-digest',
          committedAtUtc: now.subtract(const Duration(seconds: 2)),
        );
      final transport = _refreshAndMeTransport(
        expectedRefresh: 'refresh-2',
        replacementRefresh: 'refresh-3',
      );
      final coordinator = _coordinator(store, transport, now);

      await coordinator.restoreSession();

      expect(store.refresh?.value, 'refresh-3');
      expect(coordinator.hasMemoryAccessToken, isTrue);
    });

    test(
      'C/D: rotation never publishes access before replacement readback',
      () async {
        final now = DateTime.utc(2026, 8, 9, 10);
        final store = _MemoryCredentialStore()
          ..refresh = _refresh('refresh-1', now)
          ..failRefreshReplacementAt = 2;
        final coordinator = _coordinator(store, _refreshAndMeTransport(), now);

        await expectLater(
          coordinator.restoreSession(),
          throwsA(isA<CommercialIdentityFailure>()),
        );

        expect(coordinator.hasMemoryAccessToken, isFalse);
      },
    );

    test('E: ambiguous result replays exact predecessor only once', () async {
      final now = DateTime.utc(2026, 8, 9, 10);
      final store = _MemoryCredentialStore()
        ..refresh = _refresh('refresh-1', now);
      var refreshCalls = 0;
      final transport = _FakeTransport((request) async {
        if (request.relativePath.endsWith('/refresh')) {
          refreshCalls += 1;
          if (refreshCalls == 1) {
            throw const CommercialIdentityFailure(
              kind: CommercialIdentityFailureKind.networkAmbiguous,
              safeCode: 'IDENTITY_NETWORK_OUTCOME_UNKNOWN',
            );
          }
          expect(request.jsonBody, {'refreshToken': 'refresh-1'});
          return _ok(_authResponse('refresh-2'));
        }
        return _ok(_meResponse());
      });
      final coordinator = _coordinator(store, transport, now);

      await coordinator.restoreSession();

      expect(refreshCalls, 2);
      expect(store.refresh?.value, 'refresh-2');
    });

    test('F: concurrent callers share one refresh Future', () async {
      final now = DateTime.utc(2026, 8, 9, 10);
      final store = _MemoryCredentialStore()
        ..refresh = _refresh('refresh-1', now);
      final response = Completer<CommercialIdentityTransportResponse>();
      var refreshCalls = 0;
      final transport = _FakeTransport((request) async {
        if (request.relativePath.endsWith('/refresh')) {
          refreshCalls += 1;
          return response.future;
        }
        return _ok(_meResponse());
      });
      final coordinator = _coordinator(store, transport, now);

      final first = coordinator.restoreSession();
      final second = coordinator.validAccessToken(forceRefresh: true);
      await _until(() => refreshCalls == 1);
      response.complete(_ok(_authResponse('refresh-2')));
      await Future.wait<Object>([first, second]);

      expect(refreshCalls, 1);
    });

    test('restart after the 30-second replay window requires login', () async {
      final now = DateTime.utc(2026, 8, 9, 10);
      final store = _MemoryCredentialStore()
        ..refresh = _pendingPredecessor(
          'refresh-1',
          now.subtract(const Duration(seconds: 30)),
        );
      final transport = _refreshAndMeTransport();
      final coordinator = _coordinator(store, transport, now);

      await expectLater(
        coordinator.restoreSession(),
        throwsA(
          isA<CommercialIdentityFailure>().having(
            (failure) => failure.safeCode,
            'safeCode',
            'REFRESH_RECOVERY_WINDOW_EXPIRED',
          ),
        ),
      );

      expect(store.refresh, isNull);
      expect(transport.requests, isEmpty);
    });

    test(
      'logout epoch prevents late refresh from resurrecting session',
      () async {
        final now = DateTime.utc(2026, 8, 9, 10);
        final store = _MemoryCredentialStore()
          ..refresh = _refresh('refresh-1', now);
        final response = Completer<CommercialIdentityTransportResponse>();
        var refreshStarted = false;
        final transport = _FakeTransport((request) async {
          if (request.relativePath.endsWith('/refresh')) {
            refreshStarted = true;
            return response.future;
          }
          return _ok(_meResponse());
        });
        final coordinator = _coordinator(store, transport, now);
        final restore = coordinator.restoreSession();
        await _until(() => refreshStarted);

        final logout = coordinator.logout(allSessions: false);
        response.complete(_ok(_authResponse('refresh-2')));
        await expectLater(restore, throwsA(isA<CommercialIdentityFailure>()));
        await logout;

        expect(coordinator.hasMemoryAccessToken, isFalse);
        expect(store.refresh, isNull);
      },
    );

    test(
      'Device reactivation epoch prevents late refresh publication',
      () async {
        final now = DateTime.utc(2026, 8, 9, 10);
        final store = _MemoryCredentialStore()
          ..device = _device()
          ..refresh = _refresh('refresh-1', now);
        final refreshResponse =
            Completer<CommercialIdentityTransportResponse>();
        var refreshStarted = false;
        final transport = _FakeTransport((request) async {
          if (request.relativePath.endsWith('/refresh')) {
            refreshStarted = true;
            return refreshResponse.future;
          }
          if (request.relativePath.endsWith('/redeem')) {
            return _ok(_activationResponse());
          }
          return _ok(_meResponse());
        });
        final coordinator = _coordinator(store, transport, now);
        final restore = coordinator.restoreSession();
        await _until(() => refreshStarted);

        final activation = coordinator.activateDevice(
          workspaceCode: 'FARM-ONE',
          activationCode: 'ACT-SECRET',
          deviceLabel: 'Yard tablet',
        );
        refreshResponse.complete(_ok(_authResponse('refresh-2')));
        await expectLater(restore, throwsA(isA<CommercialIdentityFailure>()));
        await activation;

        expect(coordinator.hasMemoryAccessToken, isFalse);
        expect(store.refresh, isNull);
        expect(store.device?.secret, 'device-secret');
      },
    );

    test(
      'logout-all uses a valid access token then clears local session',
      () async {
        var now = DateTime.utc(2026, 8, 9, 10);
        final store = _MemoryCredentialStore()..device = _device();
        var refreshCalls = 0;
        var logoutAllCalls = 0;
        final transport = _FakeTransport((request) async {
          if (request.relativePath.endsWith('/login')) {
            return _ok(_authResponse('refresh-2'));
          }
          if (request.relativePath.endsWith('/me')) {
            return _ok(_meResponse());
          }
          if (request.relativePath.endsWith('/refresh')) {
            refreshCalls += 1;
            return _ok(
              _authResponse(
                'refresh-3',
                access: 'access-3',
                accessExpiresAtUtc: '2026-08-09T12:00:00Z',
              ),
            );
          }
          if (request.relativePath.endsWith('/logout-all')) {
            logoutAllCalls += 1;
            expect(request.bearerAccessToken, 'access-2');
            return _noContent();
          }
          throw StateError('Unexpected request ${request.relativePath}');
        });
        final coordinator = CommercialRefreshCoordinator(
          identityService: _service(
            transport: transport,
            store: store,
            now: now,
          ),
          credentialStore: store,
          clock: () => now,
        );
        await coordinator.login(
          workspaceCode: 'FARM-ONE',
          login: 'owner@example.test',
          password: 'correct horse battery staple',
        );

        await coordinator.logout(allSessions: true);

        expect(refreshCalls, 0);
        expect(logoutAllCalls, 1);
        expect(coordinator.hasMemoryAccessToken, isFalse);
        expect(store.refresh, isNull);
      },
    );

    test('expired access refreshes once before logout-all', () async {
      var now = DateTime.utc(2026, 8, 9, 10);
      final store = _MemoryCredentialStore()..device = _device();
      var refreshCalls = 0;
      final transport = _FakeTransport((request) async {
        if (request.relativePath.endsWith('/login')) {
          return _ok(_authResponse('refresh-2'));
        }
        if (request.relativePath.endsWith('/refresh')) {
          refreshCalls += 1;
          return _ok(
            _authResponse(
              'refresh-3',
              access: 'access-3',
              accessExpiresAtUtc: '2026-08-09T13:00:00Z',
            ),
          );
        }
        if (request.relativePath.endsWith('/me')) {
          return _ok(_meResponse());
        }
        if (request.relativePath.endsWith('/logout-all')) {
          expect(request.bearerAccessToken, 'access-3');
          return _noContent();
        }
        throw StateError('Unexpected request ${request.relativePath}');
      });
      final service = _service(transport: transport, store: store, now: now);
      final coordinator = CommercialRefreshCoordinator(
        identityService: service,
        credentialStore: store,
        clock: () => now,
      );
      await coordinator.login(
        workspaceCode: 'FARM-ONE',
        login: 'owner@example.test',
        password: 'correct horse battery staple',
      );
      now = DateTime.utc(2026, 8, 9, 11);

      await coordinator.logout(allSessions: true);

      expect(refreshCalls, 1);
      expect(store.refresh, isNull);
      expect(coordinator.hasMemoryAccessToken, isFalse);
    });

    test('concurrent refresh and logout-all share one refresh', () async {
      var now = DateTime.utc(2026, 8, 9, 10);
      final store = _MemoryCredentialStore()..device = _device();
      final refreshResponse = Completer<CommercialIdentityTransportResponse>();
      var refreshCalls = 0;
      final transport = _FakeTransport((request) async {
        if (request.relativePath.endsWith('/login')) {
          return _ok(_authResponse('refresh-2'));
        }
        if (request.relativePath.endsWith('/refresh')) {
          refreshCalls += 1;
          return refreshResponse.future;
        }
        if (request.relativePath.endsWith('/me')) {
          return _ok(_meResponse());
        }
        if (request.relativePath.endsWith('/logout-all')) {
          expect(request.bearerAccessToken, 'access-3');
          return _noContent();
        }
        throw StateError('Unexpected request ${request.relativePath}');
      });
      final coordinator = CommercialRefreshCoordinator(
        identityService: _service(transport: transport, store: store, now: now),
        credentialStore: store,
        clock: () => now,
      );
      await coordinator.login(
        workspaceCode: 'FARM-ONE',
        login: 'owner@example.test',
        password: 'correct horse battery staple',
      );
      now = DateTime.utc(2026, 8, 9, 11);

      final refresh = coordinator.validAccessToken();
      final logout = coordinator.logout(allSessions: true);
      await _until(() => refreshCalls == 1);
      refreshResponse.complete(
        _ok(
          _authResponse(
            'refresh-3',
            access: 'access-3',
            accessExpiresAtUtc: '2026-08-09T13:00:00Z',
          ),
        ),
      );
      await refresh;
      await logout;

      expect(refreshCalls, 1);
      expect(store.refresh, isNull);
      expect(coordinator.hasMemoryAccessToken, isFalse);
    });

    test(
      'refresh rejection cannot be reported as logout-all success',
      () async {
        var now = DateTime.utc(2026, 8, 9, 10);
        final store = _MemoryCredentialStore()..device = _device();
        var logoutAllCalls = 0;
        final transport = _FakeTransport((request) async {
          if (request.relativePath.endsWith('/login')) {
            return _ok(_authResponse('refresh-2'));
          }
          if (request.relativePath.endsWith('/me')) {
            return _ok(_meResponse());
          }
          if (request.relativePath.endsWith('/refresh')) {
            return _error(401, 'REFRESH_TOKEN_INVALID');
          }
          if (request.relativePath.endsWith('/logout-all')) {
            logoutAllCalls += 1;
            return _noContent();
          }
          throw StateError('Unexpected request ${request.relativePath}');
        });
        final coordinator = CommercialRefreshCoordinator(
          identityService: _service(
            transport: transport,
            store: store,
            now: now,
          ),
          credentialStore: store,
          clock: () => now,
        );
        await coordinator.login(
          workspaceCode: 'FARM-ONE',
          login: 'owner@example.test',
          password: 'correct horse battery staple',
        );
        now = DateTime.utc(2026, 8, 9, 11);

        await expectLater(
          coordinator.logout(allSessions: true),
          throwsA(
            isA<CommercialIdentityFailure>().having(
              (failure) => failure.kind,
              'kind',
              CommercialIdentityFailureKind.remoteLogoutAllUnconfirmed,
            ),
          ),
        );

        expect(logoutAllCalls, 0);
        expect(coordinator.hasMemoryAccessToken, isFalse);
        expect(store.refresh, isNull);
      },
    );

    for (final scenario in <(String, Object)>[
      (
        'network ambiguity',
        const CommercialIdentityFailure(
          kind: CommercialIdentityFailureKind.networkAmbiguous,
          safeCode: 'IDENTITY_NETWORK_OUTCOME_UNKNOWN',
        ),
      ),
      ('server 401', _error(401, 'ACCESS_TOKEN_INVALID')),
      ('server 403', _error(403, 'DEVICE_NOT_ACTIVE')),
    ]) {
      test(
        'logout-all ${scenario.$1} is unconfirmed with local fallback',
        () async {
          final now = DateTime.utc(2026, 8, 9, 10);
          final store = _MemoryCredentialStore()..device = _device();
          final transport = _FakeTransport((request) async {
            if (request.relativePath.endsWith('/login')) {
              return _ok(_authResponse('refresh-2'));
            }
            if (request.relativePath.endsWith('/me')) {
              return _ok(_meResponse());
            }
            if (request.relativePath.endsWith('/logout-all')) {
              final result = scenario.$2;
              if (result is CommercialIdentityFailure) {
                throw result;
              }
              return result as CommercialIdentityTransportResponse;
            }
            if (request.relativePath.endsWith('/logout')) {
              return _noContent();
            }
            throw StateError('Unexpected request ${request.relativePath}');
          });
          final coordinator = CommercialRefreshCoordinator(
            identityService: _service(
              transport: transport,
              store: store,
              now: now,
            ),
            credentialStore: store,
            clock: () => now,
          );
          await coordinator.login(
            workspaceCode: 'FARM-ONE',
            login: 'owner@example.test',
            password: 'correct horse battery staple',
          );

          await expectLater(
            coordinator.logout(allSessions: true),
            throwsA(
              isA<CommercialIdentityFailure>().having(
                (failure) => failure.kind,
                'kind',
                CommercialIdentityFailureKind.remoteLogoutAllUnconfirmed,
              ),
            ),
          );
          expect(store.refresh, isNotNull);

          await coordinator.logout(allSessions: false);
          expect(store.refresh, isNull);
          expect(coordinator.hasMemoryAccessToken, isFalse);
        },
      );
    }

    test('logout-all epoch prevents late refresh publication', () async {
      var now = DateTime.utc(2026, 8, 9, 10);
      final store = _MemoryCredentialStore()..device = _device();
      final logoutResponse = Completer<CommercialIdentityTransportResponse>();
      final refreshResponse = Completer<CommercialIdentityTransportResponse>();
      var logoutStarted = false;
      var refreshStarted = false;
      final transport = _FakeTransport((request) async {
        if (request.relativePath.endsWith('/login')) {
          return _ok(_authResponse('refresh-2'));
        }
        if (request.relativePath.endsWith('/me')) {
          return _ok(_meResponse());
        }
        if (request.relativePath.endsWith('/logout-all')) {
          logoutStarted = true;
          return logoutResponse.future;
        }
        if (request.relativePath.endsWith('/refresh')) {
          refreshStarted = true;
          return refreshResponse.future;
        }
        throw StateError('Unexpected request ${request.relativePath}');
      });
      final coordinator = CommercialRefreshCoordinator(
        identityService: _service(transport: transport, store: store, now: now),
        credentialStore: store,
        clock: () => now,
      );
      await coordinator.login(
        workspaceCode: 'FARM-ONE',
        login: 'owner@example.test',
        password: 'correct horse battery staple',
      );

      final logout = coordinator.logout(allSessions: true);
      await _until(() => logoutStarted);
      final refresh = coordinator.validAccessToken(forceRefresh: true);
      await _until(() => refreshStarted);
      logoutResponse.complete(_noContent());
      await Future<void>.delayed(Duration.zero);
      refreshResponse.complete(
        _ok(
          _authResponse(
            'refresh-late',
            access: 'access-late',
            accessExpiresAtUtc: '2026-08-09T13:00:00Z',
          ),
        ),
      );

      await expectLater(refresh, throwsA(isA<CommercialIdentityFailure>()));
      await logout;
      expect(coordinator.hasMemoryAccessToken, isFalse);
      expect(store.refresh, isNull);
    });

    test(
      'context mismatch clears refresh and attempts logout-all once',
      () async {
        final now = DateTime.utc(2026, 8, 9, 10);
        final store = _MemoryCredentialStore()
          ..refresh = _refresh('refresh-1', now);
        var logoutAllCalls = 0;
        final transport = _FakeTransport((request) async {
          if (request.relativePath.endsWith('/refresh')) {
            return _ok(_authResponse('refresh-2'));
          }
          if (request.relativePath.endsWith('/me')) {
            return _ok(
              _meResponse(workspaceId: '99999999-9999-4999-8999-999999999999'),
            );
          }
          if (request.relativePath.endsWith('/logout-all')) {
            logoutAllCalls += 1;
            return _noContent();
          }
          throw StateError('Unexpected request ${request.relativePath}');
        });
        final coordinator = _coordinator(store, transport, now);

        await expectLater(
          coordinator.restoreSession(),
          throwsA(
            isA<CommercialIdentityFailure>().having(
              (failure) => failure.kind,
              'kind',
              CommercialIdentityFailureKind.identityContextMismatch,
            ),
          ),
        );

        expect(store.refresh, isNull);
        expect(coordinator.hasMemoryAccessToken, isFalse);
        expect(logoutAllCalls, 1);
      },
    );
  });

  group('identity controller and minimal UI', () {
    test(
      'login binding write failure clears provisional authority and recovers',
      () async {
        final now = DateTime.utc(2026, 8, 9, 10);
        final originalDevice = _device();
        final store = _MemoryCredentialStore()..device = originalDevice;
        final bindingBarrier = Completer<void>();
        final binding = _MemoryBindingPort()
          ..bindBarrier = bindingBarrier
          ..bindFailure = StateError('synthetic Drift binding write failure');
        final transport = _FakeTransport((request) async {
          if (request.relativePath.endsWith('/login')) {
            return _ok(_authResponse('refresh-2'));
          }
          if (request.relativePath.endsWith('/me')) {
            return _ok(_meResponse());
          }
          throw StateError('Unexpected request ${request.relativePath}');
        });
        final controller = _controller(
          store,
          transport,
          binding: binding,
          now: now,
        );
        await controller.start();

        final login = controller.login(
          workspaceCode: 'FARM-ONE',
          login: 'owner@example.test',
          password: 'correct horse battery staple',
        );
        await _until(() => binding.bindAttempts == 1);

        expect(store.refresh?.value, 'refresh-2');
        expect(controller.refreshCoordinator.hasMemoryAccessToken, isFalse);
        expect(
          controller.status,
          CommercialAuthenticationStatus.authenticating,
        );

        bindingBarrier.complete();
        await login;

        expect(
          controller.status,
          CommercialAuthenticationStatus.lockedFailClosed,
        );
        expect(controller.safeFailureCode, 'IDENTITY_BINDING_STORAGE_FAILED');
        expect(controller.identity, isNull);
        expect(controller.refreshCoordinator.hasMemoryAccessToken, isFalse);
        expect(store.refresh, isNull);
        expect(identical(store.device, originalDevice), isTrue);
        expect(binding.binding, isNull);
        expect(store.databaseCiphertextSentinel, 'commercial-db-ciphertext');
        expect(store.databaseKeySentinel, 'commercial-db-key');

        controller.dispose();
        await controller.refreshCoordinator.dispose();
        binding
          ..bindBarrier = null
          ..bindFailure = null;
        final restarted = _controller(
          store,
          transport,
          binding: binding,
          now: now,
        );
        await restarted.start();
        expect(
          restarted.status,
          CommercialAuthenticationStatus.deviceRegisteredNoSession,
        );
        await restarted.login(
          workspaceCode: 'FARM-ONE',
          login: 'owner@example.test',
          password: 'correct horse battery staple',
        );
        expect(
          restarted.status,
          CommercialAuthenticationStatus.identityContextReady,
        );
        expect(restarted.refreshCoordinator.hasMemoryAccessToken, isTrue);
        expect(binding.binding, isNotNull);
      },
    );

    test(
      'refresh binding write failure clears replacement and preserves binding',
      () async {
        final now = DateTime.utc(2026, 8, 9, 10);
        final originalDevice = _device();
        final originalBinding = _binding(now);
        final originalSnapshot = _snapshot(now);
        final store = _MemoryCredentialStore()
          ..device = originalDevice
          ..refresh = _refresh('refresh-1', now);
        final bindingBarrier = Completer<void>();
        final binding = _MemoryBindingPort()
          ..binding = originalBinding
          ..snapshot = originalSnapshot
          ..bindBarrier = bindingBarrier
          ..bindFailure = StateError('synthetic Drift snapshot write failure');
        final controller = _controller(
          store,
          _refreshAndMeTransport(),
          binding: binding,
          now: now,
        );

        final start = controller.start();
        await _until(() => binding.bindAttempts == 1);

        expect(store.refreshReplacementValues, contains('refresh-2'));
        expect(controller.refreshCoordinator.hasMemoryAccessToken, isFalse);
        expect(controller.status, CommercialAuthenticationStatus.refreshing);

        bindingBarrier.complete();
        await start;

        expect(
          controller.status,
          CommercialAuthenticationStatus.lockedFailClosed,
        );
        expect(controller.safeFailureCode, 'IDENTITY_BINDING_STORAGE_FAILED');
        expect(controller.refreshCoordinator.hasMemoryAccessToken, isFalse);
        expect(store.refresh, isNull);
        expect(identical(store.device, originalDevice), isTrue);
        expect(identical(binding.binding, originalBinding), isTrue);
        expect(identical(binding.snapshot, originalSnapshot), isTrue);
        expect(store.databaseCiphertextSentinel, 'commercial-db-ciphertext');
        expect(store.databaseKeySentinel, 'commercial-db-key');
      },
    );

    testWidgets('shows activation without any Commercial business UI', (
      tester,
    ) async {
      final store = _MemoryCredentialStore();
      final controller = _controller(
        store,
        _FakeTransport((_) async => throw StateError('No request expected')),
      );
      await controller.start();

      await tester.pumpWidget(
        MaterialApp(home: CommercialIdentityScreen(controller: controller)),
      );

      expect(find.text('Activate Device'), findsOneWidget);
      expect(find.byKey(const Key('activation-code')), findsOneWidget);
      expect(find.textContaining('Receiving'), findsNothing);
      expect(find.textContaining('Inventory'), findsNothing);
    });

    testWidgets('invalid Device label leaves activation form correctable', (
      tester,
    ) async {
      final store = _MemoryCredentialStore();
      final transport = _FakeTransport(
        (_) async => throw StateError('No request expected'),
      );
      final controller = _controller(store, transport);
      await controller.start();
      await tester.pumpWidget(
        MaterialApp(home: CommercialIdentityScreen(controller: controller)),
      );

      await tester.enterText(
        find.byKey(const Key('activation-workspace-code')),
        'FARM-ONE',
      );
      await tester.enterText(
        find.byKey(const Key('activation-code')),
        'ACT-SECRET',
      );
      await tester.enterText(
        find.byKey(const Key('activation-device-label')),
        List.filled(201, 'D').join(),
      );
      await tester.tap(find.byKey(const Key('activate-device')));
      await tester.pumpAndSettle();

      expect(
        controller.status,
        CommercialAuthenticationStatus.deviceUnregistered,
      );
      expect(find.text('Activate Device'), findsOneWidget);
      expect(find.text('DEVICE_LABEL_INVALID'), findsOneWidget);
      expect(store.activationAttempt, isNull);
      expect(transport.requests, isEmpty);
    });

    test(
      'pending activation wins over a committed Device credential',
      () async {
        final store = _MemoryCredentialStore()
          ..device = _device(secret: 'committed-secret')
          ..activationAttempt = _activationAttempt();
        final transport = _FakeTransport((request) async {
          expect(
            request.relativePath,
            CommercialIdentityContract.activationPath,
          );
          expect(request.idempotencyKey, 'stable-key');
          return _ok(_activationResponse(secret: 'committed-secret'));
        });
        final controller = _controller(store, transport);

        await controller.start();

        expect(
          controller.status,
          CommercialAuthenticationStatus.activationOutcomeUnknown,
        );
        expect(transport.requests, isEmpty);

        await controller.recoverActivation();

        expect(
          controller.status,
          CommercialAuthenticationStatus.deviceRegisteredNoSession,
        );
        expect(store.activationAttempt, isNull);
        expect(store.device?.secret, 'committed-secret');
        expect(transport.requests, hasLength(1));
      },
    );

    test(
      'pending reactivation wins over old Device credential after restart',
      () async {
        final now = DateTime.utc(2026, 8, 9, 10);
        final originalBinding = _binding(now);
        final originalSnapshot = _snapshot(now);
        final binding = _MemoryBindingPort()
          ..binding = originalBinding
          ..snapshot = originalSnapshot;
        final store = _MemoryCredentialStore()
          ..device = _device(secret: 'old-device-secret')
          ..activationAttempt = _activationAttempt();
        final transport = _FakeTransport((request) async {
          expect(
            request.relativePath,
            CommercialIdentityContract.activationPath,
          );
          expect(request.idempotencyKey, 'stable-key');
          expect(request.jsonBody, {
            'workspaceCode': 'FARM-ONE',
            'activationCode': 'ACT-SECRET',
            'clientInstallationReference': 'installation-reference',
            'deviceLabel': 'Yard tablet',
            'platform': 'Android',
          });
          return _ok(
            _activationResponse(secret: 'new-device-secret', version: 2),
          );
        });
        final controller = _controller(
          store,
          transport,
          binding: binding,
          now: now,
        );

        await controller.start();

        expect(
          controller.status,
          CommercialAuthenticationStatus.activationOutcomeUnknown,
        );
        expect(transport.requests, isEmpty);

        await controller.recoverActivation();

        expect(store.device?.deviceId.value, _deviceId);
        expect(store.device?.secret, 'new-device-secret');
        expect(store.device?.secretVersion, 2);
        expect(store.activationAttempt, isNull);
        expect(identical(binding.binding, originalBinding), isTrue);
        expect(identical(binding.snapshot, originalSnapshot), isTrue);
        expect(transport.requests, hasLength(1));
        expect(
          controller.status,
          CommercialAuthenticationStatus.deviceRegisteredNoSession,
        );
      },
    );

    test(
      'malformed activation attempt fails closed before login restore',
      () async {
        final store = _MemoryCredentialStore()
          ..device = _device()
          ..activationReadOverride = const CommercialCredentialRead(
            CommercialCredentialStoreState.malformed,
          );
        final transport = _FakeTransport(
          (_) async => throw StateError('No request expected'),
        );
        final controller = _controller(store, transport);

        await controller.start();

        expect(
          controller.status,
          CommercialAuthenticationStatus.lockedFailClosed,
        );
        expect(controller.safeFailureCode, 'ACTIVATION_ATTEMPT_STORE_INVALID');
        expect(transport.requests, isEmpty);
      },
    );

    for (final failure in <CommercialIdentityFailure>[
      const CommercialIdentityFailure(
        kind: CommercialIdentityFailureKind.tlsFailure,
        safeCode: 'IDENTITY_TLS_FAILURE',
      ),
      const CommercialIdentityFailure(
        kind: CommercialIdentityFailureKind.networkAmbiguous,
        safeCode: 'IDENTITY_NETWORK_OUTCOME_UNKNOWN',
      ),
    ]) {
      testWidgets(
        'first activation ${failure.kind.name} stays recoverable across restart',
        (tester) async {
          final store = _MemoryCredentialStore();
          var shouldFail = true;
          final transport = _FakeTransport((request) async {
            expect(request.idempotencyKey, 'stable-key');
            if (shouldFail) {
              throw failure;
            }
            return _ok(_activationResponse());
          });
          final first = _controller(
            store,
            transport,
            activationKeys: _FixedActivationKeys(),
          );
          await first.start();

          await first.activate(
            workspaceCode: 'FARM-ONE',
            activationCode: 'ACT-SECRET',
            deviceLabel: 'Yard tablet',
          );

          expect(
            first.status,
            CommercialAuthenticationStatus.activationOutcomeUnknown,
          );
          expect(store.activationAttempt?.idempotencyKey, 'stable-key');
          final originalBody = transport.requests.single.jsonBody;

          final restarted = _controller(store, transport);
          await restarted.start();
          await tester.pumpWidget(
            MaterialApp(home: CommercialIdentityScreen(controller: restarted)),
          );
          expect(
            find.text('Activation outcome needs recovery'),
            findsOneWidget,
          );
          expect(find.text('Workspace sign in'), findsNothing);

          shouldFail = false;
          await tester.tap(find.byKey(const Key('recover-activation')));
          await tester.pumpAndSettle();

          expect(transport.requests, hasLength(2));
          expect(transport.requests[1].idempotencyKey, 'stable-key');
          expect(transport.requests[1].jsonBody, originalBody);
          expect(store.activationAttempt, isNull);
          expect(find.text('Workspace sign in'), findsOneWidget);
        },
      );
    }

    testWidgets('shows login and obscures the password', (tester) async {
      final store = _MemoryCredentialStore()..device = _device();
      final controller = _controller(
        store,
        _FakeTransport((_) async => throw StateError('No request expected')),
      );
      await controller.start();

      await tester.pumpWidget(
        MaterialApp(home: CommercialIdentityScreen(controller: controller)),
      );

      expect(find.text('Workspace sign in'), findsOneWidget);
      expect(
        tester
            .widget<TextField>(find.byKey(const Key('login-password')))
            .obscureText,
        isTrue,
      );
    });

    testWidgets('invalid login remains correctable and can be resubmitted', (
      tester,
    ) async {
      final store = _MemoryCredentialStore()..device = _device();
      final transport = _FakeTransport((request) async {
        if (request.relativePath.endsWith('/login')) {
          return _ok(_authResponse('refresh-2'));
        }
        if (request.relativePath.endsWith('/me')) {
          return _ok(_meResponse());
        }
        throw StateError('Unexpected request ${request.relativePath}');
      });
      final controller = _controller(store, transport);
      await controller.start();
      await tester.pumpWidget(
        MaterialApp(home: CommercialIdentityScreen(controller: controller)),
      );
      await tester.enterText(
        find.byKey(const Key('login-workspace-code')),
        'FARM-ONE',
      );
      await tester.enterText(
        find.byKey(const Key('login-password')),
        'correct horse battery staple',
      );

      await tester.tap(find.byKey(const Key('sign-in')));
      await tester.pumpAndSettle();

      expect(
        controller.status,
        CommercialAuthenticationStatus.deviceRegisteredNoSession,
      );
      expect(find.text('Workspace sign in'), findsOneWidget);
      expect(find.text('AUTHENTICATION_FAILED'), findsOneWidget);
      expect(transport.requests, isEmpty);

      await tester.enterText(
        find.byKey(const Key('login-name')),
        'owner@example.test',
      );
      await tester.enterText(
        find.byKey(const Key('login-password')),
        'correct horse battery staple',
      );
      await tester.tap(find.byKey(const Key('sign-in')));
      await tester.pumpAndSettle();

      expect(find.text('Identity confirmed'), findsOneWidget);
      expect(transport.requests, hasLength(2));
    });

    testWidgets('offline bound state is explicit and non-authorizing', (
      tester,
    ) async {
      final now = DateTime.utc(2026, 8, 9, 10);
      final store = _MemoryCredentialStore()
        ..device = _device()
        ..refresh = _refresh('refresh-1', now);
      final binding = _MemoryBindingPort()
        ..binding = _binding(now)
        ..snapshot = CommercialIdentitySnapshot(
          workspaceCode: 'FARM-ONE',
          userDisplayName: 'Owner One',
          role: CommercialRole.owner,
          deviceLabel: 'Yard tablet',
          lastConfirmedAtUtc: now,
        );
      final controller = _controller(
        store,
        _FakeTransport((_) async {
          throw const CommercialIdentityFailure(
            kind: CommercialIdentityFailureKind.networkUnavailable,
            safeCode: 'IDENTITY_NETWORK_UNAVAILABLE',
          );
        }),
        binding: binding,
        now: now,
      );
      await controller.start();

      await tester.pumpWidget(
        MaterialApp(home: CommercialIdentityScreen(controller: controller)),
      );

      expect(
        controller.status,
        CommercialAuthenticationStatus.boundOfflineRevalidationRequired,
      );
      expect(
        find.text('Online identity revalidation required'),
        findsOneWidget,
      );
      expect(
        find.textContaining('No Commercial business operation'),
        findsOneWidget,
      );
    });

    testWidgets('ready UI exposes safe identity and logout retains Device', (
      tester,
    ) async {
      final now = DateTime.utc(2026, 8, 9, 10);
      final store = _MemoryCredentialStore()..device = _device();
      final transport = _FakeTransport((request) async {
        if (request.relativePath.endsWith('/login')) {
          return _ok(_authResponse('refresh-2'));
        }
        if (request.relativePath.endsWith('/me')) {
          return _ok(_meResponse());
        }
        if (request.relativePath.endsWith('/logout')) {
          return _noContent();
        }
        throw StateError('Unexpected request ${request.relativePath}');
      });
      final controller = _controller(store, transport, now: now);
      await controller.start();
      await controller.login(
        workspaceCode: 'FARM-ONE',
        login: 'owner@example.test',
        password: 'correct horse battery staple',
      );

      await tester.pumpWidget(
        MaterialApp(home: CommercialIdentityScreen(controller: controller)),
      );
      expect(find.text('Identity confirmed'), findsOneWidget);
      expect(find.textContaining('Owner One'), findsOneWidget);
      expect(find.textContaining('access-2'), findsNothing);
      expect(find.textContaining('refresh-2'), findsNothing);

      await tester.tap(find.byKey(const Key('sign-out')));
      await tester.pumpAndSettle();
      expect(find.text('Workspace sign in'), findsOneWidget);
      expect(store.device, isNotNull);
      expect(store.refresh, isNull);
    });

    testWidgets('mismatch and Device revocation render blocking states', (
      tester,
    ) async {
      final now = DateTime.utc(2026, 8, 9, 10);
      final mismatchStore = _MemoryCredentialStore()
        ..device = _device()
        ..refresh = _refresh('refresh-1', now);
      final mismatchController = _controller(
        mismatchStore,
        _FakeTransport((request) async {
          if (request.relativePath.endsWith('/refresh')) {
            return _ok(_authResponse('refresh-2'));
          }
          if (request.relativePath.endsWith('/me')) {
            return _ok(
              _meResponse(workspaceId: '99999999-9999-4999-8999-999999999999'),
            );
          }
          if (request.relativePath.endsWith('/logout-all')) {
            return _noContent();
          }
          throw StateError('Unexpected request');
        }),
        now: now,
      );
      await mismatchController.start();
      await tester.pumpWidget(
        MaterialApp(
          home: CommercialIdentityScreen(controller: mismatchController),
        ),
      );
      expect(
        find.text('Identity does not match this installation'),
        findsOneWidget,
      );

      final revokedStore = _MemoryCredentialStore()
        ..device = _device()
        ..refresh = _refresh('refresh-1', now);
      final revokedController = _controller(
        revokedStore,
        _FakeTransport((_) async => _error(403, 'DEVICE_NOT_ACTIVE')),
        now: now,
      );
      await revokedController.start();
      await tester.pumpWidget(
        MaterialApp(
          home: CommercialIdentityScreen(controller: revokedController),
        ),
      );
      expect(find.text('Device is inactive'), findsOneWidget);
      expect(revokedStore.retired, isTrue);
    });

    testWidgets(
      'revoked Device reactivates same identity without clearing local data',
      (tester) async {
        final now = DateTime.utc(2026, 8, 9, 10);
        final originalBinding = _binding(now);
        final originalSnapshot = _snapshot(now);
        final binding = _MemoryBindingPort()
          ..binding = originalBinding
          ..snapshot = originalSnapshot;
        final store = _MemoryCredentialStore()
          ..device = _device(secret: 'old-device-secret')
          ..refresh = _refresh('refresh-1', now);
        final transport = _FakeTransport((request) async {
          if (request.relativePath.endsWith('/refresh')) {
            return _error(403, 'DEVICE_NOT_ACTIVE');
          }
          if (request.relativePath.endsWith('/redeem')) {
            expect(request.jsonBody?['workspaceCode'], 'FARM-ONE');
            expect(request.jsonBody?['deviceLabel'], 'Yard tablet');
            return _ok(
              _activationResponse(secret: 'new-device-secret', version: 2),
            );
          }
          throw StateError('Unexpected request ${request.relativePath}');
        });
        final controller = _controller(
          store,
          transport,
          binding: binding,
          activationKeys: _FixedActivationKeys(),
          now: now,
        );
        await controller.start();
        await tester.pumpWidget(
          MaterialApp(home: CommercialIdentityScreen(controller: controller)),
        );

        expect(
          find.byKey(const Key('open-device-reactivation')),
          findsOneWidget,
        );
        expect(store.device, isNull);
        expect(store.retired, isTrue);
        expect(binding.binding?.deviceId.value, _deviceId);

        await tester.tap(find.byKey(const Key('open-device-reactivation')));
        await tester.pumpAndSettle();
        expect(find.text('Reactivate Device'), findsOneWidget);
        expect(
          tester
              .widget<TextField>(
                find.byKey(const Key('activation-workspace-code')),
              )
              .controller
              ?.text,
          'FARM-ONE',
        );
        expect(
          tester
              .widget<TextField>(
                find.byKey(const Key('activation-device-label')),
              )
              .controller
              ?.text,
          'Yard tablet',
        );
        await tester.enterText(
          find.byKey(const Key('activation-code')),
          'ACT-SECRET',
        );
        await tester.tap(find.byKey(const Key('reactivate-device')));
        await tester.pumpAndSettle();

        expect(find.text('Workspace sign in'), findsOneWidget);
        expect(store.device?.deviceId.value, _deviceId);
        expect(store.device?.secret, 'new-device-secret');
        expect(store.device?.secretVersion, 2);
        expect(store.retired, isFalse);
        expect(store.refresh, isNull);
        expect(store.activationAttempt, isNull);
        expect(identical(binding.binding, originalBinding), isTrue);
        expect(identical(binding.snapshot, originalSnapshot), isTrue);
        expect(store.databaseCiphertextSentinel, 'commercial-db-ciphertext');
        expect(store.databaseKeySentinel, 'commercial-db-key');
      },
    );

    testWidgets('ambiguous reactivation is exact-replay recoverable', (
      tester,
    ) async {
      final now = DateTime.utc(2026, 8, 9, 10);
      final binding = _MemoryBindingPort()
        ..binding = _binding(now)
        ..snapshot = _snapshot(now);
      final store = _MemoryCredentialStore()
        ..device = _device(secret: 'old-device-secret')
        ..refresh = _refresh('refresh-1', now);
      var redeemCalls = 0;
      final transport = _FakeTransport((request) async {
        if (request.relativePath.endsWith('/refresh')) {
          return _error(403, 'DEVICE_NOT_ACTIVE');
        }
        if (request.relativePath.endsWith('/redeem')) {
          redeemCalls += 1;
          if (redeemCalls == 1) {
            throw const CommercialIdentityFailure(
              kind: CommercialIdentityFailureKind.networkAmbiguous,
              safeCode: 'IDENTITY_NETWORK_OUTCOME_UNKNOWN',
            );
          }
          return _ok(
            _activationResponse(secret: 'new-device-secret', version: 2),
          );
        }
        throw StateError('Unexpected request ${request.relativePath}');
      });
      final first = _controller(
        store,
        transport,
        binding: binding,
        activationKeys: _FixedActivationKeys(),
        now: now,
      );
      await first.start();
      await first.activate(
        workspaceCode: 'FARM-ONE',
        activationCode: 'ACT-SECRET',
        deviceLabel: 'Yard tablet',
      );

      expect(
        first.status,
        CommercialAuthenticationStatus.activationOutcomeUnknown,
      );
      expect(store.activationAttempt, isNotNull);
      final firstRedeem = transport.requests.last;

      final restarted = _controller(
        store,
        transport,
        binding: binding,
        now: now,
      );
      await restarted.start();
      await tester.pumpWidget(
        MaterialApp(home: CommercialIdentityScreen(controller: restarted)),
      );
      expect(find.text('Activation outcome needs recovery'), findsOneWidget);
      await tester.tap(find.byKey(const Key('recover-activation')));
      await tester.pumpAndSettle();

      final recoveredRedeem = transport.requests.last;
      expect(recoveredRedeem.idempotencyKey, firstRedeem.idempotencyKey);
      expect(recoveredRedeem.jsonBody, firstRedeem.jsonBody);
      expect(store.device?.deviceId.value, _deviceId);
      expect(store.device?.secret, 'new-device-secret');
      expect(store.activationAttempt, isNull);
      expect(find.text('Workspace sign in'), findsOneWidget);
    });

    testWidgets('unconfirmed logout-all offers retry and local fallback', (
      tester,
    ) async {
      final now = DateTime.utc(2026, 8, 9, 10);
      final store = _MemoryCredentialStore()..device = _device();
      final transport = _FakeTransport((request) async {
        if (request.relativePath.endsWith('/login')) {
          return _ok(_authResponse('refresh-2'));
        }
        if (request.relativePath.endsWith('/me')) {
          return _ok(_meResponse());
        }
        if (request.relativePath.endsWith('/logout-all')) {
          throw const CommercialIdentityFailure(
            kind: CommercialIdentityFailureKind.networkAmbiguous,
            safeCode: 'IDENTITY_NETWORK_OUTCOME_UNKNOWN',
          );
        }
        if (request.relativePath.endsWith('/logout')) {
          return _noContent();
        }
        throw StateError('Unexpected request ${request.relativePath}');
      });
      final controller = _controller(store, transport, now: now);
      await controller.start();
      await controller.login(
        workspaceCode: 'FARM-ONE',
        login: 'owner@example.test',
        password: 'correct horse battery staple',
      );
      await tester.pumpWidget(
        MaterialApp(home: CommercialIdentityScreen(controller: controller)),
      );

      await tester.tap(find.byKey(const Key('sign-out-all')));
      await tester.pumpAndSettle();

      expect(find.text('Sign out all was not confirmed'), findsOneWidget);
      expect(find.byKey(const Key('retry-sign-out-all')), findsOneWidget);
      expect(find.byKey(const Key('fallback-local-sign-out')), findsOneWidget);
      expect(store.refresh, isNotNull);

      await tester.tap(find.byKey(const Key('fallback-local-sign-out')));
      await tester.pumpAndSettle();

      expect(find.text('Workspace sign in'), findsOneWidget);
      expect(store.refresh, isNull);
      expect(controller.refreshCoordinator.hasMemoryAccessToken, isFalse);
    });

    for (final kind in <CommercialIdentityFailureKind>[
      CommercialIdentityFailureKind.secureStoreUnavailable,
      CommercialIdentityFailureKind.credentialPersistenceFailed,
      CommercialIdentityFailureKind.secureStoreMalformed,
    ]) {
      testWidgets('logout ${kind.name} exits spinner fail-closed', (
        tester,
      ) async {
        final now = DateTime.utc(2026, 8, 9, 10);
        final originalDevice = _device();
        final store = _MemoryCredentialStore()..device = originalDevice;
        final binding = _MemoryBindingPort();
        final transport = _FakeTransport((request) async {
          if (request.relativePath.endsWith('/login')) {
            return _ok(_authResponse('refresh-2'));
          }
          if (request.relativePath.endsWith('/me')) {
            return _ok(_meResponse());
          }
          if (request.relativePath.endsWith('/logout')) {
            return _noContent();
          }
          throw StateError('Unexpected request ${request.relativePath}');
        });
        final controller = _controller(
          store,
          transport,
          binding: binding,
          now: now,
        );
        await controller.start();
        await controller.login(
          workspaceCode: 'FARM-ONE',
          login: 'owner@example.test',
          password: 'correct horse battery staple',
        );
        final originalBinding = binding.binding;
        store.clearRefreshFailure = CommercialIdentityFailure(
          kind: kind,
          safeCode: 'TEST_SECURE_STORE_CLEAR_FAILED',
        );
        await tester.pumpWidget(
          MaterialApp(home: CommercialIdentityScreen(controller: controller)),
        );

        await tester.tap(find.byKey(const Key('sign-out')));
        await tester.pumpAndSettle();

        expect(
          controller.status,
          CommercialAuthenticationStatus.lockedFailClosed,
        );
        expect(find.text('Signing out…'), findsNothing);
        expect(
          find.text('Secure identity storage unavailable'),
          findsOneWidget,
        );
        expect(controller.refreshCoordinator.hasMemoryAccessToken, isFalse);
        expect(store.refresh, isNotNull);
        expect(identical(store.device, originalDevice), isTrue);
        expect(identical(binding.binding, originalBinding), isTrue);
        expect(store.databaseCiphertextSentinel, 'commercial-db-ciphertext');
        expect(store.databaseKeySentinel, 'commercial-db-key');
      });
    }
  });
}

CommercialIdentityController _controller(
  _MemoryCredentialStore store,
  _FakeTransport transport, {
  _MemoryBindingPort? binding,
  ActivationIdempotencyKeyGenerator? activationKeys,
  DateTime? now,
}) {
  final bindingPort = binding ?? _MemoryBindingPort();
  final service = _service(
    transport: transport,
    store: store,
    binding: bindingPort,
    activationKeys: activationKeys,
    now: now,
  );
  final coordinator = CommercialRefreshCoordinator(
    identityService: service,
    credentialStore: store,
    clock: () => now ?? DateTime.utc(2026, 8, 9, 10),
  );
  return CommercialIdentityController(
    identityService: service,
    refreshCoordinator: coordinator,
    credentialStore: store,
    activationAttemptStore: store,
    bindingPort: bindingPort,
  );
}

CommercialIdentityService _service({
  required _FakeTransport transport,
  required _MemoryCredentialStore store,
  _MemoryBindingPort? binding,
  ActivationIdempotencyKeyGenerator? activationKeys,
  DateTime? now,
}) => CommercialIdentityService(
  transport: transport,
  credentialStore: store,
  activationAttemptStore: store,
  bindingPort: binding ?? _MemoryBindingPort(),
  normalizedApiOrigin: 'https://identity.example.test/',
  installationReferenceReader: () async => 'installation-reference',
  activationKeyGenerator: activationKeys,
  clock: () => now ?? DateTime.utc(2026, 8, 9, 10),
);

CommercialRefreshCoordinator _coordinator(
  _MemoryCredentialStore store,
  _FakeTransport transport,
  DateTime now,
) => CommercialRefreshCoordinator(
  identityService: _service(transport: transport, store: store, now: now),
  credentialStore: store,
  clock: () => now,
);

_FakeTransport _refreshAndMeTransport({
  String expectedRefresh = 'refresh-1',
  String replacementRefresh = 'refresh-2',
}) => _FakeTransport((request) async {
  if (request.relativePath.endsWith('/refresh')) {
    expect(request.jsonBody, {'refreshToken': expectedRefresh});
    return _ok(_authResponse(replacementRefresh));
  }
  if (request.relativePath.endsWith('/me')) {
    return _ok(_meResponse());
  }
  throw StateError('Unexpected request ${request.relativePath}');
});

final class _FakeTransport implements CommercialIdentityTransport {
  _FakeTransport(this.handler);

  final Future<CommercialIdentityTransportResponse> Function(
    CommercialIdentityTransportRequest request,
  )
  handler;
  final List<CommercialIdentityTransportRequest> requests = [];

  @override
  Future<CommercialIdentityTransportResponse> send(
    CommercialIdentityTransportRequest request,
  ) {
    requests.add(request);
    return handler(request);
  }

  @override
  void dispose() {}
}

final class _MemoryCredentialStore
    implements
        CommercialIdentityCredentialStore,
        CommercialActivationAttemptStore {
  DeviceCredential? device;
  RefreshCredential? refresh;
  CommercialActivationAttempt? activationAttempt;
  bool retired = false;
  bool failDeviceReplacement = false;
  String databaseCiphertextSentinel = 'commercial-db-ciphertext';
  String databaseKeySentinel = 'commercial-db-key';
  CommercialCredentialRead<CommercialActivationAttempt>? activationReadOverride;
  CommercialIdentityFailure? clearRefreshFailure;
  int? failRefreshReplacementAt;
  int refreshReplacementCount = 0;
  final List<String> refreshReplacementValues = [];

  @override
  Future<CommercialCredentialRead<DeviceCredential>>
  readDeviceCredential() async => retired
      ? const CommercialCredentialRead(CommercialCredentialStoreState.retired)
      : device == null
      ? const CommercialCredentialRead(CommercialCredentialStoreState.empty)
      : CommercialCredentialRead(
          CommercialCredentialStoreState.valid,
          value: device,
        );

  @override
  Future<void> replaceDeviceCredential(
    DeviceCredential credential, {
    String? bindingFingerprint,
  }) async {
    if (failDeviceReplacement) {
      throw const CommercialIdentityFailure(
        kind: CommercialIdentityFailureKind.credentialPersistenceFailed,
        safeCode: 'IDENTITY_CREDENTIAL_PERSISTENCE_FAILED',
      );
    }
    device = credential;
    retired = false;
  }

  @override
  Future<void> retireDeviceCredential({required DeviceId deviceId}) async {
    device = null;
    retired = true;
  }

  @override
  Future<CommercialCredentialRead<RefreshCredential>>
  readRefreshCredential() async => refresh == null
      ? const CommercialCredentialRead(CommercialCredentialStoreState.empty)
      : CommercialCredentialRead(
          CommercialCredentialStoreState.valid,
          value: refresh,
        );

  @override
  Future<void> replaceRefreshCredential(RefreshCredential credential) async {
    refreshReplacementCount += 1;
    if (failRefreshReplacementAt == refreshReplacementCount) {
      throw const CommercialIdentityFailure(
        kind: CommercialIdentityFailureKind.credentialPersistenceFailed,
        safeCode: 'IDENTITY_CREDENTIAL_PERSISTENCE_FAILED',
      );
    }
    refreshReplacementValues.add(credential.value);
    refresh = credential;
  }

  @override
  Future<void> clearRefreshCredential() async {
    final failure = clearRefreshFailure;
    if (failure != null) {
      throw failure;
    }
    refresh = null;
  }

  @override
  Future<CommercialCredentialRead<CommercialActivationAttempt>>
  readActivationAttempt() async =>
      activationReadOverride ??
      (activationAttempt == null
          ? const CommercialCredentialRead(CommercialCredentialStoreState.empty)
          : CommercialCredentialRead(
              CommercialCredentialStoreState.valid,
              value: activationAttempt,
            ));

  @override
  Future<void> writeActivationAttempt(
    CommercialActivationAttempt attempt,
  ) async {
    activationAttempt = attempt;
  }

  @override
  Future<void> clearActivationAttempt() async {
    activationAttempt = null;
  }
}

final class _MemoryBindingPort implements CommercialIdentityBindingPort {
  CommercialAccountBinding? binding;
  CommercialIdentitySnapshot? snapshot;
  Completer<void>? bindBarrier;
  Object? bindFailure;
  int bindAttempts = 0;

  @override
  Future<CommercialAccountBinding?> readBinding() async => binding;

  @override
  Future<CommercialIdentitySnapshot?> readSnapshot() async => snapshot;

  @override
  Future<void> bindOrMatch(
    CommercialAccountBinding proposed,
    CommercialIdentitySnapshot proposedSnapshot,
  ) async {
    bindAttempts += 1;
    final barrier = bindBarrier;
    if (barrier != null) {
      await barrier.future;
    }
    final failure = bindFailure;
    if (failure != null) {
      throw failure;
    }
    if (binding != null && !binding!.matches(proposed)) {
      throw const CommercialIdentityFailure(
        kind: CommercialIdentityFailureKind.identityContextMismatch,
        safeCode: 'IDENTITY_CONTEXT_MISMATCH',
      );
    }
    binding ??= proposed;
    snapshot = proposedSnapshot;
  }
}

final class _FixedActivationKeys implements ActivationIdempotencyKeyGenerator {
  @override
  String generate() => 'stable-key';
}

DeviceCredential _device({String secret = 'device-secret', int version = 1}) =>
    DeviceCredential(
      deviceId: DeviceId(_deviceId),
      secret: secret,
      secretVersion: version,
    );

CommercialAccountBinding _binding(DateTime now) => CommercialAccountBinding(
  apiOrigin: 'https://identity.example.test/',
  workspaceId: WorkspaceId(_workspaceId),
  companyId: CompanyId(_companyId),
  defaultBranchId: BranchId(_branchId),
  userId: UserId(_userId),
  deviceId: DeviceId(_deviceId),
  firstBoundAtUtc: now,
);

CommercialIdentitySnapshot _snapshot(DateTime now) =>
    CommercialIdentitySnapshot(
      workspaceCode: 'FARM-ONE',
      userDisplayName: 'Owner One',
      role: CommercialRole.owner,
      deviceLabel: 'Yard tablet',
      lastConfirmedAtUtc: now,
    );

CommercialActivationAttempt _activationAttempt() => CommercialActivationAttempt(
  idempotencyKey: 'stable-key',
  workspaceCode: 'FARM-ONE',
  activationCode: 'ACT-SECRET',
  clientInstallationReference: 'installation-reference',
  deviceLabel: 'Yard tablet',
  platform: 'Android',
  createdAtUtc: DateTime.utc(2026, 8, 9, 10),
);

RefreshCredential _refresh(String value, DateTime now) => RefreshCredential(
  value: value,
  expiresAtUtc: now.add(const Duration(days: 1)),
  bindingFingerprint: _binding(now).fingerprint,
);

RefreshCredential _pendingPredecessor(String value, DateTime committedAt) {
  final digest = _sha256(value);
  return RefreshCredential(
    value: value,
    expiresAtUtc: committedAt.add(const Duration(days: 1)),
    bindingFingerprint: _binding(committedAt).fingerprint,
    predecessorDigest: digest,
    committedAtUtc: committedAt,
  );
}

String _sha256(String value) {
  // This fixed digest is SHA-256('refresh-1') and makes the marker itself
  // independently inspectable without importing coordinator internals.
  if (value == 'refresh-1') {
    return 'bd473e5dcdce2510c2df4f0c5a54a605fa3647fe74afae20a8cdb8b91b1a1f63';
  }
  throw ArgumentError.value(value);
}

CommercialAuthenticationResult _authentication(String refresh) =>
    CommercialAuthenticationResult(
      accessToken: AccessTokenLease(
        value: 'access-2',
        expiresAtUtc: DateTime.utc(2026, 8, 9, 11),
      ),
      refreshCredential: RefreshCredential(
        value: refresh,
        expiresAtUtc: DateTime.utc(2026, 8, 10, 10),
      ),
      context: _context(),
    );

CommercialIdentityContext _context() => CommercialIdentityContext(
  workspaceId: WorkspaceId(_workspaceId),
  workspaceCode: 'FARM-ONE',
  companyId: CompanyId(_companyId),
  defaultBranchId: BranchId(_branchId),
  userId: UserId(_userId),
  userDisplayName: 'Owner One',
  role: CommercialRole.owner,
  deviceId: DeviceId(_deviceId),
  deviceLabel: 'Yard tablet',
);

Map<String, Object?> _activationResponse({
  String secret = 'device-secret',
  int version = 1,
}) => <String, Object?>{
  'deviceId': _deviceId,
  'deviceSecret': secret,
  'secretVersion': version,
  'activatedAtUtc': '2026-08-09T10:00:00Z',
};

Map<String, Object?> _authResponse(
  String refresh, {
  String access = 'access-2',
  String accessExpiresAtUtc = '2026-08-09T11:00:00Z',
}) => <String, Object?>{
  'accessToken': access,
  'accessTokenExpiresAtUtc': accessExpiresAtUtc,
  'refreshToken': refresh,
  'refreshTokenExpiresAtUtc': '2026-08-10T10:00:00Z',
  ..._contextResponse(),
};

Map<String, Object?> _meResponse({String workspaceId = _workspaceId}) =>
    <String, Object?>{
      ..._contextResponse(workspaceId: workspaceId),
      'tokenFamilyId': _familyId,
    };

Map<String, Object?> _contextResponse({
  String workspaceId = _workspaceId,
}) => <String, Object?>{
  'user': <String, Object?>{
    'userId': _userId,
    'displayName': 'Owner One',
    'role': 'Owner',
  },
  'workspace': <String, Object?>{
    'workspaceId': workspaceId,
    'workspaceCode': 'FARM-ONE',
  },
  'company': <String, Object?>{
    'companyId': _companyId,
    'defaultBranchId': _branchId,
  },
  'device': <String, Object?>{'deviceId': _deviceId, 'label': 'Yard tablet'},
};

CommercialIdentityTransportResponse _ok(Map<String, Object?> body) =>
    CommercialIdentityTransportResponse(
      statusCode: 200,
      correlationId: _correlationId,
      jsonBody: body,
      headers: const {},
    );

CommercialIdentityTransportResponse _noContent() =>
    const CommercialIdentityTransportResponse(
      statusCode: 204,
      correlationId: _correlationId,
      jsonBody: null,
      headers: {},
    );

CommercialIdentityTransportResponse _error(int status, String code) =>
    CommercialIdentityTransportResponse(
      statusCode: status,
      correlationId: _correlationId,
      jsonBody: <String, Object?>{
        'error': <String, Object?>{
          'code': code,
          'message': 'Safe test message',
          'category': 'Conflict',
          'retryable': false,
        },
        'meta': <String, Object?>{'correlationId': _correlationId},
      },
      headers: const {},
    );

Future<void> _until(bool Function() condition) async {
  for (var attempt = 0; attempt < 100; attempt++) {
    if (condition()) {
      return;
    }
    await Future<void>.delayed(Duration.zero);
  }
  throw StateError('Condition was not reached.');
}
