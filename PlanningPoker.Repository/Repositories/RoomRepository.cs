using System.Collections.Concurrent;
using PlanningPoker.Repository.Mappers;
using PlanningPoker.Repository.Models;
using PlanningPoker.Repository.Services;

namespace PlanningPoker.Repository.Repositories;

/// <summary>
/// The live rooms in memory. Every change to a room is reported to the change log so the room is written to the store
/// afterwards; the repository itself never waits for the store.
/// </summary>
public class RoomRepository(IRoomEntityCopyMapper mapper, IRoomChangeLog changeLog) : IRoomRepository
{
    private readonly ConcurrentDictionary<string, RoomEntity> _rooms = new();

    public RoomEntity? Get(string roomId)
    {
        if (!_rooms.TryGetValue(roomId, out var room))
        {
            return null;
        }

        lock (room)
        {
            return mapper.Map(room);
        }
    }

    public bool TryAdd(RoomEntity room)
    {
        var copy = mapper.Map(room);
        if (copy.LastActivityAt == default)
        {
            copy.LastActivityAt = copy.CreatedAt;
        }

        if (!_rooms.TryAdd(room.Id, copy))
        {
            return false;
        }

        changeLog.MarkChanged(room.Id);
        return true;
    }

    public bool Remove(string roomId)
    {
        if (!_rooms.TryRemove(roomId, out _))
        {
            return false;
        }

        changeLog.MarkChanged(roomId);
        return true;
    }

    public IReadOnlyCollection<string> Ids()
    {
        return [.. _rooms.Keys];
    }

    public bool Update<T>(string roomId, Func<RoomEntity, RoomUpdate<T>> mutate, out T? result)
    {
        if (!_rooms.TryGetValue(roomId, out var room))
        {
            result = default;
            return false;
        }

        lock (room)
        {
            var update = mutate(room);
            result = update.Result;
            if (update.Changed)
            {
                changeLog.MarkChanged(roomId);
            }

            return true;
        }
    }
}
