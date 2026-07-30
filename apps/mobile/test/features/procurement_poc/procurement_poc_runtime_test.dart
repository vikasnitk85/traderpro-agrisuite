import 'dart:async';
import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:traderpro_agrisuite_mobile/app/app.dart';
import 'package:traderpro_agrisuite_mobile/app/procurement_poc_runtime.dart';
import 'package:traderpro_agrisuite_mobile/core/database/database.dart';
import 'package:traderpro_agrisuite_mobile/core/measurements/weight_processing_method.dart';
import 'package:traderpro_agrisuite_mobile/features/procurement_poc/procurement_poc.dart';
import 'package:traderpro_agrisuite_mobile/features/receiving/receiving.dart';

const _enabled = ProcurementPocFeatureConfiguration(
  compileTimeEnabled: true,
  releaseMode: false,
);

void main() {
  test(
    'automatic cycles refresh operator state and serialize timer/manual work',
    () async {
      final database = LocalDatabaseOpeners.openInMemoryForTest();
      final scheduler = _RuntimeScheduler();
      final clock = _RuntimeClock(DateTime.utc(2026, 7, 30, 10));
      final api = _RuntimeApi(clock);
      final runtime = ProcurementPocRuntime.fromDatabase(
        database: database,
        feature: _enabled,
        scheduler: scheduler,
        clock: clock,
        apiFactory: (_) => api,
        bootstrapApiFactory: (_) => api,
      );
      final profile = await runtime.repository.saveActiveProfile(
        _profile(
          role: PocDisplayRole.operator,
          deviceId: '019fad0f-2d6a-7000-8000-0000000006a1',
          clock: clock,
        ),
      );
      await runtime.start();
      runtime.didChangeAppLifecycleState(AppLifecycleState.resumed);
      await runtime.controller.createLocalSession();

      await runtime.coordinator.runAutomaticCycle();

      expect(runtime.controller.activeCloudState!.cloudReference, 'RS-RUNTIME');
      expect(runtime.controller.activeCloudState!.leaseId, isNotNull);
      expect(
        runtime.controller.localOperations.single.status.storageValue,
        'Accepted',
      );
      expect(runtime.controller.diagnostics!.completed, 1);

      await runtime.controller.recordManualWeight(
        productReference: 'AUTO-REFRESH',
        bagTypeReference: 'JUTE',
        bagCount: 1,
        rawWeightKg: '10.00',
        decimalPlaces: 2,
        processingMethod: WeightProcessingMethod.standard,
        weightSource: WeightSource.manualSpike,
        capturedAtDeviceUtcText: '2026-07-30T10:00:00.000Z',
      );
      await runtime.coordinator.runAutomaticCycle();
      expect(
        runtime.controller.localOperations.where(
          (item) => item.status.storageValue == 'Accepted',
        ),
        hasLength(2),
      );

      final beforePause = api.snapshot;
      for (final state in [
        AppLifecycleState.inactive,
        AppLifecycleState.hidden,
        AppLifecycleState.paused,
        AppLifecycleState.detached,
      ]) {
        runtime.didChangeAppLifecycleState(state);
        await runtime.coordinator.runAutomaticCycle();
        await runtime.coordinator.runManualCycle();
      }
      expect(api.snapshot, beforePause);

      runtime.didChangeAppLifecycleState(AppLifecycleState.resumed);
      final blocker = api.blockNextPoll();
      final manual = runtime.coordinator.runManualCycle();
      await blocker.started.future;
      scheduler.fire();
      blocker.release.complete();
      await manual;
      await Future<void>.delayed(Duration.zero);
      expect(api.readEventCalls, beforePause.readEventCalls + 1);

      clock.value = clock.value.add(const Duration(minutes: 4));
      final queued = await Future.wait([
        runtime.controlService.queueDueHeartbeats(),
        runtime.controlService.queueDueHeartbeats(),
      ]);
      expect(queued.fold<int>(0, (sum, item) => sum + item), 1);
      expect(
        (await runtime.repository.listControlCommands(
          profile,
        )).where((item) => item.commandType == PocControlCommandType.heartbeat),
        hasLength(1),
      );
      final oldExpiry = runtime.controller.activeCloudState!.leaseExpiresAtUtc;
      await runtime.coordinator.runAutomaticCycle();
      expect(api.heartbeatCalls, 1);
      expect(
        runtime.controller.activeCloudState!.leaseExpiresAtUtc!.isAfter(
          oldExpiry!,
        ),
        isTrue,
      );

      final safeTimerFailure = Completer<void>();
      void observeSafeTimerFailure() {
        if (runtime.controller.diagnostics?.lastPollErrorCode ==
                'POC_EVENT_APPLY_FAILED' &&
            !safeTimerFailure.isCompleted) {
          safeTimerFailure.complete();
        }
      }

      runtime.controller.addListener(observeSafeTimerFailure);
      api.throwNextPoll = true;
      scheduler.fire();
      await safeTimerFailure.future.timeout(const Duration(seconds: 2));
      runtime.controller.removeListener(observeSafeTimerFailure);

      var notifications = 0;
      runtime.controller.addListener(() => notifications++);
      await runtime.disposeRuntime();
      await runtime.disposeRuntime();
      final callsAtDispose = api.snapshot;
      scheduler.fire();
      await Future<void>.delayed(Duration.zero);
      await runtime.coordinator.runAutomaticCycle();
      await runtime.controller.refreshAfterCycle(
        const ProcurementPocCycleNotification(
          automatic: true,
          message: 'must not notify',
        ),
      );
      expect(api.snapshot, callsAtDispose);
      expect(scheduler.cancelCount, 1);
      expect(notifications, 0);
      await expectLater(
        database.customSelect('SELECT 1').get(),
        throwsA(anything),
      );
    },
  );

  testWidgets(
    'owner polling refreshes projection and route pop keeps lifecycle owner',
    (tester) async {
      final database = LocalDatabaseOpeners.openInMemoryForTest();
      final scheduler = _RuntimeScheduler();
      final clock = _RuntimeClock(DateTime.utc(2026, 7, 30, 10));
      final api = _RuntimeApi(clock)
        ..events.add(
          MobileSyncEvent(
            sequence: 1,
            eventId: '019fad0f-2d6a-7000-8000-0000000006b1',
            eventType: 'ReceivingSessionPocStarted',
            eventVersion: 1,
            aggregateType: 'ReceivingSessionPoc',
            aggregateId: '019fad0f-2d6a-7000-8000-0000000006b2',
            aggregateVersion: 1,
            occurredAtUtc: clock.nowUtc(),
            correlationId: 'runtime-owner',
            payloadJson: jsonEncode(<String, Object?>{
              'sessionId': '019fad0f-2d6a-7000-8000-0000000006b2',
              'cloudReference': 'RS-OWNER-AUTO',
              'status': 'ReceivingInProgress',
              'editorDeviceId': '019fad0f-2d6a-7000-8000-0000000006b3',
              'leaseExpiresAtUtc': clock
                  .nowUtc()
                  .add(const Duration(minutes: 5))
                  .toIso8601String(),
              'version': 1,
            }),
          ),
        );
      final runtime = ProcurementPocRuntime.fromDatabase(
        database: database,
        feature: _enabled,
        scheduler: scheduler,
        clock: clock,
        apiFactory: (_) => api,
        bootstrapApiFactory: (_) => api,
      );
      await runtime.repository.saveActiveProfile(
        _profile(
          role: PocDisplayRole.owner,
          deviceId: '019fad0f-2d6a-7000-8000-0000000006b4',
          clock: clock,
        ),
      );
      await tester.pumpWidget(
        ProcurementPocRuntimeHost(
          runtime: runtime,
          child: TraderProAgriSuiteApp(
            procurementPocFeature: _enabled,
            procurementPocBuilder: (_) =>
                ProcurementPocHome(controller: runtime.controller),
          ),
        ),
      );
      await runtime.start();
      runtime.didChangeAppLifecycleState(AppLifecycleState.resumed);
      await runtime.coordinator.runAutomaticCycle();
      await tester.pump();
      expect(
        runtime.controller.remoteSessions.single.cloudReference,
        'RS-OWNER-AUTO',
      );

      await tester.tap(find.byKey(const Key('open-procurement-poc')));
      await tester.pumpAndSettle();
      expect(find.textContaining('RS-OWNER-AUTO'), findsOneWidget);
      Navigator.of(tester.element(find.byType(ProcurementPocHome))).pop();
      await tester.pumpAndSettle();
      final beforePopCycle = api.readEventCalls;
      await runtime.coordinator.runAutomaticCycle();
      expect(api.readEventCalls, beforePopCycle + 1);

      await tester.pumpWidget(const SizedBox.shrink());
      await tester.pump();
      await runtime.disposeRuntime();
      expect(scheduler.cancelCount, 1);
    },
  );
}

