using PlanningPoker.Controller.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Controller.Mappers;

public class AlarmResponseMapper : IAlarmResponseMapper
{
    public AlarmResponse Map(AlarmState alarm)
    {
        return new AlarmResponse { Status = alarm.Status.ToString().ToLowerInvariant(), EndsAt = alarm.EndsAt };
    }
}
