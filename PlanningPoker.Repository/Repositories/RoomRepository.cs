using System.Collections.Concurrent;
using System.Text.Json;
using PlanningPoker.Repository.Mappers;
using PlanningPoker.Repository.Models;
using PlanningPoker.Repository.Services;

namespace PlanningPoker.Repository.Repositories;

/// <summary>
/// The live rooms in memory. When a change log is given, every change to a room is reported to it so the room is written
/// to the store afterwards; the repository itself never waits for the store.
/// </summary>
public class RoomRepository(IRoomEntityCopyMapper mapper, IRoomChangeLog? changeLog = null, TimeProvider? time = null) : IRoomRepository
{
    private readonly ConcurrentDictionary<string, RoomEntity> _rooms = new();

    /// <summary>The serialized state of each room as of its last change, so an update serializes the room once, after the change, to see whether it differs.</summary>
    private readonly ConcurrentDictionary<string, string> _fingerprints = new();
    private readonly TimeProvider _time = time ?? TimeProvider.System;

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

        if (changeLog is not null)
        {
            _fingerprints[room.Id] = JsonSerializer.Serialize(copy);
            changeLog.MarkChanged(room.Id);
        }

        return true;
    }

    public bool Remove(string roomId)
    {
        if (!_rooms.TryRemove(roomId, out _))
        {
            return false;
        }

        _fingerprints.TryRemove(roomId, out _);
        changeLog?.MarkChanged(roomId);
        return true;
    }

    public IReadOnlyCollection<string> Ids()
    {
        return [.. _rooms.Keys];
    }

    public bool Update<T>(string roomId, Func<RoomEntity, T> mutate, out T? result)
    {
        if (!_rooms.TryGetValue(roomId, out var room))
        {
            result = default;
            return false;
        }

        lock (room)
        {
            result = mutate(room);
            if (changeLog is not null && _fingerprints.TryGetValue(roomId, out var before))
            {
                var after = JsonSerializer.Serialize(room);
                if (after != before)
                {
                    room.LastActivityAt = _time.GetUtcNow();
                    _fingerprints[roomId] = JsonSerializer.Serialize(room);
                    changeLog.MarkChanged(roomId);
                }
            }

            return true;
        }
    }
}
