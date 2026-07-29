namespace TraderPro.Application.Common.Tenancy;

public interface ICurrentWorkspaceAccessor
{
    Guid? WorkspaceId { get; }
}
