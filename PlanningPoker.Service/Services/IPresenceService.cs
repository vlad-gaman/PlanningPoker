using PlanningPoker.Common.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Service.Services;

/// <summary>
/// Presence of the members of a room: online while they have a connection or dropped less than the grace period ago,
/// offgrid after that (still a member, vote kept, no longer waited for), removed once the removal window has passed.
/// </summary>
public interface IPresenceService
{
    /// <summary>
    /// Closes this connection. When it was the member's last one in the room they are dropped, not removed: they stay a member
    /// with their vote. A returning <see cref="IRoomService.Join"/> restores them. <c>not_in_room</c> means the connection is unknown.
    /// </summary>
    ServiceResponse Disconnect(Guid personGuid, string connectionId);

    /// <summary>
    /// Turns the people whose grace period lapsed offgrid, removes those offgrid for the whole removal window (with their
    /// vote), hands a room over when its owner goes offgrid or is removed, and deletes rooms nobody has been online in for the
    /// empty-room lifetime. Called by the background sweep.
    /// </summary>
    PresenceSweepResult SweepPresence();
}
