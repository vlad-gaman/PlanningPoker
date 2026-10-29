using PlanningPoker.Controller.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Controller.Mappers;

public interface IRoomCreatedResponseMapper
{
    RoomCreatedResponse Map(Room room);
}
