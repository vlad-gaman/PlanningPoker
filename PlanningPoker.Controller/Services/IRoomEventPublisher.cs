using PlanningPoker.Service.Models;

namespace PlanningPoker.Controller.Services;

/// <summary>Sends the room hub's broadcasts to the other members of a room.</summary>
public interface IRoomEventPublisher
{
    /// <summary>
    /// Tells everyone in the room except the joiner's connection that a person joined, and resets the countdown when the room calls for it.
    /// Sends nothing when <see cref="JoinRoomResult.PersonAdded"/> is false.
    /// </summary>
    Task PublishPersonJoinedAsync(JoinRoomResult result, string joinerConnectionId);

    /// <summary>Tells the room that a person left, followed by any ownership transfer and room disposal.</summary>
    Task PublishPersonExitedAsync(ExitRoomResult result);
}
