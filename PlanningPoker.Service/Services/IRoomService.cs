using PlanningPoker.Common.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Service.Services;

public interface IRoomService
{
    ServiceResponse<Room> Create(Guid ownerGuid, RoomSettings settings);

    ServiceResponse<string> Exists(string roomId);

    /// <summary>Adds the connection to the room. Joining the same room again on the same connection is a no-op.</summary>
    ServiceResponse<JoinRoomResult> Join(Guid personGuid, string connectionId, string roomId);

    /// <summary>
    /// Removes this connection from its room. Success with <see cref="ExitRoomResult.PersonLeft"/> false means the person
    /// still has another connection to that room, so nothing else changed. <c>not_in_room</c> means the connection is unknown.
    /// </summary>
    ServiceResponse<ExitRoomResult> Exit(Guid personGuid, string connectionId);
}
