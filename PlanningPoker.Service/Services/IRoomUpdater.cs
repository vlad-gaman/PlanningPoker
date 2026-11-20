using PlanningPoker.Repository.Models;

namespace PlanningPoker.Service.Services;

/// <summary>
/// The way the Service layer mutates a room. It owns the Abandoned rule: a mutation that reports it
/// <see cref="RoomUpdate{T}.Changed"/> the room counts as activity, and no one else writes <see cref="RoomEntity.LastActivityAt"/>
/// after the room is created.
/// </summary>
public interface IRoomUpdater
{
    /// <summary>Runs the mutation under the room lock; returns false when the room does not exist. Stamps the room's last activity when the mutation changed it.</summary>
    bool Update<T>(string roomId, Func<RoomEntity, RoomUpdate<T>> mutate, out T? result);
}
