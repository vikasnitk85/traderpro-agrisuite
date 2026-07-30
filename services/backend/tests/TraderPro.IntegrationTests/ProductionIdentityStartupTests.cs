using System.Security.Cryptography;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;

namespace TraderPro.IntegrationTests;

public sealed class ProductionIdentityStartupTests
{
    [Theory]
    [InlineData("TraderPro:Spikes:IdentityBootstrap:Enabled")]
    [InlineData("TraderPro:Spikes:Enabled")]
    [InlineData("TraderPro:Spikes:ProcurementPoc:Enabled")]
    public void Production_refuses_development_spikes(string setting)
    {
        using var factory = new ProductionStartupFactory(
            new Dictionary<string, string>
            {
                [setting] = "true",
            });

        var exception = Assert.ThrowsAny<Exception>(
            () => factory.CreateClient());
        Assert.Contains(
            "development spike is enabled",
            exception.ToString(),
            StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("TraderPro:Authentication:SigningKey", "")]
    [InlineData("TraderPro:Authentication:SigningKey", "dG9vLXNob3J0")]
    [InlineData("TraderPro:Authentication:Issuer", "")]
    [InlineData("TraderPro:Authentication:Audience", "")]
    [InlineData(
        "TraderPro:Authentication:DataProtectionKeyRingPath",
        "")]
    [InlineData("TraderPro:Authentication:RequireHttps", "false")]
    public void Production_refuses_incomplete_authentication_configuration(
        string setting,
        string value)
    {
        using var factory = new ProductionStartupFactory(
            new Dictionary<string, string>
            {
                [setting] = value,
            });

        var exception = Assert.ThrowsAny<Exception>(
            () => factory.CreateClient());
        Assert.Contains(
            "AUTHENTICATION_CONFIGURATION_INVALID",
            exception.ToString(),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task Production_http_authentication_is_rejected_before_secret_handling()
    {
        using var factory = new ProductionStartupFactory(
            new Dictionary<string, string>());
        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { },
            TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("HTTPS_REQUIRED", body, StringComparison.Ordinal);
        Assert.Contains(
            "no-store",
            response.Headers.CacheControl?.ToString() ?? string.Empty,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Development_can_explicitly_allow_http_authentication()
    {
        using var factory = new ProductionStartupFactory(
            new Dictionary<string, string>
            {
                ["TraderPro:Authentication:RequireHttps"] = "false",
            },
            "Development");
        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { },
            TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.DoesNotContain("HTTPS_REQUIRED", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Untrusted_forwarded_proto_does_not_bypass_https()
    {
        using var factory = new ProductionStartupFactory(
            new Dictionary<string, string>
            {
                ["TraderPro:Authentication:ForwardedHeadersEnabled"] =
                    "true",
                ["TraderPro:Authentication:TrustedProxyAddresses:0"] =
                    "203.0.113.10",
            });
        using var client = factory.CreateClient();
        var responseContext = await factory.Server.SendAsync(
            context =>
            {
                context.Connection.RemoteIpAddress =
                    IPAddress.Parse("198.51.100.20");
                context.Request.Method = HttpMethods.Post;
                context.Request.Path = "/api/v1/auth/login";
                context.Request.Scheme = "http";
                context.Request.Headers["X-Forwarded-Proto"] = "https";
                context.Request.ContentType = "application/json";
                context.Request.Body = new MemoryStream(
                    Encoding.UTF8.GetBytes("{}"));
            },
            TestContext.Current.CancellationToken);

        Assert.Equal(
            StatusCodes.Status401Unauthorized,
            responseContext.Response.StatusCode);
    }

    [Fact]
    public async Task Explicitly_trusted_proxy_can_forward_https()
    {
        const string trustedProxy = "203.0.113.10";
        using var factory = new ProductionStartupFactory(
            new Dictionary<string, string>
            {
                ["TraderPro:Authentication:ForwardedHeadersEnabled"] =
                    "true",
                ["TraderPro:Authentication:TrustedProxyAddresses:0"] =
                    trustedProxy,
            });
        using var client = factory.CreateClient();
        var responseContext = await factory.Server.SendAsync(
            context =>
            {
                context.Connection.RemoteIpAddress =
                    IPAddress.Parse(trustedProxy);
                context.Request.Method = HttpMethods.Post;
                context.Request.Path = "/api/v1/auth/login";
                context.Request.Scheme = "http";
                context.Request.Headers["X-Forwarded-Proto"] = "https";
                context.Request.ContentType = "application/json";
                context.Request.Body = new MemoryStream(
                    Encoding.UTF8.GetBytes("{}"));
            },
            TestContext.Current.CancellationToken);

        Assert.Equal(
            StatusCodes.Status400BadRequest,
            responseContext.Response.StatusCode);
        Assert.Contains(
            "no-store",
            responseContext.Response.Headers.CacheControl.ToString(),
            StringComparison.OrdinalIgnoreCase);
    }

    private sealed class ProductionStartupFactory(
        IReadOnlyDictionary<string, string> overrides,
        string environment = "Production") :
        WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment(environment);
            var safe = new Dictionary<string, string>
            {
                ["ConnectionStrings:TraderPro"] =
                    "Host=127.0.0.1;Database=unused;Username=unused;Password=unused",
                ["TraderPro:Authentication:Issuer"] =
                    "TraderPro.ProductionStartupTests",
                ["TraderPro:Authentication:Audience"] =
                    "TraderPro.ProductionStartupTests.Client",
                ["TraderPro:Authentication:SigningKey"] =
                    Convert.ToBase64String(
                        RandomNumberGenerator.GetBytes(32)),
                ["TraderPro:Authentication:DataProtectionKeyRingPath"] =
                    Path.Combine(
                        Environment.CurrentDirectory,
                        "artifacts",
                        "production-startup-test-key-ring"),
            };
            foreach (var (name, value) in overrides)
            {
                safe[name] = value;
            }

            foreach (var (name, value) in safe)
            {
                builder.UseSetting(name, value);
            }
        }
    }
}
