using Microsoft.Extensions.DependencyInjection;

namespace TraderPro.Infrastructure.Modules.Procurement;

/// <summary>
/// Registration boundary for the future Procurement module.
/// </summary>
public static class ProcurementModuleRegistration
{
    public static IServiceCollection AddProcurementModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return services;
    }
}
