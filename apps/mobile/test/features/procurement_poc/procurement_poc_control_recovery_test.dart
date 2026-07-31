import 'dart:async';
import 'dart:io';

import 'package:flutter_test/flutter_test.dart';
import 'package:traderpro_agrisuite_mobile/core/database/database.dart';
import 'package:traderpro_agrisuite_mobile/features/procurement_poc/procurement_poc.dart';
import 'package:traderpro_agrisuite_mobile/features/receiving/receiving.dart';

import '../../support/offline_store_test_harness.dart';

void main() {
  test(
    'approval retains its key after timeout and surfaces stale version',
    () async {
      final database = LocalDatabaseOpeners.openInMemoryForTest();
      addTearDown(database.close);
      final clock = _ControlClock(DateTime.utc(2026, 7, 29, 10));
      final ids = SequentialLocalIdGenerator();
      final repository = _repository(database);
      final profile = PocDeviceProfile(
        backendBaseUrl: 'http://192.168.1.50:5000',
        workspaceId: '019fad0f-2d6a-7000-8000-000000000320',
        deviceId: '019fad0f-2d6a-7000-8000-000000000321',
        displayRole: PocDisplayRole.owner,
        createdAtUtc: clock.nowUtc(),
        updatedAtUtc: clock.nowUtc(),
      );
      await repository.saveActiveProfile(profile);
      final api = _ApprovalApi()..dropFirstResponse = true;
      ProcurementPocControlService buildService() {
        return ProcurementPocControlService(
          feature: const ProcurementPocFeatureConfiguration(
            compileTimeEnabled: true,
            releaseMode: false,
          ),
          profileStore: repository,
          syncStore: repository,
          controlStore: repository,
          apiFactory: (_) => api,
          clock: clock,
          idGenerator: ids,
        );
      }

      var service = buildService();
      final approval = await service.queueApproval(
        sessionId: '019fad0f-2d6a-7000-8000-000000000322',
        expectedVersion: 7,
      );
      expect((await service.executePending()).pending, 1);
      service = buildService();
      expect((await service.executePending()).completed, 1);
      expect(api.keys, [approval.commandId, approval.commandId]);

      api.staleVersion = true;
      final stale = await service.queueApproval(
        sessionId: '019fad0f-2d6a-7000-8000-000000000323',
        expectedVersion: 2,
      );
      expect((await service.executePending()).needsAttention, 1);
      final stored = (await repository.listControlCommands(
        profile,
      )).singleWhere((command) => command.commandId == stale.commandId);
      expect(stored.status, PocControlCommandStatus.needsAttention);
      expect(stored.lastErrorCode, 'RECEIVING_POC_VERSION_CONFLICT');
    },
  );

  test(
    'finalization timeout, restart, and explicit replay reuse one key',
    () async {
      final directory = await Directory.systemTemp.createTemp(
        'traderpro-poc-finalize-',
      );
      final file = File(
        '${directory.path}${Platform.pathSeparator}local.sqlite',
      );
      final clock = _ControlClock(DateTime.utc(2026, 7, 29, 10));
      final ids = SequentialLocalIdGenerator();
      final api = _FinalizationApi()..dropFirstResponse = true;
      var database = LocalDatabaseOpeners.openFileForTest(file);
      var repository = _repository(database);
      const sessionId = '019fad0f-2d6a-7000-8000-000000000310';
      final profile = PocDeviceProfile(
        backendBaseUrl: 'http://192.168.1.50:5000',
        workspaceId: '019fad0f-2d6a-7000-8000-000000000300',
        deviceId: '019fad0f-2d6a-7000-8000-000000000301',
        displayRole: PocDisplayRole.owner,
        createdAtUtc: clock.nowUtc(),
        updatedAtUtc: clock.nowUtc(),
      );
      try {
        await repository.saveActiveProfile(profile);
        var service = _service(repository, api, clock, ids);
        final queued = await service.queueFinalization(
          sessionId: sessionId,
          expectedVersion: 5,
        );
        final firstRun = await service.executePending();
        expect(firstRun.pending, 1);
        expect(api.keys, [queued.commandId]);
        final afterLoss = (await repository.listControlCommands(
          profile,
        )).single;
        expect(afterLoss.status, PocControlCommandStatus.pending);
        expect(afterLoss.commandId, queued.commandId);

        await database.close();
        database = LocalDatabaseOpeners.openFileForTest(file);
        repository = _repository(database);
        service = _service(repository, api, clock, ids);
        final recovered = await service.executePending();
        expect(recovered.completed, 1);
        expect(api.keys, [queued.commandId, queued.commandId]);
        var commands = await repository.listControlCommands(profile);
        expect(commands, hasLength(1));
        expect(commands.single.status, PocControlCommandStatus.completed);
        expect(
          commands.single.successfulResponseJson,
          contains('019fad0f-2d6a-7000-8000-000000000399'),
        );
        expect(
          (await repository.getRemoteSession(
            profile,
            sessionId,
          ))!.finalizationId,
          '019fad0f-2d6a-7000-8000-000000000399',
        );

        final replay = await service.retrySameFinalization(sessionId);
        expect(replay.commandId, queued.commandId);
        final replayed = await service.executePending();
        expect(replayed.completed, 1);
        expect(api.keys, [
          queued.commandId,
          queued.commandId,
          queued.commandId,
        ]);
        commands = await repository.listControlCommands(profile);
        expect(commands, hasLength(1));
        expect(commands.single.commandId, queued.commandId);
        expect(commands.single.status, PocControlCommandStatus.completed);
        expect(api.finalizationIds.toSet(), {
          '019fad0f-2d6a-7000-8000-000000000399',
        });
      } finally {
        await database.close();
        if (directory.existsSync()) {
          await directory.delete(recursive: true);
        }
      }
    },
  );

  test(
    'malformed successful approval and finalization remain retryable',
    () async {
      final database = LocalDatabaseOpeners.openInMemoryForTest();
      addTearDown(database.close);
      final clock = _ControlClock(DateTime.utc(2026, 7, 30, 10));
      final repository = _repository(database);
      final profile = await repository.saveActiveProfile(
        PocDeviceProfile(
          backendBaseUrl: 'http://192.168.1.50:5000',
          workspaceId: '019fad0f-2d6a-7000-8000-0000000003a0',
          deviceId: '019fad0f-2d6a-7000-8000-0000000003a1',
          displayRole: PocDisplayRole.owner,
          createdAtUtc: clock.nowUtc(),
          updatedAtUtc: clock.nowUtc(),
        ),
      );
      final api = _MalformedControlApi();
      final service = ProcurementPocControlService(
        feature: const ProcurementPocFeatureConfiguration(
          compileTimeEnabled: true,
          releaseMode: false,
        ),
        profileStore: repository,
        syncStore: repository,
        controlStore: repository,
        apiFactory: (_) => api,
        clock: clock,
        idGenerator: SequentialLocalIdGenerator(),
      );
      final approval = await service.queueApproval(
        sessionId: '019fad0f-2d6a-7000-8000-0000000003a2',
        expectedVersion: 4,
      );
      final finalization = await service.queueFinalization(
        sessionId: '019fad0f-2d6a-7000-8000-0000000003a3',
        expectedVersion: 8,
      );

      final result = await service.executePending();

      expect(result.pending, 2);
      expect(result.completed, 0);
      final commands = await repository.listControlCommands(profile);
      expect(commands.map((item) => item.status).toSet(), {
        PocControlCommandStatus.pending,
      });
      expect(
        commands.every((item) => item.successfulResponseJson == null),
        isTrue,
      );
      expect(api.keys, [approval.commandId, finalization.commandId]);
    },
  );

  test('completed finalization cannot cross device context', () async {
    final database = LocalDatabaseOpeners.openInMemoryForTest();
    addTearDown(database.close);
    final clock = _ControlClock(DateTime.utc(2026, 7, 30, 10));
    final repository = _repository(database);
    final profileA = await repository.saveActiveProfile(
      PocDeviceProfile(
        backendBaseUrl: 'http://192.168.1.50:5000',
        workspaceId: '019fad0f-2d6a-7000-8000-0000000003b0',
        deviceId: '019fad0f-2d6a-7000-8000-0000000003b1',
        displayRole: PocDisplayRole.owner,
        createdAtUtc: clock.nowUtc(),
        updatedAtUtc: clock.nowUtc(),
      ),
    );
    final api = _FinalizationApi();
    var service = _service(
      repository,
      api,
      clock,
      SequentialLocalIdGenerator(),
    );
    const sessionId = '019fad0f-2d6a-7000-8000-0000000003b2';
    final original = await service.queueFinalization(
      sessionId: sessionId,
      expectedVersion: 5,
    );
    expect((await service.executePending()).completed, 1);

    final profileB = await repository.saveActiveProfile(
      PocDeviceProfile(
        backendBaseUrl: profileA.backendBaseUrl,
        workspaceId: profileA.workspaceId,
        deviceId: '019fad0f-2d6a-7000-8000-0000000003b3',
        displayRole: PocDisplayRole.owner,
        createdAtUtc: profileA.createdAtUtc,
        updatedAtUtc: clock.nowUtc(),
      ),
    );
    service = _service(repository, api, clock, SequentialLocalIdGenerator());
    await expectLater(
      service.retrySameFinalization(sessionId),
      throwsA(isA<StateError>()),
    );
    expect(await repository.listControlCommands(profileB), isEmpty);

    await repository.saveActiveProfile(profileA);
    service = _service(repository, api, clock, SequentialLocalIdGenerator());
    final replay = await service.retrySameFinalization(sessionId);
    expect(replay.commandId, original.commandId);
    expect((await service.executePending()).completed, 1);
    expect(api.keys, [original.commandId, original.commandId]);
  });
}

