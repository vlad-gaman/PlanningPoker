using PlanningPoker.Common.Constants;
using PlanningPoker.Repository.Models;
using PlanningPoker.Service.Mappers;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Service.Services;

public class RoomRound(IRevealedVotesEntityMapper revealedMapper, IRoundResultEntityMapper roundResultMapper, TimeProvider time) : IRoomRound
{
    public RoundDecision Vote(RoomEntity room, Guid personGuid, string mark)
    {
        room.Votes[personGuid] = mark;
        InvalidateCountdown(room);
        return Decide(room);
    }

    public bool Reveal(RoomEntity room)
    {
        if (!room.VotingEnabled)
        {
            return false;
        }

        // The votes are kept for the statistics, the round timer restarts, and the round is added to the room's history
        // (numbered, with the average of its votes). The oldest entries are dropped beyond the limit.
        var now = time.GetUtcNow();
        var frozen = revealedMapper.Map(room);
        room.Revealed = frozen;
        room.VotingEnabled = false;
        room.RoundStartedAt = now;
        InvalidateCountdown(room);
        room.RoundsRevealed++;
        room.RoundHistory.Add(roundResultMapper.Map(room.RoundsRevealed, frozen, now));
        if (room.RoundHistory.Count > RoomConstraints.RoundHistoryLimit)
        {
            room.RoundHistory.RemoveRange(0, room.RoundHistory.Count - RoomConstraints.RoundHistoryLimit);
        }

        return true;
    }

    public void Clear(RoomEntity room, bool restartTimer)
    {
        room.Votes.Clear();
        room.DepartedVotes.Clear();
        room.Revealed = null;
        room.VotingEnabled = true;
        if (restartTimer)
        {
            room.RoundStartedAt = time.GetUtcNow();
        }

        InvalidateCountdown(room);
    }

    public RoundTick Tick(RoomEntity room, long revision)
    {
        if (!room.VotingEnabled || room.CountdownRevision != revision || room.Revision != revision)
        {
            return new RoundTick(TickOutcome.Stale, 0);
        }

        if (room.CountdownRemaining > 1)
        {
            room.CountdownRemaining--;
            return new RoundTick(TickOutcome.Ticked, room.CountdownRemaining);
        }

        room.CountdownRemaining = 0;
        Reveal(room);
        return new RoundTick(TickOutcome.Revealed, 0);
    }

    public void InvalidateCountdown(RoomEntity room)
    {
        room.Revision++;
    }

    public RoundDecision Decide(RoomEntity room)
    {
        // A countdown already running at this revision made the same decision first.
        if (!room.VotingEnabled || !room.EveryoneVoted || room.CountdownRunning)
        {
            return RoundDecision.None;
        }

        var seconds = room.Configuration.CountdownSeconds;
        if (seconds == 0)
        {
            Reveal(room);
            return new RoundDecision(null, true);
        }

        room.CountdownRevision = room.Revision;
        room.CountdownRemaining = seconds;
        return new RoundDecision(new CountdownStart { Revision = room.Revision, Seconds = seconds }, false);
    }

    public RoleChange ChangeRole(RoomEntity room, Guid personGuid, string role)
    {
        if (room.RoleOf(personGuid) == role)
        {
            return new RoleChange(false, RoundDecision.None);
        }

        room.Roles[personGuid] = role;
        if (!room.VotingEnabled)
        {
            return new RoleChange(true, RoundDecision.None);
        }

        InvalidateCountdown(room);
        return new RoleChange(true, Decide(room));
    }
}
