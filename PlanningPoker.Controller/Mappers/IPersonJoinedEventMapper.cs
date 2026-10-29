using PlanningPoker.Controller.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Controller.Mappers;

public interface IPersonJoinedEventMapper
{
    PersonJoinedEvent Map(JoinRoomResult result);
}
