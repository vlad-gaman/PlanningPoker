using PlanningPoker.Common.Models;
using PlanningPoker.Repository.Models;
using PlanningPoker.Service.Mappers;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Service.Services;

public class RoomAlarm(IAlarmStateMapper stateMapper, TimeProvider time) : IRoomAlarm
{
    public ServiceResponse<AlarmState> Start(RoomEntity room)
    {
        if (!room.Configuration.AlarmEnabled)
        {
            return ServiceResponse<AlarmState>.Failure(ServiceError.AlarmDisabled());
        }

        if (room.AlarmElapsed || room.AlarmEndsAt is not null)
        {
            return ServiceResponse<AlarmState>.Failure(ServiceError.AlarmNotIdle());
        }

        room.AlarmEndsAt = time.GetUtcNow().AddMinutes(room.Configuration.AlarmMinutes);
        return ServiceResponse<AlarmState>.Success(stateMapper.Map(room));
    }

    public ServiceResponse<AlarmState> Cancel(RoomEntity room)
    {
        if (room.AlarmElapsed || room.AlarmEndsAt is null)
        {
            return ServiceResponse<AlarmState>.Failure(ServiceError.AlarmNotRunning());
        }

        room.AlarmEndsAt = null;
        return ServiceResponse<AlarmState>.Success(stateMapper.Map(room));
    }

    public ServiceResponse<AlarmState> Reset(RoomEntity room)
    {
        if (!room.AlarmElapsed)
        {
            return ServiceResponse<AlarmState>.Failure(ServiceError.AlarmNotElapsed());
        }

        room.AlarmElapsed = false;
        room.AlarmEndsAt = null;
        return ServiceResponse<AlarmState>.Success(stateMapper.Map(room));
    }

    public ServiceResponse<AlarmState> Elapse(RoomEntity room, DateTimeOffset endsAt)
    {
        if (room.AlarmElapsed || room.AlarmEndsAt != endsAt)
        {
            return ServiceResponse<AlarmState>.Failure(ServiceError.AlarmNotRunning());
        }

        room.AlarmElapsed = true;
        return ServiceResponse<AlarmState>.Success(stateMapper.Map(room));
    }

    public bool IdleWhenDisabled(RoomEntity room)
    {
        if (room.Configuration.AlarmEnabled || (room.AlarmEndsAt is null && !room.AlarmElapsed))
        {
            return false;
        }

        room.AlarmEndsAt = null;
        room.AlarmElapsed = false;
        return true;
    }
}
