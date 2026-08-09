import 'dart:async';

import 'package:flutter_test/flutter_test.dart';
import 'package:traderpro_agrisuite_mobile/app/commercial_secure_startup_state.dart';
import 'package:traderpro_agrisuite_mobile/core/security/commercial_storage_failure.dart';
import 'package:traderpro_agrisuite_mobile/infrastructure/runtime/commercial_secure_runtime.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  test(
    'concurrent callers share one open and close happens exactly once',
    () async {
      final opening = Completer<void>();
      var openCount = 0;
      var closeCount = 0;
      final runtime = CommercialSecureRuntime(
        storageOpener: () {
          openCount += 1;
          return opening.future;
        },
        storageCloser: () async {
          closeCount += 1;
        },
        observeApplicationLifecycle: false,
      );

      final first = runtime.open();
      final second = runtime.open();
      expect(identical(first, second), isTrue);
      expect(openCount, 1);

      opening.complete();
      expect((await first).status, CommercialSecureStartupStatus.ready);
      expect((await second).status, CommercialSecureStartupStatus.ready);

      final firstClose = runtime.close();
      final secondClose = runtime.close();
      expect(identical(firstClose, secondClose), isTrue);
      await firstClose;
      expect(closeCount, 1);
      expect(
        runtime.startupState?.status,
        CommercialSecureStartupStatus.closed,
      );
    },
  );

  test('typed failures expose only their stable safe code', () async {
    final runtime = CommercialSecureRuntime(
      storageOpener: () async {
        throw const CommercialStorageException(
          CommercialStorageFailureCode.secureStoreUnavailable,
          phase: 'platform-message-must-not-cross-boundary',
        );
      },
      storageCloser: () async {},
      observeApplicationLifecycle: false,
    );

    final state = await runtime.open();
    expect(state.status, CommercialSecureStartupStatus.unavailable);
    expect(
      state.safeFailureCode,
      CommercialStorageFailureCode.secureStoreUnavailable.safeCode,
    );
    expect(state.safeFailureCode, isNot(contains('platform-message')));
    await runtime.close();
  });

  test('unexpected failures map to a stable redacted code', () async {
    final runtime = CommercialSecureRuntime(
      storageOpener: () async => throw StateError('sensitive native detail'),
      storageCloser: () async {},
      observeApplicationLifecycle: false,
    );

    final state = await runtime.open();
    expect(
      state.safeFailureCode,
      CommercialStorageFailureCode.unexpectedStorageFailure.safeCode,
    );
    expect(state.safeFailureCode, isNot(contains('sensitive')));
    await runtime.close();
  });
}
