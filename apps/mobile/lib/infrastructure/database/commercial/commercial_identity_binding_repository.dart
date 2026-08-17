import 'package:drift/drift.dart';

import '../../../core/identity/commercial_identity_failure.dart';
import '../../../core/identity/commercial_identity_models.dart' as domain;
import '../../../core/identity/commercial_identity_ports.dart';
import 'commercial_database.dart';

final class CommercialIdentityBindingRepository
    implements CommercialIdentityBindingPort {
  CommercialIdentityBindingRepository(this._database);

  final CommercialDatabase _database;

  @override
  Future<domain.CommercialAccountBinding?> readBinding() async {
    final row = await (_database.select(
      _database.commercialIdentityBindings,
    )..where((candidate) => candidate.singletonId.equals(1))).getSingleOrNull();
    if (row == null) {
      return null;
    }
    if (row.bindingContractVersion !=
        CommercialDatabase.identityBindingContractVersion) {
      throw _protocolFailure();
    }
    try {
      return domain.CommercialAccountBinding(
        apiOrigin: row.apiOrigin,
        workspaceId: domain.WorkspaceId(row.workspaceId),
        companyId: domain.CompanyId(row.companyId),
        defaultBranchId: domain.BranchId(row.defaultBranchId),
        userId: domain.UserId(row.userId),
        deviceId: domain.DeviceId(row.deviceId),
        firstBoundAtUtc: DateTime.fromMicrosecondsSinceEpoch(
          row.firstBoundAtUtcMicros,
          isUtc: true,
        ),
      );
    } on Object {
      throw _protocolFailure();
    }
  }

  @override
  Future<domain.CommercialIdentitySnapshot?> readSnapshot() async {
    final row = await (_database.select(
      _database.commercialIdentitySnapshots,
    )..where((candidate) => candidate.singletonId.equals(1))).getSingleOrNull();
    if (row == null) {
      return null;
    }
    try {
      return domain.CommercialIdentitySnapshot(
        workspaceCode: row.workspaceCode,
        userDisplayName: row.userDisplayName,
        role: domain.CommercialRole.parse(row.role),
        deviceLabel: row.deviceLabel,
        lastConfirmedAtUtc: DateTime.fromMicrosecondsSinceEpoch(
          row.lastConfirmedAtUtcMicros,
          isUtc: true,
        ),
      );
    } on Object {
      throw _protocolFailure();
    }
  }

  @override
  Future<void> bindOrMatch(
    domain.CommercialAccountBinding binding,
    domain.CommercialIdentitySnapshot snapshot,
  ) => _database.transaction(() async {
    final existing = await readBinding();
    if (existing == null) {
      await _database
          .into(_database.commercialIdentityBindings)
          .insert(
            CommercialIdentityBindingsCompanion.insert(
              singletonId: const Value(1),
              bindingContractVersion:
                  CommercialDatabase.identityBindingContractVersion,
              apiOrigin: binding.apiOrigin,
              workspaceId: binding.workspaceId.value,
              companyId: binding.companyId.value,
              defaultBranchId: binding.defaultBranchId.value,
              userId: binding.userId.value,
              deviceId: binding.deviceId.value,
              firstBoundAtUtcMicros: binding.firstBoundAtUtc
                  .toUtc()
                  .microsecondsSinceEpoch,
            ),
          );
    } else if (!existing.matches(binding)) {
      throw const CommercialIdentityFailure(
        kind: CommercialIdentityFailureKind.identityContextMismatch,
        safeCode: 'IDENTITY_CONTEXT_MISMATCH',
      );
    }

    await _database
        .into(_database.commercialIdentitySnapshots)
        .insertOnConflictUpdate(
          CommercialIdentitySnapshotsCompanion.insert(
            singletonId: const Value(1),
            workspaceCode: snapshot.workspaceCode,
            userDisplayName: snapshot.userDisplayName,
            role: snapshot.role.wireValue,
            deviceLabel: snapshot.deviceLabel,
            lastConfirmedAtUtcMicros: snapshot.lastConfirmedAtUtc
                .toUtc()
                .microsecondsSinceEpoch,
          ),
        );
  });

  static CommercialIdentityFailure _protocolFailure() =>
      const CommercialIdentityFailure(
        kind: CommercialIdentityFailureKind.protocolContractMismatch,
        safeCode: 'IDENTITY_BINDING_STORAGE_INVALID',
      );
}
