using TraderPro.Application.Common.Tenancy;

namespace TraderPro.Infrastructure.Tenancy;

public sealed class CurrentWorkspaceAccessor : ICurrentWorkspaceAccessor
{
    public Guid? WorkspaceId { get; private set; }

    public void SetWorkspace(Guid workspaceId)
    {
        if (workspaceId == Guid.Empty)
        {
            throw new ArgumentException(
                "A non-empty workspace identifier is required.",
                nameof(workspaceId));
        }

        if (WorkspaceId is not null && WorkspaceId != workspaceId)
        {
            throw new InvalidOperationException(
                "The current scope is already bound to another workspace.");
        }

        WorkspaceId = workspaceId;
    }
}
