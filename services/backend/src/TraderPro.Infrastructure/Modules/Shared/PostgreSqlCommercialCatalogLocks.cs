using Microsoft.EntityFrameworkCore;
using TraderPro.Infrastructure.Persistence;

namespace TraderPro.Infrastructure.Modules.Shared;

internal static class PostgreSqlCommercialCatalogLocks
{
    public static Task AcquireSupplierScopeAsync(
        TraderProDbContext dbContext,
        Guid workspaceId,
        Guid companyId,
        Guid supplierId,
        CancellationToken cancellationToken)
    {
        return dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT procurement.lock_supplier_product_scope({workspaceId}, {companyId}, {supplierId})",
            cancellationToken);
    }

    public static Task AcquireProductBagDefaultAsync(
        TraderProDbContext dbContext,
        Guid workspaceId,
        Guid companyId,
        Guid productId,
        CancellationToken cancellationToken)
    {
        return dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT catalog.lock_product_bag_default({workspaceId}, {companyId}, {productId})",
            cancellationToken);
    }

    public static Task AcquireBagTypeUsageAsync(
        TraderProDbContext dbContext,
        Guid workspaceId,
        Guid companyId,
        Guid bagTypeId,
        CancellationToken cancellationToken)
    {
        return dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT catalog.lock_bag_type_usage({workspaceId}, {companyId}, {bagTypeId})",
            cancellationToken);
    }
}
