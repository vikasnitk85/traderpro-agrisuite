using Microsoft.Extensions.DependencyInjection;
using TraderPro.Application.Platform.CommandProbes;

namespace TraderPro.Infrastructure.Modules.Platform;

/// <summary>
/// Registration boundary for the future Platform module.
/// </summary>
public static class PlatformModuleRegistration
{
    public static IServiceCollection AddPlatformModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped<CommandProbeService>();
        services.AddScoped<ICommandProbeCommandExecutor>(
            provider => provider.GetRequiredService<CommandProbeService>());
        services.AddScoped<ICommandProbeReader>(
            provider => provider.GetRequiredService<CommandProbeService>());
        services.AddScoped<ICloudEventCursorReader, CloudEventCursorReader>();
        services.AddScoped<ITemporaryWorkspaceContextResolver,
            TemporaryWorkspaceContextResolver>();
        services.AddScoped<CreateCommandProbeHandler>();
        services.AddScoped<IncrementCommandProbeHandler>();
        return services;
    }
}
