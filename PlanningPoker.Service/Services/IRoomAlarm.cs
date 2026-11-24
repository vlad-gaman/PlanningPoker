using PlanningPoker.Common.Models;
using PlanningPoker.Repository.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Service.Services;

/// <summary>
/// Owns the Alarm of a room: idle, running until an end time, elapsed until reset. Stateless: the state lives on the room,
/// and the caller holds the room lock and has checked the member.
/// </summary>
public interface IRoomAlarm
{
    /// <summary>Starts the Alarm for the room's configured minutes. Fails when it is turned off or is not idle.</summary>
    ServiceResponse<AlarmState> Start(RoomEntity room);

    /// <summary>Returns a running Alarm to idle without a sound. Fails when it is not running.</summary>
    ServiceResponse<AlarmState> Cancel(RoomEntity room);

    /// <summary>Returns an elapsed Alarm to idle without starting it again. Fails when it has not elapsed.</summary>
    ServiceResponse<AlarmState> Reset(RoomEntity room);

    /// <summary>Elapses the run that ends at <paramref name="endsAt"/>. Fails (stale) when that run was cancelled, replaced or already elapsed.</summary>
    ServiceResponse<AlarmState> Elapse(RoomEntity room, DateTimeOffset endsAt);

    /// <summary>Returns a running or elapsed Alarm to idle when the room has turned the Alarm off.</summary>
    void IdleWhenDisabled(RoomEntity room);
}
