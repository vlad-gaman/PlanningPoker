using PlanningPoker.Controller.Models;

namespace PlanningPoker.Controller.Mappers;

public interface IRoomCountdownEventMapper
{
    RoomCountdownEvent Map(int countdown, bool reset);
}
