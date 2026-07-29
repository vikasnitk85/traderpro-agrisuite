using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TraderPro.Application.Common.Time;
using TraderPro.Domain.Common;

namespace TraderPro.Infrastructure.Persistence.Interceptors;

public sealed class UtcTimestampInterceptor(IClock clock) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ApplyAndValidate(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplyAndValidate(eventData.Context);
        return ValueTask.FromResult(result);
    }

    private void ApplyAndValidate(DbContext? context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var now = clock.UtcNow;
        EnsureUtc(now, nameof(IClock.UtcNow));

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Added &&
                entry.Entity is ICreatedAtUtc)
            {
                entry.Property(nameof(ICreatedAtUtc.CreatedAtUtc))
                    .CurrentValue = now;
            }

            if (entry.State is EntityState.Added or EntityState.Modified &&
                entry.Entity is IUpdatedAtUtc)
            {
                entry.Property(nameof(IUpdatedAtUtc.UpdatedAtUtc))
                    .CurrentValue = now;
            }

            if (entry.State == EntityState.Added &&
                entry.Entity is IOccurredAtUtc)
            {
                entry.Property(nameof(IOccurredAtUtc.OccurredAtUtc))
                    .CurrentValue = now;
            }

            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                ValidateDateTimeOffsetProperties(entry);
            }
        }
    }

    private static void ValidateDateTimeOffsetProperties(EntityEntry entry)
    {
        foreach (var property in entry.Properties)
        {
            switch (property.CurrentValue)
            {
                case DateTimeOffset value:
                    EnsureUtc(value, property.Metadata.Name);
                    break;
            }
        }
    }

    private static void EnsureUtc(DateTimeOffset value, string propertyName)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new InvalidOperationException(
                $"{propertyName} must have a zero UTC offset.");
        }
    }
}
