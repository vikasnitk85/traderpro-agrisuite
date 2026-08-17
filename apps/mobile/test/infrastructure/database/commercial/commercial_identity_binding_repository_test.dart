import 'package:drift/drift.dart' show Value;
import 'package:drift/native.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:traderpro_agrisuite_mobile/core/identity/commercial_identity_failure.dart';
import 'package:traderpro_agrisuite_mobile/core/identity/commercial_identity_models.dart';
import 'package:traderpro_agrisuite_mobile/infrastructure/database/commercial/commercial_database.dart'
    hide CommercialIdentitySnapshot;
import 'package:traderpro_agrisuite_mobile/infrastructure/database/commercial/commercial_identity_binding_repository.dart';

void main() {
  late CommercialDatabase database;
  late CommercialIdentityBindingRepository repository;
  late CommercialAccountBinding binding;
  late CommercialIdentitySnapshot snapshot;

  setUp(() async {
    database = CommercialDatabase(NativeDatabase.memory());
    await database.customStatement('PRAGMA foreign_keys = ON');
    repository = CommercialIdentityBindingRepository(database);
    binding = _binding();
    snapshot = _snapshot();
  });

  tearDown(() => database.close());

  test(
    'first authoritative context binds identity and snapshot atomically',
    () async {
      await repository.bindOrMatch(binding, snapshot);

      final storedBinding = await repository.readBinding();
      final storedSnapshot = await repository.readSnapshot();
      expect(storedBinding?.matches(binding), isTrue);
      expect(storedBinding?.firstBoundAtUtc, binding.firstBoundAtUtc);
      expect(storedSnapshot?.workspaceCode, snapshot.workspaceCode);
      expect(storedSnapshot?.role, snapshot.role);
    },
  );

  test(
    'exact identity match permits mutable role and display refresh',
    () async {
      await repository.bindOrMatch(binding, snapshot);
      final changed = CommercialIdentitySnapshot(
        workspaceCode: snapshot.workspaceCode,
        userDisplayName: 'Updated Operator',
        role: CommercialRole.owner,
        deviceLabel: 'Updated device label',
        lastConfirmedAtUtc: DateTime.utc(2026, 8, 9, 1),
      );
      await repository.bindOrMatch(
        _binding(firstBoundAtUtc: DateTime.utc(2030)),
        changed,
      );

      final storedBinding = await repository.readBinding();
      final storedSnapshot = await repository.readSnapshot();
      expect(storedBinding?.firstBoundAtUtc, binding.firstBoundAtUtc);
      expect(storedSnapshot?.userDisplayName, changed.userDisplayName);
      expect(storedSnapshot?.role, CommercialRole.owner);
      expect(storedSnapshot?.deviceLabel, changed.deviceLabel);
    },
  );

  test(
    'every immutable tuple mismatch fails closed without snapshot mutation',
    () async {
      await repository.bindOrMatch(binding, snapshot);
      final mismatches = <CommercialAccountBinding>[
        _binding(apiOrigin: 'https://other.example.com/'),
        _binding(
          workspaceId: WorkspaceId('aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa'),
        ),
        _binding(companyId: CompanyId('bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb')),
        _binding(
          defaultBranchId: BranchId('cccccccc-cccc-4ccc-8ccc-cccccccccccc'),
        ),
        _binding(userId: UserId('dddddddd-dddd-4ddd-8ddd-dddddddddddd')),
        _binding(deviceId: DeviceId('eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee')),
      ];

      for (final mismatch in mismatches) {
        await expectLater(
          repository.bindOrMatch(
            mismatch,
            CommercialIdentitySnapshot(
              workspaceCode: 'MUST-NOT-COMMIT',
              userDisplayName: 'Must not commit',
              role: CommercialRole.owner,
              deviceLabel: 'Must not commit',
              lastConfirmedAtUtc: DateTime.utc(2030),
            ),
          ),
          throwsA(
            isA<CommercialIdentityFailure>().having(
              (failure) => failure.kind,
              'kind',
              CommercialIdentityFailureKind.identityContextMismatch,
            ),
          ),
        );
        expect(
          (await repository.readSnapshot())?.workspaceCode,
          'DEMO-WORKSPACE',
        );
      }
    },
  );

  test('database rejects binding update and delete', () async {
    await repository.bindOrMatch(binding, snapshot);
    await expectLater(
      database.customStatement(
        "UPDATE commercial_identity_binding SET api_origin = 'https://other.example.com/'",
      ),
      throwsA(isNotNull),
    );
    await expectLater(
      database.customStatement('DELETE FROM commercial_identity_binding'),
      throwsA(isNotNull),
    );
    expect((await repository.readBinding())?.apiOrigin, binding.apiOrigin);
  });

  test('schema contains no secret-shaped identity columns', () async {
    const prohibited = <String>{
      'password',
      'access_token',
      'refresh_token',
      'device_secret',
      'activation_code',
      'jwt',
      'authorization',
    };
    for (final table in <String>[
      'commercial_storage_metadata',
      'commercial_identity_binding',
      'commercial_identity_snapshot',
    ]) {
      final rows = await database
          .customSelect('PRAGMA table_info($table)')
          .get();
      final columns = rows.map((row) => row.read<String>('name')).toSet();
      expect(columns.intersection(prohibited), isEmpty, reason: table);
    }
  });

  test('binding singleton check and relation are database-enforced', () async {
    await expectLater(
      database
          .into(database.commercialIdentityBindings)
          .insert(
            CommercialIdentityBindingsCompanion.insert(
              singletonId: const Value(2),
              bindingContractVersion: 1,
              apiOrigin: binding.apiOrigin,
              workspaceId: binding.workspaceId.value,
              companyId: binding.companyId.value,
              defaultBranchId: binding.defaultBranchId.value,
              userId: binding.userId.value,
              deviceId: binding.deviceId.value,
              firstBoundAtUtcMicros:
                  binding.firstBoundAtUtc.microsecondsSinceEpoch,
            ),
          ),
      throwsA(isNotNull),
    );
    await expectLater(
      database
          .into(database.commercialIdentitySnapshots)
          .insert(
            CommercialIdentitySnapshotsCompanion.insert(
              singletonId: const Value(1),
              workspaceCode: snapshot.workspaceCode,
              userDisplayName: snapshot.userDisplayName,
              role: snapshot.role.wireValue,
              deviceLabel: snapshot.deviceLabel,
              lastConfirmedAtUtcMicros:
                  snapshot.lastConfirmedAtUtc.microsecondsSinceEpoch,
            ),
          ),
      throwsA(isNotNull),
    );
  });
}

