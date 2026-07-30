import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:traderpro_agrisuite_mobile/core/database/database.dart';
import 'package:traderpro_agrisuite_mobile/core/measurements/weight_processing_method.dart';
import 'package:traderpro_agrisuite_mobile/features/procurement_poc/procurement_poc.dart';
import 'package:traderpro_agrisuite_mobile/features/receiving/receiving.dart';

import '../../support/offline_store_test_harness.dart';

void main() {
  testWidgets('Owner is read-only and Operator has no owner controls', (
    tester,
  ) async {
    final database = LocalDatabaseOpeners.openInMemoryForTest();
    addTearDown(database.close);
    final clock = _PresentationClock(DateTime.utc(2026, 7, 29, 10));
    final receiving = LocalReceivingStore(
      database: database,
      installReference: 'widgets',
      clock: FixedLocalDeviceClock(),
      idGenerator: SequentialLocalIdGenerator(),
    );
    final repository = ProcurementPocLocalRepository(
      database: database,
      localReceivingStore: receiving,
    );
    var profile = PocDeviceProfile(
      backendBaseUrl: 'http://192.168.1.50:5000',
      workspaceId: '019fad0f-2d6a-7000-8000-000000000400',
      deviceId: '019fad0f-2d6a-7000-8000-000000000401',
      displayRole: PocDisplayRole.owner,
      createdAtUtc: clock.nowUtc(),
      updatedAtUtc: clock.nowUtc(),
    );
    await repository.saveActiveProfile(profile);
    await repository.saveSessionList(
      profile: profile,
      sessions: [
        RemoteReceivingSessionProjection(
          sessionId: '019fad0f-2d6a-7000-8000-000000000410',
          cloudReference: 'RS-POC-000410',
          status: 'SubmittedForReview',
          editorDeviceId: '019fad0f-2d6a-7000-8000-000000000411',
          leaseExpiresAtUtc: null,
          entryCount: 5,
          processedTotalWeightKg: '250.000000',
          cloudVersion: 7,
          approvedByDeviceId: null,
          finalizationId: null,
          lastCloudUpdateAtUtc: clock.nowUtc(),
        ),
      ],
    );
    var controller = _controller(repository, receiving, clock);
    await controller.initialize();

    await tester.pumpWidget(
      MaterialApp(home: ProcurementPocHome(controller: controller)),
    );
    await tester.pumpAndSettle();
    expect(find.byKey(const Key('owner-session-list-screen')), findsOneWidget);
    expect(find.byKey(const Key('operator-entry-form')), findsNothing);
    expect(find.byKey(const Key('operator-submit')), findsNothing);
    await tester.tap(find.textContaining('RS-POC-000410'));
    await tester.pumpAndSettle();
    expect(find.byKey(const Key('owner-live-view-screen')), findsOneWidget);
    expect(find.byKey(const Key('owner-approve')), findsOneWidget);
    expect(find.byKey(const Key('operator-entry-form')), findsNothing);
    expect(find.byKey(const Key('operator-submit')), findsNothing);
    Navigator.of(
      tester.element(find.byKey(const Key('owner-live-view-screen'))),
    ).pop();
    await tester.pumpAndSettle();
    controller.dispose();

    profile = profile.copyWith(
      displayRole: PocDisplayRole.operator,
      updatedAtUtc: clock.nowUtc(),
    );
    await repository.saveActiveProfile(profile);
    controller = _controller(repository, receiving, clock);
    await controller.initialize();
    await tester.pumpWidget(
      MaterialApp(home: ProcurementPocHome(controller: controller)),
    );
    await tester.pumpAndSettle();
    expect(find.byKey(const Key('operator-session-screen')), findsOneWidget);
    expect(find.byKey(const Key('owner-approve')), findsNothing);
    expect(find.byKey(const Key('owner-finalize')), findsNothing);
    expect(find.byKey(const Key('owner-retry-finalization')), findsNothing);
    controller.dispose();
  });

  test(
    'missing, non-UTC, and malformed capture times create no facts',
    () async {
      final database = LocalDatabaseOpeners.openInMemoryForTest();
      addTearDown(database.close);
      final clock = _PresentationClock(DateTime.utc(2026, 7, 30, 10));
      final receiving = LocalReceivingStore(
        database: database,
        installReference: 'utc-validation',
        clock: FixedLocalDeviceClock(),
        idGenerator: SequentialLocalIdGenerator(),
      );
      final repository = ProcurementPocLocalRepository(
        database: database,
        localReceivingStore: receiving,
      );
      final profile = await repository.saveActiveProfile(
        PocDeviceProfile(
          backendBaseUrl: 'http://192.168.1.50:5000',
          workspaceId: '019fad0f-2d6a-7000-8000-0000000004a0',
          deviceId: '019fad0f-2d6a-7000-8000-0000000004a1',
          displayRole: PocDisplayRole.operator,
          createdAtUtc: clock.nowUtc(),
          updatedAtUtc: clock.nowUtc(),
        ),
      );
      final controller = _controller(repository, receiving, clock);
      addTearDown(controller.dispose);
      await controller.initialize();
      await controller.createLocalSession();
      final sessionId = controller.activeLocalSession!.id;

      for (final timestamp in [
        '',
        '2026-07-30T10:00:00.000',
        'not-a-timestamp',
      ]) {
        await controller.recordManualWeight(
          productReference: 'UTC-TEST',
          bagTypeReference: 'JUTE',
          bagCount: 1,
          rawWeightKg: '10.00',
          decimalPlaces: 2,
          processingMethod: WeightProcessingMethod.standard,
          weightSource: WeightSource.manualSpike,
          capturedAtDeviceUtcText: timestamp,
        );
        expect(
          controller.lastErrorCode,
          ProcurementPocLocalException.captureTimestampInvalid,
        );
        expect(await repository.listLocalReceivingEntries(sessionId), isEmpty);
        expect(
          await repository.listLocalOutboxOperations(aggregateId: sessionId),
          hasLength(1),
        );
      }
      expect(profile.deviceId, controller.profile!.deviceId);
    },
  );

  testWidgets(
    'finalization replay is visible only for this device completed command',
    (tester) async {
      final database = LocalDatabaseOpeners.openInMemoryForTest();
      addTearDown(database.close);
      final clock = _PresentationClock(DateTime.utc(2026, 7, 30, 10));
      final receiving = LocalReceivingStore(
        database: database,
        installReference: 'replay-visibility',
        clock: FixedLocalDeviceClock(),
        idGenerator: SequentialLocalIdGenerator(),
      );
      final repository = ProcurementPocLocalRepository(
        database: database,
        localReceivingStore: receiving,
      );
      final profileA = await repository.saveActiveProfile(
        PocDeviceProfile(
          backendBaseUrl: 'http://192.168.1.50:5000',
          workspaceId: '019fad0f-2d6a-7000-8000-0000000004b0',
          deviceId: '019fad0f-2d6a-7000-8000-0000000004b1',
          displayRole: PocDisplayRole.owner,
          createdAtUtc: clock.nowUtc(),
          updatedAtUtc: clock.nowUtc(),
        ),
      );
      const sessionId = '019fad0f-2d6a-7000-8000-0000000004b2';
      const commandId = '019fad0f-2d6a-7000-8000-0000000004b3';
      await repository.saveSessionList(
        profile: profileA,
        sessions: [
          RemoteReceivingSessionProjection(
            sessionId: sessionId,
            cloudReference: 'RS-POC-REPLAY',
            status: 'Finalized',
            editorDeviceId: '019fad0f-2d6a-7000-8000-0000000004b4',
            leaseExpiresAtUtc: null,
            entryCount: 5,
            processedTotalWeightKg: '50.000000',
            cloudVersion: 9,
            approvedByDeviceId: profileA.deviceId,
            finalizationId: '019fad0f-2d6a-7000-8000-0000000004b5',
            lastCloudUpdateAtUtc: clock.nowUtc(),
          ),
        ],
      );
      var controller = _controller(repository, receiving, clock);
      await controller.initialize();
      await controller.openRemoteSession(sessionId);
      await tester.pumpWidget(
        MaterialApp(home: OwnerLiveViewScreen(controller: controller)),
      );
      await tester.pumpAndSettle();
      expect(find.byKey(const Key('owner-retry-finalization')), findsNothing);
      controller.dispose();

      await database.customStatement(
        'INSERT INTO poc_control_commands '
        '(command_id, source_key, acting_device_id, command_type, session_id, '
        'expected_cloud_version, lease_id, status, attempt_count, '
        'next_attempt_at_utc, last_attempt_at_utc, last_error_code, '
        'last_error_message, successful_response_json, created_at_utc, '
        'updated_at_utc) VALUES '
        "(?, ?, ?, 'Finalize', ?, 8, NULL, 'Completed', 1, NULL, ?, NULL, "
        "NULL, '{\"result\":{\"status\":\"Finalized\"}}', ?, ?)",
        [
          commandId,
          profileA.sourceKey,
          profileA.deviceId,
          sessionId,
          clock.nowUtc().toIso8601String(),
          clock.nowUtc().toIso8601String(),
          clock.nowUtc().toIso8601String(),
        ],
      );
      controller = _controller(repository, receiving, clock);
      await controller.initialize();
      await controller.openRemoteSession(sessionId);
      await tester.pumpWidget(
        MaterialApp(home: OwnerLiveViewScreen(controller: controller)),
      );
      await tester.pumpAndSettle();
      expect(find.byKey(const Key('owner-retry-finalization')), findsOneWidget);
      expect(controller.controlCommands.single.commandId, commandId);
      controller.dispose();

      final profileB = await repository.saveActiveProfile(
        PocDeviceProfile(
          backendBaseUrl: profileA.backendBaseUrl,
          workspaceId: profileA.workspaceId,
          deviceId: '019fad0f-2d6a-7000-8000-0000000004b6',
          displayRole: PocDisplayRole.owner,
          createdAtUtc: profileA.createdAtUtc,
          updatedAtUtc: clock.nowUtc(),
        ),
      );
      controller = _controller(repository, receiving, clock);
      await controller.initialize();
      await controller.openRemoteSession(sessionId);
      await tester.pumpWidget(
        MaterialApp(home: OwnerLiveViewScreen(controller: controller)),
      );
      await tester.pumpAndSettle();
      expect(find.byKey(const Key('owner-retry-finalization')), findsNothing);
      expect(await repository.listControlCommands(profileB), isEmpty);
      controller.dispose();
    },
  );
}

