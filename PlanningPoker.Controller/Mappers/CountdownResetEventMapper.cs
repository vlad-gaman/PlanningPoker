using PlanningPoker.Controller.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Controller.Mappers;

public class CountdownResetEventMapper : ICountdownResetEventMapper
{
    public CountdownResetEvent Map(Room room)
    {
        return new CountdownResetEvent { Countdown = room.Configuration.CountdownSeconds, Reset = true };
    }
}
