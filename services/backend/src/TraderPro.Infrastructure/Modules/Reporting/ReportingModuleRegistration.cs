using Microsoft.Extensions.DependencyInjection;

namespace TraderPro.Infrastructure.Modules.Reporting;

/// <summary>
/// Registration boundary for the future Reporting module.
/// </summary>
public static class ReportingModuleRegistration
{
    public static IServiceCollection AddReportingModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return services;
    }
}
