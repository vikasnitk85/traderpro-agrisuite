using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TraderPro.Application.Platform.Identity;
using TraderPro.Domain.Platform;

namespace TraderPro.Infrastructure.Modules.Platform.Identity;

public static class ProductionIdentityRegistration
{
    public static IServiceCollection AddTraderProProductionIdentity(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        var options = AuthenticationConfiguration.ReadOptions(configuration);
        AuthenticationConfiguration.Validate(
            options,
            environment.EnvironmentName == Environments.Production);

        var keyRing = new DirectoryInfo(
            Path.GetFullPath(options.DataProtectionKeyRingPath));
        keyRing.Create();

        services.AddSingleton(options);
        services.AddDataProtection()
            .SetApplicationName(
                AuthenticationConfiguration.DataProtectionApplicationName)
            .PersistKeysToFileSystem(keyRing);
        services.AddSingleton<IPasswordHasher<PlatformUser>,
            PasswordHasher<PlatformUser>>();
        services.AddSingleton<JwtAccessTokenIssuer>();
        services.AddScoped<CurrentAuthenticatedTraderProContext>();
        services.AddScoped<IAuthenticatedTraderProContext>(
            provider => provider.GetRequiredService<
                CurrentAuthenticatedTraderProContext>());
        services.AddScoped<ICommercialIdentityContextResolver,
            CommercialIdentityContextResolver>();
        services.AddScoped<PostgreSqlIdentitySessionLock>();
        services.AddScoped<IProductionIdentityService,
            ProductionIdentityService>();
        return services;
    }
}
