using Microsoft.Extensions.DependencyInjection;
using TraderPro.Application.Operations;

namespace TraderPro.Infrastructure.Modules.Operations;

public static class OperationsModuleRegistration
{
    public static IServiceCollection AddOperationsModule(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped<IBusinessLocationService, BusinessLocationService>();
        return services;
    }
}
