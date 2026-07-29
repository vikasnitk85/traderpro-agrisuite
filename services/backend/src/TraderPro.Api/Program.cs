using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using TraderPro.Api.Http;
using TraderPro.Application.Procurement.Poc;
using TraderPro.Infrastructure;
using TraderPro.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);
var spikesEnabled =
    builder.Configuration.GetValue<bool>("TraderPro:Spikes:Enabled") &&
    (builder.Environment.IsDevelopment() ||
     builder.Environment.IsEnvironment("Testing"));
var procurementPocEnabled =
    spikesEnabled &&
    builder.Configuration.GetValue<bool>(
        "TraderPro:Spikes:ProcurementPoc:Enabled");
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
builder.Services.AddSingleton(
    new ProcurementPocOptions(
        leaseMinutes > 0
            ? leaseMinutes
            : ProcurementPocOptions.DevelopmentDefault.LeaseMinutes));

var app = builder.Build();

app.UseMiddleware<ApiProblemMiddleware>();

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

public partial class Program
{
}
