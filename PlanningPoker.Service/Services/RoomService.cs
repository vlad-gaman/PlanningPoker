using PlanningPoker.Common.Constants;
using PlanningPoker.Common.Models;
using PlanningPoker.Repository.Models;
using PlanningPoker.Repository.Repositories;
using PlanningPoker.Service.Mappers;
using PlanningPoker.Service.Models;
using PlanningPoker.Service.Validations;

namespace PlanningPoker.Service.Services;

public class RoomService(
    IRoomRepository rooms,
    IPersonRepository people,
    IRoomNameGenerator nameGenerator,
    IRoomEntityMapper roomEntityMapper,
    IRoomMapper roomMapper,
    IPersonMapper personMapper,
    IVoteStatusMapper voteStatusMapper,
    IJoinRoomResultMapper joinRoomResultMapper,
    IExitRoomResultMapper exitRoomResultMapper,
    RoomIdentifierValidation identifierValidation,
    RoomExistsValidation existsValidation) : IRoomService
{
    public ServiceResponse<Room> Create(Guid ownerGuid, RoomSettings settings)
    {
        for (var attempt = 0; attempt < RoomConstraints.IdGenerationAttempts; attempt++)
        {
            var id = settings.UseFunRoomName ? nameGenerator.Generate() : Guid.NewGuid().ToString();
            var entity = roomEntityMapper.Map(id, ownerGuid, settings.Configuration);
            if (rooms.TryAdd(entity))
            {
                return ServiceResponse<Room>.Success(roomMapper.Map(entity));
            }
        }

        return ServiceResponse<Room>.Failure(ServiceError.Create(ErrorCodes.RoomIdGenerationFailed, "A unique room id could not be generated."));
    }

    public ServiceResponse<string> Exists(string roomId)
    {
        var exists = existsValidation.Validate(roomId);
        return exists.IsSuccess ? ServiceResponse<string>.Success(roomId) : ServiceResponse<string>.Failure(exists);
    }

    public ServiceResponse<JoinRoomResult> Join(Guid personGuid, string connectionId, string roomId)
    {
        var identifier = identifierValidation.Validate(roomId);
        if (!identifier.IsSuccess)
        {
            return ServiceResponse<JoinRoomResult>.Failure(identifier);
        }

        var exists = existsValidation.Validate(roomId);
        if (!exists.IsSuccess)
        {
            return ServiceResponse<JoinRoomResult>.Failure(exists);
        }

        // The connection is registered before the membership is added, so an Exit of an older connection
        // running at the same time sees this one and keeps the person in the room.
        var registered = people.Update(personGuid, entity =>
        {
            entity.Connections[connectionId] = roomId;
            return personMapper.Map(entity);
        }, out var joiner);
        if (!registered)
        {
            return ServiceResponse<JoinRoomResult>.Failure(ServiceError.NoSession());
        }

        var admitted = rooms.Update(roomId, room =>
        {
            if (room.IsDisposed)
            {
                return false;
            }

            if (!room.MemberGuids.Contains(personGuid))
            {
                room.MemberGuids.Add(personGuid);
            }

            return true;
        }, out var added) && added;

        var snapshot = admitted ? rooms.Get(roomId) : null;
        if (snapshot is null)
        {
            people.Update(personGuid, entity => entity.Connections.Remove(connectionId), out _);
            return ServiceResponse<JoinRoomResult>.Failure(ServiceError.Create(ErrorCodes.RoomNotFound, "The room does not exist.", ("room_id", roomId)));
        }

        var members = Members(snapshot);
        return ServiceResponse<JoinRoomResult>.Success(joinRoomResultMapper.Map(
            roomMapper.Map(snapshot),
            joiner!,
            members.Select(personMapper.Map).ToList(),
            voteStatusMapper.Map(snapshot, members)));
    }

    public ServiceResponse<ExitRoomResult> Exit(Guid personGuid, string connectionId)
    {
        // Person lock first: drop this connection and note whether the person still has another one in the same room.
        // The person lock is released before the room lock is taken, so the two locks are never nested.
        var released = people.Update(personGuid, entity =>
        {
            var person = personMapper.Map(entity);
            if (!entity.Connections.TryGetValue(connectionId, out var roomId))
            {
                return (Found: false, RoomId: string.Empty, Person: person, StillInRoom: false);
            }

            entity.Connections.Remove(connectionId);
            return (Found: true, RoomId: roomId, Person: person, StillInRoom: entity.Connections.Values.Contains(roomId));
        }, out var release);
        if (!released || !release.Found)
        {
            return ServiceResponse<ExitRoomResult>.Failure(ServiceError.Create(ErrorCodes.RoomNotFound, "The person is not in a room."));
        }

        if (release.StillInRoom)
        {
            return ServiceResponse<ExitRoomResult>.Failure(ServiceError.Create(
                ErrorCodes.PersonStillInRoom,
                "The person still has another connection to this room.",
                ("room_id", release.RoomId)));
        }

        // Room lock: the disposal and the ownership transfer are decided here, from the state this lock sees.
        var left = rooms.Update(release.RoomId, room =>
        {
            room.MemberGuids.Remove(personGuid);
            room.Votes.Remove(personGuid);
            if (room.MemberGuids.Count == 0)
            {
                room.IsDisposed = true;
                return (Disposed: true, NewOwnerGuid: (Guid?)null);
            }

            if (room.OwnerGuid != personGuid)
            {
                return (Disposed: false, NewOwnerGuid: (Guid?)null);
            }

            room.OwnerGuid = room.MemberGuids[Random.Shared.Next(room.MemberGuids.Count)];
            return (Disposed: false, NewOwnerGuid: (Guid?)room.OwnerGuid);
        }, out var outcome);
        if (!left)
        {
            return ServiceResponse<ExitRoomResult>.Failure(ServiceError.Create(ErrorCodes.RoomNotFound, "The room does not exist.", ("room_id", release.RoomId)));
        }

        if (outcome.Disposed)
        {
            rooms.Remove(release.RoomId);
        }

        var newOwner = outcome.NewOwnerGuid is { } newOwnerGuid ? people.Get(newOwnerGuid) : null;
        return ServiceResponse<ExitRoomResult>.Success(exitRoomResultMapper.Map(
            release.RoomId,
            release.Person,
            outcome.Disposed,
            newOwner is null ? null : personMapper.Map(newOwner)));
    }

    private List<PersonEntity> Members(RoomEntity room)
    {
        return room.MemberGuids.Select(people.Get).OfType<PersonEntity>().ToList();
    }
}
