using Microsoft.Extensions.Logging;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Service.Services;

public class RoomTimerService(IRoomService rooms, IRoomCountdownNotifier notifier, TimeProvider time, ILogger<RoomTimerService> logger) : IRoomTimerService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(1);

    private readonly Lock _gate = new();
    private readonly Dictionary<string, Ticker> _tickers = [];

    public void Start(string roomId, long revision)
    {
        Ticker ticker;
        lock (_gate)
        {
            if (_tickers.TryGetValue(roomId, out var current))
            {
                if (current.Revision > revision)
                {
                    return;
                }

                current.Cancel.Cancel();
            }

            ticker = new Ticker(revision, new CancellationTokenSource());
            _tickers[roomId] = ticker;
        }

        _ = RunAsync(roomId, ticker);
    }

    private async Task RunAsync(string roomId, Ticker ticker)
    {
        try
        {
            using var timer = new PeriodicTimer(Interval, time);
            while (await timer.WaitForNextTickAsync(ticker.Cancel.Token))
            {
                var tick = rooms.Tick(roomId, ticker.Revision);
                if (tick.Outcome == TickOutcome.Stale)
                {
                    return;
                }

                if (tick.Outcome == TickOutcome.Revealed)
                {
                    await notifier.VotesRevealedAsync(roomId, tick.Revealed!);
                    return;
                }

                await notifier.CountdownTickedAsync(roomId, tick.Remaining);
            }
        }
        catch (OperationCanceledException)
        {
            // Replaced by a newer countdown.
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Countdown of room {RoomId} failed", roomId);
        }
        finally
        {
            lock (_gate)
            {
                if (_tickers.TryGetValue(roomId, out var current) && ReferenceEquals(current, ticker))
                {
                    _tickers.Remove(roomId);
                }
            }

            ticker.Cancel.Dispose();
        }
    }

    private sealed record Ticker(long Revision, CancellationTokenSource Cancel);
}
