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

    /// <summary>
    /// The follow-up to a change that may have left everyone voted (a person leaving or going offgrid): reads the room, then
    /// decides whether to start the countdown or reveal. <see cref="VotingFollowUp.None"/> when voting is closed or the room is gone.
    /// </summary>
    VotingFollowUp FollowUpAfterChange(string roomId);

    /// <summary>
    /// Stores or replaces the caller's vote in the room their connection is in. When that makes everyone voted, the
    /// result says which countdown to tick (or carries the votes when the countdown is 0 and they were revealed at once).
    /// </summary>
    ServiceResponse<VoteResult> Vote(Guid personGuid, string connectionId, string mark);

    /// <summary>Drops all votes, reopens voting, cancels any countdown and restarts the round timer.</summary>
    ServiceResponse<ClearVotesResult> ClearVotes(Guid personGuid, string connectionId);

    /// <summary>Reveals the votes at once. Success with a null <see cref="RevealResult.VoteStatus"/> means they were already revealed.</summary>
    ServiceResponse<RevealResult> ForceShowVotes(Guid personGuid, string connectionId);

    /// <summary>
    /// Sets the caller's person type. The same type is a success with <see cref="PersonTypeChangeResult.Changed"/> false. While
    /// voting is open the change cancels a running countdown and then decides again whether everyone has voted; after the
    /// reveal the votes and the statistics stay as they were.
    /// </summary>
    ServiceResponse<PersonTypeChangeResult> ChangePersonType(Guid personGuid, string connectionId, string personType);

    /// <summary>
    /// Replaces the configuration of the caller's room; owner only. Shown votes are cleared, and so are votes cast for a card
    /// set that is no longer the room's. Any change cancels a running countdown.
    /// </summary>
    ServiceResponse<RoomConfigurationChangeResult> UpdateConfiguration(Guid personGuid, string connectionId, RoomConfiguration configuration);

    /// <summary>Hands the room to another member; owner only. Handing it to the current owner changes nothing.</summary>
    ServiceResponse<OwnershipTransferResult> TransferOwnership(Guid personGuid, string connectionId, Guid targetPersonGuid);

    /// <summary>
    /// One second of the countdown that started at <paramref name="revision"/>. Stale when any later change cancelled it
    /// (the check is made under the room lock, so a stale tick never reveals).
    /// </summary>
    TickResult Tick(string roomId, long revision);
}
