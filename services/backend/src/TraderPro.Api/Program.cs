using System.Threading.RateLimiting;
using System.Net;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using TraderPro.Api.Http;
using TraderPro.Application.Common.Errors;
using TraderPro.Application.Common.Time;
using TraderPro.Application.Platform.Identity;
using TraderPro.Application.Procurement.Poc;
using TraderPro.Application.Procurement.Receiving;
using TraderPro.Infrastructure;
using TraderPro.Infrastructure.Modules.Platform.Identity;
using TraderPro.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);
var spikesRequested =
    builder.Configuration.GetValue<bool>("TraderPro:Spikes:Enabled");
var procurementPocRequested =
    builder.Configuration.GetValue<bool>(
        "TraderPro:Spikes:ProcurementPoc:Enabled");
var identityBootstrapRequested =
    builder.Configuration.GetValue<bool>(
        "TraderPro:Spikes:IdentityBootstrap:Enabled");
if (builder.Environment.IsProduction() &&
    (spikesRequested ||
     procurementPocRequested ||
     identityBootstrapRequested))
{
    throw new InvalidOperationException(
        "Production startup refused because a TraderPro development spike is enabled.");
}

var safeDevelopmentEnvironment =
    builder.Environment.IsDevelopment() ||
    builder.Environment.IsEnvironment("Testing");
var spikesEnabled = spikesRequested && safeDevelopmentEnvironment;
var procurementPocEnabled =
    spikesEnabled && procurementPocRequested;
var identityBootstrapEnabled =
    identityBootstrapRequested && safeDevelopmentEnvironment;
var leaseMinutes = builder.Configuration.GetValue<int?>(
        "TraderPro:Spikes:ProcurementPoc:LeaseMinutes") ??
    ProcurementPocOptions.DevelopmentDefault.LeaseMinutes;
if (procurementPocEnabled && leaseMinutes <= 0)
{
    throw new InvalidOperationException(
        "TraderPro:Spikes:ProcurementPoc:LeaseMinutes must be positive.");
}

builder.Services.AddTraderProModules();
builder.Services.AddTraderProPersistence(builder.Configuration);
builder.Services.AddTraderProProductionIdentity(
    builder.Configuration,
    builder.Environment);
builder.Services.AddSingleton(
    new ProcurementPocOptions(
        leaseMinutes > 0
            ? leaseMinutes
            : ProcurementPocOptions.DevelopmentDefault.LeaseMinutes));
var commercialReceivingLeaseMinutes = builder.Configuration.GetValue<int?>(
        "TraderPro:CommercialReceiving:LeaseMinutes") ??
    CommercialReceivingOptions.DefaultLeaseMinutes;
if (commercialReceivingLeaseMinutes <= 0)
{
    throw new InvalidOperationException(
        "TraderPro:CommercialReceiving:LeaseMinutes must be positive.");
}
builder.Services.AddSingleton(
    new CommercialReceivingOptions(commercialReceivingLeaseMinutes));
builder.Services.AddTraderProCommercialAuthorization();

var authenticationOptions =
    AuthenticationConfiguration.ReadOptions(builder.Configuration);
var signingKey = AuthenticationConfiguration.DecodeSigningKey(
    authenticationOptions.SigningKey);
if (authenticationOptions.ForwardedHeadersEnabled)
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedProto;
        options.ForwardLimit = 1;
        options.KnownProxies.Clear();
        options.KnownIPNetworks.Clear();
        foreach (var address in authenticationOptions.TrustedProxyAddresses)
        {
            options.KnownProxies.Add(IPAddress.Parse(address));
        }
    });
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.IncludeErrorDetails = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = authenticationOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = authenticationOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(signingKey),
            ValidateLifetime = true,
            RequireExpirationTime = true,
            RequireSignedTokens = true,
            ClockSkew = TimeSpan.Zero,
        };
        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();
                if (!context.Response.HasStarted)
                {
                    await ApiProblemWriter.WriteAsync(
                        context.HttpContext,
                        new ApplicationProblemException(
                            "ACCESS_TOKEN_INVALID",
                            "A valid access token is required.",
                            ApplicationErrorCategory.Authentication),
                        CorrelationId(context.HttpContext));
                }
            },
            OnForbidden = async context =>
            {
                if (!context.Response.HasStarted)
                {
                    var endpointOptions = context.HttpContext.GetEndpoint()?
                        .Metadata
                        .GetMetadata<TraderProCommercialContextOptions>();
                    var ownerRequired =
                        endpointOptions?.AuthorizationKind is
                            TraderProCommercialAuthorizationKind.Owner;
                    await ApiProblemWriter.WriteAsync(
                        context.HttpContext,
                        new ApplicationProblemException(
                            ownerRequired
                                ? "OWNER_ROLE_REQUIRED"
                                : "AUTHORIZATION_DENIED",
                            ownerRequired
                                ? "The Owner role is required."
                                : "The request is not authorized.",
                            ApplicationErrorCategory.Authorization),
                        CorrelationId(context.HttpContext));
                }
            },
        };
    });
