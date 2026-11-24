using Microsoft.Extensions.Logging;

namespace PlanningPoker.Service.Services;

public class RoomAlarmTimerService(IRoomService rooms, IRoomAlarmNotifier notifier, TimeProvider time, ILogger<RoomAlarmTimerService> logger) : IRoomAlarmTimer
{
    private readonly Lock _gate = new();
    private readonly Dictionary<string, CancellationTokenSource> _pending = [];

    public void Schedule(string roomId, DateTimeOffset endsAt)
    {
        var cancel = new CancellationTokenSource();
        lock (_gate)
        {
            if (_pending.Remove(roomId, out var older))
            {
                Dispose(older);
            }

            _pending[roomId] = cancel;
        }

        _ = WaitAsync(roomId, endsAt, cancel);
    }

    public void Cancel(string roomId)
    {
        lock (_gate)
        {
            if (_pending.Remove(roomId, out var older))
            {
                Dispose(older);
            }
        }
    }

    private async Task WaitAsync(string roomId, DateTimeOffset endsAt, CancellationTokenSource cancel)
    {
        try
        {
            var wait = endsAt - time.GetUtcNow();
            if (wait > TimeSpan.Zero)
            {
                await Task.Delay(wait, time, cancel.Token);
            }

            var elapsed = rooms.ElapseAlarm(roomId, endsAt);
            if (elapsed.IsSuccess)
            {
                await notifier.AlarmChangedAsync(elapsed.Data!);
            }
        }
        catch (OperationCanceledException)
        {
            // Cancelled or replaced by a newer run.
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Alarm of room {RoomId} failed", roomId);
        }
        finally
        {
            lock (_gate)
            {
                if (_pending.TryGetValue(roomId, out var current) && ReferenceEquals(current, cancel))
                {
                    _pending.Remove(roomId);
                }
            }
        }
    }

    private static void Dispose(CancellationTokenSource source)
    {
        source.Cancel();
        source.Dispose();
    }
}
