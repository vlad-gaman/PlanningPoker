using PlanningPoker.Common.Models;

namespace PlanningPoker.Service.Services;

/// <summary>What the Operator can do to live rooms. Each action is recorded in the issues log.</summary>
public interface IAdminActionService
{
    /// <summary>
    /// Closes the room: it is disposed and deleted from memory and from the store, and everyone in it is told it was closed by
    /// the Operator. <c>room_not_found</c> when there is no such room.
    /// </summary>
    Task<ServiceResponse> CloseRoomAsync(string roomId);

    /// <summary>
    /// Removes a member, online or offgrid, from the room as Removed: their vote is discarded, ownership is handed on when they
    /// owned it, a countdown or reveal that follows is started, and the room is told (the person's own client can tell it was the
    /// Operator). <c>room_not_found</c> or <c>person_not_found</c> when there is no such room or member.
    /// </summary>
    Task<ServiceResponse> RemovePersonAsync(string roomId, Guid personGuid);
}