builder.Services
    .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IClock>((options, clock) =>
    {
        options.TokenValidationParameters.LifetimeValidator =
            (notBefore, expires, _, _) =>
            {
                var now = clock.UtcNow.UtcDateTime;
                return expires is not null &&
                    expires.Value > now &&
                    (notBefore is null || notBefore.Value <= now);
            };
    });
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, _) =>
    {
        await ApiProblemWriter.WriteAsync(
            context.HttpContext,
            new ApplicationProblemException(
                "RATE_LIMIT_EXCEEDED",
                "Too many requests. Try again later.",
                ApplicationErrorCategory.TooManyRequests,
                retryable: true),
            CorrelationId(context.HttpContext));
    };
    options.AddPolicy(
        IdentityEndpointRegistration.LoginRatePolicy,
        context => FixedWindow(context, 10));
    options.AddPolicy(
        IdentityEndpointRegistration.ActivationRatePolicy,
        context => FixedWindow(context, 5));
    options.AddPolicy(
        IdentityEndpointRegistration.RefreshRatePolicy,
        context => FixedWindow(context, 30));
});

var app = builder.Build();

app.UseMiddleware<ApiProblemMiddleware>();
if (authenticationOptions.ForwardedHeadersEnabled)
{
    app.UseForwardedHeaders();
}

app.UseAuthentication();
app.UseMiddleware<CommercialIdentityRequestMiddleware>();
app.UseAuthorization();
app.UseRateLimiter();

app.MapHealthChecks(
    "/health/live",
    new HealthCheckOptions
    {
        Predicate = _ => false,
    });
app.MapHealthChecks(
    "/health/ready",
    new HealthCheckOptions
    {
        Predicate = registration => registration.Tags.Contains("ready"),
    });
app.MapTraderProIdentityEndpoints(
    authenticationOptions.RateLimitingEnabled,
    identityBootstrapEnabled,
    builder.Environment.IsEnvironment("Testing"));
app.MapTraderProCommercialMasterDataEndpoints();
app.MapTraderProCommercialSupplierCatalogEndpoints();
app.MapTraderProCommercialReceivingEndpoints();

if (spikesEnabled)
{
    app.UseMiddleware<SpikeRequestContextMiddleware>();
    app.MapTraderProSpikeEndpoints();
    if (procurementPocEnabled)
    {
        app.MapTraderProProcurementPocEndpoints();
    }
}

app.Run();

static RateLimitPartition<string> FixedWindow(
    HttpContext context,
    int permitLimit)
{
    var partition = context.Connection.RemoteIpAddress?.ToString() ??
        "unknown";
    return RateLimitPartition.GetFixedWindowLimiter(
        partition,
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = permitLimit,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true,
        });
}

static string CorrelationId(HttpContext context)
{
    if (context.Items.TryGetValue(
            CommercialIdentityRequestMiddleware.CorrelationItemKey,
            out var commercial) &&
        commercial is string commercialId)
    {
        return commercialId;
    }

    if (context.Items.TryGetValue(
            SpikeRequestContextMiddleware.CorrelationItemKey,
            out var spike) &&
        spike is string spikeId)
    {
        return spikeId;
    }

    var created = Guid.CreateVersion7().ToString("D");
    context.Response.Headers["X-Correlation-ID"] = created;
    return created;
}

public partial class Program
{
}
