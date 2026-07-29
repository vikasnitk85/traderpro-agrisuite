using Microsoft.Extensions.DependencyInjection;
using TraderPro.Application.Procurement.Poc;
using TraderPro.Infrastructure.Modules.Procurement.Poc;

namespace TraderPro.Infrastructure.Modules.Procurement;

/// <summary>
/// Registration boundary for the future Procurement module.
/// </summary>
public static class ProcurementModuleRegistration
{
    public static IServiceCollection AddProcurementModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped<IReceivingPocService, ReceivingPocService>();
        services.AddScoped<ITemporaryDeviceContextResolver,
            TemporaryDeviceContextResolver>();
        return services;
    }
}