CommercialAccountBinding _binding({
  String apiOrigin = 'https://api.example.com/',
  WorkspaceId? workspaceId,
  CompanyId? companyId,
  BranchId? defaultBranchId,
  UserId? userId,
  DeviceId? deviceId,
  DateTime? firstBoundAtUtc,
}) => CommercialAccountBinding(
  apiOrigin: apiOrigin,
  workspaceId:
      workspaceId ?? WorkspaceId('11111111-1111-4111-8111-111111111111'),
  companyId: companyId ?? CompanyId('22222222-2222-4222-8222-222222222222'),
  defaultBranchId:
      defaultBranchId ?? BranchId('019fc400-0000-7000-8000-000000000202'),
  userId: userId ?? UserId('33333333-3333-4333-8333-333333333333'),
  deviceId: deviceId ?? DeviceId('019fc400-0000-7000-8000-000000000201'),
  firstBoundAtUtc: firstBoundAtUtc ?? DateTime.utc(2026, 8, 9),
);

CommercialIdentitySnapshot _snapshot() => CommercialIdentitySnapshot(
  workspaceCode: 'DEMO-WORKSPACE',
  userDisplayName: 'Receiving Operator',
  role: CommercialRole.operator,
  deviceLabel: 'Receiving device',
  lastConfirmedAtUtc: DateTime.utc(2026, 8, 9),
);
