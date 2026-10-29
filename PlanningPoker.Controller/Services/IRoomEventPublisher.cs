using PlanningPoker.Service.Models;

namespace PlanningPoker.Controller.Services;

/// <summary>Sends the room hub broadcasts to the members of a room.</summary>
public interface IRoomEventPublisher
{
    /// <summary>
    /// Tells everyone in the room except the joiner's connection that a person joined, and resets the countdown when the room calls for it.
    /// Sends nothing when <see cref="JoinRoomResult.PersonAdded"/> is false.
    /// </summary>
    Task PublishPersonJoinedAsync(JoinRoomResult result, string joinerConnectionId);

    /// <summary>Tells the room that a person left, followed by any ownership transfer and room disposal.</summary>
    Task PublishPersonExitedAsync(ExitRoomResult result);

    /// <summary>Tells the whole room, the voter included, that a vote was cast, with the masked vote status.</summary>
    Task PublishVoteCastAsync(VoteResult result);

    /// <summary>Tells the whole room that the votes were cleared.</summary>
    Task PublishVotesClearedAsync(string roomId);

    /// <summary>Tells the whole room that the votes were revealed.</summary>
    Task PublishVotesShownAsync(string roomId, VoteStatus status);

    /// <summary>Tells the whole room how many seconds are left before the votes are revealed.</summary>
    Task PublishCountdownUpdateAsync(string roomId, int countdown);
}
