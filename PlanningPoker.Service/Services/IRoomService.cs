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
    /// Stores or replaces the caller's vote in the room their connection is in. When that makes everyone voted, the
    /// result says which countdown to tick (or carries the votes when the countdown is 0 and they were revealed at once).
    /// </summary>
    ServiceResponse<VoteResult> Vote(Guid personGuid, string connectionId, string mark);

    /// <summary>Drops all votes, reopens voting and cancels any countdown. Returns the room id.</summary>
    ServiceResponse<string> ClearVotes(Guid personGuid, string connectionId);

    /// <summary>Reveals the votes at once. Success with a null <see cref="RevealResult.VoteStatus"/> means they were already revealed.</summary>
    ServiceResponse<RevealResult> ForceShowVotes(Guid personGuid, string connectionId);

    /// <summary>
    /// One second of the countdown that started at <paramref name="revision"/>. Stale when any later change cancelled it
    /// (the check is made under the room lock, so a stale tick never reveals).
    /// </summary>
    TickResult Tick(string roomId, long revision);
}
