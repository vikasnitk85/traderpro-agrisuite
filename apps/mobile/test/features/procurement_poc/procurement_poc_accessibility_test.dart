import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:traderpro_agrisuite_mobile/core/database/database.dart';
import 'package:traderpro_agrisuite_mobile/features/procurement_poc/procurement_poc.dart';
import 'package:traderpro_agrisuite_mobile/features/receiving/receiving.dart';

import '../../support/offline_store_test_harness.dart';

const _phoneSize = Size(360, 640);

void main() {
  testWidgets('POC Setup has Scaffold and Material ancestry', (tester) async {
    final harness = await _AccessibilityHarness.create();
    addTearDown(harness.dispose);

    await _pumpPoc(tester, harness.controller, textScale: 1);

    final baseUrl = find.byKey(const Key('poc-base-url'));
    expect(find.byKey(const Key('poc-setup-scaffold')), findsOneWidget);
    expect(find.byType(SafeArea), findsWidgets);
    expect(baseUrl, findsOneWidget);
    expect(
      find.ancestor(
        of: baseUrl,
        matching: find.byKey(const Key('poc-setup-scaffold')),
      ),
      findsOneWidget,
    );
    expect(
      find.ancestor(of: baseUrl, matching: find.byType(Material)),
      findsWidgets,
    );
    expect(tester.takeException(), isNull);
  });

  testWidgets('POC base URL TextField receives text', (tester) async {
    final harness = await _AccessibilityHarness.create();
    addTearDown(harness.dispose);

    await _pumpPoc(tester, harness.controller, textScale: 1);
    final baseUrl = find.byKey(const Key('poc-base-url'));
    await tester.enterText(baseUrl, 'http://192.168.1.20:5000');
    await tester.pump();

    final field = tester.widget<TextField>(baseUrl);
    expect(field.controller!.text, 'http://192.168.1.20:5000');
    expect(tester.takeException(), isNull);
  });

  testWidgets('POC Setup remains usable at text scale 1.0', (tester) async {
    final harness = await _AccessibilityHarness.create();
    addTearDown(harness.dispose);

    await _pumpPoc(tester, harness.controller, textScale: 1);
    await _scrollUntilVisible(
      tester,
      screen: find.byKey(const Key('poc-setup-screen')),
      target: find.text('Save development profile'),
    );

    expect(find.text('Save development profile'), findsOneWidget);
    expect(tester.takeException(), isNull);
  });

  testWidgets('POC Setup remains usable at text scale 2.0', (tester) async {
    final harness = await _AccessibilityHarness.create();
    addTearDown(harness.dispose);

    await _pumpPoc(tester, harness.controller, textScale: 2);
    final screen = find.byKey(const Key('poc-setup-screen'));
    await _scrollUntilVisible(
      tester,
      screen: screen,
      target: find.byKey(const Key('poc-base-url')),
    );
    expect(find.byKey(const Key('poc-base-url')), findsOneWidget);
    await _scrollUntilVisible(
      tester,
      screen: screen,
      target: find.text('Save development profile'),
    );

    expect(find.text('Save development profile'), findsOneWidget);
    expect(tester.takeException(), isNull);
  });

  testWidgets('Operator screen remains usable at text scale 2.0', (
    tester,
  ) async {
    final harness = await _AccessibilityHarness.create(
      role: PocDisplayRole.operator,
      createLocalSession: true,
    );
    addTearDown(harness.dispose);

    await _pumpPoc(tester, harness.controller, textScale: 2);
    final screen = find.byKey(const Key('operator-session-screen'));
    expect(screen, findsOneWidget);
    expect(tester.takeException(), isNull);
    await _scrollUntilVisible(
      tester,
      screen: screen,
      target: find.byKey(const Key('save-manual-entry')),
    );

    expect(find.byKey(const Key('save-manual-entry')), findsOneWidget);
    expect(tester.takeException(), isNull);
  });

  testWidgets('Owner list and live view remain usable at text scale 2.0', (
    tester,
  ) async {
    final harness = await _AccessibilityHarness.create(
      role: PocDisplayRole.owner,
      createRemoteSession: true,
    );
    addTearDown(harness.dispose);

    await _pumpPoc(tester, harness.controller, textScale: 2);
    final listScreen = find.byKey(const Key('owner-session-list-screen'));
    final session = find.textContaining('RS-POC-ACCESSIBLE');
    expect(listScreen, findsOneWidget);
    await _scrollUntilVisible(tester, screen: listScreen, target: session);
    await tester.tap(session);
    await tester.pumpAndSettle();

    final liveScreen = find.byKey(const Key('owner-live-view-screen'));
    expect(liveScreen, findsOneWidget);
    await _scrollUntilVisible(
      tester,
      screen: liveScreen,
      target: find.byKey(const Key('owner-approve')),
    );

    expect(find.byKey(const Key('owner-approve')), findsOneWidget);
    expect(tester.takeException(), isNull);
  });
}

