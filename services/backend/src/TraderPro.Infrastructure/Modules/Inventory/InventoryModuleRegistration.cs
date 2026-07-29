using Microsoft.Extensions.DependencyInjection;

namespace TraderPro.Infrastructure.Modules.Inventory;

/// <summary>
/// Registration boundary for the future Inventory module.
/// </summary>
public static class InventoryModuleRegistration
{
    public static IServiceCollection AddInventoryModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return services;
    }
}
