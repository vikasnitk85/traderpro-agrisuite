using Microsoft.Extensions.DependencyInjection;

namespace TraderPro.Infrastructure.Modules.Documents;

/// <summary>
/// Registration boundary for the future Documents module.
/// </summary>
public static class DocumentsModuleRegistration
{
    public static IServiceCollection AddDocumentsModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return services;
    }
}
