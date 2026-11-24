using PlanningPoker.Common.Models;
using PlanningPoker.Repository.Models;
using PlanningPoker.Service.Mappers;
using PlanningPoker.Service.Models;
using PlanningPoker.Service.Validations;

namespace PlanningPoker.Service.Services;

public class RoomAlarm(AlarmValidation validation, IAlarmStateMapper stateMapper, TimeProvider time) : IRoomAlarm
{
    public ServiceResponse<AlarmState> Start(RoomEntity room)
    {
        var allowed = validation.CanStart(room);
        if (!allowed.IsSuccess)
        {
            return ServiceResponse<AlarmState>.Failure(allowed);
        }

        room.AlarmEndsAt = time.GetUtcNow().AddMinutes(room.Configuration.AlarmMinutes);
        return ServiceResponse<AlarmState>.Success(stateMapper.Map(room));
    }

    public ServiceResponse<AlarmState> Cancel(RoomEntity room)
    {
        var allowed = validation.CanCancel(room);
        if (!allowed.IsSuccess)
        {
            return ServiceResponse<AlarmState>.Failure(allowed);
        }

        room.AlarmEndsAt = null;
        return ServiceResponse<AlarmState>.Success(stateMapper.Map(room));
    }

    public ServiceResponse<AlarmState> Reset(RoomEntity room)
    {
        var allowed = validation.CanReset(room);
        if (!allowed.IsSuccess)
        {
            return ServiceResponse<AlarmState>.Failure(allowed);
        }

        room.AlarmElapsed = false;
        room.AlarmEndsAt = null;
        return ServiceResponse<AlarmState>.Success(stateMapper.Map(room));
    }

    public ServiceResponse<AlarmState> Elapse(RoomEntity room, DateTimeOffset endsAt)
    {
        var allowed = validation.CanElapse(room, endsAt);
        if (!allowed.IsSuccess)
        {
            return ServiceResponse<AlarmState>.Failure(allowed);
        }

        room.AlarmElapsed = true;
        return ServiceResponse<AlarmState>.Success(stateMapper.Map(room));
    }

    public void IdleWhenDisabled(RoomEntity room)
    {
        if (room.Configuration.AlarmEnabled)
        {
            return;
        }

        room.AlarmEndsAt = null;
        room.AlarmElapsed = false;
    }
}
