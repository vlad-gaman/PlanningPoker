using PlanningPoker.Repository.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Service.Mappers;

public class AlarmStateMapper : IAlarmStateMapper
{
    public AlarmState Map(RoomEntity room)
    {
        var status = room.AlarmElapsed ? AlarmStatus.Elapsed : room.AlarmEndsAt is null ? AlarmStatus.Idle : AlarmStatus.Running;
        return new AlarmState { Status = status, EndsAt = room.AlarmEndsAt };
    }
}
