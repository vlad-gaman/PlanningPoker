using PlanningPoker.Controller.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Controller.Mappers;

public interface IRoomConfigurationResponseMapper
{
    RoomConfigurationResponse Map(RoomConfiguration configuration);
}
