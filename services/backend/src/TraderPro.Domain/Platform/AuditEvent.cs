using TraderPro.Domain.Common;

namespace TraderPro.Domain.Platform;

public sealed class AuditEvent :
    IWorkspaceScoped,
    IOccurredAtUtc
{
    private AuditEvent()
    {
    }

    private AuditEvent(
        Guid workspaceId,
        Guid? companyId,
        Guid? branchId,
        Guid? actorUserId,
        Guid? actorDeviceId,
        string action,
        string aggregateType,
        Guid aggregateId,
        string? permissionKey,
        string? reason,
        string? beforeSnapshotJson,
        string? afterSnapshotJson,
        string correlationId,
        DateTimeOffset occurredAtUtc)
    {
        if (branchId is not null && companyId is null)
        {
            throw new ArgumentException(
                "CompanyId is required when BranchId is provided.",
                nameof(companyId));
        }

        Id = Uuid7.NewGuid();
        WorkspaceId = workspaceId;
        CompanyId = companyId;
        BranchId = branchId;
        ActorUserId = actorUserId;
        ActorDeviceId = actorDeviceId;
        Action = PlatformEntityGuard.Required(action, 200, nameof(action));
        AggregateType = PlatformEntityGuard.Required(
            aggregateType,
            200,
            nameof(aggregateType));
        AggregateId = PlatformEntityGuard.RequiredId(
            aggregateId,
            nameof(aggregateId));
        PermissionKey = PlatformEntityGuard.Optional(
            permissionKey,
            200,
            nameof(permissionKey));
        Reason = PlatformEntityGuard.Optional(reason, 2000, nameof(reason));
        BeforeSnapshotJson = PlatformEntityGuard.OptionalJson(
            beforeSnapshotJson,
            nameof(beforeSnapshotJson));
        AfterSnapshotJson = PlatformEntityGuard.OptionalJson(
            afterSnapshotJson,
            nameof(afterSnapshotJson));
        CorrelationId = PlatformEntityGuard.Required(
            correlationId,
            100,
            nameof(correlationId));
        OccurredAtUtc = PlatformEntityGuard.Utc(
            occurredAtUtc,
            nameof(occurredAtUtc));
    }

    public Guid Id { get; private set; }

    public Guid WorkspaceId { get; private set; }

    public Guid? CompanyId { get; private set; }

    public Guid? BranchId { get; private set; }

    public Guid? ActorUserId { get; private set; }

    public Guid? ActorDeviceId { get; private set; }

    public string Action { get; private set; } = string.Empty;

    public string AggregateType { get; private set; } = string.Empty;

    public Guid AggregateId { get; private set; }

    public string? PermissionKey { get; private set; }

    public string? Reason { get; private set; }

    public string? BeforeSnapshotJson { get; private set; }

    public string? AfterSnapshotJson { get; private set; }

    public string CorrelationId { get; private set; } = string.Empty;

    public DateTimeOffset OccurredAtUtc { get; private set; }

    public static AuditEvent Create(
        Guid workspaceId,
        Guid? companyId,
        Guid? branchId,
        Guid? actorUserId,
        Guid? actorDeviceId,
        string action,
        string aggregateType,
        Guid aggregateId,
        string? permissionKey,
        string? reason,
        string? beforeSnapshotJson,
        string? afterSnapshotJson,
        string correlationId,
        DateTimeOffset occurredAtUtc)
    {
        return new AuditEvent(
            workspaceId,
            companyId,
            branchId,
            actorUserId,
            actorDeviceId,
            action,
            aggregateType,
            aggregateId,
            permissionKey,
            reason,
            beforeSnapshotJson,
            afterSnapshotJson,
            correlationId,
            occurredAtUtc);
    }
}
