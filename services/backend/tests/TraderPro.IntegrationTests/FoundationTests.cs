using TraderPro.Api;
using TraderPro.Infrastructure;

namespace TraderPro.IntegrationTests;

public sealed class FoundationTests
{
    [Fact]
    public void Host_and_infrastructure_assemblies_are_available()
    {
        Assert.Equal(
            "TraderPro.Api",
            typeof(ApiAssemblyMarker).Assembly.GetName().Name);
        Assert.Equal(
            "TraderPro.Infrastructure",
            typeof(InfrastructureAssemblyMarker).Assembly.GetName().Name);
    }
}
