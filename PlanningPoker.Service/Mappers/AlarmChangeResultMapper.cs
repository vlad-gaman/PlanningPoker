using PlanningPoker.Service.Models;

namespace PlanningPoker.Service.Mappers;

public class AlarmChangeResultMapper : IAlarmChangeResultMapper
{
    public AlarmChangeResult Map(string roomId, AlarmState alarm)
    {
        return new AlarmChangeResult { RoomId = roomId, Alarm = alarm };
    }
}
