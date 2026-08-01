using Microsoft.Extensions.DependencyInjection;
using TraderPro.Application.Catalog;

namespace TraderPro.Infrastructure.Modules.Catalog;

public static class CatalogModuleRegistration
{
    public static IServiceCollection AddCatalogModule(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped<CatalogService>();
        services.AddScoped<ICatalogService>(provider =>
            provider.GetRequiredService<CatalogService>());
        services.AddScoped<IBagTypeProductStandardUsageReader>(provider =>
            provider.GetRequiredService<CatalogService>());
        return services;
    }
}