PocDeviceProfile _profile({
  required PocDisplayRole role,
  required String deviceId,
  required _RuntimeClock clock,
}) {
  return PocDeviceProfile(
    backendBaseUrl: 'http://192.168.1.50:5000',
    workspaceId: '019fad0f-2d6a-7000-8000-0000000006a0',
    deviceId: deviceId,
    displayRole: role,
    createdAtUtc: clock.nowUtc(),
    updatedAtUtc: clock.nowUtc(),
  );
}

final class _RuntimeClock implements ProcurementPocClock {
  _RuntimeClock(this.value);

  DateTime value;

  @override
  DateTime nowUtc() => value;
}

final class _RuntimeScheduler implements ProcurementPocScheduler {
  void Function()? callback;
  var cancelCount = 0;

  @override
  ProcurementPocScheduledTask schedulePeriodic(
    Duration interval,
    void Function() callback,
  ) {
    this.callback = callback;
    return _RuntimeTask(() => cancelCount++);
  }

  void fire() => callback?.call();
}

final class _RuntimeTask implements ProcurementPocScheduledTask {
  _RuntimeTask(this._onCancel);

  final void Function() _onCancel;
  var _cancelled = false;

  @override
  void cancel() {
    if (_cancelled) {
      return;
    }
    _cancelled = true;
    _onCancel();
  }
}

