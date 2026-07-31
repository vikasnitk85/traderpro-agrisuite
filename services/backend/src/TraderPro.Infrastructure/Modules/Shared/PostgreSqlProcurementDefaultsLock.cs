using Microsoft.EntityFrameworkCore;
using TraderPro.Infrastructure.Persistence;

namespace TraderPro.Infrastructure.Modules.Shared;

internal static class PostgreSqlProcurementDefaultsLock
{
    public static Task AcquireAsync(
        TraderProDbContext dbContext,
        Guid workspaceId,
        Guid companyId,
        CancellationToken cancellationToken)
    {
        return dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT procurement.lock_procurement_defaults({workspaceId}, {companyId})",
            cancellationToken);
    }
}
