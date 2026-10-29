using PlanningPoker.Common.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Service.Services;

public interface IRoomService
{
    ServiceResponse<Room> Create(Guid ownerGuid, RoomSettings settings);

    ServiceResponse<string> Exists(string roomId);

    ServiceResponse<JoinRoomResult> Join(Guid personGuid, string connectionId, string roomId);

    /// <summary>Removes the person from their room when <paramref name="connectionId"/> is still their current connection.</summary>
    ServiceResponse<ExitRoomResult> Exit(Guid personGuid, string connectionId);
}
