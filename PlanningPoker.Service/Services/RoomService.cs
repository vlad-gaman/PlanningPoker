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
/// Presence (dropped, offgrid and removed members) lives in <see cref="PresenceService"/>.
/// </summary>
public class RoomService(
    IRoomRepository rooms,
    IRoomUpdater roomUpdater,
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
    IRoomConfigurationEntityMapper configurationEntityMapper,
    IRoomConfigurationMapper configurationMapper,
    ICardSetService cardSets,
    IPlanningPokerMetrics metrics,
    IRoomMembership membership,
    IRoomRound round,
    IRoomAlarm alarm,
    IAlarmStateMapper alarmStateMapper,
    IAlarmChangeResultMapper alarmChangeResultMapper,
    TimeProvider time) : IRoomService
{
    public ServiceResponse<Room> Create(Guid ownerGuid, RoomSettings settings)
    {
        for (var attempt = 0; attempt < RoomConstraints.IdGenerationAttempts; attempt++)
        {
            var id = settings.UseFunRoomName ? nameGenerator.Generate() : Guid.NewGuid().ToString();
            var entity = roomEntityMapper.Map(id, ownerGuid, settings.Configuration);
            if (rooms.TryAdd(entity))
            {
                metrics.RoomCreated();
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
        var admission = roomUpdater.Update(roomId, room => MembershipUpdate(membership.Join(room, personGuid, connectionId, join.Person.PersonType)), out var outcome) ? outcome : null;

        if (admission is not { Admitted: true })
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
            personMapper.Map(join.Person, snapshot.RoleOf(personGuid)),
            members,
            voteStatusMapper.Map(snapshot, members),
            cardSets.GetCards(snapshot.Configuration.CardSet).Data!,
            admission.PersonAdded,
            admission.WasOffgrid,
            admission.ResetCountdown,
            snapshot.Votes.GetValueOrDefault(personGuid)));
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

            entity.LastSeenAt = time.GetUtcNow();
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
        var left = roomUpdater.Update(index.RoomId, room =>
        {
            var connections = room.MemberConnections.GetValueOrDefault(personGuid);
            if (connections is null || !connections.Remove(connectionId))
            {
                return RoomUpdate.Unchanged((Member: false, Role: room.RoleOf(personGuid), PersonLeft: false, Disposed: false, NewOwnerGuid: (Guid?)null, NewOwnerRole: (string?)null, Decision: RoundDecision.None));
            }

            if (connections.Count > 0)
            {
                return RoomUpdate.Changed((Member: true, Role: room.RoleOf(personGuid), PersonLeft: false, Disposed: false, NewOwnerGuid: (Guid?)null, NewOwnerRole: (string?)null, Decision: RoundDecision.None));
            }

            // The people left may all have voted: the round decides in the same step as the leave.
            var change = membership.Leave(room, personGuid, LeaveReason.Deliberate);
            var decision = change.PersonLeft && !change.Disposed ? round.Decide(room) : RoundDecision.None;
            return RoomUpdate.Changed((Member: true, Role: change.PersonType, change.PersonLeft, change.Disposed, change.NewOwnerGuid, NewOwnerRole: change.NewOwnerPersonType, Decision: decision));
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
        var newOwner = outcome.NewOwnerGuid is { } newOwnerGuid ? SnapshotPerson(newOwnerGuid, outcome.NewOwnerRole!) : null;
        var followUp = outcome.Decision.IsNone ? VotingFollowUp.None : ToFollowUp(outcome.Decision, CurrentStatus(index.RoomId));
        return ServiceResponse<ExitRoomResult>.Success(exitRoomResultMapper.Map(
            index.RoomId,
            personMapper.Map(index.Person, outcome.Role),
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

        var roomId = located.Data.RoomId;
        string? cardSet = null;
        var found = roomUpdater.Update(roomId, room =>
        {
            if (!IsMember(room, personGuid, connectionId))
            {
                return RoomUpdate.Unchanged((Response: ServiceResponse.Failure(ServiceError.NotInRoom()), Decision: RoundDecision.None));
            }

            if (room.RoleOf(personGuid) == PersonTypes.Observer)
            {
                return RoomUpdate.Unchanged((Response: ServiceResponse.Failure(ServiceError.ObserverCannotVote()), Decision: RoundDecision.None));
            }

            if (!room.VotingEnabled)
            {
                return RoomUpdate.Unchanged((Response: ServiceResponse.Failure(ServiceError.VotingClosed()), Decision: RoundDecision.None));
            }

            var valid = voteMarkValidation.Validate(room.Configuration.CardSet, mark);
            if (!valid.IsSuccess)
            {
                return RoomUpdate.Unchanged((Response: valid, Decision: RoundDecision.None));
            }

            cardSet = room.Configuration.CardSet;
            return RoomUpdate.Changed((Response: ServiceResponse.Success(), Decision: round.Vote(room, personGuid, mark)));
        }, out var outcome);
        if (!found)
        {
            return ServiceResponse<VoteResult>.Failure(ServiceError.RoomNotFound(roomId));
        }

        if (!outcome.Response.IsSuccess)
        {
            return ServiceResponse<VoteResult>.Failure(outcome.Response);
        }

        metrics.VoteCast(cardSet!);

        var status = CurrentStatus(roomId);
        if (status is null)
        {
            return ServiceResponse<VoteResult>.Failure(ServiceError.RoomNotFound(roomId));
        }

        return ServiceResponse<VoteResult>.Success(new VoteResult { RoomId = roomId, PersonGuid = personGuid, VoteStatus = status, FollowUp = ToFollowUp(outcome.Decision, status) });
    }

    public ServiceResponse<ClearVotesResult> ClearVotes(Guid personGuid, string connectionId)
    {
        var roundStartedAt = DateTimeOffset.MinValue;
        var cleared = InRoom(personGuid, connectionId, room =>
        {
            round.Clear(room, restartTimer: true);
            roundStartedAt = room.RoundStartedAt;
            return true;
        });
        return cleared.IsSuccess
            ? ServiceResponse<ClearVotesResult>.Success(new ClearVotesResult { RoomId = cleared.Data!.RoomId, RoundStartedAt = roundStartedAt })
            : ServiceResponse<ClearVotesResult>.Failure(cleared);
    }

    public ServiceResponse<RevealResult> ForceShowVotes(Guid personGuid, string connectionId)
    {
        var revealed = InRoom(personGuid, connectionId, round.Reveal);
        if (!revealed.IsSuccess)
        {
            return ServiceResponse<RevealResult>.Failure(revealed);
        }

        var roomId = revealed.Data!.RoomId;
        return ServiceResponse<RevealResult>.Success(new RevealResult { RoomId = roomId, VoteStatus = revealed.Data.Changed ? CurrentStatus(roomId) : null });
    }

    public ServiceResponse<PersonTypeChangeResult> ChangePersonType(Guid personGuid, string connectionId, string personType)
    {
        var located = Locate(personGuid, connectionId);
        if (!located.IsSuccess)
        {
            return ServiceResponse<PersonTypeChangeResult>.Failure(located);
        }

        // The room alone holds the role: it changes under the room lock, which cancels a running countdown while voting is open.
        var (roomId, profile) = located.Data;
        var found = roomUpdater.Update(
            roomId,
            room =>
            {
                if (!IsMember(room, personGuid, connectionId))
                {
                    return RoomUpdate.Unchanged((Member: false, Change: new RoleChange(false, RoundDecision.None)));
                }

                var change = round.ChangeRole(room, personGuid, personType);
                return RoomUpdate.Of((Member: true, Change: change), change.Changed);
            },
            out var outcome);
        if (!found)
        {
            return ServiceResponse<PersonTypeChangeResult>.Failure(ServiceError.RoomNotFound(roomId));
        }

        if (!outcome.Member)
        {
            return ServiceResponse<PersonTypeChangeResult>.Failure(ServiceError.NotInRoom());
        }

        var status = CurrentStatus(roomId);
        if (status is null)
        {
            return ServiceResponse<PersonTypeChangeResult>.Failure(ServiceError.RoomNotFound(roomId));
        }

        return ServiceResponse<PersonTypeChangeResult>.Success(new PersonTypeChangeResult
        {
            RoomId = roomId,
            Person = personMapper.Map(profile, personType),
            Changed = outcome.Change.Changed,
            VoteStatus = status,
            FollowUp = ToFollowUp(outcome.Change.Decision, status)
        });
    }

    public ServiceResponse<RoomConfigurationChangeResult> UpdateConfiguration(Guid personGuid, string connectionId, RoomConfiguration configuration)
    {
        var located = Locate(personGuid, connectionId);
        if (!located.IsSuccess)
        {
            return ServiceResponse<RoomConfigurationChangeResult>.Failure(located);
        }

        var (roomId, person) = located.Data;
        var cards = cardSets.GetCards(configuration.CardSet);
        if (!cards.IsSuccess)
        {
            return ServiceResponse<RoomConfigurationChangeResult>.Failure(cards);
        }

        var found = roomUpdater.Update(roomId, room =>
        {
            if (!IsMember(room, personGuid, connectionId))
            {
                return RoomUpdate.Unchanged((Response: ServiceResponse.Failure(ServiceError.NotInRoom()), Cleared: false, Decision: RoundDecision.None));
            }

            if (room.OwnerGuid != personGuid)
            {
                return RoomUpdate.Unchanged((Response: ServiceResponse.Failure(ServiceError.NotRoomOwner()), Cleared: false, Decision: RoundDecision.None));
            }

            // Shown votes are dropped (the old rule). So are votes cast for another card set: their marks are not cards of the new one.
            var alarmOnly = room.Configuration.DiffersOnlyInAlarmFrom(configurationEntityMapper.Map(configuration));
            var cleared = !alarmOnly && (!room.VotingEnabled || room.Configuration.CardSet != configuration.CardSet);
            room.Configuration = configurationEntityMapper.Map(configuration);
            alarm.IdleWhenDisabled(room);
            if (alarmOnly)
            {
                // The Alarm settings do not touch the round: shown votes stay and a running countdown keeps ticking.
                return RoomUpdate.Changed((Response: ServiceResponse.Success(), Cleared: false, Decision: RoundDecision.None));
            }

            if (cleared)
            {
                round.Clear(room, restartTimer: false);
                return RoomUpdate.Changed((Response: ServiceResponse.Success(), Cleared: true, Decision: RoundDecision.None));
            }

            // The new settings may change the countdown, so the round decides again.
            round.InvalidateCountdown(room);
            return RoomUpdate.Changed((Response: ServiceResponse.Success(), Cleared: false, Decision: round.Decide(room)));
        }, out var outcome);
        if (!found)
        {
            return ServiceResponse<RoomConfigurationChangeResult>.Failure(ServiceError.RoomNotFound(roomId));
        }

        if (!outcome.Response.IsSuccess)
        {
            return ServiceResponse<RoomConfigurationChangeResult>.Failure(outcome.Response);
        }

        var snapshot = rooms.Get(roomId);
        if (snapshot is null)
        {
            return ServiceResponse<RoomConfigurationChangeResult>.Failure(ServiceError.RoomNotFound(roomId));
        }

        var status = voteStatusMapper.Map(snapshot, Members(snapshot));
        return ServiceResponse<RoomConfigurationChangeResult>.Success(new RoomConfigurationChangeResult
        {
            RoomId = roomId,
            Configuration = configurationMapper.Map(snapshot.Configuration),
            Cards = cards.Data!,
            UpdatedBy = person.Name,
            Alarm = alarmStateMapper.Map(snapshot),
            VotesCleared = outcome.Cleared,
            VoteStatus = status,
            FollowUp = ToFollowUp(outcome.Decision, status)
        });
    }

    public ServiceResponse<AlarmChangeResult> StartAlarm(Guid personGuid, string connectionId)
    {
        return ChangeAlarm(personGuid, connectionId, alarm.Start);
    }

    public ServiceResponse<AlarmChangeResult> CancelAlarm(Guid personGuid, string connectionId)
    {
        return ChangeAlarm(personGuid, connectionId, alarm.Cancel);
    }

    public ServiceResponse<AlarmChangeResult> ResetAlarm(Guid personGuid, string connectionId)
    {
        return ChangeAlarm(personGuid, connectionId, alarm.Reset);
    }

    public ServiceResponse<AlarmChangeResult> ElapseAlarm(string roomId, DateTimeOffset endsAt)
    {
        return UpdateAlarm(roomId, room => alarm.Elapse(room, endsAt));
    }

    /// <summary>Runs an Alarm change in the connection's room under the room lock, for any member.</summary>
    private ServiceResponse<AlarmChangeResult> ChangeAlarm(Guid personGuid, string connectionId, Func<RoomEntity, ServiceResponse<AlarmState>> change)
    {
        var located = Locate(personGuid, connectionId);
        if (!located.IsSuccess)
        {
            return ServiceResponse<AlarmChangeResult>.Failure(located);
        }

        return UpdateAlarm(located.Data.RoomId, room => IsMember(room, personGuid, connectionId) ? change(room) : ServiceResponse<AlarmState>.Failure(ServiceError.NotInRoom()));
    }

    /// <summary>Runs an Alarm change under the room lock; only a change that succeeded counts as room activity.</summary>
    private ServiceResponse<AlarmChangeResult> UpdateAlarm(string roomId, Func<RoomEntity, ServiceResponse<AlarmState>> change)
    {
        var found = roomUpdater.Update(roomId, room =>
        {
            var changed = change(room);
            return RoomUpdate.Of(changed, changed.IsSuccess);
        }, out var outcome);
        if (!found || outcome is null)
        {
            return ServiceResponse<AlarmChangeResult>.Failure(ServiceError.RoomNotFound(roomId));
        }

        return outcome.IsSuccess
            ? ServiceResponse<AlarmChangeResult>.Success(alarmChangeResultMapper.Map(roomId, outcome.Data!))
            : ServiceResponse<AlarmChangeResult>.Failure(outcome);
    }

    public ServiceResponse<OwnershipTransferResult> TransferOwnership(Guid personGuid, string connectionId, Guid targetPersonGuid)
    {
        var located = Locate(personGuid, connectionId);
        if (!located.IsSuccess)
        {
            return ServiceResponse<OwnershipTransferResult>.Failure(located);
        }

        var roomId = located.Data.RoomId;
        var found = roomUpdater.Update(roomId, room =>
        {
            if (!IsMember(room, personGuid, connectionId))
            {
                return RoomUpdate.Unchanged((Response: ServiceResponse.Failure(ServiceError.NotInRoom()), Changed: false, Role: string.Empty));
            }

            if (room.OwnerGuid != personGuid)
            {
                return RoomUpdate.Unchanged((Response: ServiceResponse.Failure(ServiceError.NotRoomOwner()), Changed: false, Role: string.Empty));
            }

            if (!room.MemberGuids.Contains(targetPersonGuid))
            {
                return RoomUpdate.Unchanged((Response: ServiceResponse.Failure(ServiceError.ValidationFailed("targetPersonGuid", "not_a_member")), Changed: false, Role: string.Empty));
            }

            if (room.OwnerGuid == targetPersonGuid)
            {
                return RoomUpdate.Unchanged((Response: ServiceResponse.Success(), Changed: false, Role: string.Empty));
            }

            room.OwnerGuid = targetPersonGuid;
            return RoomUpdate.Changed((Response: ServiceResponse.Success(), Changed: true, Role: room.RoleOf(targetPersonGuid)));
        }, out var outcome);
        if (!found)
        {
            return ServiceResponse<OwnershipTransferResult>.Failure(ServiceError.RoomNotFound(roomId));
        }

        if (!outcome.Response.IsSuccess)
        {
            return ServiceResponse<OwnershipTransferResult>.Failure(outcome.Response);
        }

        // Read after the room lock is released: the new owner's details come from their own person lock.
        return ServiceResponse<OwnershipTransferResult>.Success(new OwnershipTransferResult
        {
            RoomId = roomId,
            NewOwner = outcome.Changed ? SnapshotPerson(targetPersonGuid, outcome.Role) : null
        });
    }

    /// <summary>The vote status of the room as it is now; null when the room is gone.</summary>
    private VoteStatus? CurrentStatus(string roomId)
    {
        var snapshot = rooms.Get(roomId);
        return snapshot is null ? null : voteStatusMapper.Map(snapshot, Members(snapshot));
    }

    public TickResult Tick(string roomId, long revision)
    {
        if (!roomUpdater.Update(roomId, room => TickUpdate(round.Tick(room, revision)), out var tick) || tick is null || tick.Outcome == TickOutcome.Stale)
        {
            return new TickResult { Outcome = TickOutcome.Stale };
        }

        return new TickResult
        {
            Outcome = tick.Outcome,
            Remaining = tick.Remaining,
            Revealed = tick.Outcome == TickOutcome.Revealed ? CurrentStatus(roomId) : null
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
        var found = roomUpdater.Update(
            roomId,
            room =>
            {
                if (!IsMember(room, personGuid, connectionId))
                {
                    return RoomUpdate.Unchanged((Member: false, Changed: false));
                }

                var changed = change(room);
                return RoomUpdate.Of((Member: true, Changed: changed), changed);
            },
            out var outcome);
        if (!found)
        {
            return ServiceResponse<RoomChange>.Failure(ServiceError.RoomNotFound(roomId));
        }

        return outcome.Member
            ? ServiceResponse<RoomChange>.Success(new RoomChange(roomId, outcome.Changed))
            : ServiceResponse<RoomChange>.Failure(ServiceError.NotInRoom());
    }

    /// <summary>The follow-up to a change that may have left everyone voted (a person leaving): the round decides under the room lock.</summary>
    public VotingFollowUp FollowUpAfterChange(string roomId)
    {
        if (!roomUpdater.Update(roomId, room => DecisionUpdate(round.Decide(room)), out var decision) || decision is null || decision.IsNone)
        {
            return VotingFollowUp.None;
        }

        return ToFollowUp(decision, CurrentStatus(roomId));
    }

    /// <summary>Maps what the round decided to the follow-up the caller acts on; <paramref name="status"/> is the room's status after the change.</summary>
    private static VotingFollowUp ToFollowUp(RoundDecision decision, VoteStatus? status)
    {
        return decision.IsNone ? VotingFollowUp.None : new VotingFollowUp { Countdown = decision.Countdown, Revealed = decision.Revealed ? status : null };
    }

    private sealed record RoomChange(string RoomId, bool Changed);

    /// <summary>A join changes the room whenever the person was admitted; a refused one touched nothing.</summary>
    private static RoomUpdate<MembershipJoined> MembershipUpdate(MembershipJoined joined)
    {
        return RoomUpdate.Of(joined, joined.Admitted);
    }

    /// <summary>A stale tick touched nothing; any other advances the countdown or reveals.</summary>
    private static RoomUpdate<RoundTick> TickUpdate(RoundTick tick)
    {
        return RoomUpdate.Of(tick, tick.Outcome != TickOutcome.Stale);
    }

    /// <summary>Deciding changes the room only when it starts a countdown or reveals.</summary>
    private static RoomUpdate<RoundDecision> DecisionUpdate(RoundDecision decision)
    {
        return RoomUpdate.Of(decision, !decision.IsNone);
    }

    private List<Person> Members(RoomEntity room)
    {
        return room.MemberGuids.Select(guid => SnapshotPerson(guid, room.RoleOf(guid), room.OffgridGuids.Contains(guid))).OfType<Person>().ToList();
    }

    /// <summary>Maps a person under their lock, so the copy never reads a half-applied change. The type is their role in the room.</summary>
    private Person? SnapshotPerson(Guid personGuid, string personType, bool isOffgrid = false)
    {
        return people.Update(personGuid, entity => personMapper.Map(entity, isOffgrid, personType), out var snapshot) ? snapshot : null;
    }
}
