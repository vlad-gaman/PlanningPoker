using PlanningPoker.Controller.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Controller.Mappers;

public interface IRoomConfigurationRequestMapper
{
    RoomConfiguration Map(RoomConfigurationRequest request);
}