ProcurementPocLocalRepository _repository(TraderProLocalDatabase database) {
  return ProcurementPocLocalRepository(
    database: database,
    localReceivingStore: LocalReceivingStore(
      database: database,
      installReference: 'control',
      clock: FixedLocalDeviceClock(),
      idGenerator: SequentialLocalIdGenerator(),
    ),
  );
}

ProcurementPocControlService _service(
  ProcurementPocLocalRepository repository,
  _FinalizationApi api,
  _ControlClock clock,
  SequentialLocalIdGenerator ids,
) {
  return ProcurementPocControlService(
    feature: const ProcurementPocFeatureConfiguration(
      compileTimeEnabled: true,
      releaseMode: false,
    ),
    profileStore: repository,
    syncStore: repository,
    controlStore: repository,
    apiFactory: (_) => api,
    clock: clock,
    idGenerator: ids,
  );
}

final class _ControlClock implements ProcurementPocClock {
  _ControlClock(this.value);

  DateTime value;

  @override
  DateTime nowUtc() => value;
}

final class _FinalizationApi implements ProcurementPocApi {
  var dropFirstResponse = false;
  var _dropped = false;
  final keys = <String>[];
  final finalizationIds = <String>[];

  @override
  Future<ProcurementPocCommandResult> finalize({
    required String sessionId,
    required int expectedVersion,
    required String idempotencyKey,
  }) async {
    keys.add(idempotencyKey);
    const finalizationId = '019fad0f-2d6a-7000-8000-000000000399';
    finalizationIds.add(finalizationId);
    if (dropFirstResponse && !_dropped) {
      _dropped = true;
      throw TimeoutException(
        'Deterministic timeout after finalization may have committed.',
      );
    }
    return ProcurementPocCommandResult(
      sessionId: sessionId,
      cloudReference: 'RS-POC-000310',
      status: 'Finalized',
      editorDeviceId: '019fad0f-2d6a-7000-8000-000000000311',
      leaseId: null,
      leaseExpiresAtUtc: null,
      entryCount: 5,
      processedTotalWeightKg: '250.000000',
      version: 6,
      finalizationId: finalizationId,
      idempotencyStatus: keys.length == 1 ? 'Accepted' : 'PreviouslyProcessed',
      rawResponseJson:
          '{"result":{"finalizationId":"$finalizationId",'
          '"processedTotalWeightKg":"250.000000"}}',
    );
  }

