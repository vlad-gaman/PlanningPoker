using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Service.Services;

/// <summary>
/// Runs <see cref="IRoomService.SweepPresence"/> on every tick of the configured interval until the host stops, and tells the
/// rooms what changed: who went offgrid, who was removed, and the countdown or reveal that followed.
/// </summary>
public class PresenceBackgroundService(
    IRoomService rooms,
    IRoomPresenceNotifier presenceNotifier,
    IRoomCountdownNotifier countdownNotifier,
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
            var sweep = rooms.SweepPresence();
            foreach (var offgrid in sweep.Offgrid)
            {
                await presenceNotifier.PersonOffgridAsync(offgrid);
                await ApplyFollowUpAsync(offgrid.RoomId, offgrid.FollowUp);
            }

            foreach (var removed in sweep.Removed)
            {
                await presenceNotifier.PersonRemovedAsync(removed);
                await ApplyFollowUpAsync(removed.RoomId, removed.FollowUp);
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

    /// <summary>Starts the countdown ticker and sends its first second, or announces an immediate reveal.</summary>
    private async Task ApplyFollowUpAsync(string roomId, VotingFollowUp followUp)
    {
        if (followUp.Revealed is { } revealed)
        {
            await countdownNotifier.VotesRevealedAsync(roomId, revealed);
        }
        else if (followUp.Countdown is { } countdown)
        {
            timer.Start(roomId, countdown.Revision);
            await countdownNotifier.CountdownTickedAsync(roomId, countdown.Seconds);
        }
    }
}
