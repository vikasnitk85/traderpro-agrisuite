using TraderPro.Domain;

namespace TraderPro.Application;

/// <summary>
/// Identifies the Application assembly for dependency-boundary tests.
/// </summary>
public sealed class ApplicationAssemblyMarker
{
    private ApplicationAssemblyMarker()
    {
    }

    internal static Type DomainDependency => typeof(DomainAssemblyMarker);
}
