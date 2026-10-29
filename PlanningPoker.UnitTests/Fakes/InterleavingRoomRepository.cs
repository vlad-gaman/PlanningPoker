using PlanningPoker.Repository.Models;
using PlanningPoker.Repository.Repositories;

namespace PlanningPoker.UnitTests.Fakes;

/// <summary>
/// Wraps a real room repository. <see cref="AfterNextUpdate"/> runs once, right after the next Update returns and
/// before the caller continues, so a test can remove the room between two steps of one operation.
/// </summary>
public sealed class InterleavingRoomRepository(IRoomRepository inner) : IRoomRepository
{
    public Action? AfterNextUpdate { get; set; }

    public RoomEntity? Get(string roomId) => inner.Get(roomId);

    public bool TryAdd(RoomEntity room) => inner.TryAdd(room);

    public bool Remove(string roomId) => inner.Remove(roomId);

    public bool Update<T>(string roomId, Func<RoomEntity, T> mutate, out T? result)
    {
        var found = inner.Update(roomId, mutate, out result);
        var hook = AfterNextUpdate;
        AfterNextUpdate = null;
        hook?.Invoke();
        return found;
    }
}
