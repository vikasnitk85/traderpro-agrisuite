using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TraderPro.Infrastructure;
using TraderPro.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddTraderProModules();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();

await host.RunAsync();
