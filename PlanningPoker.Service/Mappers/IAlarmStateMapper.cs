using PlanningPoker.Repository.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Service.Mappers;

public interface IAlarmStateMapper
{
    AlarmState Map(RoomEntity room);
}
