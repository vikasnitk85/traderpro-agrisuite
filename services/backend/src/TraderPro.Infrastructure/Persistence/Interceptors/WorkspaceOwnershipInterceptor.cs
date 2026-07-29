using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TraderPro.Application.Common.Tenancy;
using TraderPro.Domain.Common;

namespace TraderPro.Infrastructure.Persistence.Interceptors;

public sealed class WorkspaceOwnershipInterceptor(
    ICurrentWorkspaceAccessor currentWorkspace) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        Validate(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Validate(eventData.Context);
        return ValueTask.FromResult(result);
    }

    private void Validate(DbContext? context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var entries = context.ChangeTracker
            .Entries<IWorkspaceScoped>()
            .Where(entry => entry.State is
                EntityState.Added or
                EntityState.Modified or
                EntityState.Deleted);

        foreach (var entry in entries)
        {
            ValidateEntry(entry);
        }
    }

    private void ValidateEntry(EntityEntry<IWorkspaceScoped> entry)
    {
        var activeWorkspaceId = currentWorkspace.WorkspaceId ??
            throw new InvalidOperationException(
                "A current workspace is required for workspace-owned writes.");
        var workspaceProperty = entry.Property(
            nameof(IWorkspaceScoped.WorkspaceId));

        if (entry.State == EntityState.Added)
        {
            var requestedWorkspaceId = (Guid)workspaceProperty.CurrentValue!;
            if (requestedWorkspaceId == Guid.Empty)
            {
                workspaceProperty.CurrentValue = activeWorkspaceId;
                return;
            }

            if (requestedWorkspaceId != activeWorkspaceId)
            {
                throw new InvalidOperationException(
                    "A workspace-owned record cannot be inserted for another workspace.");
            }

            return;
        }

        var originalWorkspaceId = (Guid)workspaceProperty.OriginalValue!;
        var currentWorkspaceId = (Guid)workspaceProperty.CurrentValue!;

        if (originalWorkspaceId != currentWorkspaceId)
        {
            throw new InvalidOperationException(
                "Workspace ownership cannot be changed after creation.");
        }

        if (originalWorkspaceId != activeWorkspaceId)
        {
            throw new InvalidOperationException(
                "A workspace-owned record from another workspace cannot be updated or deleted.");
        }
    }
}
