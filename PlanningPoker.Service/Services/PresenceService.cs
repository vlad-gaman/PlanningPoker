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
    IPersonRepository people,
    IPersonMapper personMapper,
    IExitRoomResultMapper exitRoomResultMapper,
    IRoomService roomService,
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
        var found = rooms.Update(index.RoomId, room =>
        {
            var connections = room.MemberConnections.GetValueOrDefault(personGuid);
            if (connections is null || !connections.Remove(connectionId))
            {
                return false;
            }

            if (connections.Count == 0)
            {
                room.MemberConnections.Remove(personGuid);
                room.DroppedAt.TryAdd(personGuid, now);
            }

            return true;
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
            if (!rooms.Update(roomId, room => ApplyPresence(room, now), out var change) || change is null || change.IsEmpty)
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
            var roomOffgrid = change.Offgrid.Select(guid => SnapshotPerson(guid, isOffgrid: true)).OfType<Person>().ToList();
            var roomRemoved = change.Removed.Select(guid => SnapshotPerson(guid)).OfType<Person>().ToList();
            var newOwner = change.NewOwnerGuid is { } ownerGuid ? SnapshotPerson(ownerGuid) : null;

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

    /// <summary>
    /// Removes the members whose removal window has passed, turns the ones past the grace period offgrid, hands the room over
    /// when its owner is one of them and disposes the room when nobody has been online for the empty-room lifetime. Runs under the room lock.
    /// </summary>
    private PresenceChange ApplyPresence(RoomEntity room, DateTimeOffset now)
    {
        var options = presence.Value;
        var removed = room.DroppedAt.Where(dropped => now - dropped.Value >= options.RemovalWindow).Select(dropped => dropped.Key).ToList();
        foreach (var guid in removed)
        {
            room.MemberGuids.Remove(guid);
            room.DroppedAt.Remove(guid);
            room.OffgridGuids.Remove(guid);
            room.Votes.Remove(guid);
        }

        if (removed.Count > 0)
        {
            // The people left may all have voted, and a running countdown no longer applies to the old group.
            room.Revision++;
        }

        var lapsed = room.DroppedAt
            .Where(dropped => now - dropped.Value >= options.GracePeriod && !room.OffgridGuids.Contains(dropped.Key))
            .Select(dropped => dropped.Key)
            .ToList();
        room.OffgridGuids.UnionWith(lapsed);

        // Online means connected or still inside the grace period. A room that never had a member is left to the abandoned sweep.
        var hadMembers = removed.Count > 0 || room.MemberGuids.Count > 0;
        var nobodyOnline = room.MemberConnections.Count == 0 && !room.DroppedAt.Values.Any(dropped => now - dropped < options.GracePeriod);
        if (hadMembers && nobodyOnline && NobodyOnlineLongEnough(room, now, options))
        {
            room.IsDisposed = true;
            return new PresenceChange([], removed, null, null, Disposed: true);
        }

        var previousOwner = removed.Contains(room.OwnerGuid) || lapsed.Contains(room.OwnerGuid) ? room.OwnerGuid : (Guid?)null;
        var newOwner = previousOwner is null ? null : HandOver(room, removedOwner: removed.Contains(room.OwnerGuid));
        return new PresenceChange(lapsed, removed, newOwner is null ? null : previousOwner, newOwner, Disposed: false);
    }

    /// <summary>
    /// Gives the room to a random connected member right away. An owner who merely went offgrid keeps the room when nobody
    /// else is connected; a removed owner still needs a successor from whoever is left.
    /// </summary>
    private static Guid? HandOver(RoomEntity room, bool removedOwner)
    {
        var candidates = room.MemberGuids.Where(guid => guid != room.OwnerGuid && room.MemberConnections.ContainsKey(guid)).ToList();
        if (candidates.Count == 0 && removedOwner)
        {
            candidates = [.. room.MemberGuids];
        }

        if (candidates.Count == 0)
        {
            return null;
        }

        room.OwnerGuid = candidates[Random.Shared.Next(candidates.Count)];
        return room.OwnerGuid;
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
    private Person? SnapshotPerson(Guid personGuid, bool isOffgrid = false)
    {
        return people.Update(personGuid, entity => personMapper.Map(entity, isOffgrid), out var snapshot) ? snapshot : null;
    }
}
