using Microsoft.Extensions.DependencyInjection;

namespace TraderPro.Infrastructure.Modules.Sales;

/// <summary>
/// Registration boundary for the future Sales module.
/// </summary>
public static class SalesModuleRegistration
{
    public static IServiceCollection AddSalesModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return services;
    }
}
