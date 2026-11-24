using PlanningPoker.Service.Models;

namespace PlanningPoker.Service.Mappers;

public interface IAlarmChangeResultMapper
{
    AlarmChangeResult Map(string roomId, AlarmState alarm);
}
