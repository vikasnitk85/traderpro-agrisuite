using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using TraderPro.Api.Http;
using TraderPro.Infrastructure;
using TraderPro.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);
var spikesEnabled =
    builder.Configuration.GetValue<bool>("TraderPro:Spikes:Enabled") &&
    (builder.Environment.IsDevelopment() ||
     builder.Environment.IsEnvironment("Testing"));

builder.Services.AddTraderProModules();
builder.Services.AddTraderProPersistence(builder.Configuration);

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
}

app.Run();

public partial class Program
{
}
