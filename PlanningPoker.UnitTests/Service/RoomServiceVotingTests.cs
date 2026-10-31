using PlanningPoker.Common.Constants;
using PlanningPoker.Repository.Mappers;
using PlanningPoker.Repository.Models;
using PlanningPoker.Repository.Repositories;
using PlanningPoker.Service.Models;
using PlanningPoker.Service.Services;
using PlanningPoker.UnitTests.Fakes;

namespace PlanningPoker.UnitTests.Service;

public class RoomServiceVotingTests
{
    private static readonly string[] TwoDevs = [PersonTypes.Developer, PersonTypes.Developer];
    private static readonly string[] ThreeDevs = [PersonTypes.Developer, PersonTypes.Developer, PersonTypes.Developer];

    private readonly PersonRepository _people = new();
    private readonly RoomRepository _rooms = new(new RoomEntityCopyMapper(new RoomConfigurationEntityCopyMapper(), new RevealedVotesEntityCopyMapper()));
    private readonly RoomService _service;

    public RoomServiceVotingTests()
    {
        _service = RoomServiceFactory.Create(_rooms, _people);
    }

    private static RoomSettings Settings(int countdown)
    {
        return new RoomSettings
        {
            UseFunRoomName = false,
            Configuration = new RoomConfiguration { CardSet = "linear", CountdownSeconds = countdown, ShowFireworks = false, MaxParticipants = 9 }
        };
    }

    private Guid AddPerson(string type = PersonTypes.Developer)
    {
        var entity = new PersonEntity { Guid = Guid.NewGuid(), Name = "p", PersonType = type };
        _people.TryAdd(entity);
        return entity.Guid;
    }

    /// <summary>A room with one connected member per type; their connections are "c1", "c2" and so on.</summary>
    private (string RoomId, Guid[] People) RoomWith(int countdown, params string[] types)
    {
        var people = types.Select(AddPerson).ToArray();
        var roomId = _service.Create(people[0], Settings(countdown)).Data!.Id;
        for (var i = 0; i < people.Length; i++)
        {
            Assert.True(_service.Join(people[i], $"c{i + 1}", roomId).IsSuccess);
        }

        return (roomId, people);
    }

    [Fact]
    public void Vote_StoresTheMarkAndReportsItMasked()
    {
        var (roomId, people) = RoomWith(5, TwoDevs);

        var response = _service.Vote(people[0], "c1", "5");

        Assert.True(response.IsSuccess);
        Assert.Equal(roomId, response.Data!.RoomId);
        Assert.Equal(people[0], response.Data.PersonGuid);
        Assert.Equal("5", _rooms.Get(roomId)!.Votes[people[0]]);
        Assert.Equal("hide", response.Data.VoteStatus.Votes[people[0]]);
        Assert.False(response.Data.VoteStatus.HasEveryoneVoted);
        Assert.Null(response.Data.FollowUp.Countdown);
    }

    [Fact]
    public void Vote_AgainReplacesTheMark()
    {
        var (roomId, people) = RoomWith(5, TwoDevs);
        _service.Vote(people[0], "c1", "5");

        _service.Vote(people[0], "c1", "8");

        Assert.Equal("8", _rooms.Get(roomId)!.Votes[people[0]]);
    }

    [Fact]
    public void Vote_BumpsTheRevision()
    {
        var (roomId, people) = RoomWith(5, TwoDevs);
        var before = _rooms.Get(roomId)!.Revision;

        _service.Vote(people[0], "c1", "5");

        Assert.True(_rooms.Get(roomId)!.Revision > before);
    }

    [Fact]
    public void Vote_LastVoter_StartsTheCountdownAtTheRevisionOfTheRoom()
    {
        var (roomId, people) = RoomWith(5, TwoDevs);
        _service.Vote(people[0], "c1", "5");

        var response = _service.Vote(people[1], "c2", "8");

        var start = response.Data!.FollowUp.Countdown!;
        var room = _rooms.Get(roomId)!;
        Assert.True(response.Data.VoteStatus.HasEveryoneVoted);
        Assert.Equal(5, start.Seconds);
        Assert.Equal(room.Revision, start.Revision);
        Assert.True(room.CountdownRunning);
        Assert.Equal(5, room.CountdownRemaining);
        Assert.Equal(5, response.Data.VoteStatus.Countdown);
    }