ProcurementPocController _controller(
  ProcurementPocLocalRepository repository,
  LocalReceivingStore receiving,
  _PresentationClock clock,
) {
  const feature = ProcurementPocFeatureConfiguration(
    compileTimeEnabled: true,
    releaseMode: false,
  );
  ProcurementPocApi factory(PocDeviceProfile _) => _PresentationApi();
  final sync = ProcurementPocSyncEngine(
    feature: feature,
    profileStore: repository,
    syncStore: repository,
    apiFactory: factory,
    clock: clock,
  );
  final events = ProcurementPocEventPoller(
    feature: feature,
    profileStore: repository,
    eventStore: repository,
    apiFactory: factory,
    clock: clock,
  );
  final controls = ProcurementPocControlService(
    feature: feature,
    profileStore: repository,
    syncStore: repository,
    controlStore: repository,
    apiFactory: factory,
    clock: clock,
    idGenerator: SequentialLocalIdGenerator(),
  );
  final coordinator = ProcurementPocCoordinator(
    profileStore: repository,
    syncEngine: sync,
    eventPoller: events,
    controlService: controls,
    scheduler: _NoopScheduler(),
  );
  return ProcurementPocController(
    profileStore: repository,
    syncStore: repository,
    readStore: repository,
    receivingStore: receiving,
    controlService: controls,
    coordinator: coordinator,
    apiFactory: factory,
    bootstrapApiFactory: (_) => _PresentationApi(),
    clock: clock,
  );
}

final class _PresentationClock implements ProcurementPocClock {
  _PresentationClock(this.value);

  DateTime value;

  @override
  DateTime nowUtc() => value;
}

final class _NoopScheduler implements ProcurementPocScheduler {
  @override
  ProcurementPocScheduledTask schedulePeriodic(
    Duration interval,
    void Function() callback,
  ) {
    return _NoopTask();
  }
}

final class _NoopTask implements ProcurementPocScheduledTask {
  @override
  void cancel() {}
}

final class _PresentationApi implements ProcurementPocApi {
  @override
  void dispose() {}

  @override
  dynamic noSuchMethod(Invocation invocation) => super.noSuchMethod(invocation);
}
