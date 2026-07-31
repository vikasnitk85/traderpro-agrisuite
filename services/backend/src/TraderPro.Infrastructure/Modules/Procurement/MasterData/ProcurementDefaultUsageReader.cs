using Microsoft.EntityFrameworkCore;
using TraderPro.Application.Operations;
using TraderPro.Application.Platform.Identity;
using TraderPro.Infrastructure.Modules.Shared;
using TraderPro.Infrastructure.Persistence;

namespace TraderPro.Infrastructure.Modules.Procurement.MasterData;

internal sealed class ProcurementDefaultUsageReader(
    TraderProDbContext dbContext,
    IAuthenticatedTraderProContext context) :
    IBusinessLocationDefaultUsageReader
{
    public async Task<bool> IsCurrentDefaultAsync(
        Guid locationId,
        CancellationToken cancellationToken)
    {
        await PostgreSqlProcurementDefaultsLock.AcquireAsync(
            dbContext,
            context.WorkspaceId,
            context.CompanyId,
            cancellationToken);
        return await dbContext.CompanyProcurementSettings.AnyAsync(
            item =>
                item.CompanyId == context.CompanyId &&
                item.DefaultBranchId == context.DefaultBranchId &&
                item.DefaultDestinationLocationId == locationId,
            cancellationToken);
    }
}
