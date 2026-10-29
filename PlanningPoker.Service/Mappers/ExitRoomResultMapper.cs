using PlanningPoker.Service.Models;

namespace PlanningPoker.Service.Mappers;

public class ExitRoomResultMapper : IExitRoomResultMapper
{
    public ExitRoomResult Map(string roomId, Person person, bool roomDisposed, Person? newOwner)
    {
        return new ExitRoomResult { RoomId = roomId, Person = person, RoomDisposed = roomDisposed, NewOwner = newOwner };
    }
}
