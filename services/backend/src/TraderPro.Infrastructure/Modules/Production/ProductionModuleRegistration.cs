using Microsoft.Extensions.DependencyInjection;

namespace TraderPro.Infrastructure.Modules.Production;

/// <summary>
/// Registration boundary for the future Production module.
/// </summary>
public static class ProductionModuleRegistration
{
    public static IServiceCollection AddProductionModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return services;
    }
}
