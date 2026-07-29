using TraderPro.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTraderProModules();

var app = builder.Build();

app.MapGet("/", () => Results.Ok(new
{
    Service = "TraderPro.Api",
    Status = "Foundation scaffold",
}));

app.Run();

public partial class Program
{
}
