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

        var person = people.Get(personGuid);
        if (person is null)
        {
            return ServiceResponse<JoinRoomResult>.Failure(ServiceError.Create(ErrorCodes.NoSession, "There is no valid session."));
        }

        person.ConnectionId = connectionId;
        person.IsConnected = true;
        var joined = rooms.Update(roomId, room =>
        {
            if (!room.MemberGuids.Contains(personGuid))
            {
                room.MemberGuids.Add(personGuid);
            }

            return true;
        }, out _);

        var snapshot = joined ? rooms.Get(roomId) : null;
        if (snapshot is null)
        {
            return ServiceResponse<JoinRoomResult>.Failure(ServiceError.Create(ErrorCodes.RoomNotFound, "The room does not exist.", ("room_id", roomId)));
        }

        var members = Members(snapshot);
        return ServiceResponse<JoinRoomResult>.Success(joinRoomResultMapper.Map(
            roomMapper.Map(snapshot),
            personMapper.Map(person),
            members.Select(personMapper.Map).ToList(),
            voteStatusMapper.Map(snapshot, members)));
    }

    public ServiceResponse<ExitRoomResult> Exit(Guid personGuid, string connectionId)
    {
        var person = people.Get(personGuid);
        var room = person?.ConnectionId == connectionId ? rooms.FindByPerson(personGuid) : null;
        if (person is null || room is null)
        {
            return ServiceResponse<ExitRoomResult>.Failure(ServiceError.Create(ErrorCodes.RoomNotFound, "The person is not in a room."));
        }

        person.IsConnected = false;
        person.ConnectionId = null;
        rooms.Update(room.Id, current =>
        {
            current.MemberGuids.Remove(personGuid);
            current.Votes.Remove(personGuid);
            if (current.MemberGuids.Count > 0 && current.OwnerGuid == personGuid)
            {
                current.OwnerGuid = current.MemberGuids[Random.Shared.Next(current.MemberGuids.Count)];
            }

            return true;
        }, out _);

        var updated = rooms.Get(room.Id);
        var disposed = updated is null || updated.MemberGuids.Count == 0;
        if (disposed)
        {
            rooms.Remove(room.Id);
        }

        var newOwner = !disposed && room.OwnerGuid == personGuid ? people.Get(updated!.OwnerGuid) : null;
        return ServiceResponse<ExitRoomResult>.Success(exitRoomResultMapper.Map(
            room.Id,
            personMapper.Map(person),
            disposed,
            newOwner is null ? null : personMapper.Map(newOwner)));
    }

    private List<PersonEntity> Members(RoomEntity room)
    {
        return room.MemberGuids.Select(people.Get).OfType<PersonEntity>().ToList();
    }
}
