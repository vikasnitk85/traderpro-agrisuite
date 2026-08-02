using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TraderPro.Application.Common.Commands;
using TraderPro.Application.Common.Errors;
using TraderPro.Application.Procurement.Receiving;
using TraderPro.Domain.Platform;
using TraderPro.Domain.Procurement.Receiving;
using TraderPro.Infrastructure.Modules.Shared;

namespace TraderPro.Infrastructure.Modules.Procurement.Receiving;

internal sealed partial class CommercialReceivingService
{
    public Task<IdempotentCommandResult<CommercialReceivingLeaseResult>> HeartbeatAsync(
        CommercialReceivingLeaseCommand command,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        RequireOperatorContext();
        RequireIdempotencyKey(idempotencyKey);
        if (command.LeaseId is null) throw Problem("RECEIVING_LEASE_REQUIRED", "A lease ID is required.", ApplicationErrorCategory.Validation);
        var hash = CommercialReceivingRequestHash.ForDirectCommand(CommercialReceivingCommandTypes.Heartbeat, current.WorkspaceId, current.CompanyId, current.UserId, current.DeviceId, command.SessionId, command.OwnershipGeneration, command.LeaseId);
        return idempotency.ExecuteAsync(current.WorkspaceId, CommercialReceivingCommandTypes.Heartbeat, idempotencyKey, hash, current.CorrelationId, 200,
            async () =>
            {
                var loaded = await LoadSessionForUpdateAsync(command.SessionId, cancellationToken);
                Domain(() => loaded.Session.RequireInProgress());
                Domain(() => loaded.Ownership.Heartbeat(current.DeviceId, command.OwnershipGeneration, command.LeaseId.Value, clock.UtcNow, options.LeaseDuration));
                return Lease(loaded.Session, loaded.Ownership);
            }, IsReplayableLease, null, null, OutboxCommitOrdering.IndependentInternal, cancellationToken);
    }

    public Task<IdempotentCommandResult<CommercialReceivingLeaseResult>> ReacquireAsync(
        CommercialReceivingLeaseCommand command,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        RequireOperatorContext();
        RequireIdempotencyKey(idempotencyKey);
        var hash = CommercialReceivingRequestHash.ForDirectCommand(CommercialReceivingCommandTypes.Reacquire, current.WorkspaceId, current.CompanyId, current.UserId, current.DeviceId, command.SessionId, command.OwnershipGeneration);
        return idempotency.ExecuteAsync(current.WorkspaceId, CommercialReceivingCommandTypes.Reacquire, idempotencyKey, hash, current.CorrelationId, 200,
            async () =>
            {
                var loaded = await LoadSessionForUpdateAsync(command.SessionId, cancellationToken);
                Domain(() => loaded.Session.RequireInProgress());
                var now = clock.UtcNow;
                Domain(() => loaded.Ownership.Reacquire(current.DeviceId, command.OwnershipGeneration, now, options.LeaseDuration));
                AddAudit("Procurement.CommercialReceiving.LeaseReacquired", loaded.Session.Id, null, JsonSerializer.Serialize(new
                {
                    sessionId = loaded.Session.Id,
                    editorDeviceId = loaded.Ownership.EditorDeviceId,
                    loaded.Ownership.OwnershipGeneration,
                    loaded.Ownership.LeaseExpiresAtUtc,
                    loaded.Ownership.Version,
                }, JsonOptions), now);
                return Lease(loaded.Session, loaded.Ownership);
            }, IsReplayableLease, null, null, OutboxCommitOrdering.IndependentInternal, cancellationToken);
    }

