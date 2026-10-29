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
    RoomExistsValidation existsValidation,
    VoteMarkValidation voteMarkValidation,
    ICardSetService cardSets) : IRoomService
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

        // The room lock decides admission. The connection joins the member's connection set in the same step, and the
        // person is only new to the room when no connection of theirs was already in it.
        var admission = rooms.Update(roomId, room =>
        {
            if (room.IsDisposed)
            {
                return (Admitted: false, PersonAdded: false);
            }

            var personAdded = !room.MemberGuids.Contains(personGuid);
            if (personAdded)
            {
                room.MemberGuids.Add(personGuid);
                if (room.VotingEnabled && join.Person.PersonType != PersonTypes.Observer)
                {
                    // A new voter has not voted yet, so a running countdown no longer applies.
                    room.Revision++;
                }
            }

            var connections = room.MemberConnections.GetValueOrDefault(personGuid) ?? new HashSet<string>();
            connections.Add(connectionId);
            room.MemberConnections[personGuid] = connections;
            return (Admitted: true, PersonAdded: personAdded);
        }, out var outcome) ? outcome : default;

        if (!admission.Admitted)
        {
            ReleaseConnection(personGuid, connectionId, roomId);
            return ServiceResponse<JoinRoomResult>.Failure(ServiceError.RoomNotFound(roomId));
        }

        // The room can be removed between admission and this read when its last member leaves. Nothing was announced yet,
        // so the connection is released and the join reports the room as gone.
        var snapshot = rooms.Get(roomId);
        if (snapshot is null)
        {
            ReleaseConnection(personGuid, connectionId, roomId);
            return ServiceResponse<JoinRoomResult>.Failure(ServiceError.RoomNotFound(roomId));
        }

        var members = Members(snapshot);
        return ServiceResponse<JoinRoomResult>.Success(joinRoomResultMapper.Map(
            roomMapper.Map(snapshot),
            join.Person,
            members,
            voteStatusMapper.Map(snapshot, members),
            cardSets.GetCards(snapshot.Configuration.CardSet).Data!,
            admission.PersonAdded));
    }

    /// <summary>Drops the connection's index entry for the room, if it still points there (person lock only).</summary>
    private void ReleaseConnection(Guid personGuid, string connectionId, string roomId)
    {
        people.Update(personGuid, entity =>
        {
            if (entity.Connections.TryGetValue(connectionId, out var indexedRoomId) && indexedRoomId == roomId)
            {
                entity.Connections.Remove(connectionId);
            }

            return true;
        }, out _);
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
            return ServiceResponse<ExitRoomResult>.Failure(ServiceError.NotInRoom());
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
            room.Revision++;
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
            return ServiceResponse<ExitRoomResult>.Failure(ServiceError.NotInRoom());
        }

        if (outcome.Disposed)
        {
            rooms.Remove(index.RoomId);
        }

        // Read after the room lock is released: the new owner's details are looked up under their own person lock.
        var newOwner = outcome.NewOwnerGuid is { } newOwnerGuid ? SnapshotPerson(newOwnerGuid) : null;
        var followUp = outcome.PersonLeft && !outcome.Disposed ? FollowUpAfterChange(index.RoomId) : VotingFollowUp.None;
        return ServiceResponse<ExitRoomResult>.Success(exitRoomResultMapper.Map(
            index.RoomId,
            index.Person,
            outcome.PersonLeft,
            outcome.Disposed,
            newOwner,
            followUp));
    }

    public ServiceResponse<VoteResult> Vote(Guid personGuid, string connectionId, string mark)
    {
        var located = Locate(personGuid, connectionId);
        if (!located.IsSuccess)
        {
            return ServiceResponse<VoteResult>.Failure(located);
        }

        var (roomId, person) = located.Data;
        if (person.PersonType == PersonTypes.Observer)
        {
            return ServiceResponse<VoteResult>.Failure(ServiceError.ObserverCannotVote());
        }

        var found = rooms.Update(roomId, room =>
        {
            if (!IsMember(room, personGuid, connectionId))
            {
                return ServiceResponse.Failure(ServiceError.NotInRoom());
            }

            if (!room.VotingEnabled)
            {
                return ServiceResponse.Failure(ServiceError.VotingClosed());
            }

            var valid = voteMarkValidation.Validate(room.Configuration.CardSet, mark);
            if (!valid.IsSuccess)
            {
                return valid;
            }

            room.Votes[personGuid] = mark;
            room.Revision++;
            return ServiceResponse.Success();
        }, out var stored);
        if (!found)
        {
            return ServiceResponse<VoteResult>.Failure(ServiceError.RoomNotFound(roomId));
        }

        if (!stored!.IsSuccess)
        {
            return ServiceResponse<VoteResult>.Failure(stored);
        }

        // Read after the room lock is released: member types come from their own person locks.
        var snapshot = rooms.Get(roomId);
        if (snapshot is null)
        {
            return ServiceResponse<VoteResult>.Failure(ServiceError.RoomNotFound(roomId));
        }

        var members = Members(snapshot);
        var status = voteStatusMapper.Map(snapshot, members);
        var followUp = Decide(snapshot, status);
        if (followUp.Countdown is not null && rooms.Get(roomId) is { } started)
        {
            status = voteStatusMapper.Map(started, members);
        }

        return ServiceResponse<VoteResult>.Success(new VoteResult { RoomId = roomId, PersonGuid = personGuid, VoteStatus = status, FollowUp = followUp });
    }

    public ServiceResponse<string> ClearVotes(Guid personGuid, string connectionId)
    {
        var cleared = InRoom(personGuid, connectionId, room =>
        {
            room.Votes.Clear();
            room.VotingEnabled = true;
            room.Revision++;
            return true;
        });
        return cleared.IsSuccess ? ServiceResponse<string>.Success(cleared.Data!.RoomId) : ServiceResponse<string>.Failure(cleared);
    }

    public ServiceResponse<RevealResult> ForceShowVotes(Guid personGuid, string connectionId)
    {
        var revealed = InRoom(personGuid, connectionId, room =>
        {
            if (!room.VotingEnabled)
            {
                return false;
            }

            room.VotingEnabled = false;
            room.Revision++;
            return true;
        });
        if (!revealed.IsSuccess)
        {
            return ServiceResponse<RevealResult>.Failure(revealed);
        }

        var change = revealed.Data!;
        return ServiceResponse<RevealResult>.Success(new RevealResult { RoomId = change.RoomId, VoteStatus = change.Changed ? RevealedStatus(change.RoomId) : null });
    }

    public TickResult Tick(string roomId, long revision)
    {
        var found = rooms.Update(roomId, room =>
        {
            if (!room.VotingEnabled || room.CountdownRevision != revision || room.Revision != revision)
            {
                return (Outcome: TickOutcome.Stale, Remaining: 0);
            }

            room.CountdownRemaining--;
            if (room.CountdownRemaining > 0)
            {
                return (Outcome: TickOutcome.Ticked, Remaining: room.CountdownRemaining);
            }

            room.VotingEnabled = false;
            room.Revision++;
            return (Outcome: TickOutcome.Revealed, Remaining: 0);
        }, out var tick);
        if (!found || tick.Outcome == TickOutcome.Stale)
        {
            return new TickResult { Outcome = TickOutcome.Stale };
        }

        return new TickResult
        {
            Outcome = tick.Outcome,
            Remaining = tick.Remaining,
            Revealed = tick.Outcome == TickOutcome.Revealed ? RevealedStatus(roomId) : null
        };
    }

    private static bool IsMember(RoomEntity room, Guid personGuid, string connectionId)
    {
        return room.MemberConnections.TryGetValue(personGuid, out var connections) && connections.Contains(connectionId);
    }

    /// <summary>Finds the room a connection is indexed to, under the person lock only.</summary>
    private ServiceResponse<(string RoomId, Person Person)> Locate(Guid personGuid, string connectionId)
    {
        var found = people.Update(personGuid, entity => (
            Indexed: entity.Connections.TryGetValue(connectionId, out var indexedRoomId),
            RoomId: indexedRoomId ?? string.Empty,
            Person: personMapper.Map(entity)), out var index);
        if (!found)
        {
            return ServiceResponse<(string RoomId, Person Person)>.Failure(ServiceError.NoSession());
        }

        return index.Indexed
            ? ServiceResponse<(string RoomId, Person Person)>.Success((index.RoomId, index.Person))
            : ServiceResponse<(string RoomId, Person Person)>.Failure(ServiceError.NotInRoom());
    }

    /// <summary>Runs a change in the connection's room under the room lock, after checking the connection is a member there.</summary>
    private ServiceResponse<RoomChange> InRoom(Guid personGuid, string connectionId, Func<RoomEntity, bool> change)
    {
        var located = Locate(personGuid, connectionId);
        if (!located.IsSuccess)
        {
            return ServiceResponse<RoomChange>.Failure(located);
        }

        var roomId = located.Data.RoomId;
        var found = rooms.Update(
            roomId,
            room => IsMember(room, personGuid, connectionId) ? (Member: true, Changed: change(room)) : (Member: false, Changed: false),
            out var outcome);
        if (!found)
        {
            return ServiceResponse<RoomChange>.Failure(ServiceError.RoomNotFound(roomId));
        }

        return outcome.Member
            ? ServiceResponse<RoomChange>.Success(new RoomChange(roomId, outcome.Changed))
            : ServiceResponse<RoomChange>.Failure(ServiceError.NotInRoom());
    }

    /// <summary>The follow-up to a change that may have left everyone voted (a person leaving): read a snapshot, then decide.</summary>
    private VotingFollowUp FollowUpAfterChange(string roomId)
    {
        var snapshot = rooms.Get(roomId);
        return snapshot is { VotingEnabled: true } ? Decide(snapshot, voteStatusMapper.Map(snapshot, Members(snapshot))) : VotingFollowUp.None;
    }

    /// <summary>
    /// Starts the countdown (or reveals at once for a countdown of 0) when everyone voted, but only if no other change
    /// reached the room since the snapshot: that newer change makes its own decision, so the last one wins.
    /// </summary>
    private VotingFollowUp Decide(RoomEntity snapshot, VoteStatus status)
    {
        if (!snapshot.VotingEnabled || !status.HasEveryoneVoted)
        {
            return VotingFollowUp.None;
        }

        var seconds = snapshot.Configuration.CountdownSeconds;
        var found = rooms.Update(snapshot.Id, room =>
        {
            // A countdown already running at this revision means a concurrent change made the same decision first.
            if (room.Revision != snapshot.Revision || !room.VotingEnabled || room.CountdownRevision == room.Revision)
            {
                return false;
            }

            if (seconds == 0)
            {
                room.VotingEnabled = false;
                room.Revision++;
            }
            else
            {
                room.CountdownRevision = room.Revision;
                room.CountdownRemaining = seconds;
            }

            return true;
        }, out var applied);
        if (!found || !applied)
        {
            return VotingFollowUp.None;
        }

        return seconds > 0
            ? new VotingFollowUp { Countdown = new CountdownStart { Revision = snapshot.Revision, Seconds = seconds } }
            : new VotingFollowUp { Revealed = RevealedStatus(snapshot.Id) };
    }

    private VoteStatus? RevealedStatus(string roomId)
    {
        var snapshot = rooms.Get(roomId);
        return snapshot is null ? null : voteStatusMapper.Map(snapshot, Members(snapshot));
    }

    private sealed record RoomChange(string RoomId, bool Changed);

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
