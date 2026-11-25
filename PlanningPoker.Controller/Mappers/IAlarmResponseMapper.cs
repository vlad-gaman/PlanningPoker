using PlanningPoker.Controller.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Controller.Mappers;

public interface IAlarmResponseMapper
{
    AlarmResponse Map(AlarmState alarm);
}