    public Task<IdempotentCommandResult<CommercialReceivingLeaseResult>> TransferAsync(
        CommercialReceivingTransferCommand command,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        RequireContext();
        if (!current.IsOwner) throw Problem("OWNER_ROLE_REQUIRED", "The Owner role is required.", ApplicationErrorCategory.Authorization);
        RequireIdempotencyKey(idempotencyKey);
        if (string.IsNullOrWhiteSpace(command.Reason) || command.Reason != command.Reason.Trim() || command.Reason.Length > 500 || command.Reason.Any(char.IsControl))
            throw Problem("RECEIVING_OWNERSHIP_TARGET_INVALID", "A safe transfer reason is required.", ApplicationErrorCategory.Validation);
        var hash = CommercialReceivingRequestHash.ForDirectCommand(CommercialReceivingCommandTypes.Transfer, current.WorkspaceId, current.CompanyId, current.UserId, current.DeviceId, command.SessionId, command.TargetDeviceId, command.ExpectedSessionVersion, command.ExpectedOwnershipGeneration, command.Reason);
        return idempotency.ExecuteAsync(current.WorkspaceId, CommercialReceivingCommandTypes.Transfer, idempotencyKey, hash, current.CorrelationId, 200,
            async () =>
            {
                var loaded = await LoadSessionForUpdateAsync(command.SessionId, cancellationToken);
                Domain(() => loaded.Session.RequireInProgress());
                var targetDevice = await dbContext.Devices
                    .FromSqlInterpolated($"SELECT * FROM platform.devices WHERE workspace_id = {current.WorkspaceId} AND id = {command.TargetDeviceId} FOR KEY SHARE")
                    .SingleOrDefaultAsync(cancellationToken);
                var targetCredential = await dbContext.DeviceCredentials
                    .FromSqlInterpolated($"SELECT * FROM platform.device_credentials WHERE workspace_id = {current.WorkspaceId} AND device_id = {command.TargetDeviceId} FOR KEY SHARE")
                    .SingleOrDefaultAsync(cancellationToken);
                if (targetDevice is not null)
                    await dbContext.Entry(targetDevice).ReloadAsync(cancellationToken);
                if (targetCredential is not null)
                    await dbContext.Entry(targetCredential).ReloadAsync(cancellationToken);
                var targetValid = targetDevice?.Status is DeviceStatus.Active &&
                    targetCredential is not null &&
                    targetCredential.RevokedAtUtc is null;
                if (!targetValid) throw Problem("RECEIVING_OWNERSHIP_TARGET_INVALID", "The target Device is not active and credentialed.", ApplicationErrorCategory.Conflict);
                var previousDevice = loaded.Ownership.EditorDeviceId;
                var previousGeneration = loaded.Ownership.OwnershipGeneration;
                var now = clock.UtcNow;
                Domain(() => loaded.Session.RecordOwnershipTransfer(
                    command.ExpectedSessionVersion,
                    command.ExpectedOwnershipGeneration,
                    now));
                Domain(() => loaded.Ownership.Transfer(
                    command.TargetDeviceId,
                    current.UserId,
                    command.ExpectedOwnershipGeneration,
                    now));
                AddAudit("Procurement.CommercialReceiving.OwnershipTransferred", loaded.Session.Id, command.Reason, JsonSerializer.Serialize(new
                {
                    sessionId = loaded.Session.Id,
                    previousEditorDeviceId = previousDevice,
                    editorDeviceId = loaded.Ownership.EditorDeviceId,
                    previousGeneration,
                    ownershipGeneration = loaded.Ownership.OwnershipGeneration,
                }, JsonOptions), now);
                var payload = JsonSerializer.Serialize(new
                {
                    sessionId = loaded.Session.Id,
                    loaded.Session.CloudReference,
                    previousEditorDeviceId = previousDevice,
                    editorDeviceId = loaded.Ownership.EditorDeviceId,
                    previousGeneration,
                    ownershipGeneration = loaded.Ownership.OwnershipGeneration,
                    changedAtUtc = now,
                    leaseAcquisitionRequired = true,
                }, JsonOptions);
                AddReceivingEvent("CommercialReceivingOwnershipChanged", loaded.Session, payload, OutboxAudience.OwnerBroadcast, null, now);
                AddReceivingEvent("CommercialReceivingOwnershipChanged", loaded.Session, payload, OutboxAudience.TargetDevice, previousDevice, now);
                AddReceivingEvent("CommercialReceivingOwnershipChanged", loaded.Session, payload, OutboxAudience.TargetDevice, loaded.Ownership.EditorDeviceId, now);
                return Lease(loaded.Session, loaded.Ownership);
            }, IsReplayableLease, null, MapPersistence, OutboxCommitOrdering.CommercialMobileSync, current.CompanyId, cancellationToken);
    }

    public async Task<CommercialReceivingReferencePolicyResult> GetReferencePolicyAsync(CancellationToken cancellationToken)
    {
        RequireContext();
        var policy = await dbContext.CommercialReceivingReferencePolicies.AsNoTracking().SingleOrDefaultAsync(item => item.CompanyId == current.CompanyId, cancellationToken)
            ?? throw Problem("RECEIVING_REFERENCE_POLICY_NOT_CONFIGURED", "The Receiving reference policy is not configured.", ApplicationErrorCategory.NotFound);
        return Policy(policy);
    }

