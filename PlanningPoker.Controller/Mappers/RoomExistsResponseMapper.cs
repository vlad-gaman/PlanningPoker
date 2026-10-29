using PlanningPoker.Controller.Models;

namespace PlanningPoker.Controller.Mappers;

public class RoomExistsResponseMapper : IRoomExistsResponseMapper
{
    public RoomExistsResponse Map(string roomId)
    {
        return new RoomExistsResponse { Id = roomId, Exists = true };
    }
}