    [Fact]
    public void Vote_ChangedDuringTheCountdown_RestartsItAtTheFullLength()
    {
        var (roomId, people) = RoomWith(5, TwoDevs);
        _service.Vote(people[0], "c1", "5");
        var first = _service.Vote(people[1], "c2", "8").Data!.FollowUp.Countdown!;
        _service.Tick(roomId, first.Revision);

        var second = _service.Vote(people[1], "c2", "3").Data!.FollowUp.Countdown!;

        Assert.True(second.Revision > first.Revision);
        Assert.Equal(5, second.Seconds);
        Assert.Equal(5, _rooms.Get(roomId)!.CountdownRemaining);
        Assert.Equal(TickOutcome.Stale, _service.Tick(roomId, first.Revision).Outcome);
    }

    [Fact]
    public void Vote_ObserversAreNotCounted()
    {
        var (_, people) = RoomWith(5, PersonTypes.Developer, PersonTypes.Observer);

        var response = _service.Vote(people[0], "c1", "5");

        Assert.True(response.Data!.VoteStatus.HasEveryoneVoted);
        Assert.NotNull(response.Data.FollowUp.Countdown);
    }

    [Fact]
    public void Vote_WithCountdownZero_RevealsAtOnce()
    {
        var (roomId, people) = RoomWith(0, TwoDevs);
        _service.Vote(people[0], "c1", "5");

        var response = _service.Vote(people[1], "c2", "8");

        var revealed = response.Data!.FollowUp.Revealed!;
        Assert.Null(response.Data.FollowUp.Countdown);
        Assert.True(revealed.VotingFinished);
        Assert.Equal("5", revealed.Votes[people[0]]);
        Assert.Equal("8", revealed.Votes[people[1]]);
        Assert.False(_rooms.Get(roomId)!.VotingEnabled);
    }

    [Fact]
    public void Vote_ByAnObserver_IsRefused()
    {
        var (roomId, people) = RoomWith(5, PersonTypes.Developer, PersonTypes.Observer);

        var response = _service.Vote(people[1], "c2", "5");

        Assert.Equal(ErrorCodes.ObserverCannotVote, Assert.Single(response.Errors).Code);
        Assert.Empty(_rooms.Get(roomId)!.Votes);
    }

