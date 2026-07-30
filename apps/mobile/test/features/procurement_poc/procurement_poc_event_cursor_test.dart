import 'dart:io';

import 'package:drift/drift.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:traderpro_agrisuite_mobile/core/database/database.dart';
import 'package:traderpro_agrisuite_mobile/features/procurement_poc/procurement_poc.dart';
import 'package:traderpro_agrisuite_mobile/features/receiving/receiving.dart';

import '../../support/offline_store_test_harness.dart';

void main() {
  test(
    'event inbox, projection, duplicate, malformed, and restart cursor',
    () async {
      final directory = await Directory.systemTemp.createTemp(
        'traderpro-poc-events-',
      );
      final file = File(
        '${directory.path}${Platform.pathSeparator}local.sqlite',
      );
      final clock = DateTime.utc(2026, 7, 29, 9, 30);
      var database = LocalDatabaseOpeners.openFileForTest(file);
      var repository = _repository(database);
      final profile = PocDeviceProfile(
        backendBaseUrl: 'http://192.168.1.50:5000',
        workspaceId: '019fad0f-2d6a-7000-8000-000000000200',
        deviceId: '019fad0f-2d6a-7000-8000-000000000201',
        displayRole: PocDisplayRole.owner,
        createdAtUtc: clock,
        updatedAtUtc: clock,
      );
      const sessionId = '019fad0f-2d6a-7000-8000-000000000210';
      const startedPayload =
          '{ "sessionId" : "019fad0f-2d6a-7000-8000-000000000210", '
          '"cloudReference" : "RS-POC-000210", '
          '"status" : "ReceivingInProgress", '
          '"editorDeviceId" : "019fad0f-2d6a-7000-8000-000000000211", '
          '"leaseExpiresAtUtc" : "2026-07-29T09:35:00.000Z", "version" : 1 }';
      try {
        await repository.saveActiveProfile(profile);
        final started = MobileSyncEvent(
          sequence: 10,
          eventId: '019fad0f-2d6a-7000-8000-000000000220',
          eventType: 'ReceivingSessionPocStarted',
          eventVersion: 1,
          aggregateType: 'ReceivingSessionPoc',
          aggregateId: sessionId,
          aggregateVersion: 1,
          occurredAtUtc: clock,
          correlationId: 'corr-start',
          payloadJson: startedPayload,
        );
        await repository.applyMobileSyncEvent(
          profile: profile,
          event: started,
          receivedAtUtc: clock,
        );
        await repository.applyMobileSyncEvent(
          profile: profile,
          event: MobileSyncEvent(
            sequence: 11,
            eventId: '019fad0f-2d6a-7000-8000-000000000221',
            eventType: 'ReceivingEntryPocAccepted',
            eventVersion: 1,
            aggregateType: 'ReceivingSessionPoc',
            aggregateId: sessionId,
            aggregateVersion: 2,
            occurredAtUtc: clock.add(const Duration(seconds: 1)),
            correlationId: 'corr-entry',
            payloadJson:
                '{"sessionId":"$sessionId","entryId":'
                '"019fad0f-2d6a-7000-8000-000000000212","localSequence":2,'
                '"productReference":"PADDY","bagTypeReference":"JUTE-50",'
                '"bagCount":10,"processedWeightKg":"50.230000",'
                '"entryCount":1,"processedTotalWeightKg":"50.230000",'
                '"version":2}',
          ),
          receivedAtUtc: clock.add(const Duration(seconds: 1)),
        );
        await repository.applyMobileSyncEvent(
          profile: profile,
          event: started,
          receivedAtUtc: clock.add(const Duration(seconds: 2)),
        );
        await repository.applyMobileSyncEvent(
          profile: profile,
          event: MobileSyncEvent(
            sequence: 12,
            eventId: '019fad0f-2d6a-7000-8000-000000000222',
            eventType: 'FutureReceivingPocEvent',
            eventVersion: 1,
            aggregateType: 'ReceivingSessionPoc',
            aggregateId: sessionId,
            aggregateVersion: 3,
            occurredAtUtc: clock.add(const Duration(seconds: 2)),
            correlationId: 'corr-future',
            payloadJson: '{"future":true}',
          ),
          receivedAtUtc: clock.add(const Duration(seconds: 2)),
        );

        expect(await repository.loadEventCursor(profile), 12);
        final projection = await repository.getRemoteSession(
          profile,
          sessionId,
        );
        expect(projection!.entryCount, 1);
        expect(projection.processedTotalWeightKg, '50.230000');
        expect(projection.cloudVersion, 2);
        expect(projection.recentEntries, hasLength(1));
        expect(projection.recentEntries.single.localSequence, 2);
        final storedStarted = await database
            .customSelect(
              'SELECT payload_json FROM mobile_sync_event_inbox '
              'WHERE source_key = ? AND event_sequence = 10',
              variables: [Variable<String>(profile.sourceKey)],
            )
            .getSingle();
        expect(storedStarted.read<String>('payload_json'), startedPayload);
        expect(startedPayload, isNot(contains('leaseId')));
        expect(
          await database
              .customSelect(
                'SELECT COUNT(*) AS count FROM mobile_sync_event_inbox',
              )
              .map((row) => row.read<int>('count'))
              .getSingle(),
          3,
        );

        await expectLater(
          repository.applyMobileSyncEvent(
            profile: profile,
            event: MobileSyncEvent(
              sequence: 13,
              eventId: '019fad0f-2d6a-7000-8000-000000000223',
              eventType: 'ReceivingSessionPocSubmitted',
              eventVersion: 1,
              aggregateType: 'ReceivingSessionPoc',
              aggregateId: sessionId,
              aggregateVersion: 3,
              occurredAtUtc: clock.add(const Duration(seconds: 3)),
              correlationId: 'corr-malformed',
              payloadJson: '{"sessionId":"$sessionId","status":42}',
            ),
            receivedAtUtc: clock.add(const Duration(seconds: 3)),
          ),
          throwsA(
            isA<ProcurementPocLocalException>().having(
              (error) => error.code,
              'code',
              ProcurementPocLocalException.eventInvalid,
            ),
          ),
        );
        expect(await repository.loadEventCursor(profile), 12);
        expect(
          await database
              .customSelect(
                'SELECT COUNT(*) AS count FROM mobile_sync_event_inbox',
              )
              .map((row) => row.read<int>('count'))
              .getSingle(),
          3,
        );

        await database.close();
        database = LocalDatabaseOpeners.openFileForTest(file);
        repository = _repository(database);
        expect(
          (await repository.loadActiveProfile())!.sourceKey,
          profile.sourceKey,
        );
        expect(await repository.loadEventCursor(profile), 12);
        expect(
          (await repository.getRemoteSession(profile, sessionId))!.entryCount,
          1,
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

ProcurementPocLocalRepository _repository(TraderProLocalDatabase database) {
  return ProcurementPocLocalRepository(
    database: database,
    localReceivingStore: LocalReceivingStore(
      database: database,
      installReference: 'event-poc',
      clock: FixedLocalDeviceClock(),
      idGenerator: SequentialLocalIdGenerator(),
    ),
  );
}
