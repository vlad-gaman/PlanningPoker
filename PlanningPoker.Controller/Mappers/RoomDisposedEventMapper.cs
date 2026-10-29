using PlanningPoker.Controller.Models;

namespace PlanningPoker.Controller.Mappers;

public class RoomDisposedEventMapper : IRoomDisposedEventMapper
{
    public RoomDisposedEvent Map(string roomId)
    {
        return new RoomDisposedEvent { RoomId = roomId };
    }
}