  @override
  void dispose() {}

  @override
  dynamic noSuchMethod(Invocation invocation) => super.noSuchMethod(invocation);
}

final class _ApprovalApi implements ProcurementPocApi {
  var dropFirstResponse = false;
  var staleVersion = false;
  var _dropped = false;
  final keys = <String>[];

  @override
  Future<ProcurementPocCommandResult> approve({
    required String sessionId,
    required int expectedVersion,
    required String idempotencyKey,
  }) async {
    keys.add(idempotencyKey);
    if (staleVersion) {
      throw const ProcurementPocApiException(
        code: 'RECEIVING_POC_VERSION_CONFLICT',
        message: 'Refresh the live view before approval.',
        retryable: false,
        responseAmbiguous: false,
      );
    }
    if (dropFirstResponse && !_dropped) {
      _dropped = true;
      throw const ProcurementPocApiException(
        code: 'POC_NETWORK_AMBIGUOUS',
        message: 'Fake approval response lost after commit.',
        retryable: true,
        responseAmbiguous: true,
      );
    }
    return ProcurementPocCommandResult(
      sessionId: sessionId,
      cloudReference: 'RS-POC-000322',
      status: 'Approved',
      editorDeviceId: '019fad0f-2d6a-7000-8000-000000000324',
      leaseId: null,
      leaseExpiresAtUtc: null,
      entryCount: 5,
      processedTotalWeightKg: '250.000000',
      version: expectedVersion + 1,
      finalizationId: null,
      idempotencyStatus: keys.length == 1 ? 'Accepted' : 'PreviouslyProcessed',
      rawResponseJson: '{"result":{"status":"Approved"}}',
    );
  }

