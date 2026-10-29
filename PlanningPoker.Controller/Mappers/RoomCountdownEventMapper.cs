using PlanningPoker.Controller.Models;

namespace PlanningPoker.Controller.Mappers;

public class RoomCountdownEventMapper : IRoomCountdownEventMapper
{
    public RoomCountdownEvent Map(int countdown, bool reset)
    {
        return new RoomCountdownEvent { Countdown = countdown, Reset = reset };
    }
}
