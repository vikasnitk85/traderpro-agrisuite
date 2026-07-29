using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TraderPro.Worker;

/// <summary>
/// Keeps the background-worker host alive until real jobs are introduced.
/// </summary>
public sealed class Worker(ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "TraderPro Worker foundation started; background jobs are not implemented.");

        await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
    }
}
