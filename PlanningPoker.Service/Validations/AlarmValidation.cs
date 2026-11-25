using PlanningPoker.Common.Models;
using PlanningPoker.Repository.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Service.Validations;

/// <summary>Which Alarm commands a room's Alarm accepts now: start when it is on and idle, cancel while running, reset once elapsed.</summary>
public class AlarmValidation
{
    public ServiceResponse CanStart(RoomEntity room)
    {
        if (!room.Configuration.AlarmEnabled)
        {
            return ServiceResponse.Failure(ServiceError.AlarmDisabled());
        }

        return Is(room) == AlarmStatus.Idle ? ServiceResponse.Success() : ServiceResponse.Failure(ServiceError.AlarmNotIdle());
    }

    public ServiceResponse CanCancel(RoomEntity room)
    {
        return Is(room) == AlarmStatus.Running ? ServiceResponse.Success() : ServiceResponse.Failure(ServiceError.AlarmNotRunning());
    }

    public ServiceResponse CanReset(RoomEntity room)
    {
        return Is(room) == AlarmStatus.Elapsed ? ServiceResponse.Success() : ServiceResponse.Failure(ServiceError.AlarmNotElapsed());
    }

    /// <summary>The run that ends at <paramref name="endsAt"/> is still the running one; false once it was cancelled, replaced or elapsed.</summary>
    public ServiceResponse CanElapse(RoomEntity room, DateTimeOffset endsAt)
    {
        return Is(room) == AlarmStatus.Running && room.AlarmEndsAt == endsAt ? ServiceResponse.Success() : ServiceResponse.Failure(ServiceError.AlarmNotRunning());
    }

    private static AlarmStatus Is(RoomEntity room)
    {
        return room.AlarmElapsed ? AlarmStatus.Elapsed : room.AlarmEndsAt is null ? AlarmStatus.Idle : AlarmStatus.Running;
    }
}
