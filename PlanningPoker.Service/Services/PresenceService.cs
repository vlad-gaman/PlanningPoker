using Microsoft.Extensions.Options;
using PlanningPoker.Common.Models;
using PlanningPoker.Repository.Models;
using PlanningPoker.Repository.Repositories;
using PlanningPoker.Service.Mappers;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Service.Services;

/// <summary>Follows the same locking as <see cref="RoomService"/>: membership is decided under the room lock, person details are read after it.</summary>
public class PresenceService(
    IRoomRepository rooms,
    IRoomUpdater roomUpdater,
    IPersonRepository people,
    IPersonMapper personMapper,
    IExitRoomResultMapper exitRoomResultMapper,
    IRoomService roomService,
    IRoomMembership membership,
    TimeProvider time,
    IOptions<PresenceOptions> presence) : IPresenceService
{
    public ServiceResponse Disconnect(Guid personGuid, string connectionId)
    {
        var now = time.GetUtcNow();

        // Step 1 (person lock): drop the index entry of this connection and find its room.
        var indexed = people.Update(personGuid, entity =>
        {
            if (!entity.Connections.Remove(connectionId, out var roomId))
            {
                return (Found: false, RoomId: string.Empty);
            }

            entity.LastSeenAt = now;
            return (Found: true, RoomId: roomId);
        }, out var index);
        if (!indexed)
        {
            return ServiceResponse.Failure(ServiceError.NoSession());
        }

        if (!index.Found)
        {
            return ServiceResponse.Failure(ServiceError.NotInRoom());
        }

        // Step 2 (room lock): the member is dropped only when this was their last connection to the room. The first drop
        // time stays, and a Join that ran before this point keeps them online.
        var found = roomUpdater.Update(index.RoomId, room =>
        {
            var connections = room.MemberConnections.GetValueOrDefault(personGuid);
            if (connections is null || !connections.Remove(connectionId))
            {
                return RoomUpdate.Unchanged(false);
            }

            if (connections.Count == 0)
            {
                membership.Drop(room, personGuid, now);
            }

            return RoomUpdate.Changed(true);
        }, out var member);
        if (!found)
        {
            return ServiceResponse.Failure(ServiceError.RoomNotFound(index.RoomId));
        }

        return member ? ServiceResponse.Success() : ServiceResponse.Failure(ServiceError.NotInRoom());
    }

    public PresenceSweepResult SweepPresence()
    {
        var offgrid = new List<PersonOffgridResult>();
        var removed = new List<ExitRoomResult>();
        var disposed = new List<string>();
        foreach (var roomId in rooms.Ids())
        {
            var now = time.GetUtcNow();
            if (!roomUpdater.Update(roomId, room => ApplyPresenceTracked(room, now), out var change) || change is null || change.IsEmpty)
            {
                continue;
            }

            if (change.Disposed)
            {
                rooms.Remove(roomId);
                disposed.Add(roomId);
                continue;
            }

            // Read after the room lock is released: person details come from their own person locks.
            var roomOffgrid = change.Offgrid.Select(guid => SnapshotPerson(guid, change.Roles[guid], isOffgrid: true)).OfType<Person>().ToList();
            var roomRemoved = change.Removed.Select(guid => SnapshotPerson(guid, change.Roles[guid])).OfType<Person>().ToList();
            var newOwner = change.NewOwnerGuid is { } ownerGuid ? SnapshotPerson(ownerGuid, change.Roles[ownerGuid]) : null;

            // One decision per room for everything that changed: the people still online may all have voted now.
            var followUp = roomService.FollowUpAfterChange(roomId);
            for (var i = 0; i < roomOffgrid.Count; i++)
            {
                var isLast = i == roomOffgrid.Count - 1 && roomRemoved.Count == 0;
                var ownerLeft = roomOffgrid[i].Guid == change.PreviousOwnerGuid;
                offgrid.Add(new PersonOffgridResult
                {
                    RoomId = roomId,
                    Person = roomOffgrid[i],
                    NewOwner = ownerLeft ? newOwner : null,
                    FollowUp = isLast ? followUp : VotingFollowUp.None
                });
            }

            for (var i = 0; i < roomRemoved.Count; i++)
            {
                var isLast = i == roomRemoved.Count - 1;
                var ownerLeft = roomRemoved[i].Guid == change.PreviousOwnerGuid;
                removed.Add(exitRoomResultMapper.Map(roomId, roomRemoved[i], personLeft: true, roomDisposed: false, ownerLeft ? newOwner : null, isLast ? followUp : VotingFollowUp.None));
            }
        }

        return new PresenceSweepResult { Offgrid = offgrid, Removed = removed, DisposedRoomIds = disposed };
    }

    /// <summary>Applies the presence pass and reports whether it changed the room: a member moved, the room was disposed, or a restored room's wait for its people ended.</summary>
    private RoomUpdate<PresenceChange> ApplyPresenceTracked(RoomEntity room, DateTimeOffset now)
    {
        var awaitingReturn = room.AwaitingReturn;
        var change = ApplyPresence(room, now);
        return RoomUpdate.Of(change, !change.IsEmpty || room.AwaitingReturn != awaitingReturn);
    }

    /// <summary>
    /// Removes the members whose removal window has passed, turns the ones past the grace period offgrid, hands the room over
    /// when its owner is one of them and disposes the room when nobody has been online for the empty-room lifetime. Runs under the room lock.
    /// </summary>
    private PresenceChange ApplyPresence(RoomEntity room, DateTimeOffset now)
    {
        var options = presence.Value;
        var removed = room.DroppedAt.Where(dropped => now - dropped.Value >= options.RemovalWindow).Select(dropped => dropped.Key).ToList();
        var ownerAtStart = room.OwnerGuid;
        var roles = new Dictionary<Guid, string>();
        foreach (var guid in removed)
        {
            roles[guid] = membership.Leave(room, guid, LeaveReason.Timeout).PersonType;
        }

        var lapsed = room.DroppedAt
            .Where(dropped => now - dropped.Value >= options.GracePeriod && !room.OffgridGuids.Contains(dropped.Key))
            .Select(dropped => dropped.Key)
            .ToList();
        foreach (var guid in lapsed)
        {
            roles[guid] = room.RoleOf(guid);
            membership.GoOffgrid(room, guid);
        }

        // Online means connected or still inside the grace period. A room that never had a member lives for the never-joined lifetime.
        var hadMembers = removed.Count > 0 || room.MemberGuids.Count > 0;
        if (!hadMembers && room.MemberConnections.Count == 0 && now - room.CreatedAt >= options.NeverJoinedRoomLifetime)
        {
            membership.Dispose(room);
            return new PresenceChange([], removed, null, null, roles, Disposed: true);
        }

        var nobodyOnline = room.MemberConnections.Count == 0 && !room.DroppedAt.Values.Any(dropped => now - dropped < options.GracePeriod);
        if (room.MemberConnections.Count > 0)
        {
            room.AwaitingReturn = false;
        }

        // A restored room whose people have not come back yet keeps them until the removal window removes them.
        var awaitingReturn = room.AwaitingReturn && room.MemberGuids.Count > 0;
        if (hadMembers && nobodyOnline && !awaitingReturn && NobodyOnlineLongEnough(room, now, options))
        {
            membership.Dispose(room);
            return new PresenceChange([], removed, null, null, roles, Disposed: true);
        }

        // A removed owner was already replaced by Leave; an owner who merely went offgrid is replaced here when someone else is connected.
        if (room.OwnerGuid == ownerAtStart && lapsed.Contains(ownerAtStart))
        {
            membership.HandOver(room, ownerLeaving: false);
        }

        Guid? newOwner = room.OwnerGuid == ownerAtStart ? null : room.OwnerGuid;
        if (newOwner is { } ownerGuid)
        {
            roles[ownerGuid] = room.RoleOf(ownerGuid);
        }

        return new PresenceChange(lapsed, removed, newOwner is null ? null : ownerAtStart, newOwner, roles, Disposed: false);
    }

    private static bool NobodyOnlineLongEnough(RoomEntity room, DateTimeOffset now, PresenceOptions options)
    {
        if (room.MemberGuids.Count == 0)
        {
            return true;
        }

        // The last moment anyone was online is when the latest dropped member's grace period lapsed.
        return room.DroppedAt.Count > 0 && now - (room.DroppedAt.Values.Max() + options.GracePeriod) >= options.EmptyRoomLifetime;
    }

    /// <summary>Maps a person under their lock. The type is their role in the room, not the one on their profile.</summary>
    private Person? SnapshotPerson(Guid personGuid, string personType, bool isOffgrid = false)
    {
        return people.Update(personGuid, entity => personMapper.Map(entity, isOffgrid, personType), out var snapshot) ? snapshot : null;
    }
}
