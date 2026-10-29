using PlanningPoker.Common.Constants;
using PlanningPoker.Common.Models;
using PlanningPoker.Repository.Models;
using PlanningPoker.Repository.Repositories;
using PlanningPoker.Service.Mappers;
using PlanningPoker.Service.Models;
using PlanningPoker.Service.Validations;

namespace PlanningPoker.Service.Services;

/// <summary>
/// Locking: every membership decision (who is in a room, whether it is disposed, who owns it) is made inside
/// <see cref="IRoomRepository.Update{T}"/>. The person lock only edits the connection index and is never held while the
/// room lock is taken, and the room lock never reads a person under its lock, so the two locks are never nested.
/// </summary>
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

        // Index the connection first. A connection that is already indexed to another room is refused here.
        var indexed = people.Update(personGuid, entity =>
        {
            var person = personMapper.Map(entity);
            if (entity.Connections.TryGetValue(connectionId, out var indexedRoomId) && indexedRoomId != roomId)
            {
                return (Conflict: true, Person: person);
            }

            entity.Connections[connectionId] = roomId;
            return (Conflict: false, Person: person);
        }, out var join);
        if (!indexed)
        {
            return ServiceResponse<JoinRoomResult>.Failure(ServiceError.NoSession());
        }

        if (join.Conflict)
        {
            return ServiceResponse<JoinRoomResult>.Failure(ServiceError.ValidationFailed("connectionId", "already_in_room"));
        }

        // The room lock decides admission. The connection joins the member's connection set in the same step.
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

            var connections = room.MemberConnections.GetValueOrDefault(personGuid) ?? new HashSet<string>();
            connections.Add(connectionId);
            room.MemberConnections[personGuid] = connections;
            return true;
        }, out var added) && added;

        if (!admitted)
        {
            people.Update(personGuid, entity =>
            {
                if (entity.Connections.TryGetValue(connectionId, out var indexedRoomId) && indexedRoomId == roomId)
                {
                    entity.Connections.Remove(connectionId);
                }

                return true;
            }, out _);
            return ServiceResponse<JoinRoomResult>.Failure(ServiceError.RoomNotFound(roomId));
        }

        var snapshot = rooms.Get(roomId);
        if (snapshot is null)
        {
            return ServiceResponse<JoinRoomResult>.Failure(ServiceError.RoomNotFound(roomId));
        }

        var members = Members(snapshot);
        return ServiceResponse<JoinRoomResult>.Success(joinRoomResultMapper.Map(
            roomMapper.Map(snapshot),
            join.Person,
            members,
            voteStatusMapper.Map(snapshot, members)));
    }

    public ServiceResponse<ExitRoomResult> Exit(Guid personGuid, string connectionId)
    {
        // Step 1 (person lock): find the room this connection was indexed to and drop the index entry.
        var indexed = people.Update(personGuid, entity =>
        {
            var person = personMapper.Map(entity);
            if (!entity.Connections.Remove(connectionId, out var roomId))
            {
                return (Found: false, RoomId: string.Empty, Person: person);
            }

            return (Found: true, RoomId: roomId, Person: person);
        }, out var index);
        if (!indexed)
        {
            return ServiceResponse<ExitRoomResult>.Failure(ServiceError.NoSession());
        }

        if (!index.Found)
        {
            return ServiceResponse<ExitRoomResult>.Failure(ServiceError.Create(ErrorCodes.NotInRoom, "The connection is not in a room."));
        }

        // Step 2 (room lock): remove this connection from the member's set. The member leaves only when no connection
        // of theirs remains in this room, and that is decided here, so a Join that ran before this point keeps them in.
        var left = rooms.Update(index.RoomId, room =>
        {
            var connections = room.MemberConnections.GetValueOrDefault(personGuid);
            if (connections is null || !connections.Remove(connectionId))
            {
                return (Member: false, PersonLeft: false, Disposed: false, NewOwnerGuid: (Guid?)null);
            }

            if (connections.Count > 0)
            {
                return (Member: true, PersonLeft: false, Disposed: false, NewOwnerGuid: (Guid?)null);
            }

            room.MemberConnections.Remove(personGuid);
            room.MemberGuids.Remove(personGuid);
            room.Votes.Remove(personGuid);
            if (room.MemberGuids.Count == 0)
            {
                room.IsDisposed = true;
                return (Member: true, PersonLeft: true, Disposed: true, NewOwnerGuid: (Guid?)null);
            }

            if (room.OwnerGuid != personGuid)
            {
                return (Member: true, PersonLeft: true, Disposed: false, NewOwnerGuid: (Guid?)null);
            }

            room.OwnerGuid = room.MemberGuids[Random.Shared.Next(room.MemberGuids.Count)];
            return (Member: true, PersonLeft: true, Disposed: false, NewOwnerGuid: (Guid?)room.OwnerGuid);
        }, out var outcome);
        if (!left)
        {
            return ServiceResponse<ExitRoomResult>.Failure(ServiceError.RoomNotFound(index.RoomId));
        }

        if (!outcome.Member)
        {
            return ServiceResponse<ExitRoomResult>.Failure(ServiceError.Create(ErrorCodes.NotInRoom, "The connection is not in a room."));
        }

        if (outcome.Disposed)
        {
            rooms.Remove(index.RoomId);
        }

        // Read after the room lock is released: the new owner's details are looked up under their own person lock.
        var newOwner = outcome.NewOwnerGuid is { } newOwnerGuid ? SnapshotPerson(newOwnerGuid) : null;
        return ServiceResponse<ExitRoomResult>.Success(exitRoomResultMapper.Map(
            index.RoomId,
            index.Person,
            outcome.PersonLeft,
            outcome.Disposed,
            newOwner));
    }

    private List<Person> Members(RoomEntity room)
    {
        return room.MemberGuids.Select(SnapshotPerson).OfType<Person>().ToList();
    }

    /// <summary>Maps a person under their lock, so the copy never reads a half-applied change.</summary>
    private Person? SnapshotPerson(Guid personGuid)
    {
        return people.Update(personGuid, entity => personMapper.Map(entity), out var snapshot) ? snapshot : null;
    }
}