Future<void> _pumpPoc(
  WidgetTester tester,
  ProcurementPocController controller, {
  required double textScale,
}) async {
  tester.view.physicalSize = _phoneSize;
  tester.view.devicePixelRatio = 1;
  addTearDown(tester.view.resetPhysicalSize);
  addTearDown(tester.view.resetDevicePixelRatio);

  await tester.pumpWidget(
    MaterialApp(
      builder: (context, child) => MediaQuery(
        data: MediaQuery.of(
          context,
        ).copyWith(textScaler: TextScaler.linear(textScale)),
        child: child!,
      ),
      home: ProcurementPocHome(controller: controller),
    ),
  );
  await tester.pumpAndSettle();
}

Future<void> _scrollUntilVisible(
  WidgetTester tester, {
  required Finder screen,
  required Finder target,
}) async {
  final scrollable = find
      .descendant(of: screen, matching: find.byType(Scrollable))
      .first;
  await tester.scrollUntilVisible(target, 300, scrollable: scrollable);
  await tester.pumpAndSettle();
}

final class _AccessibilityHarness {
  _AccessibilityHarness({required this.database, required this.controller});

  final TraderProLocalDatabase database;
  final ProcurementPocController controller;

  static Future<_AccessibilityHarness> create({
    PocDisplayRole? role,
    bool createLocalSession = false,
    bool createRemoteSession = false,
  }) async {
    final database = LocalDatabaseOpeners.openInMemoryForTest();
    final clock = _AccessibilityClock(DateTime.utc(2026, 7, 30, 10));
    final receiving = LocalReceivingStore(
      database: database,
      installReference: 'poc-accessibility',
      clock: FixedLocalDeviceClock(),
      idGenerator: SequentialLocalIdGenerator(),
    );
    final repository = ProcurementPocLocalRepository(
      database: database,
      localReceivingStore: receiving,
    );

    if (role != null) {
      final profile = await repository.saveActiveProfile(
        PocDeviceProfile(
          backendBaseUrl: 'http://192.168.1.50:5000',
          workspaceId: '019fad0f-2d6a-7000-8000-000000000700',
          deviceId: '019fad0f-2d6a-7000-8000-000000000701',
          displayRole: role,
          displayLabel: 'Accessibility device',
          createdAtUtc: clock.nowUtc(),
          updatedAtUtc: clock.nowUtc(),
        ),
      );
      if (createRemoteSession) {
        await repository.saveSessionList(
          profile: profile,
          sessions: [
            RemoteReceivingSessionProjection(
              sessionId: '019fad0f-2d6a-7000-8000-000000000710',
              cloudReference: 'RS-POC-ACCESSIBLE',
              status: 'SubmittedForReview',
              editorDeviceId: '019fad0f-2d6a-7000-8000-000000000711',
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
      }
    }

    final controller = _controller(repository, receiving, clock);
    if (role != null) {
      await controller.initialize();
    }
    if (createLocalSession) {
      await controller.createLocalSession();
    }
    return _AccessibilityHarness(database: database, controller: controller);
  }

  Future<void> dispose() async {
    controller.dispose();
    await database.close();
  }
}

ProcurementPocController _controller(
  ProcurementPocLocalRepository repository,
  LocalReceivingStore receiving,
  _AccessibilityClock clock,
) {
  const feature = ProcurementPocFeatureConfiguration(
    compileTimeEnabled: true,
    releaseMode: false,
  );
  ProcurementPocApi factory(PocDeviceProfile _) => _AccessibilityApi();
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
    bootstrapApiFactory: (_) => _AccessibilityApi(),
    clock: clock,
  );
}

final class _AccessibilityClock implements ProcurementPocClock {
  _AccessibilityClock(this.value);

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

final class _AccessibilityApi implements ProcurementPocApi {
  @override
  void dispose() {}

  @override
  dynamic noSuchMethod(Invocation invocation) => super.noSuchMethod(invocation);
}
