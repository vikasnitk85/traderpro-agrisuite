import 'dart:async';

import 'package:flutter_test/flutter_test.dart';
import 'package:traderpro_agrisuite_mobile/core/database/database.dart';
import 'package:traderpro_agrisuite_mobile/features/procurement_poc/procurement_poc.dart';
import 'package:traderpro_agrisuite_mobile/features/receiving/receiving.dart';

import '../../support/offline_store_test_harness.dart';

const _enabled = ProcurementPocFeatureConfiguration(
  compileTimeEnabled: true,
  releaseMode: false,
);

void main() {
  test(
    'delayed profile lookup cannot bypass coordinator poller or control gates',
    () async {
      final database = LocalDatabaseOpeners.openInMemoryForTest();
      addTearDown(database.close);
      final repository = ProcurementPocLocalRepository(
        database: database,
        localReceivingStore: LocalReceivingStore(
          database: database,
          installReference: 'overlap-gates',
          clock: FixedLocalDeviceClock(),
          idGenerator: SequentialLocalIdGenerator(),
        ),
      );
      await repository.saveActiveProfile(
        PocDeviceProfile(
          backendBaseUrl: 'http://192.168.1.50:5000',
          workspaceId: '019fad0f-2d6a-7000-8000-0000000005a0',
          deviceId: '019fad0f-2d6a-7000-8000-0000000005a1',
          displayRole: PocDisplayRole.operator,
          createdAtUtc: DateTime.utc(2026, 7, 30, 10),
          updatedAtUtc: DateTime.utc(2026, 7, 30, 10),
        ),
      );
      final profiles = _DelayedProfileStore(repository);
      final api = _GateApi();
      final sync = ProcurementPocSyncEngine(
        feature: _enabled,
        profileStore: profiles,
        syncStore: repository,
        apiFactory: (_) => api,
      );
      final poller = ProcurementPocEventPoller(
        feature: _enabled,
        profileStore: profiles,
        eventStore: repository,
        apiFactory: (_) => api,
      );
      final controls = ProcurementPocControlService(
        feature: _enabled,
        profileStore: profiles,
        syncStore: repository,
        controlStore: repository,
        apiFactory: (_) => api,
        idGenerator: SequentialLocalIdGenerator(),
      );
      final coordinator = ProcurementPocCoordinator(
        profileStore: profiles,
        syncEngine: sync,
        eventPoller: poller,
        controlService: controls,
      )..setForeground(true);

      var before = profiles.loadCalls;
      var blocker = profiles.blockNextLoad();
      final automatic = coordinator.runAutomaticCycle();
      await blocker.started.future;
      await coordinator.runManualCycle();
      expect(profiles.loadCalls, before + 1);
      blocker.release.complete();
      await automatic;
      expect(api.readEventCalls, 1);

      before = api.readEventCalls;
      blocker = profiles.blockNextLoad();
      final cycleBeingPaused = coordinator.runAutomaticCycle();
      await blocker.started.future;
      coordinator.setAutomaticSyncPaused(true);
      blocker.release.complete();
      await cycleBeingPaused;
      expect(api.readEventCalls, before);
      coordinator.setAutomaticSyncPaused(false);

      before = profiles.loadCalls;
      blocker = profiles.blockNextLoad();
      final firstPoll = poller.pollOnce();
      await blocker.started.future;
      await poller.pollOnce();
      expect(profiles.loadCalls, before + 1);
      blocker.release.complete();
      await firstPoll;

      before = profiles.loadCalls;
      blocker = profiles.blockNextLoad();
      final firstExecution = controls.executePending();
      await blocker.started.future;
      await controls.executePending();
      expect(profiles.loadCalls, before + 1);
      blocker.release.complete();
      await firstExecution;

      before = profiles.loadCalls;
      blocker = profiles.blockNextLoad();
      final firstScheduling = controls.queueDueHeartbeats();
      await blocker.started.future;
      expect(await controls.queueDueHeartbeats(), 0);
      expect(profiles.loadCalls, before + 1);
      blocker.release.complete();
      await firstScheduling;
    },
  );
}

final class _LookupBlocker {
  final started = Completer<void>();
  final release = Completer<void>();
}

final class _DelayedProfileStore implements ProcurementPocProfileStore {
  _DelayedProfileStore(this.inner);

  final ProcurementPocProfileStore inner;
  var loadCalls = 0;
  _LookupBlocker? _blocker;

  _LookupBlocker blockNextLoad() => _blocker = _LookupBlocker();

  @override
  Future<PocDeviceProfile?> loadActiveProfile() async {
    loadCalls++;
    final blocker = _blocker;
    if (blocker != null) {
      _blocker = null;
      blocker.started.complete();
      await blocker.release.future;
    }
    return inner.loadActiveProfile();
  }

  @override
  Future<PocDeviceProfile> saveActiveProfile(PocDeviceProfile profile) =>
      inner.saveActiveProfile(profile);

  @override
  Future<PocDeviceProfile> setAutomaticSyncPaused(
    bool paused,
    DateTime nowUtc,
  ) => inner.setAutomaticSyncPaused(paused, nowUtc);
}

final class _GateApi implements ProcurementPocApi {
  var readEventCalls = 0;

  @override
  Future<MobileSyncEventPage> readEvents({
    required int after,
    required int limit,
  }) async {
    readEventCalls++;
    return MobileSyncEventPage(
      events: const [],
      nextCursor: after,
      hasMore: false,
    );
  }

  @override
  void dispose() {}

  @override
  dynamic noSuchMethod(Invocation invocation) => super.noSuchMethod(invocation);
}
