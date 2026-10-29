using PlanningPoker.Controller.Models;

namespace PlanningPoker.Controller.Mappers;

public interface IRoomDisposedEventMapper
{
    RoomDisposedEvent Map(string roomId);
}
