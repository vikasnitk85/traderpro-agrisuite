using TraderPro.Domain.Common;

namespace TraderPro.Domain.Procurement.Receiving;

public sealed class CommercialReceivingOwnership :
    IWorkspaceScoped,
    IVersionedEntity,
    ICreatedAtUtc,
    IUpdatedAtUtc
{
    private CommercialReceivingOwnership()
    {
    }

    private CommercialReceivingOwnership(
        Guid workspaceId,
        Guid companyId,
        Guid receivingSessionId,
        Guid editorDeviceId,
        DateTimeOffset now,
        TimeSpan duration)
    {
        Id = Uuid7.NewGuid();
        WorkspaceId = CommercialReceivingSession.RequiredId(workspaceId, nameof(workspaceId));
        CompanyId = CommercialReceivingSession.RequiredId(companyId, nameof(companyId));
        ReceivingSessionId = CommercialReceivingSession.RequiredId(receivingSessionId, nameof(receivingSessionId));
        EditorDeviceId = CommercialReceivingSession.RequiredId(editorDeviceId, nameof(editorDeviceId));
        OwnershipGeneration = 1;
        LeaseId = Uuid7.NewGuid();
        LastHeartbeatAtUtc = CommercialReceivingSession.Utc(now, nameof(now));
        LeaseExpiresAtUtc = LeaseExpiry(now, duration);
        CreatedAtUtc = now;
        UpdatedAtUtc = now;
        Version = 1;
    }

    public Guid Id { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid ReceivingSessionId { get; private set; }
    public Guid EditorDeviceId { get; private set; }
    public long OwnershipGeneration { get; private set; }
    public Guid? LeaseId { get; private set; }
    public DateTimeOffset? LeaseExpiresAtUtc { get; private set; }
    public DateTimeOffset? LastHeartbeatAtUtc { get; private set; }
    public DateTimeOffset? LastReacquiredAtUtc { get; private set; }
    public DateTimeOffset? LastTransferredAtUtc { get; private set; }
    public Guid? LastTransferredByUserId { get; private set; }
    public long Version { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static CommercialReceivingOwnership Start(
        Guid workspaceId,
        Guid companyId,
        Guid receivingSessionId,
        Guid editorDeviceId,
        DateTimeOffset now,
        TimeSpan duration) =>
        new(workspaceId, companyId, receivingSessionId, editorDeviceId, now, duration);

    public void RequireLease(
        Guid deviceId,
        long ownershipGeneration,
        Guid? leaseId,
        DateTimeOffset now)
    {
        if (deviceId != EditorDeviceId)
        {
            throw new CommercialReceivingDomainException("RECEIVING_OWNERSHIP_DEVICE_MISMATCH", "The authenticated Device is not the durable editor.");
        }

        if (ownershipGeneration != OwnershipGeneration)
        {
            throw new CommercialReceivingDomainException("RECEIVING_OWNERSHIP_GENERATION_STALE", "The ownership generation is stale.");
        }

        if (LeaseId is null || LeaseExpiresAtUtc is null || leaseId is null)
        {
            throw new CommercialReceivingDomainException("RECEIVING_LEASE_REQUIRED", "A current lease is required.");
        }

        if (leaseId != LeaseId)
        {
            throw new CommercialReceivingDomainException("RECEIVING_LEASE_INVALID", "The lease is invalid.");
        }

        if (LeaseExpiresAtUtc <= CommercialReceivingSession.Utc(now, nameof(now)))
        {
            throw new CommercialReceivingDomainException("RECEIVING_LEASE_REACQUISITION_REQUIRED", "The lease expired and must be reacquired by the durable editor.");
        }
    }

    public void Heartbeat(
        Guid deviceId,
        long ownershipGeneration,
        Guid leaseId,
        DateTimeOffset now,
        TimeSpan duration)
    {
        RequireLease(deviceId, ownershipGeneration, leaseId, now);
        LastHeartbeatAtUtc = now;
        LeaseExpiresAtUtc = LeaseExpiry(now, duration);
        Advance(now);
    }

    public void Reacquire(
        Guid deviceId,
        long ownershipGeneration,
        DateTimeOffset now,
        TimeSpan duration)
    {
        if (deviceId != EditorDeviceId)
        {
            throw new CommercialReceivingDomainException("RECEIVING_OWNERSHIP_DEVICE_MISMATCH", "Only the durable editor may reacquire the lease.");
        }

        if (ownershipGeneration != OwnershipGeneration)
        {
            throw new CommercialReceivingDomainException("RECEIVING_OWNERSHIP_GENERATION_STALE", "The ownership generation is stale.");
        }

        if (LeaseId is not null && LeaseExpiresAtUtc > CommercialReceivingSession.Utc(now, nameof(now)))
        {
            throw new CommercialReceivingDomainException("RECEIVING_LEASE_STILL_ACTIVE", "The current lease is still active.");
        }

        LeaseId = Uuid7.NewGuid();
        LastHeartbeatAtUtc = now;
        LastReacquiredAtUtc = now;
        LeaseExpiresAtUtc = LeaseExpiry(now, duration);
        Advance(now);
    }

    public void Transfer(
        Guid targetDeviceId,
        Guid actorUserId,
        long expectedGeneration,
        DateTimeOffset now)
    {
        if (expectedGeneration != OwnershipGeneration)
        {
            throw new CommercialReceivingDomainException("RECEIVING_OWNERSHIP_TRANSFER_CONFLICT", "The ownership generation changed before transfer.");
        }

        targetDeviceId = CommercialReceivingSession.RequiredId(targetDeviceId, nameof(targetDeviceId));
        if (targetDeviceId == EditorDeviceId)
        {
            throw new CommercialReceivingDomainException("RECEIVING_OWNERSHIP_TARGET_INVALID", "The target Device is already the editor.");
        }

        EditorDeviceId = targetDeviceId;
        OwnershipGeneration = checked(OwnershipGeneration + 1);
        LeaseId = null;
        LastHeartbeatAtUtc = null;
        LastTransferredAtUtc = now;
        LastTransferredByUserId = CommercialReceivingSession.RequiredId(actorUserId, nameof(actorUserId));
        LeaseExpiresAtUtc = null;
        Advance(now);
    }

    public void Close(DateTimeOffset now)
    {
        LeaseId = null;
        LeaseExpiresAtUtc = null;
        LastHeartbeatAtUtc = null;
        Advance(now);
    }

    private void Advance(DateTimeOffset now)
    {
        UpdatedAtUtc = CommercialReceivingSession.Utc(now, nameof(now));
        Version = checked(Version + 1);
    }

    private static DateTimeOffset LeaseExpiry(DateTimeOffset now, TimeSpan duration)
    {
        CommercialReceivingSession.Utc(now, nameof(now));
        if (duration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration));
        }

        return now.Add(duration);
    }
}
