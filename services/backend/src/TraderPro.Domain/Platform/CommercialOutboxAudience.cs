using TraderPro.Domain.Common;

namespace TraderPro.Domain.Platform;

public sealed class CommercialOutboxAudience : IWorkspaceScoped
{
    private CommercialOutboxAudience()
    {
    }

    private CommercialOutboxAudience(
        Guid outboxMessageId,
        Guid workspaceId,
        Guid companyId,
        OutboxAudience audience,
        Guid? targetDeviceId)
    {
        OutboxMessageId = PlatformEntityGuard.RequiredId(
            outboxMessageId,
            nameof(outboxMessageId));
        WorkspaceId = PlatformEntityGuard.RequiredId(
            workspaceId,
            nameof(workspaceId));
        CompanyId = PlatformEntityGuard.RequiredId(companyId, nameof(companyId));
        Audience = PlatformEntityGuard.Defined(audience, nameof(audience));
        TargetDeviceId = targetDeviceId;
        if (audience is OutboxAudience.TargetDevice &&
            (targetDeviceId is null || targetDeviceId == Guid.Empty))
        {
            throw new ArgumentException(
                "A TargetDevice audience requires a target Device.");
        }

        if (audience is OutboxAudience.OwnerBroadcast && targetDeviceId is not null)
        {
            throw new ArgumentException(
                "An OwnerBroadcast audience cannot target a Device.");
        }
    }

    public Guid OutboxMessageId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid CompanyId { get; private set; }
    public OutboxAudience Audience { get; private set; }
    public Guid? TargetDeviceId { get; private set; }

    public static CommercialOutboxAudience Create(
        OutboxMessage message,
        Guid companyId,
        OutboxAudience audience,
        Guid? targetDeviceId = null)
    {
        ArgumentNullException.ThrowIfNull(message);
        if (message.EventStream is not OutboxEventStream.CommercialMobileSync)
        {
            throw new ArgumentException(
                "Commercial audience metadata requires CommercialMobileSync.",
                nameof(message));
        }

        return new(
            message.Id,
            message.WorkspaceId,
            companyId,
            audience,
            targetDeviceId);
    }
}
