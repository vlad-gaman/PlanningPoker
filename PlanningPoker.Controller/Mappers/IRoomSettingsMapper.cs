using PlanningPoker.Controller.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Controller.Mappers;

public interface IRoomSettingsMapper
{
    RoomSettings Map(CreateRoomRequest request);
}
