using TraderPro.Infrastructure;

namespace TraderPro.IntegrationTests;

public sealed class FoundationTests
{
    [Fact]
    public void Infrastructure_assembly_is_available()
    {
        Assert.Equal(
            "TraderPro.Infrastructure",
            typeof(InfrastructureAssemblyMarker).Assembly.GetName().Name);
    }
}
