using TraderPro.Application;
using TraderPro.Domain;

namespace TraderPro.UnitTests;

public sealed class FoundationTests
{
    [Fact]
    public void Core_layer_assemblies_are_available()
    {
        Assert.Equal(
            "TraderPro.Application",
            typeof(ApplicationAssemblyMarker).Assembly.GetName().Name);
        Assert.Equal(
            "TraderPro.Domain",
            typeof(DomainAssemblyMarker).Assembly.GetName().Name);
    }
}
