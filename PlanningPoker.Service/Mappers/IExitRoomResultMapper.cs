using PlanningPoker.Service.Models;

namespace PlanningPoker.Service.Mappers;

public interface IExitRoomResultMapper
{
    ExitRoomResult Map(string roomId, Person person, bool roomDisposed, Person? newOwner);
}
