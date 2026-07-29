using Microsoft.EntityFrameworkCore;
using TraderPro.Application.Platform.CommandProbes;
using TraderPro.Infrastructure.Persistence;
using TraderPro.Infrastructure.Tenancy;

namespace TraderPro.Infrastructure.Modules.Platform;

internal sealed class TemporaryWorkspaceContextResolver(
    TraderProDbContext dbContext,
    CurrentWorkspaceAccessor currentWorkspace) :
    ITemporaryWorkspaceContextResolver
{
    public async Task<bool> TryBindAsync(
        Guid workspaceId,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.Workspaces
            .AsNoTracking()
            .AnyAsync(
                workspace => workspace.Id == workspaceId,
                cancellationToken);
        if (!exists)
        {
            return false;
        }

        currentWorkspace.SetWorkspace(workspaceId);
        return true;
    }
}
