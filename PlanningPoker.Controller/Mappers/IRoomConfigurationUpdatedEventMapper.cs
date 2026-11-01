using PlanningPoker.Controller.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Controller.Mappers;

public interface IRoomConfigurationUpdatedEventMapper
{
    RoomConfigurationUpdatedEvent Map(RoomConfigurationChangeResult result);
}
