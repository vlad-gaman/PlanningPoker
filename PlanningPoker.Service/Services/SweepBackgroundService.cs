using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PlanningPoker.Common.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Service.Services;

/// <summary>Runs <see cref="ISweepService.Sweep"/> on every tick of the configured interval until the host stops.</summary>
public class SweepBackgroundService(ISweepService sweep, IOptions<SweepOptions> options, TimeProvider time, IIssueLog issues, IServerStatistics statistics, ILogger<SweepBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(options.Value.Interval, time);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                RunOnce();
            }
        }
        catch (OperationCanceledException)
        {
            // The host is stopping.
        }
    }

    /// <summary>One sweep. A failure is logged and does not end the loop.</summary>
    public void RunOnce()
    {
        try
        {
            var result = sweep.Sweep();
            statistics.RecordSweep(result.Rooms, result.People);
            if (result.Rooms > 0 || result.People > 0)
            {
                logger.LogInformation("Swept {Rooms} rooms and {People} people", result.Rooms, result.People);
                issues.Record(IssueLevel.Info, "Sweep", $"Swept {result.Rooms} rooms and {result.People} people");
            }

            issues.Prune();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Sweeping abandoned rooms and people failed");
        }
    }
}
