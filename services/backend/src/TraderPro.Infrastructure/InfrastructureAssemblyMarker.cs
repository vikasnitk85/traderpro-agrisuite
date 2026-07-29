using TraderPro.Application;
using TraderPro.Domain;

namespace TraderPro.Infrastructure;

/// <summary>
/// Identifies the Infrastructure assembly for dependency-boundary tests.
/// </summary>
public sealed class InfrastructureAssemblyMarker
{
    private InfrastructureAssemblyMarker()
    {
    }

    internal static Type ApplicationDependency => typeof(ApplicationAssemblyMarker);

    internal static Type DomainDependency => typeof(DomainAssemblyMarker);
}