    public Task<IdempotentCommandResult<CommercialReceivingReferencePolicyResult>> UpdateReferencePolicyAsync(
        UpdateCommercialReceivingReferencePolicyCommand command,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        RequireContext();
        if (!current.IsOwner) throw Problem("OWNER_ROLE_REQUIRED", "The Owner role is required.", ApplicationErrorCategory.Authorization);
        RequireIdempotencyKey(idempotencyKey);
        if (!Enum.TryParse<CommercialReceivingReferenceResetPolicy>(command.ResetPolicy, false, out var reset) || !Enum.IsDefined(reset))
            throw Problem("RECEIVING_REFERENCE_POLICY_INVALID", "The Receiving reference reset policy is invalid.", ApplicationErrorCategory.Validation);
        var hash = CommercialReceivingRequestHash.ForDirectCommand(CommercialReceivingCommandTypes.UpdateReferencePolicy, current.WorkspaceId, current.CompanyId, current.UserId, current.DeviceId, command.FormatTemplate, command.ResetPolicy, command.StartingNumber, command.ExpectedVersion);
        return idempotency.ExecuteAsync(current.WorkspaceId, CommercialReceivingCommandTypes.UpdateReferencePolicy, idempotencyKey, hash, current.CorrelationId, 200,
            async () =>
            {
                await AcquireReferenceSeriesLockAsync(cancellationToken);
                var policy = await dbContext.CommercialReceivingReferencePolicies.SingleOrDefaultAsync(item => item.CompanyId == current.CompanyId, cancellationToken);
                var now = clock.UtcNow;
                if (policy is null)
                {
                    if (command.ExpectedVersion != 0) throw Problem("RECEIVING_REFERENCE_POLICY_NOT_CONFIGURED", "The Receiving reference policy is not configured.", ApplicationErrorCategory.NotFound);
                    policy = DomainValue(() => CommercialReceivingReferencePolicy.Create(current.WorkspaceId, current.CompanyId, command.FormatTemplate, reset, command.StartingNumber, now));
                    dbContext.CommercialReceivingReferencePolicies.Add(policy);
                    AddAudit("Procurement.CommercialReceiving.ReferencePolicyConfigured", policy.Id, null, JsonSerializer.Serialize(Policy(policy), JsonOptions), now, "Procurement.CommercialReceivingReferencePolicy");
                }
                else
                {
                    if (policy.Version != command.ExpectedVersion) throw Problem("RECEIVING_REFERENCE_CONFLICT", "The Receiving reference policy version changed.", ApplicationErrorCategory.Conflict);
                    var issued =
                        await dbContext.CommercialReceivingReferenceCounters.AnyAsync(
                            item => item.PolicyId == policy.Id && item.NextNumber > policy.StartingNumber,
                            cancellationToken) ||
                        await dbContext.CommercialReceivingReferenceReservations.AnyAsync(
                            item => item.PolicyId == policy.Id,
                            cancellationToken);
                    Domain(() => policy.Update(command.FormatTemplate, reset, command.StartingNumber, issued, now));
                    AddAudit("Procurement.CommercialReceiving.ReferencePolicyUpdated", policy.Id, null, JsonSerializer.Serialize(Policy(policy), JsonOptions), now, "Procurement.CommercialReceivingReferencePolicy");
                }
                return Policy(policy);
            }, result => result.Id != Guid.Empty && result.Version > 0, null, MapPersistence, OutboxCommitOrdering.IndependentInternal, cancellationToken);
    }

    private static CommercialReceivingLeaseResult Lease(CommercialReceivingSession session, CommercialReceivingOwnership ownership) =>
        new(session.Id, session.Version, ownership.EditorDeviceId, ownership.OwnershipGeneration, ownership.LeaseId, ownership.LeaseExpiresAtUtc, ownership.Version);
    private static bool IsReplayableLease(CommercialReceivingLeaseResult result) => result.SessionId != Guid.Empty && result.OwnershipGeneration > 0 && result.OwnershipVersion > 0;
    private static CommercialReceivingReferencePolicyResult Policy(CommercialReceivingReferencePolicy policy) =>
        new(policy.Id, policy.DocumentType, policy.FormatTemplate, policy.ResetPolicy.ToString(), policy.StartingNumber, policy.Version, policy.CreatedAtUtc, policy.UpdatedAtUtc);
    private static void RequireIdempotencyKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value != value.Trim() || value.Length > 200)
            throw Problem("IDEMPOTENCY_KEY_INVALID", "A trimmed Idempotency-Key of at most 200 characters is required.", ApplicationErrorCategory.Validation);
    }
}
