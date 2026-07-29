using TraderPro.Application;
using TraderPro.Infrastructure;

namespace TraderPro.Api;

/// <summary>
/// Identifies the API assembly for dependency-boundary tests.
/// </summary>
public sealed class ApiAssemblyMarker
{
    private ApiAssemblyMarker()
    {
    }

    internal static Type ApplicationDependency => typeof(ApplicationAssemblyMarker);

    internal static Type InfrastructureDependency => typeof(InfrastructureAssemblyMarker);
}
