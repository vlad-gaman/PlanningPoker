using PlanningPoker.Controller.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Controller.Mappers;

public interface IPersonExitedEventMapper
{
    PersonExitedEvent Map(ExitRoomResult result);
}
