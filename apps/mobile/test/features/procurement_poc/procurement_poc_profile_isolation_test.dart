import 'package:flutter_test/flutter_test.dart';
import 'package:traderpro_agrisuite_mobile/core/database/database.dart';
import 'package:traderpro_agrisuite_mobile/features/procurement_poc/procurement_poc.dart';
import 'package:traderpro_agrisuite_mobile/features/receiving/receiving.dart';

import '../../support/offline_store_test_harness.dart';

void main() {
  test('profile identity cannot change while local work is pending', () async {
    final database = LocalDatabaseOpeners.openInMemoryForTest();
    addTearDown(database.close);
    final receiving = LocalReceivingStore(
      database: database,
      installReference: 'isolation',
      clock: FixedLocalDeviceClock(),
      idGenerator: SequentialLocalIdGenerator(),
    );
    final repository = ProcurementPocLocalRepository(
      database: database,
      localReceivingStore: receiving,
    );
    final profileA = _profile(
      workspaceId: '019fad0f-2d6a-7000-8000-000000000700',
      deviceId: '019fad0f-2d6a-7000-8000-000000000701',
    );
    await repository.saveActiveProfile(profileA);
    await receiving.createLocalReceivingSession();

    await expectLater(
      repository.saveActiveProfile(
        _profile(
          workspaceId: '019fad0f-2d6a-7000-8000-000000000702',
          deviceId: '019fad0f-2d6a-7000-8000-000000000703',
        ),
      ),
      throwsA(
        isA<ProcurementPocLocalException>().having(
          (error) => error.code,
          'code',
          ProcurementPocLocalException.profileChangeBlocked,
        ),
      ),
    );
    expect(
      (await repository.loadActiveProfile())!.workspaceId,
      profileA.workspaceId,
    );
    expect((await repository.loadActiveProfile())!.deviceId, profileA.deviceId);
  });

  test(
    'event cursors are isolated by normalized backend and workspace',
    () async {
      final database = LocalDatabaseOpeners.openInMemoryForTest();
      addTearDown(database.close);
      final repository = ProcurementPocLocalRepository(
        database: database,
        localReceivingStore: LocalReceivingStore(
          database: database,
          installReference: 'cursor-scope',
          clock: FixedLocalDeviceClock(),
          idGenerator: SequentialLocalIdGenerator(),
        ),
      );
      final profileA = _profile(
        workspaceId: '019fad0f-2d6a-7000-8000-000000000710',
        deviceId: '019fad0f-2d6a-7000-8000-000000000711',
      );
      final profileB = _profile(
        workspaceId: '019fad0f-2d6a-7000-8000-000000000712',
        deviceId: '019fad0f-2d6a-7000-8000-000000000713',
      );
      await repository.saveActiveProfile(profileA);
      await repository.applyMobileSyncEvent(
        profile: profileA,
        event: MobileSyncEvent(
          sequence: 99,
          eventId: '019fad0f-2d6a-7000-8000-000000000714',
          eventType: 'UnknownFutureEvent',
          eventVersion: 1,
          aggregateType: 'FutureAggregate',
          aggregateId: '019fad0f-2d6a-7000-8000-000000000715',
          aggregateVersion: 1,
          occurredAtUtc: DateTime.utc(2026, 7, 29, 10),
          correlationId: 'corr',
          payloadJson: '{"future":true}',
        ),
        receivedAtUtc: DateTime.utc(2026, 7, 29, 10),
      );
      await repository.saveActiveProfile(profileB);

      expect(profileA.sourceKey, isNot(profileB.sourceKey));
      expect(await repository.loadEventCursor(profileA), 99);
      expect(await repository.loadEventCursor(profileB), 0);
    },
  );
}

PocDeviceProfile _profile({
  required String workspaceId,
  required String deviceId,
}) {
  return PocDeviceProfile(
    backendBaseUrl: 'http://192.168.1.50:5000/',
    workspaceId: workspaceId,
    deviceId: deviceId,
    displayRole: PocDisplayRole.operator,
    createdAtUtc: DateTime.utc(2026, 7, 29, 10),
    updatedAtUtc: DateTime.utc(2026, 7, 29, 10),
  );
}
