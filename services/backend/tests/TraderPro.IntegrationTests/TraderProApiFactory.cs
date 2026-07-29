using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TraderPro.Application.Common.Time;

namespace TraderPro.IntegrationTests;

public sealed class TraderProApiFactory(
    string connectionString,
    bool spikesEnabled = true,
    bool procurementPocEnabled = false,
    int leaseMinutes = 5,
    DateTimeOffset? utcNow = null) : WebApplicationFactory<Program>
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
        builder.UseSetting(
            "TraderPro:Spikes:ProcurementPoc:Enabled",
            procurementPocEnabled.ToString());
        builder.ConfigureAppConfiguration(
            (_, configuration) =>
            {
                configuration.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:TraderPro"] = connectionString,
                        ["TraderPro:Spikes:Enabled"] =
                            spikesEnabled.ToString(),
                        ["TraderPro:Spikes:ProcurementPoc:Enabled"] =
                            procurementPocEnabled.ToString(),
                        ["TraderPro:Spikes:ProcurementPoc:LeaseMinutes"] =
                            leaseMinutes.ToString(
                                System.Globalization.CultureInfo.InvariantCulture),
                    });
            });
        if (utcNow is not null)
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IClock>();
                services.AddSingleton<IClock>(new FixedClock(utcNow.Value));
            });
        }
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }
}