  @override
  void dispose() {}

  @override
  dynamic noSuchMethod(Invocation invocation) => super.noSuchMethod(invocation);
}

final class _MalformedControlApi implements ProcurementPocApi {
  final keys = <String>[];

  @override
  Future<ProcurementPocCommandResult> approve({
    required String sessionId,
    required int expectedVersion,
    required String idempotencyKey,
  }) async {
    keys.add(idempotencyKey);
    return _result(
      sessionId: sessionId,
      status: 'Finalized',
      version: expectedVersion + 1,
      finalizationId: '019fad0f-2d6a-7000-8000-0000000003af',
    );
  }

  @override
  Future<ProcurementPocCommandResult> finalize({
    required String sessionId,
    required int expectedVersion,
    required String idempotencyKey,
  }) async {
    keys.add(idempotencyKey);
    return _result(
      sessionId: sessionId,
      status: 'Approved',
      version: expectedVersion + 1,
      finalizationId: null,
    );
  }

  static ProcurementPocCommandResult _result({
    required String sessionId,
    required String status,
    required int version,
    required String? finalizationId,
  }) {
    return ProcurementPocCommandResult(
      sessionId: sessionId,
      cloudReference: 'RS-POC-MALFORMED',
      status: status,
      editorDeviceId: '019fad0f-2d6a-7000-8000-0000000003ae',
      leaseId: null,
      leaseExpiresAtUtc: null,
      entryCount: 5,
      processedTotalWeightKg: '250.000000',
      version: version,
      finalizationId: finalizationId,
      idempotencyStatus: 'Accepted',
      rawResponseJson: '{"result":{"malformed":true}}',
    );
  }

  @override
  void dispose() {}

  @override
  dynamic noSuchMethod(Invocation invocation) => super.noSuchMethod(invocation);
}
