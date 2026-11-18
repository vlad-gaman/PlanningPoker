using PlanningPoker.Repository.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Service.Services;

/// <summary>
/// Owns the membership transitions of a room: who is in it, who is online or offgrid, whose vote is kept, who owns it and
/// when it is disposed. Stateless: every method edits the <see cref="RoomEntity"/> it is given, so the caller must hold the
/// room lock (<c>IRoomRepository.Update</c>). Locking, timers and I/O stay with the callers.
/// </summary>
public interface IRoomMembership
{
    /// <summary>Adds a connection of the person, admitting them when new and bringing them back when dropped or offgrid.</summary>
    MembershipJoined Join(RoomEntity room, Guid personGuid, string connectionId, bool isObserver);

    /// <summary>Marks a dropped or offgrid member as online again; true when they had been offgrid.</summary>
    bool Return(RoomEntity room, Guid personGuid);

    /// <summary>Records that the member lost their last connection at the given time; the first drop wins.</summary>
    void Drop(RoomEntity room, Guid personGuid, DateTimeOffset at);

    /// <summary>Announces a dropped member as offgrid: the room stops waiting for their vote.</summary>
    void GoOffgrid(RoomEntity room, Guid personGuid);

    /// <summary>Ends the person's membership. Only a deliberate leave keeps their vote aside.</summary>
    MembershipLeft Leave(RoomEntity room, Guid personGuid, LeaveReason reason);

    /// <summary>
    /// Gives the room to a connected member, else to anyone when the owner is leaving the room, else keeps the owner.
    /// Returns the new owner, or null when the owner stays.
    /// </summary>
    Guid? HandOver(RoomEntity room, bool ownerLeaving);

    /// <summary>Marks the room disposed so it accepts nobody new.</summary>
    void Dispose(RoomEntity room);
}
