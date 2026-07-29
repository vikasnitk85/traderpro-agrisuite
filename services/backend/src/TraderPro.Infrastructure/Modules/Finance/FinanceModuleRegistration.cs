using Microsoft.Extensions.DependencyInjection;

namespace TraderPro.Infrastructure.Modules.Finance;

/// <summary>
/// Registration boundary for the future Finance module.
/// </summary>
public static class FinanceModuleRegistration
{
    public static IServiceCollection AddFinanceModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return services;
    }
}
