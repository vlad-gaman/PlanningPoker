using PlanningPoker.Controller.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Controller.Mappers;

public interface IRoomConfigurationMapper
{
    RoomConfiguration Map(RoomConfigurationRequest request);
}
