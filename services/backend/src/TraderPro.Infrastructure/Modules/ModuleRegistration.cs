using Microsoft.Extensions.DependencyInjection;
using TraderPro.Infrastructure.Modules.Documents;
using TraderPro.Infrastructure.Modules.Finance;
using TraderPro.Infrastructure.Modules.Inventory;
using TraderPro.Infrastructure.Modules.MasterData;
using TraderPro.Infrastructure.Modules.Operations;
using TraderPro.Infrastructure.Modules.Platform;
using TraderPro.Infrastructure.Modules.Procurement;
using TraderPro.Infrastructure.Modules.Production;
using TraderPro.Infrastructure.Modules.Reporting;
using TraderPro.Infrastructure.Modules.Sales;
using TraderPro.Infrastructure.Modules.Shared;

namespace TraderPro.Infrastructure;

/// <summary>
/// Composes the empty module registration boundaries for the deployable hosts.
/// </summary>
public static class ModuleRegistration
{
    public static IServiceCollection AddTraderProModules(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<PostgreSqlIdempotentCommandExecutor>();

        return services
            .AddPlatformModule()
            .AddOperationsModule()
            .AddMasterDataModule()
            .AddProcurementModule()
            .AddInventoryModule()
            .AddSalesModule()
            .AddFinanceModule()
            .AddProductionModule()
            .AddDocumentsModule()
            .AddReportingModule();
    }
}
