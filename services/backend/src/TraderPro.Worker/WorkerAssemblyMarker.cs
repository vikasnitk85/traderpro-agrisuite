using TraderPro.Application;
using TraderPro.Infrastructure;

namespace TraderPro.Worker;

/// <summary>
/// Identifies the Worker assembly for dependency-boundary tests.
/// </summary>
public sealed class WorkerAssemblyMarker
{
    private WorkerAssemblyMarker()
    {
    }

    internal static Type ApplicationDependency => typeof(ApplicationAssemblyMarker);

    internal static Type InfrastructureDependency => typeof(InfrastructureAssemblyMarker);
}
