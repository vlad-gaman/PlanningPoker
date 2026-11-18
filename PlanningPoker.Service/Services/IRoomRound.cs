using PlanningPoker.Repository.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Service.Services;

/// <summary>
/// Owns the round of a room: votes, reveal, clear, the countdown and the roles that decide who is waited for. Stateless: every
/// method edits the <see cref="RoomEntity"/> it is given, so the caller must hold the room lock (<c>IRoomRepository.Update</c>).
/// It is the only place that bumps <see cref="RoomEntity.Revision"/>. Locking, mapping and responses stay with the callers.
/// </summary>
public interface IRoomRound
{
    /// <summary>Stores the member's vote, cancels a running countdown and decides: the caller has checked the member, role, state and mark.</summary>
    RoundDecision Vote(RoomEntity room, Guid personGuid, string mark);

    /// <summary>Freezes the votes now; false when voting was already closed.</summary>
    bool Reveal(RoomEntity room);

    /// <summary>Starts a new round: drops the votes and reopens voting. The round timer restarts only when asked to.</summary>
    void Clear(RoomEntity room, bool restartTimer);

    /// <summary>Advances the countdown that was started at the revision by one second, revealing on its last.</summary>
    RoundTick Tick(RoomEntity room, long revision);

    /// <summary>Makes a running countdown stale: a change reached the room that it no longer applies to.</summary>
    void InvalidateCountdown(RoomEntity room);

    /// <summary>Starts the countdown (or reveals at once for a countdown of 0) when every online voter has voted and none runs yet.</summary>
    RoundDecision Decide(RoomEntity room);

    /// <summary>Sets the member's role. While voting is open a real change cancels a running countdown and the round decides again.</summary>
    RoleChange ChangeRole(RoomEntity room, Guid personGuid, string role);
}
