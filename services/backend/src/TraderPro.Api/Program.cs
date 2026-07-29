using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using TraderPro.Infrastructure;
using TraderPro.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTraderProModules();
builder.Services.AddTraderProPersistence(builder.Configuration);

var app = builder.Build();

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

app.Run();

public partial class Program
{
}
