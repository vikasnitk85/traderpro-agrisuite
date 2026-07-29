using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TraderPro.Domain.Common;

namespace TraderPro.Infrastructure.Persistence.Interceptors;

public sealed class VersioningInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ApplyVersions(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplyVersions(eventData.Context);
        return ValueTask.FromResult(result);
    }

    private static void ApplyVersions(DbContext? context)
    {
        ArgumentNullException.ThrowIfNull(context);

        foreach (var entry in context.ChangeTracker.Entries<IVersionedEntity>())
        {
            var versionProperty = entry.Property(
                nameof(IVersionedEntity.Version));

            if (entry.State == EntityState.Added)
            {
                versionProperty.CurrentValue = 1L;
            }
            else if (entry.State == EntityState.Modified)
            {
                var originalVersion = (long)versionProperty.OriginalValue!;
                versionProperty.CurrentValue = checked(originalVersion + 1);
            }
        }
    }
}
