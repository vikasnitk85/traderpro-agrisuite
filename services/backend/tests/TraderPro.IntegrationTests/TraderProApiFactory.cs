using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace TraderPro.IntegrationTests;

public sealed class TraderProApiFactory(
    string connectionString,
    bool spikesEnabled = true) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting(
            "ConnectionStrings:TraderPro",
            connectionString);
        builder.UseSetting(
            "TraderPro:Spikes:Enabled",
            spikesEnabled.ToString());
        builder.ConfigureAppConfiguration(
            (_, configuration) =>
            {
                configuration.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:TraderPro"] = connectionString,
                        ["TraderPro:Spikes:Enabled"] =
                            spikesEnabled.ToString(),
                    });
            });
    }
}
