using PlanningPoker.Controller.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Controller.Mappers;

public class RoomCreatedResponseMapper : IRoomCreatedResponseMapper
{
    public RoomCreatedResponse Map(Room room)
    {
        return new RoomCreatedResponse { Id = room.Id };
    }
}
