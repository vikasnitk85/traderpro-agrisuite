using Microsoft.Extensions.DependencyInjection;

namespace TraderPro.Infrastructure.Modules.Platform;

/// <summary>
/// Registration boundary for the future Platform module.
/// </summary>
public static class PlatformModuleRegistration
{
    public static IServiceCollection AddPlatformModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return services;
    }
}
