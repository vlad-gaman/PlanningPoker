using PlanningPoker.Controller.Models;

namespace PlanningPoker.Controller.Mappers;

public interface IRoomExistsResponseMapper
{
    RoomExistsResponse Map(string roomId);
}
