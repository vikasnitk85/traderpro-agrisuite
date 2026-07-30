using System.Security.Cryptography;
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
    bool identityBootstrapEnabled = false,
    bool rateLimitingEnabled = false,
    bool requireHttps = false,
    bool forwardedHeadersEnabled = false,
    string[]? trustedProxyAddresses = null,
    int leaseMinutes = 5,
    int refreshReplaySeconds = 30,
    DateTimeOffset? utcNow = null) : WebApplicationFactory<Program>
{
    public const string TestIssuer = "TraderPro.IntegrationTests";
    public const string TestAudience =
        "TraderPro.IntegrationTests.Client";

    private readonly string _signingKey =
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    private readonly string _keyRingPath = Path.Combine(
        Path.GetTempPath(),
        "traderpro-identity-tests",
        Guid.NewGuid().ToString("N"));

    public string SigningKey => _signingKey;

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
        builder.UseSetting(
            "TraderPro:Spikes:IdentityBootstrap:Enabled",
            identityBootstrapEnabled.ToString());
        builder.UseSetting(
            "TraderPro:Authentication:Issuer",
            TestIssuer);
        builder.UseSetting(
            "TraderPro:Authentication:Audience",
            TestAudience);
        builder.UseSetting(
            "TraderPro:Authentication:SigningKey",
            _signingKey);
        builder.UseSetting(
            "TraderPro:Authentication:DataProtectionKeyRingPath",
            _keyRingPath);
        builder.UseSetting(
            "TraderPro:Authentication:RateLimitingEnabled",
            rateLimitingEnabled.ToString());
        builder.UseSetting(
            "TraderPro:Authentication:RequireHttps",
            requireHttps.ToString());
        builder.UseSetting(
            "TraderPro:Authentication:ForwardedHeadersEnabled",
            forwardedHeadersEnabled.ToString());
        if (trustedProxyAddresses is not null)
        {
            for (var index = 0; index < trustedProxyAddresses.Length; index++)
            {
                builder.UseSetting(
                    $"TraderPro:Authentication:TrustedProxyAddresses:{index}",
                    trustedProxyAddresses[index]);
            }
        }
        builder.UseSetting(
            "TraderPro:Authentication:RefreshReplaySeconds",
            refreshReplaySeconds.ToString(
                System.Globalization.CultureInfo.InvariantCulture));
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
                        ["TraderPro:Spikes:IdentityBootstrap:Enabled"] =
                            identityBootstrapEnabled.ToString(),
                        ["TraderPro:Spikes:ProcurementPoc:LeaseMinutes"] =
                            leaseMinutes.ToString(
                                System.Globalization.CultureInfo.InvariantCulture),
                        ["TraderPro:Authentication:Issuer"] =
                            TestIssuer,
                        ["TraderPro:Authentication:Audience"] =
                            TestAudience,
                        ["TraderPro:Authentication:SigningKey"] =
                            _signingKey,
                        ["TraderPro:Authentication:DataProtectionKeyRingPath"] =
                            _keyRingPath,
                        ["TraderPro:Authentication:RateLimitingEnabled"] =
                            rateLimitingEnabled.ToString(),
                        ["TraderPro:Authentication:RequireHttps"] =
                            requireHttps.ToString(),
                        ["TraderPro:Authentication:ForwardedHeadersEnabled"] =
                            forwardedHeadersEnabled.ToString(),
                        ["TraderPro:Authentication:RefreshReplaySeconds"] =
                            refreshReplaySeconds.ToString(
                                System.Globalization.CultureInfo.InvariantCulture),
                    });
                if (trustedProxyAddresses is not null)
                {
                    for (var index = 0;
                         index < trustedProxyAddresses.Length;
                         index++)
                    {
                        configuration.AddInMemoryCollection(
                            new Dictionary<string, string?>
                            {
                                [$"TraderPro:Authentication:TrustedProxyAddresses:{index}"] =
                                    trustedProxyAddresses[index],
                            });
                    }
                }
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