final class _PollBlocker {
  final started = Completer<void>();
  final release = Completer<void>();
}

final class _ApiSnapshot {
  const _ApiSnapshot({
    required this.operationCalls,
    required this.readEventCalls,
    required this.heartbeatCalls,
  });

  final int operationCalls;
  final int readEventCalls;
  final int heartbeatCalls;

  @override
  bool operator ==(Object other) {
    return other is _ApiSnapshot &&
        operationCalls == other.operationCalls &&
        readEventCalls == other.readEventCalls &&
        heartbeatCalls == other.heartbeatCalls;
  }

  @override
  int get hashCode =>
      Object.hash(operationCalls, readEventCalls, heartbeatCalls);
}

final class _RuntimeApi implements ProcurementPocApi {
  _RuntimeApi(this.clock);

  final _RuntimeClock clock;
  final events = <MobileSyncEvent>[];
  final _versions = <String, int>{};
  var operationCalls = 0;
  var readEventCalls = 0;
  var heartbeatCalls = 0;
  var throwNextPoll = false;
  _PollBlocker? _pollBlocker;

  _ApiSnapshot get snapshot => _ApiSnapshot(
    operationCalls: operationCalls,
    readEventCalls: readEventCalls,
    heartbeatCalls: heartbeatCalls,
  );

  _PollBlocker blockNextPoll() {
    return _pollBlocker = _PollBlocker();
  }

  @override
  Future<MobileSyncBatchResult> sendOperations(
    List<MobileSyncOperationEnvelope> operations,
  ) async {
    operationCalls++;
    final results = <MobileSyncOperationResult>[];
    for (final operation in operations) {
      final version = (_versions[operation.aggregateId] ?? 0) + 1;
      _versions[operation.aggregateId] = version;
      final submit = operation.operationType == 'SubmitReceivingSession';
      results.add(
        MobileSyncOperationResult(
          operationId: operation.operationId,
          aggregateId: operation.aggregateId,
          localSequence: operation.localSequence,
          resultStatus: 'Accepted',
          cloudAggregateVersion: version,
          cloudReference: 'RS-RUNTIME',
          leaseId: submit ? null : '019fad0f-2d6a-7000-8000-0000000006af',
          leaseExpiresAtUtc: submit
              ? null
              : clock.nowUtc().add(const Duration(minutes: 5)),
          errorCode: null,
          message: null,
        ),
      );
    }
    return MobileSyncBatchResult(
      operations: results,
      rawResponseJson: '{"runtime":true}',
    );
  }

  @override
  Future<MobileSyncEventPage> readEvents({
    required int after,
    required int limit,
  }) async {
    readEventCalls++;
    if (throwNextPoll) {
      throwNextPoll = false;
      throw StateError('deterministic timer failure');
    }
    final blocker = _pollBlocker;
    if (blocker != null) {
      _pollBlocker = null;
      blocker.started.complete();
      await blocker.release.future;
    }
    final selected = events
        .where((event) => event.sequence > after)
        .take(limit)
        .toList(growable: false);
    return MobileSyncEventPage(
      events: selected,
      nextCursor: selected.isEmpty ? after : selected.last.sequence,
      hasMore: false,
    );
  }

  @override
  Future<ProcurementPocCommandResult> heartbeat({
    required String sessionId,
    required String leaseId,
    required String idempotencyKey,
  }) async {
    heartbeatCalls++;
    final version = (_versions[sessionId] ?? 1) + 1;
    _versions[sessionId] = version;
    return ProcurementPocCommandResult(
      sessionId: sessionId,
      cloudReference: 'RS-RUNTIME',
      status: 'ReceivingInProgress',
      editorDeviceId: '019fad0f-2d6a-7000-8000-0000000006a1',
      leaseId: leaseId,
      leaseExpiresAtUtc: clock.nowUtc().add(const Duration(minutes: 5)),
      entryCount: 1,
      processedTotalWeightKg: '10.000000',
      version: version,
      finalizationId: null,
      idempotencyStatus: 'Accepted',
      rawResponseJson: '{"result":{"status":"ReceivingInProgress"}}',
    );
  }

  @override
  void dispose() {}

  @override
  dynamic noSuchMethod(Invocation invocation) => super.noSuchMethod(invocation);
}
