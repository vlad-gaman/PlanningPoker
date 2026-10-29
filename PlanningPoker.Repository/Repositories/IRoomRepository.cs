using PlanningPoker.Repository.Models;

namespace PlanningPoker.Repository.Repositories;

public interface IRoomRepository
{
    RoomEntity? Get(string roomId);

    bool TryAdd(RoomEntity room);

    bool Remove(string roomId);

    RoomEntity? FindByPerson(Guid personGuid);

    /// <summary>The only way to mutate a room. Runs under a per-room lock; returns false when the room does not exist.</summary>
    bool Update<T>(string roomId, Func<RoomEntity, T> mutate, out T? result);
}
