using PlanningPoker.Common.Constants;
using PlanningPoker.Common.Models;
using PlanningPoker.Repository.Models;
using PlanningPoker.Repository.Repositories;
using PlanningPoker.Service.Mappers;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Service.Services;

public class AdminActionService(
    IRoomRepository rooms,
    IPersonRepository people,
    IPersonMapper personMapper,
    IExitRoomResultMapper exitRoomResultMapper,
    IRoomService roomService,
    IRoomAdminNotifier adminNotifier,
    IRoomPresenceNotifier presenceNotifier,
    IRoomCountdownNotifier countdownNotifier,
    IRoomTimerService timer,
    IIssueLog issues) : IAdminActionService
{
    public async Task<ServiceResponse> CloseRoomAsync(string roomId)
    {
        // Disposing inside the room lock means a Join that comes later is refused, so nobody gets in while the room is going.
        var found = rooms.Update(roomId, room =>
        {
            room.IsDisposed = true;
            return (People: room.MemberGuids.Count, Connections: room.MemberConnections.Values.SelectMany(connections => connections).ToList());
        }, out var closing);
        if (!found)
        {
            return ServiceResponse.Failure(ServiceError.RoomNotFound(roomId));
        }

        // Removing the room reports it to the change log, which deletes it from the store too.
        rooms.Remove(roomId);
        await adminNotifier.RoomClosedAsync(roomId, closing.Connections);
        issues.Record(IssueLevel.Info, IssueLogConstants.AdminSource, $"Operator closed room \"{roomId}\" ({closing.People} people)");
        return ServiceResponse.Success();
    }

    public async Task<ServiceResponse> RemovePersonAsync(string roomId, Guid personGuid)
    {
        if (rooms.Get(roomId) is null)
        {
            return ServiceResponse.Failure(ServiceError.RoomNotFound(roomId));
        }

        var snapshot = people.Get(personGuid) is null ? null : SnapshotPerson(personGuid);
        if (snapshot is null)
        {
            return ServiceResponse.Failure(ServiceError.PersonNotFound(roomId, personGuid));
        }

        var found = rooms.Update(roomId, room => RemoveMember(room, personGuid), out var change);
        if (!found)
        {
            return ServiceResponse.Failure(ServiceError.RoomNotFound(roomId));
        }

        if (change is null || !change.WasMember)
        {
            return ServiceResponse.Failure(ServiceError.PersonNotFound(roomId, personGuid));
        }

        // The person's connections to this room are forgotten, so a later disconnect of theirs finds nothing to do.
        var connectionIds = new List<string>();
        people.Update(personGuid, entity =>
        {
            foreach (var pair in entity.Connections.Where(pair => pair.Value == roomId).ToList())
            {
                connectionIds.Add(pair.Key);
                entity.Connections.Remove(pair.Key);
            }

            return true;
        }, out _);

        if (change.Disposed)
        {
            rooms.Remove(roomId);
        }

        var newOwner = change.NewOwnerGuid is { } ownerGuid ? SnapshotPerson(ownerGuid) : null;
        var followUp = change.Disposed ? VotingFollowUp.None : roomService.FollowUpAfterChange(roomId);
        await presenceNotifier.PersonRemovedAsync(exitRoomResultMapper.Map(
            roomId, snapshot, personLeft: true, roomDisposed: change.Disposed, newOwner, followUp, RoomEventReasons.RemovedByOperator, connectionIds));
        await ApplyFollowUpAsync(roomId, followUp);
        issues.Record(IssueLevel.Info, IssueLogConstants.AdminSource, $"Operator removed {snapshot.Name} from room \"{roomId}\" (removed by operator)");
        return ServiceResponse.Success();
    }

    /// <summary>Takes the person out of the room, with their vote, whether they are online or offgrid. Runs under the room lock.</summary>
    private static MemberRemoval RemoveMember(RoomEntity room, Guid personGuid)
    {
        if (!room.MemberGuids.Remove(personGuid))
        {
            return new MemberRemoval(false, false, null);
        }

        room.MemberConnections.Remove(personGuid);
        room.DroppedAt.Remove(personGuid);
        room.OffgridGuids.Remove(personGuid);
        room.Votes.Remove(personGuid);
        room.DepartedVotes.Remove(personGuid);

        // The people left may all have voted, and a running countdown no longer applies to the old group.
        room.Revision++;
        if (room.MemberGuids.Count == 0)
        {
            room.IsDisposed = true;
            return new MemberRemoval(true, true, null);
        }

        return new MemberRemoval(true, false, room.OwnerGuid == personGuid ? HandOver(room) : null);
    }

    /// <summary>Gives the room to a random connected member, or to any member when nobody is connected.</summary>
    private static Guid HandOver(RoomEntity room)
    {
        var candidates = room.MemberGuids.Where(room.MemberConnections.ContainsKey).ToList();
        if (candidates.Count == 0)
        {
            candidates = [.. room.MemberGuids];
        }

        room.OwnerGuid = candidates[Random.Shared.Next(candidates.Count)];
        return room.OwnerGuid;
    }

    private Person? SnapshotPerson(Guid personGuid)
    {
        return people.Update(personGuid, entity => personMapper.Map(entity), out var snapshot) ? snapshot : null;
    }

    /// <summary>Starts the countdown ticker and sends its first second, or announces an immediate reveal.</summary>
    private async Task ApplyFollowUpAsync(string roomId, VotingFollowUp followUp)
    {
        if (followUp.Revealed is { } revealed)
        {
            await countdownNotifier.VotesRevealedAsync(roomId, revealed);
        }
        else if (followUp.Countdown is { } countdown)
        {
            timer.Start(roomId, countdown.Revision);
            await countdownNotifier.CountdownTickedAsync(roomId, countdown.Seconds);
        }
    }

    private sealed record MemberRemoval(bool WasMember, bool Disposed, Guid? NewOwnerGuid);
}
