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

    /// <summary>Runs once, right after the next Get returns, so a test can change the room between a snapshot and the step that uses it.</summary>
    public Action? AfterNextGet { get; set; }

    public RoomEntity? Get(string roomId)
    {
        var snapshot = inner.Get(roomId);
        var hook = AfterNextGet;
        AfterNextGet = null;
        hook?.Invoke();
        return snapshot;
    }

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
