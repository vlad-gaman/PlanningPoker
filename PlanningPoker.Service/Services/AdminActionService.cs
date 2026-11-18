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
    IRoomMembership membership,
    IRoomAdminNotifier adminNotifier,
    IRoomPresenceNotifier presenceNotifier,
    IRoomTimerService timer,
    IIssueLog issues) : IAdminActionService
{
    public async Task<ServiceResponse> CloseRoomAsync(string roomId)
    {
        // Disposing inside the room lock means a Join that comes later is refused, so nobody gets in while the room is going.
        var found = rooms.Update(roomId, room =>
        {
            membership.Dispose(room);
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
        var room = rooms.Get(roomId);
        if (room is null)
        {
            return ServiceResponse.Failure(ServiceError.RoomNotFound(roomId));
        }

        var snapshot = people.Get(personGuid) is null ? null : SnapshotPerson(personGuid, room.RoleOf(personGuid));
        if (snapshot is null)
        {
            return ServiceResponse.Failure(ServiceError.PersonNotFound(roomId, personGuid));
        }

        var found = rooms.Update(roomId, room => membership.Leave(room, personGuid, LeaveReason.OperatorRemoval), out var change);
        if (!found)
        {
            return ServiceResponse.Failure(ServiceError.RoomNotFound(roomId));
        }

        if (change is null || !change.PersonLeft)
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

        var newOwner = change.NewOwnerGuid is { } ownerGuid ? SnapshotPerson(ownerGuid, change.NewOwnerPersonType!) : null;
        var followUp = change.Disposed ? VotingFollowUp.None : roomService.FollowUpAfterChange(roomId);
        await presenceNotifier.PersonRemovedAsync(exitRoomResultMapper.Map(
            roomId, snapshot, personLeft: true, roomDisposed: change.Disposed, newOwner, followUp, RoomEventReasons.RemovedByOperator, connectionIds));
        await timer.ApplyAsync(roomId, followUp);
        issues.Record(IssueLevel.Info, IssueLogConstants.AdminSource, $"Operator removed {snapshot.Name} from room \"{roomId}\" (removed by operator)");
        return ServiceResponse.Success();
    }

    private Person? SnapshotPerson(Guid personGuid, string personType)
    {
        return people.Update(personGuid, entity => personMapper.Map(entity, isOffgrid: false, personType), out var snapshot) ? snapshot : null;
    }
}
