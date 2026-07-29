using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TraderPro.Application.Common.Tenancy;
using TraderPro.Application.Common.Time;
using TraderPro.Infrastructure.Persistence.Interceptors;
using TraderPro.Infrastructure.Tenancy;
using TraderPro.Infrastructure.Time;

namespace TraderPro.Infrastructure.Persistence;

public static class PersistenceRegistration
{
    public static IServiceCollection AddTraderProPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString("TraderPro");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Configure ConnectionStrings:TraderPro or the " +
                "ConnectionStrings__TraderPro environment variable.");
        }

        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<CurrentWorkspaceAccessor>();
        services.AddScoped<ICurrentWorkspaceAccessor>(
            provider => provider.GetRequiredService<CurrentWorkspaceAccessor>());
        services.AddScoped<WorkspaceOwnershipInterceptor>();
        services.AddScoped<UtcTimestampInterceptor>();
        services.AddScoped<VersioningInterceptor>();
        services.AddDbContext<TraderProDbContext>((provider, options) =>
        {
            options
                .UseNpgsql(
                    connectionString,
                    npgsql => npgsql.MigrationsHistoryTable(
                        "__ef_migrations_history",
                        "platform"))
                .AddInterceptors(
                    provider.GetRequiredService<
                        WorkspaceOwnershipInterceptor>(),
                    provider.GetRequiredService<UtcTimestampInterceptor>(),
                    provider.GetRequiredService<VersioningInterceptor>());
        });
        services.AddHealthChecks()
            .AddDbContextCheck<TraderProDbContext>(
                "postgresql",
                tags: ["ready"]);

        return services;
    }
}
