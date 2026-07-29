using Microsoft.Extensions.DependencyInjection;

namespace TraderPro.Infrastructure.Modules.MasterData;

/// <summary>
/// Registration boundary for the future MasterData module.
/// </summary>
public static class MasterDataModuleRegistration
{
    public static IServiceCollection AddMasterDataModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return services;
    }
}
