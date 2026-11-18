using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Service.Services;

/// <summary>
/// Runs <see cref="IPresenceService.SweepPresence"/> on every tick of the configured interval until the host stops, and tells the
/// rooms what changed: who went offgrid, who was removed, and the countdown or reveal that followed.
/// </summary>
public class PresenceBackgroundService(
    IPresenceService presence,
    IRoomPresenceNotifier presenceNotifier,
    IRoomTimerService timer,
    IOptions<PresenceOptions> options,
    TimeProvider time,
    ILogger<PresenceBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var ticker = new PeriodicTimer(options.Value.SweepInterval, time);
        try
        {
            while (await ticker.WaitForNextTickAsync(stoppingToken))
            {
                await RunOnceAsync();
            }
        }
        catch (OperationCanceledException)
        {
            // The host is stopping.
        }
    }

    /// <summary>One sweep. A failure is logged and does not end the loop.</summary>
    public async Task RunOnceAsync()
    {
        try
        {
            var sweep = presence.SweepPresence();
            foreach (var offgrid in sweep.Offgrid)
            {
                await presenceNotifier.PersonOffgridAsync(offgrid);
                await timer.ApplyAsync(offgrid.RoomId, offgrid.FollowUp);
            }

            foreach (var removed in sweep.Removed)
            {
                await presenceNotifier.PersonRemovedAsync(removed);
                await timer.ApplyAsync(removed.RoomId, removed.FollowUp);
            }

            if (sweep.DisposedRoomIds.Count > 0)
            {
                logger.LogInformation("Deleted {Rooms} rooms nobody was online in", sweep.DisposedRoomIds.Count);
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Sweeping the presence of the rooms failed");
        }
    }

}
