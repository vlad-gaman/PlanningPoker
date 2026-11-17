using PlanningPoker.Controller.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Controller.Mappers;

public interface IJoinRoomResponseMapper
{
    JoinRoomResponse Map(JoinRoomResult result);
}
