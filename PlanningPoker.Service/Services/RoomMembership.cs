using PlanningPoker.Common.Constants;
using PlanningPoker.Repository.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Service.Services;

public class RoomMembership(IRoomRound round) : IRoomMembership
{
    public MembershipJoined Join(RoomEntity room, Guid personGuid, string connectionId, string personType)
    {
        if (room.IsDisposed)
        {
            return new MembershipJoined(false, false, false, false);
        }

        var personAdded = !room.MemberGuids.Contains(personGuid);
        var wasOffgrid = false;
        if (personAdded)
        {
            room.Roles[personGuid] = personType;
        }

        var isObserver = room.RoleOf(personGuid) == PersonTypes.Observer;
        var isVoter = !isObserver;
        if (personAdded)
        {
            // A room whose owner is not a member (the creator has not joined yet) is owned by the first person admitted.
            if (!room.MemberGuids.Contains(room.OwnerGuid))
            {
                room.OwnerGuid = personGuid;
            }

            room.MemberGuids.Add(personGuid);
            var restored = RestoreDepartedVote(room, personGuid, isObserver);
            if (room.VotingEnabled && isVoter && !restored)
            {
                // A new voter has not voted yet, so a running countdown no longer applies.
                round.InvalidateCountdown(room);
            }
        }
        else
        {
            wasOffgrid = Return(room, personGuid);
            if (wasOffgrid && room.VotingEnabled && isVoter && !room.Votes.ContainsKey(personGuid))
            {
                // The room had stopped waiting for them; now a voter without a vote is back, so a running countdown no longer applies.
                round.InvalidateCountdown(room);
            }
        }

        var connections = room.MemberConnections.GetValueOrDefault(personGuid) ?? new HashSet<string>();
        connections.Add(connectionId);
        room.MemberConnections[personGuid] = connections;
        var resetCountdown = room.VotingEnabled && isVoter && !(wasOffgrid && room.Votes.ContainsKey(personGuid));
        return new MembershipJoined(true, personAdded, wasOffgrid, resetCountdown);
    }

    public bool Return(RoomEntity room, Guid personGuid)
    {
        room.DroppedAt.Remove(personGuid);
        room.ClosingAt.Remove(personGuid);
        return room.OffgridGuids.Remove(personGuid);
    }

    public void Drop(RoomEntity room, Guid personGuid, DateTimeOffset at)
    {
        room.MemberConnections.Remove(personGuid);
        room.DroppedAt.TryAdd(personGuid, at);
    }

    public void GoOffgrid(RoomEntity room, Guid personGuid)
    {
        room.OffgridGuids.Add(personGuid);
    }

    public MembershipLeft Leave(RoomEntity room, Guid personGuid, LeaveReason reason)
    {
        room.MemberConnections.Remove(personGuid);
        room.DroppedAt.Remove(personGuid);
        room.ClosingAt.Remove(personGuid);
        room.OffgridGuids.Remove(personGuid);
        var personType = room.RoleOf(personGuid);
        if (!room.MemberGuids.Remove(personGuid))
        {
            return new MembershipLeft(false, false, null, string.Empty, null);
        }

        room.Roles.Remove(personGuid);
        room.DepartedVotes.Remove(personGuid);
        if (room.Votes.Remove(personGuid, out var mark) && reason == LeaveReason.Deliberate)
        {
            // The vote is parked, not dropped: a rejoin of the same person gets it back (see Join).
            room.DepartedVotes[personGuid] = mark;
        }

        // The people left may all have voted, and a running countdown no longer applies to the old group.
        round.InvalidateCountdown(room);
        if (room.MemberGuids.Count == 0)
        {
            Dispose(room);
            return new MembershipLeft(true, true, null, personType, null);
        }

        var newOwner = room.OwnerGuid == personGuid ? HandOver(room, ownerLeaving: true) : null;
        return new MembershipLeft(true, false, newOwner, personType, newOwner is { } guid ? room.RoleOf(guid) : null);
    }

    public Guid? HandOver(RoomEntity room, bool ownerLeaving)
    {
        var candidates = room.MemberGuids.Where(guid => guid != room.OwnerGuid && room.MemberConnections.ContainsKey(guid)).ToList();
        if (candidates.Count == 0 && ownerLeaving)
        {
            candidates = [.. room.MemberGuids.Where(guid => guid != room.OwnerGuid)];
        }

        if (candidates.Count == 0)
        {
            return null;
        }

        room.OwnerGuid = candidates[Random.Shared.Next(candidates.Count)];
        return room.OwnerGuid;
    }

    public void Dispose(RoomEntity room)
    {
        room.IsDisposed = true;
    }

    /// <summary>
    /// Puts a parked vote back when its person rejoins an open round. A reveal, or a person who is an observer now, drops
    /// it instead: a reveal has already frozen the votes without them, and observers do not vote.
    /// </summary>
    private static bool RestoreDepartedVote(RoomEntity room, Guid personGuid, bool isObserver)
    {
        if (!room.DepartedVotes.Remove(personGuid, out var mark))
        {
            return false;
        }

        if (room.Revealed is not null || !room.VotingEnabled || isObserver)
        {
            return false;
        }

        room.Votes[personGuid] = mark;
        return true;
    }
}
