using Microsoft.Extensions.Time.Testing;
using PlanningPoker.Common.Constants;
using PlanningPoker.Repository.Models;
using PlanningPoker.Service.Models;
using PlanningPoker.UnitTests.Builders;

namespace PlanningPoker.UnitTests.Service;

public class RoomRoundTests
{
    private static readonly DateTimeOffset Start = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);

    private readonly FakeTimeProvider _time = new(Start);
    private readonly Guid _ann = Guid.NewGuid();
    private readonly Guid _bob = Guid.NewGuid();
    private readonly Guid _obi = Guid.NewGuid();

    /// <summary>A room with Ann and Bob as voters and Obi as an observer, voting open.</summary>
    private RoomEntity Room(int countdown = 5)
    {
        return new RoomEntity
        {
            Id = "blue-fox",
            OwnerGuid = _ann,
            MemberGuids = [_ann, _bob, _obi],
            Roles = new Dictionary<Guid, string> { [_ann] = PersonTypes.Developer, [_bob] = PersonTypes.Tester, [_obi] = PersonTypes.Observer },
            VotingEnabled = true,
            Configuration = new RoomConfigurationEntity { CountdownSeconds = countdown },
            RoundStartedAt = Start
        };
    }

    // Vote

    [Fact]
    public void Vote_StoresTheMarkAndCancelsARunningCountdown()
    {
        var round = Rounds.Create(_time);
        var room = Room();
        round.Vote(room, _ann, "3");
        var started = round.Vote(room, _bob, "5").Countdown!;

        round.Vote(room, _ann, "8");

        Assert.Equal("8", room.Votes[_ann]);
        Assert.Equal(TickOutcome.Stale, round.Tick(room, started.Revision).Outcome);
    }

    [Fact]
    public void Vote_WhenNotEveryoneVoted_DecidesNothing()
    {
        var decision = Rounds.Create(_time).Vote(Room(), _ann, "3");

        Assert.True(decision.IsNone);
    }

    [Fact]
    public void Vote_TheLastVoter_StartsTheCountdownAtTheNewRevision()
    {
        var round = Rounds.Create(_time);
        var room = Room(countdown: 7);
        round.Vote(room, _ann, "3");

        var decision = round.Vote(room, _bob, "5");

        Assert.Equal(7, decision.Countdown!.Seconds);
        Assert.Equal(room.Revision, decision.Countdown.Revision);
        Assert.Equal(7, room.CountdownRemaining);
        Assert.True(room.CountdownRunning);
        Assert.False(decision.Revealed);
    }

    [Fact]
    public void Vote_TheLastVoterWithCountdownZero_RevealsAtOnce()
    {
        var round = Rounds.Create(_time);
        var room = Room(countdown: 0);
        round.Vote(room, _ann, "3");

        var decision = round.Vote(room, _bob, "5");

        Assert.True(decision.Revealed);
        Assert.Null(decision.Countdown);
        Assert.False(room.VotingEnabled);
        Assert.Equal(2, room.Revealed!.Votes.Count);
    }

    // Reveal

    [Fact]
    public void Reveal_FreezesTheVotesWithTheRolesOfTheRoom_AndLeavesObserversOut()
    {
        var round = Rounds.Create(_time);
        var room = Room();
        room.Votes[_ann] = "3";
        room.Votes[_obi] = "13";

        var revealed = round.Reveal(room);

        Assert.True(revealed);
        Assert.False(room.VotingEnabled);
        Assert.Equal([_ann], room.Revealed!.Votes.Keys);
        Assert.Equal(PersonTypes.Developer, room.Revealed.Votes[_ann].PersonType);
    }

    [Fact]
    public void Reveal_RestartsTheRoundTimer_BumpsTheRevisionAndRecordsTheRound()
    {
        var round = Rounds.Create(_time);
        var room = Room();
        room.Votes[_ann] = "3";
        _time.Advance(TimeSpan.FromMinutes(2));

        round.Reveal(room);

        Assert.Equal(Start.AddMinutes(2), room.RoundStartedAt);
        Assert.Equal(1, room.Revision);
        Assert.Equal(1, room.RoundsRevealed);
        Assert.Equal(1, Assert.Single(room.RoundHistory).Round);
    }

    [Fact]
    public void Reveal_AlreadyRevealed_ChangesNothing()
    {
        var round = Rounds.Create(_time);
        var room = Room();
        round.Reveal(room);
        var revision = room.Revision;

        var again = round.Reveal(room);

        Assert.False(again);
        Assert.Equal(revision, room.Revision);
        Assert.Equal(1, room.RoundsRevealed);
    }

    [Fact]
    public void Reveal_KeepsOnlyTheLatestRoundsInTheHistory()
    {
        var round = Rounds.Create(_time);
        var room = Room();
        for (var i = 0; i < RoomConstraints.RoundHistoryLimit + 3; i++)
        {
            round.Reveal(room);
            round.Clear(room, restartTimer: false);
        }

        Assert.Equal(RoomConstraints.RoundHistoryLimit, room.RoundHistory.Count);
        Assert.Equal(4, room.RoundHistory[0].Round);
    }

    // Clear

    [Fact]
    public void Clear_DropsTheVotesAndReopensVoting()
    {
        var round = Rounds.Create(_time);
        var room = Room();
        room.Votes[_ann] = "3";
        room.DepartedVotes[Guid.NewGuid()] = "5";
        round.Reveal(room);

        round.Clear(room, restartTimer: false);

        Assert.Empty(room.Votes);
        Assert.Empty(room.DepartedVotes);
        Assert.Null(room.Revealed);
        Assert.True(room.VotingEnabled);
    }

    [Fact]
    public void Clear_RestartsTheRoundTimerOnlyWhenAsked()
    {
        var round = Rounds.Create(_time);
        var room = Room();
        _time.Advance(TimeSpan.FromMinutes(1));

        round.Clear(room, restartTimer: false);
        Assert.Equal(Start, room.RoundStartedAt);

        round.Clear(room, restartTimer: true);
        Assert.Equal(Start.AddMinutes(1), room.RoundStartedAt);
    }

    [Fact]
    public void Clear_CancelsARunningCountdown()
    {
        var round = Rounds.Create(_time);
        var room = Room();
        round.Vote(room, _ann, "3");
        var started = round.Vote(room, _bob, "5").Countdown!;

        round.Clear(room, restartTimer: true);

        Assert.Equal(TickOutcome.Stale, round.Tick(room, started.Revision).Outcome);
    }

    // Tick

    [Fact]
    public void Tick_CountsDownAndRevealsOnTheLastSecond()
    {
        var round = Rounds.Create(_time);
        var room = Room(countdown: 2);
        round.Vote(room, _ann, "3");
        var started = round.Vote(room, _bob, "5").Countdown!;

        var first = round.Tick(room, started.Revision);
        var second = round.Tick(room, started.Revision);

        Assert.Equal(new RoundTick(TickOutcome.Ticked, 1), first);
        Assert.Equal(new RoundTick(TickOutcome.Revealed, 0), second);
        Assert.False(room.VotingEnabled);
        Assert.Equal(0, room.CountdownRemaining);
    }

    [Fact]
    public void Tick_ARevisionThatIsNotTheRunningOne_IsStale()
    {
        var round = Rounds.Create(_time);
        var room = Room();
        round.Vote(room, _ann, "3");
        var started = round.Vote(room, _bob, "5").Countdown!;

        var tick = round.Tick(room, started.Revision - 1);

        Assert.Equal(TickOutcome.Stale, tick.Outcome);
        Assert.Equal(5, room.CountdownRemaining);
    }

    [Fact]
    public void Tick_AfterTheReveal_IsStale()
    {
        var round = Rounds.Create(_time);
        var room = Room();
        round.Vote(room, _ann, "3");
        var started = round.Vote(room, _bob, "5").Countdown!;
        round.Reveal(room);

        Assert.Equal(TickOutcome.Stale, round.Tick(room, started.Revision).Outcome);
    }

    // InvalidateCountdown

    [Fact]
    public void InvalidateCountdown_BumpsTheRevisionSoTheRunningCountdownIsStale()
    {
        var round = Rounds.Create(_time);
        var room = Room();
        round.Vote(room, _ann, "3");
        var started = round.Vote(room, _bob, "5").Countdown!;

        round.InvalidateCountdown(room);

        Assert.Equal(started.Revision + 1, room.Revision);
        Assert.False(room.CountdownRunning);
        Assert.Equal(TickOutcome.Stale, round.Tick(room, started.Revision).Outcome);
    }

    // Decide

    [Fact]
    public void Decide_EveryoneVotedAndNoCountdown_StartsOne()
    {
        var round = Rounds.Create(_time);
        var room = Room();
        room.Votes[_ann] = "3";
        room.Votes[_bob] = "5";

        var decision = round.Decide(room);

        Assert.Equal(5, decision.Countdown!.Seconds);
        Assert.True(room.CountdownRunning);
    }

    [Fact]
    public void Decide_ACountdownAlreadyRunsAtThisRevision_DecidesNothing()
    {
        var round = Rounds.Create(_time);
        var room = Room();
        room.Votes[_ann] = "3";
        room.Votes[_bob] = "5";
        round.Decide(room);

        Assert.True(round.Decide(room).IsNone);
    }

    [Fact]
    public void Decide_AfterTheReveal_DecidesNothing()
    {
        var round = Rounds.Create(_time);
        var room = Room();
        room.Votes[_ann] = "3";
        room.Votes[_bob] = "5";
        round.Reveal(room);

        Assert.True(round.Decide(room).IsNone);
    }

    [Fact]
    public void Decide_OnlyOnlineVotersAreWaitedFor()
    {
        var round = Rounds.Create(_time);
        var room = Room();
        room.Votes[_ann] = "3";
        room.OffgridGuids.Add(_bob);

        Assert.NotNull(round.Decide(room).Countdown);
    }

    // ChangeRole

    [Fact]
    public void ChangeRole_SameRole_ChangesNothing()
    {
        var round = Rounds.Create(_time);
        var room = Room();

        var change = round.ChangeRole(room, _ann, PersonTypes.Developer);

        Assert.False(change.Changed);
        Assert.True(change.Decision.IsNone);
        Assert.Equal(0, room.Revision);
    }

    [Fact]
    public void ChangeRole_WhileVotingIsOpen_SetsTheRoleAndCancelsARunningCountdown()
    {
        var round = Rounds.Create(_time);
        var room = Room();
        round.Vote(room, _ann, "3");
        var started = round.Vote(room, _bob, "5").Countdown!;

        var change = round.ChangeRole(room, _ann, PersonTypes.Sre);

        Assert.True(change.Changed);
        Assert.Equal(PersonTypes.Sre, room.RoleOf(_ann));
        Assert.True(room.Revision > started.Revision);
        Assert.Equal(TickOutcome.Stale, round.Tick(room, started.Revision).Outcome);
    }

    [Fact]
    public void ChangeRole_ThatLeavesEveryoneVoted_StartsTheCountdown()
    {
        var round = Rounds.Create(_time);
        var room = Room();
        round.Vote(room, _ann, "3");

        var change = round.ChangeRole(room, _bob, PersonTypes.Observer);

        Assert.Equal(5, change.Decision.Countdown!.Seconds);
    }

    [Fact]
    public void ChangeRole_AfterTheReveal_SetsTheRoleAndLeavesTheRoundAlone()
    {
        var round = Rounds.Create(_time);
        var room = Room();
        round.Reveal(room);
        var revision = room.Revision;

        var change = round.ChangeRole(room, _ann, PersonTypes.Observer);

        Assert.True(change.Changed);
        Assert.True(change.Decision.IsNone);
        Assert.Equal(revision, room.Revision);
        Assert.Equal(PersonTypes.Observer, room.RoleOf(_ann));
    }

    [Fact]
    public void ChangeRole_ToObserverMidRound_KeepsTheVoteButItIsNoLongerCounted()
    {
        var round = Rounds.Create(_time);
        var room = Room(countdown: 0);
        room.Votes[_ann] = "3";

        round.ChangeRole(room, _ann, PersonTypes.Observer);
        room.Votes[_bob] = "5";
        round.Reveal(room);

        Assert.Equal("3", room.Votes[_ann]);
        Assert.Equal([_bob], room.Revealed!.Votes.Keys);
    }
}
