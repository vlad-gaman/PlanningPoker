using PlanningPoker.Repository.Models;

namespace PlanningPoker.Repository.Repositories;

public interface IRoomRepository
{
    RoomEntity? Get(string roomId);

    bool TryAdd(RoomEntity room);

    bool Remove(string roomId);

    /// <summary>A snapshot of the stored room ids.</summary>
    IReadOnlyCollection<string> Ids();

    /// <summary>
    /// The only way to mutate a room. Runs under a per-room lock; returns false when the room does not exist. The room is
    /// reported to the change log only when the mutation says it <see cref="RoomUpdate{T}.Changed"/> something.
    /// </summary>
    bool Update<T>(string roomId, Func<RoomEntity, RoomUpdate<T>> mutate, out T? result);
}
