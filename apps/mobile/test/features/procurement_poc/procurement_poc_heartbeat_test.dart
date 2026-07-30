import 'dart:io';

import 'package:flutter_test/flutter_test.dart';
import 'package:traderpro_agrisuite_mobile/core/database/database.dart';
import 'package:traderpro_agrisuite_mobile/features/procurement_poc/procurement_poc.dart';
import 'package:traderpro_agrisuite_mobile/features/receiving/receiving.dart';

import '../../support/offline_store_test_harness.dart';

void main() {
  test(
    'heartbeat waits for lease, reuses key after restart, and expires safely',
    () async {
      final directory = await Directory.systemTemp.createTemp(
        'traderpro-poc-heartbeat-',
      );
      final file = File(
        '${directory.path}${Platform.pathSeparator}local.sqlite',
      );
      final clock = _HeartbeatClock(DateTime.utc(2026, 7, 29, 10));
      final ids = SequentialLocalIdGenerator();
      final api = _HeartbeatApi(clock);
      var database = LocalDatabaseOpeners.openFileForTest(file);
      var receiving = _receiving(database, clock, ids);
      var repository = _repository(database, receiving);
      final profile = PocDeviceProfile(
        backendBaseUrl: 'http://192.168.1.50:5000',
        workspaceId: '019fad0f-2d6a-7000-8000-000000000800',
        deviceId: '019fad0f-2d6a-7000-8000-000000000801',
        displayRole: PocDisplayRole.operator,
        createdAtUtc: clock.nowUtc(),
        updatedAtUtc: clock.nowUtc(),
      );
      try {
        await repository.saveActiveProfile(profile);
        final created = await receiving.createLocalReceivingSession();
        var controls = _controls(repository, api, clock, ids);
        expect(await controls.queueDueHeartbeats(), 0);

        final sync = ProcurementPocSyncEngine(
          feature: _enabled,
          profileStore: repository,
          syncStore: repository,
          apiFactory: (_) => api,
          clock: clock,
        );
        expect((await sync.synchronize()).completed, 1);
        expect(await controls.queueDueHeartbeats(), 1);
        final heartbeat = (await repository.listControlCommands(
          profile,
        )).single;
        api.dropFirstHeartbeatResponse = true;
        expect((await controls.executePending()).pending, 1);
        expect(api.heartbeatKeys, [heartbeat.commandId]);

        await database.close();
        database = LocalDatabaseOpeners.openFileForTest(file);
        receiving = _receiving(database, clock, ids);
        repository = _repository(database, receiving);
        controls = _controls(repository, api, clock, ids);
        expect((await controls.executePending()).completed, 1);
        expect(api.heartbeatKeys, [heartbeat.commandId, heartbeat.commandId]);
        final renewed = await repository.getCloudState(
          profile,
          created.session.id,
        );
        expect(
          renewed!.leaseExpiresAtUtc,
          clock.nowUtc().add(const Duration(minutes: 5)),
        );
        expect(
          (await repository.listControlCommands(profile)).single.status,
          PocControlCommandStatus.completed,
        );

        clock.value = clock.value.add(const Duration(minutes: 6));
        expect(await controls.queueDueHeartbeats(), 0);
        final expired = await repository.getCloudState(
          profile,
          created.session.id,
        );
        expect(expired!.lastErrorCode, 'RECEIVING_POC_LEASE_EXPIRED');
        expect(
          await repository.findOutstandingControlCommand(
            profile: profile,
            commandType: PocControlCommandType.heartbeat,
            sessionId: created.session.id,
          ),
          isNull,
        );
      } finally {
        await database.close();
        if (directory.existsSync()) {
          await directory.delete(recursive: true);
        }
      }
    },
  );
}

const _enabled = ProcurementPocFeatureConfiguration(
  compileTimeEnabled: true,
  releaseMode: false,
);

LocalReceivingStore _receiving(
  TraderProLocalDatabase database,
  _HeartbeatClock clock,
  SequentialLocalIdGenerator ids,
) {
  return LocalReceivingStore(
    database: database,
    installReference: 'heartbeat',
    clock: clock,
    idGenerator: ids,
  );
}

ProcurementPocLocalRepository _repository(
  TraderProLocalDatabase database,
  LocalReceivingStore receiving,
) {
  return ProcurementPocLocalRepository(
    database: database,
    localReceivingStore: receiving,
  );
}

ProcurementPocControlService _controls(
  ProcurementPocLocalRepository repository,
  _HeartbeatApi api,
  _HeartbeatClock clock,
  SequentialLocalIdGenerator ids,
) {
  return ProcurementPocControlService(
    feature: _enabled,
    profileStore: repository,
    syncStore: repository,
    controlStore: repository,
    apiFactory: (_) => api,
    clock: clock,
    idGenerator: ids,
  );
}

final class _HeartbeatClock implements LocalDeviceClock, ProcurementPocClock {
  _HeartbeatClock(this.value);

  DateTime value;

  @override
  DateTime nowUtc() => value;
}

final class _HeartbeatApi implements ProcurementPocApi {
  _HeartbeatApi(this.clock);

  final _HeartbeatClock clock;
  final heartbeatKeys = <String>[];
  var dropFirstHeartbeatResponse = false;
  var _dropped = false;

  @override
  Future<MobileSyncBatchResult> sendOperations(
    List<MobileSyncOperationEnvelope> operations,
  ) async {
    final operation = operations.single;
    return MobileSyncBatchResult(
      operations: [
        MobileSyncOperationResult(
          operationId: operation.operationId,
          aggregateId: operation.aggregateId,
          localSequence: operation.localSequence,
          resultStatus: 'Accepted',
          cloudAggregateVersion: 1,
          cloudReference: 'RS-POC-000800',
          leaseId: '019fad0f-2d6a-7000-8000-000000000899',
          leaseExpiresAtUtc: clock.nowUtc().add(const Duration(minutes: 1)),
          errorCode: null,
          message: null,
        ),
      ],
      rawResponseJson: '{"fake":true}',
    );
  }

  @override
  Future<ProcurementPocCommandResult> heartbeat({
    required String sessionId,
    required String leaseId,
    required String idempotencyKey,
  }) async {
    heartbeatKeys.add(idempotencyKey);
    if (dropFirstHeartbeatResponse && !_dropped) {
      _dropped = true;
      throw const ProcurementPocApiException(
        code: 'POC_NETWORK_AMBIGUOUS',
        message: 'Fake heartbeat response lost after commit.',
        retryable: true,
        responseAmbiguous: true,
      );
    }
    return ProcurementPocCommandResult(
      sessionId: sessionId,
      cloudReference: 'RS-POC-000800',
      status: 'ReceivingInProgress',
      editorDeviceId: '019fad0f-2d6a-7000-8000-000000000801',
      leaseId: leaseId,
      leaseExpiresAtUtc: clock.nowUtc().add(const Duration(minutes: 5)),
      entryCount: 0,
      processedTotalWeightKg: '0.000000',
      version: 2,
      finalizationId: null,
      idempotencyStatus: heartbeatKeys.length == 1
          ? 'Accepted'
          : 'PreviouslyProcessed',
      rawResponseJson: '{"result":{"version":2}}',
    );
  }

  @override
  void dispose() {}

  @override
  dynamic noSuchMethod(Invocation invocation) => super.noSuchMethod(invocation);
}