    [Fact]
    public void Vote_AfterTheReveal_IsRefused()
    {
        var (_, people) = RoomWith(5, TwoDevs);
        _service.ForceShowVotes(people[0], "c1");

        var response = _service.Vote(people[1], "c2", "5");

        Assert.Equal(ErrorCodes.VotingClosed, Assert.Single(response.Errors).Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("99")]
    [InlineData("hide")]
    public void Vote_InvalidMark_FailsValidation(string mark)
    {
        var (roomId, people) = RoomWith(5, TwoDevs);

        var response = _service.Vote(people[0], "c1", mark);

        var error = Assert.Single(response.Errors);
        Assert.Equal(ErrorCodes.ValidationFailed, error.Code);
        Assert.Equal("mark", error.Metadata["field"]);
        Assert.Empty(_rooms.Get(roomId)!.Votes);
    }

    [Fact]
    public void Vote_UnknownConnection_IsNotInRoom()
    {
        var (_, people) = RoomWith(5, TwoDevs);

        var response = _service.Vote(people[0], "other", "5");

        Assert.Equal(ErrorCodes.NotInRoom, Assert.Single(response.Errors).Code);
    }

    [Fact]
    public void Vote_UnknownPerson_IsNoSession()
    {
        var response = _service.Vote(Guid.NewGuid(), "c1", "5");

        Assert.Equal(ErrorCodes.NoSession, Assert.Single(response.Errors).Code);
    }

    [Fact]
    public void Vote_ConnectionNoLongerAMemberOfTheRoom_IsNotInRoom()
    {
        var (roomId, people) = RoomWith(5, TwoDevs);
        // The person index still points at the room, but the room has already dropped the connection.
        _rooms.Update(roomId, room => room.MemberConnections[people[0]].Remove("c1"), out _);

        var response = _service.Vote(people[0], "c1", "5");

        Assert.Equal(ErrorCodes.NotInRoom, Assert.Single(response.Errors).Code);
        Assert.Empty(_rooms.Get(roomId)!.Votes);
    }

    [Fact]
    public void Vote_AVoterJoinsBetweenTheSnapshotAndTheCountdownDecision_StartsNoCountdown()
    {
        var (roomId, people) = RoomWith(5, TwoDevs);
        _service.Vote(people[0], "c1", "5");
        var interleaving = new InterleavingRoomRepository(_rooms);
        var service = RoomServiceFactory.Create(interleaving, _people);
        var late = AddPerson();
        // The snapshot says everyone voted; right after it is taken, a new voter joins and the answer is out of date.
        interleaving.AfterNextGet = () => Assert.True(_service.Join(late, "c3", roomId).IsSuccess);

        var response = service.Vote(people[1], "c2", "8");

        Assert.True(response.IsSuccess);
        Assert.True(response.Data!.VoteStatus.HasEveryoneVoted);
        Assert.Null(response.Data.FollowUp.Countdown);
        Assert.False(_rooms.Get(roomId)!.CountdownRunning);
    }

    [Fact]
    public void Vote_TwoLastVotesAtOnce_StartExactlyOneCountdown()
    {
        var (roomId, people) = RoomWith(5, TwoDevs);
        var interleaving = new InterleavingRoomRepository(_rooms);
        var service = RoomServiceFactory.Create(interleaving, _people);
        VoteResult? other = null;
        // The second voter stores their vote, snapshots and decides while the first is between storing and snapshotting.
        interleaving.AfterNextUpdate = () => other = _service.Vote(people[1], "c2", "8").Data;

        var first = service.Vote(people[0], "c1", "5").Data!;

        Assert.Equal(1, new[] { first.FollowUp.Countdown, other!.FollowUp.Countdown }.Count(start => start is not null));
        Assert.True(_rooms.Get(roomId)!.CountdownRunning);
    }

    [Fact]
    public void ForceShowVotes_TwiceInARow_AnnouncesTheRevealOnce()
    {
        var (_, people) = RoomWith(5, TwoDevs);

        var first = _service.ForceShowVotes(people[0], "c1").Data!;
        var second = _service.ForceShowVotes(people[1], "c2").Data!;

        Assert.NotNull(first.VoteStatus);
        Assert.Null(second.VoteStatus);
    }

    [Fact]
    public void ClearVotes_DropsTheVotesReopensVotingAndCancelsTheCountdown()
    {
        var (roomId, people) = RoomWith(5, TwoDevs);
        _service.Vote(people[0], "c1", "5");
        var start = _service.Vote(people[1], "c2", "8").Data!.FollowUp.Countdown!;

        var response = _service.ClearVotes(people[0], "c1");

        Assert.Equal(roomId, response.Data);
        var room = _rooms.Get(roomId)!;
        Assert.Empty(room.Votes);
        Assert.True(room.VotingEnabled);
        Assert.False(room.CountdownRunning);
        Assert.Equal(TickOutcome.Stale, _service.Tick(roomId, start.Revision).Outcome);
    }

    [Fact]
    public void ClearVotes_AfterTheReveal_ReopensVoting()
    {
        var (roomId, people) = RoomWith(5, TwoDevs);
        _service.ForceShowVotes(people[0], "c1");

        _service.ClearVotes(people[1], "c2");

        Assert.True(_rooms.Get(roomId)!.VotingEnabled);
    }

    [Fact]
    public void ClearVotes_UnknownConnection_IsNotInRoom()
    {
        var (_, people) = RoomWith(5, TwoDevs);

        Assert.Equal(ErrorCodes.NotInRoom, Assert.Single(_service.ClearVotes(people[0], "other").Errors).Code);
    }

    [Fact]
    public void ForceShowVotes_RevealsTheRealMarksOfTheVotersOnly()
    {
        var (roomId, people) = RoomWith(5, PersonTypes.Developer, PersonTypes.Tester, PersonTypes.Observer);
        _service.Vote(people[0], "c1", "5");

        var response = _service.ForceShowVotes(people[2], "c3");

        var status = response.Data!.VoteStatus!;
        Assert.Equal(roomId, response.Data.RoomId);
        Assert.True(status.VotingFinished);
        Assert.Equal(["5"], status.Votes.Values);
        Assert.Equal(people[0], status.Votes.Keys.Single());
        Assert.False(_rooms.Get(roomId)!.VotingEnabled);
    }

    [Fact]
    public void ForceShowVotes_WithNoVotes_RevealsAnEmptyResult()
    {
        var (_, people) = RoomWith(5, TwoDevs);

        var status = _service.ForceShowVotes(people[0], "c1").Data!.VoteStatus!;

        Assert.True(status.VotingFinished);
        Assert.Empty(status.Votes);
    }

    [Fact]
    public void ForceShowVotes_Twice_IsANoOpTheSecondTime()
    {
        var (roomId, people) = RoomWith(5, TwoDevs);
        _service.ForceShowVotes(people[0], "c1");
        var revision = _rooms.Get(roomId)!.Revision;

        var response = _service.ForceShowVotes(people[1], "c2");

        Assert.True(response.IsSuccess);
        Assert.Null(response.Data!.VoteStatus);
        Assert.Equal(revision, _rooms.Get(roomId)!.Revision);
    }

    [Fact]
    public void ForceShowVotes_CancelsTheCountdown()
    {
        var (roomId, people) = RoomWith(5, TwoDevs);
        _service.Vote(people[0], "c1", "5");
        var start = _service.Vote(people[1], "c2", "8").Data!.FollowUp.Countdown!;

        _service.ForceShowVotes(people[0], "c1");

        Assert.Equal(TickOutcome.Stale, _service.Tick(roomId, start.Revision).Outcome);
    }

    [Fact]
    public void ForceShowVotes_UnknownConnection_IsNotInRoom()
    {
        var (_, people) = RoomWith(5, TwoDevs);

        Assert.Equal(ErrorCodes.NotInRoom, Assert.Single(_service.ForceShowVotes(people[0], "other").Errors).Code);
    }

    [Fact]
    public void Tick_CountsDownAndRevealsAtZero()
    {
        var (roomId, people) = RoomWith(2, TwoDevs);
        _service.Vote(people[0], "c1", "5");
        var start = _service.Vote(people[1], "c2", "8").Data!.FollowUp.Countdown!;

        var first = _service.Tick(roomId, start.Revision);
        var second = _service.Tick(roomId, start.Revision);

        Assert.Equal(TickOutcome.Ticked, first.Outcome);
        Assert.Equal(1, first.Remaining);
        Assert.Equal(TickOutcome.Revealed, second.Outcome);
        Assert.True(second.Revealed!.VotingFinished);
        Assert.Equal("8", second.Revealed.Votes[people[1]]);
        Assert.False(_rooms.Get(roomId)!.VotingEnabled);
        Assert.Equal(TickOutcome.Stale, _service.Tick(roomId, start.Revision).Outcome);
    }

    [Fact]
    public void Tick_UnknownRoom_IsStale()
    {
        Assert.Equal(TickOutcome.Stale, _service.Tick("missing", 1).Outcome);
    }

    [Fact]
    public void Tick_AfterAVoterJoined_IsStale()
    {
        var (roomId, people) = RoomWith(5, TwoDevs);
        _service.Vote(people[0], "c1", "5");
        var start = _service.Vote(people[1], "c2", "8").Data!.FollowUp.Countdown!;

        _service.Join(AddPerson(), "c3", roomId);

        Assert.Equal(TickOutcome.Stale, _service.Tick(roomId, start.Revision).Outcome);
    }

    [Fact]
    public void Tick_AfterAnObserverOrASecondTabJoined_KeepsCounting()
    {
        var (roomId, people) = RoomWith(5, TwoDevs);
        _service.Vote(people[0], "c1", "5");
        var start = _service.Vote(people[1], "c2", "8").Data!.FollowUp.Countdown!;

        _service.Join(AddPerson(PersonTypes.Observer), "c3", roomId);
        _service.Join(people[0], "c4", roomId);

        Assert.Equal(TickOutcome.Ticked, _service.Tick(roomId, start.Revision).Outcome);
    }

    [Fact]
    public void Tick_AfterAPersonLeft_IsStale()
    {
        var (roomId, people) = RoomWith(5, ThreeDevs);
        _service.Vote(people[0], "c1", "5");
        _service.Vote(people[1], "c2", "8");
        var start = _service.Vote(people[2], "c3", "3").Data!.FollowUp.Countdown!;

        _service.Exit(people[2], "c3");

        Assert.Equal(TickOutcome.Stale, _service.Tick(roomId, start.Revision).Outcome);
    }

    [Fact]
    public void Join_OfAVoterWhileVotingIsOpen_BumpsTheRevision_ButOtherJoinsDoNot()
    {
        var (roomId, people) = RoomWith(5, TwoDevs);
        var revision = _rooms.Get(roomId)!.Revision;

        _service.Join(people[0], "c9", roomId);
        _service.Join(AddPerson(PersonTypes.Observer), "c10", roomId);
        Assert.Equal(revision, _rooms.Get(roomId)!.Revision);

        _service.Join(AddPerson(PersonTypes.Tester), "c11", roomId);
        Assert.True(_rooms.Get(roomId)!.Revision > revision);
    }

    [Fact]
    public void Join_AfterTheReveal_SeesTheRealMarks_AndKeepsVotingClosed()
    {
        var (roomId, people) = RoomWith(5, TwoDevs);
        _service.Vote(people[0], "c1", "5");
        _service.ForceShowVotes(people[0], "c1");
        var revision = _rooms.Get(roomId)!.Revision;

        var joined = _service.Join(AddPerson(), "c3", roomId).Data!;

        Assert.True(joined.VoteStatus.VotingFinished);
        Assert.Equal("5", joined.VoteStatus.Votes[people[0]]);
        Assert.False(joined.ResetCountdown);
        Assert.Equal(revision, _rooms.Get(roomId)!.Revision);
    }

    [Fact]
    public void Join_DuringTheCountdown_ReportsItsRemainingSeconds()
    {
        var (roomId, people) = RoomWith(5, TwoDevs);
        _service.Vote(people[0], "c1", "5");
        _service.Vote(people[1], "c2", "8");

        var secondTab = _service.Join(people[0], "c9", roomId).Data!;

        Assert.Equal(5, secondTab.VoteStatus.Countdown);
    }

    [Fact]
    public void Exit_OfTheOnlyMemberWhoHadNotVoted_StartsTheCountdown()
    {
        var (roomId, people) = RoomWith(5, ThreeDevs);
        _service.Vote(people[0], "c1", "5");
        _service.Vote(people[1], "c2", "8");

        var exited = _service.Exit(people[2], "c3").Data!;

        var start = exited.FollowUp.Countdown!;
        Assert.Equal(5, start.Seconds);
        Assert.Equal(_rooms.Get(roomId)!.Revision, start.Revision);
        Assert.True(_rooms.Get(roomId)!.CountdownRunning);
    }

    [Fact]
    public void Exit_OfAVoter_RemovesTheVoteAndRestartsTheCountdown()
    {
        var (roomId, people) = RoomWith(5, ThreeDevs);
        _service.Vote(people[0], "c1", "5");
        _service.Vote(people[1], "c2", "8");
        var running = _service.Vote(people[2], "c3", "3").Data!.FollowUp.Countdown!;

        var exited = _service.Exit(people[2], "c3").Data!;

        Assert.DoesNotContain(people[2], _rooms.Get(roomId)!.Votes.Keys);
        Assert.True(exited.FollowUp.Countdown!.Revision > running.Revision);
    }

    [Fact]
    public void Exit_WithCountdownZero_RevealsAtOnce()
    {
        var (roomId, people) = RoomWith(0, ThreeDevs);
        _service.Vote(people[0], "c1", "5");
        _service.Vote(people[1], "c2", "8");

        var exited = _service.Exit(people[2], "c3").Data!;

        Assert.True(exited.FollowUp.Revealed!.VotingFinished);
        Assert.False(_rooms.Get(roomId)!.VotingEnabled);
    }

    [Fact]
    public void Exit_AfterTheReveal_StartsNothing()
    {
        var (_, people) = RoomWith(5, ThreeDevs);
        _service.Vote(people[0], "c1", "5");
        _service.ForceShowVotes(people[0], "c1");

        var exited = _service.Exit(people[2], "c3").Data!;

        Assert.Null(exited.FollowUp.Countdown);
        Assert.Null(exited.FollowUp.Revealed);
    }

    [Fact]
    public void Exit_WhenNotEveryoneRemainingVoted_StartsNothing()
    {
        var (_, people) = RoomWith(5, ThreeDevs);
        _service.Vote(people[0], "c1", "5");

        var exited = _service.Exit(people[2], "c3").Data!;

        Assert.Null(exited.FollowUp.Countdown);
    }

    [Fact]
    public void Exit_OfASecondTab_ChangesNothing()
    {
        var (roomId, people) = RoomWith(5, TwoDevs);
        _service.Join(people[0], "c9", roomId);
        _service.Vote(people[0], "c1", "5");
        var revision = _rooms.Get(roomId)!.Revision;

        var exited = _service.Exit(people[0], "c9").Data!;

        Assert.False(exited.PersonLeft);
        Assert.Equal(revision, _rooms.Get(roomId)!.Revision);
        Assert.Null(exited.FollowUp.Countdown);
    }

    [Fact]
    public void Exit_OfTheLastMember_StartsNothing()
    {
        var (_, people) = RoomWith(5, PersonTypes.Developer);

        var exited = _service.Exit(people[0], "c1").Data!;

        Assert.True(exited.RoomDisposed);
        Assert.Null(exited.FollowUp.Countdown);
    }
}
