using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PlanningPoker.Repository.Mappers;
using PlanningPoker.Repository.Models;
using PlanningPoker.Repository.Repositories;

namespace PlanningPoker.Repository.Services;

/// <summary>
/// Writes changed rooms to the store off the request path. Changes pile up per room and the newest state wins, so a slow
/// store never blocks voting; a failed write is logged and the next change of that room writes it again. The rooms still
/// pending are written before the host finishes stopping.
/// </summary>
public class RoomPersistenceWriter(
    IRoomChangeLog changeLog,
    IRoomRepository rooms,
    IPersonRepository people,
    IRoomStore store,
    IStoredRoomMapper storedRoomMapper,
    IOptions<PersistenceOptions> options,
    TimeProvider time,
    ILogger<RoomPersistenceWriter> logger) : BackgroundService
{
    /// <summary>Rooms whose last write failed; the next heartbeat marks them changed again so the store catches up once it works.</summary>
    private readonly HashSet<string> _failed = [];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.WhenAll(WriteChangesAsync(stoppingToken), HeartbeatAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            // The host is stopping.
        }
    }

    /// <summary>Writes the changes still pending once the loops have ended, so nothing a request changed just before the stop is lost.</summary>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        Flush();
    }

    private async Task WriteChangesAsync(CancellationToken stoppingToken)
    {
        while (await changeLog.Changes.WaitToReadAsync(stoppingToken))
        {
            Flush();
        }
    }

    /// <summary>Marks every room with someone connected as changed, so its stored time of last being online keeps up.</summary>
    private async Task HeartbeatAsync(CancellationToken stoppingToken)
    {
        using var ticker = new PeriodicTimer(options.Value.HeartbeatInterval, time);
        while (await ticker.WaitForNextTickAsync(stoppingToken))
        {
            foreach (var roomId in TakeFailed())
            {
                changeLog.MarkChanged(roomId);
            }

            foreach (var roomId in rooms.Ids())
            {
                if (rooms.Get(roomId) is { MemberConnections.Count: > 0 })
                {
                    changeLog.MarkChanged(roomId);
                }
            }
        }
    }

    private List<string> TakeFailed()
    {
        lock (_failed)
        {
            var failed = _failed.ToList();
            _failed.Clear();
            return failed;
        }
    }

    /// <summary>Writes every room changed so far, once each, in its latest state.</summary>
    public void Flush()
    {
        var changed = new HashSet<string>();
        while (changeLog.Changes.TryRead(out var roomId))
        {
            changed.Add(roomId);
        }

        foreach (var roomId in changed)
        {
            try
            {
                var room = rooms.Get(roomId);
                if (room is null)
                {
                    store.Delete(roomId);
                }
                else
                {
                    store.Save(storedRoomMapper.Map(room, people, time.GetUtcNow()));
                }
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Writing room {RoomId} to the store failed; it is tried again at the next heartbeat", roomId);
                lock (_failed)
                {
                    _failed.Add(roomId);
                }
            }
        }
    }
}
