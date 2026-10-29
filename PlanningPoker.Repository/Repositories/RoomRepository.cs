using System.Collections.Concurrent;
using PlanningPoker.Repository.Mappers;
using PlanningPoker.Repository.Models;

namespace PlanningPoker.Repository.Repositories;

public class RoomRepository(IRoomEntityMapper mapper) : IRoomRepository
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
        return _rooms.TryAdd(room.Id, mapper.Map(room));
    }

    public bool Remove(string roomId)
    {
        return _rooms.TryRemove(roomId, out _);
    }

    public RoomEntity? FindByPerson(Guid personGuid)
    {
        foreach (var room in _rooms.Values)
        {
            lock (room)
            {
                if (room.MemberGuids.Contains(personGuid))
                {
                    return mapper.Map(room);
                }
            }
        }

        return null;
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
            return true;
        }
    }
}
