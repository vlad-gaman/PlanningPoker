using PlanningPoker.Common.Constants;
using PlanningPoker.Repository.Mappers;
using PlanningPoker.Repository.Models;
using PlanningPoker.Repository.Repositories;
using PlanningPoker.Service.Models;
using PlanningPoker.Service.Services;
using PlanningPoker.UnitTests.Fakes;

namespace PlanningPoker.UnitTests.Service;

/// <summary>The votes frozen at the reveal, and the statistics derived from them.</summary>
public class RoomServiceResultsTests
{
    private readonly PersonRepository _people = new();
    private readonly RoomRepository _rooms = new(new RoomEntityCopyMapper(new RoomConfigurationEntityCopyMapper(), new RevealedVotesEntityCopyMapper()));
    private readonly RoomService _service;

    public RoomServiceResultsTests()
    {
        _service = RoomServiceFactory.Create(_rooms, _people);
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
        var people = types.Select(type => AddPerson(type)).ToArray();
        var settings = new RoomSettings
        {
            UseFunRoomName = false,
            Configuration = new RoomConfiguration { CardSet = "linear", CountdownSeconds = countdown, ShowFireworks = true, MaxParticipants = 9 }
        };
        var roomId = _service.Create(people[0], settings).Data!.Id;
        for (var i = 0; i < people.Length; i++)
        {
            Assert.True(_service.Join(people[i], $"c{i + 1}", roomId).IsSuccess);
        }

        return (roomId, people);
    }

    [Fact]
    public void ForceShowVotes_FreezesTheVotesWithTheirPersonTypes_AndLeavesObserversOut()
    {
        var (roomId, people) = RoomWith(5, PersonTypes.Developer, PersonTypes.Tester, PersonTypes.Observer);
        _service.Vote(people[0], "c1", "3");
        _service.Vote(people[1], "c2", "8");

        var status = _service.ForceShowVotes(people[2], "c3").Data!.VoteStatus!;

        var revealed = _rooms.Get(roomId)!.Revealed!;
        Assert.Equal(PersonTypes.Developer, revealed.Votes[people[0]].PersonType);
        Assert.Equal("8", revealed.Votes[people[1]].Mark);
        Assert.True(revealed.EveryoneVoted);
        Assert.Equal(2, revealed.Votes.Count);
        Assert.Equal(["3", "8"], status.Statistics!.All.Marks.Select(share => share.Mark));
        Assert.Equal(["3"], status.Statistics.Developers.Marks.Select(share => share.Mark));
        Assert.Equal(["8"], status.Statistics.Testers.Marks.Select(share => share.Mark));
        Assert.Equal(5.5m, status.Statistics.All.AverageMark);
    }

    [Fact]
    public void ForceShowVotes_BeforeEveryoneVoted_RecordsThatNotEveryoneVoted()
    {
        var (roomId, people) = RoomWith(5, PersonTypes.Developer, PersonTypes.Developer);
        _service.Vote(people[0], "c1", "3");

        _service.ForceShowVotes(people[0], "c1");

        Assert.False(_rooms.Get(roomId)!.Revealed!.EveryoneVoted);
    }

    [Fact]
    public void Tick_ThatReveals_FreezesTheVotes()
    {
        var (roomId, people) = RoomWith(2, PersonTypes.Developer, PersonTypes.Tester);
        _service.Vote(people[0], "c1", "5");
        var start = _service.Vote(people[1], "c2", "5").Data!.FollowUp.Countdown!;
        _service.Tick(roomId, start.Revision);

        var revealed = _service.Tick(roomId, start.Revision).Revealed!;

        Assert.True(revealed.Statistics!.Consensus);
        Assert.Equal(2, _rooms.Get(roomId)!.Revealed!.Votes.Count);
    }

    [Fact]
    public void Tick_ThatOnlyCountsDown_FreezesNothing()
    {
        var (roomId, people) = RoomWith(3, PersonTypes.Developer, PersonTypes.Developer);
        _service.Vote(people[0], "c1", "5");
        var start = _service.Vote(people[1], "c2", "5").Data!.FollowUp.Countdown!;

        _service.Tick(roomId, start.Revision);

        Assert.Null(_rooms.Get(roomId)!.Revealed);
    }

    [Fact]
    public void Vote_WithCountdownZero_RevealsWithStatistics()
    {
        var (_, people) = RoomWith(0, PersonTypes.Developer, PersonTypes.Developer);
        _service.Vote(people[0], "c1", "5");

        var revealed = _service.Vote(people[1], "c2", "8").Data!.FollowUp.Revealed!;

        Assert.Equal(["5", "8"], revealed.Statistics!.All.Marks.Select(share => share.Mark));
        Assert.False(revealed.Statistics.Consensus);
    }

    [Fact]
    public void Exit_WithCountdownZero_RevealsWithStatistics()
    {
        var (roomId, people) = RoomWith(0, PersonTypes.Developer, PersonTypes.Developer, PersonTypes.Developer);
        _service.Vote(people[0], "c1", "5");
        _service.Vote(people[1], "c2", "5");

        var revealed = _service.Exit(people[2], "c3").Data!.FollowUp.Revealed!;

        Assert.True(revealed.Statistics!.Consensus);
        Assert.Equal(2, _rooms.Get(roomId)!.Revealed!.Votes.Count);
    }

    [Fact]
    public void ALeaverAfterTheReveal_DoesNotChangeTheStatistics()
    {
        var (roomId, people) = RoomWith(5, PersonTypes.Developer, PersonTypes.Developer, PersonTypes.Developer);
        _service.Vote(people[0], "c1", "3");
        _service.Vote(people[1], "c2", "3");
        _service.Vote(people[2], "c3", "8");
        _service.ForceShowVotes(people[0], "c1");

        _service.Exit(people[2], "c3");

        var joined = _service.Join(AddPerson(), "c4", roomId).Data!.VoteStatus;
        Assert.Equal(["3", "8"], joined.Statistics!.All.Marks.Select(share => share.Mark));
        Assert.DoesNotContain("8", joined.Votes.Values);
    }

    [Fact]
    public void Join_AfterTheReveal_CarriesTheStatistics_AndAnOpenRoomCarriesNone()
    {
        var (roomId, people) = RoomWith(5, PersonTypes.Developer, PersonTypes.Developer);
        _service.Vote(people[0], "c1", "5");
        Assert.Null(_service.Join(AddPerson(), "c3", roomId).Data!.VoteStatus.Statistics);
        _service.ForceShowVotes(people[0], "c1");

        var status = _service.Join(AddPerson(), "c4", roomId).Data!.VoteStatus;

        Assert.Equal(["5"], status.Statistics!.All.Marks.Select(share => share.Mark));
    }

    [Fact]
    public void ClearVotes_DropsTheFrozenVotes_AndTheNextRoundStartsWithoutStatistics()
    {
        var (roomId, people) = RoomWith(5, PersonTypes.Developer, PersonTypes.Developer);
        _service.Vote(people[0], "c1", "5");
        _service.ForceShowVotes(people[0], "c1");

        _service.ClearVotes(people[1], "c2");

        Assert.Null(_rooms.Get(roomId)!.Revealed);
        Assert.Null(_service.Join(AddPerson(), "c3", roomId).Data!.VoteStatus.Statistics);
    }

    [Fact]
    public void ForceShowVotes_AVoterJoinsAndVotesWhileTheTypesAreRead_IsStillFrozenWithTheirType()
    {
        var (roomId, people) = RoomWith(5, PersonTypes.Developer, PersonTypes.Developer);
        _service.Vote(people[0], "c1", "3");
        var interleaving = new InterleavingRoomRepository(_rooms);
        var service = RoomServiceFactory.Create(interleaving, _people);
        var late = AddPerson(PersonTypes.Tester);
        interleaving.AfterNextGet = () =>
        {
            Assert.True(_service.Join(late, "c3", roomId).IsSuccess);
            Assert.True(_service.Vote(late, "c3", "8").IsSuccess);
        };

        service.ForceShowVotes(people[0], "c1");

        var revealed = _rooms.Get(roomId)!.Revealed!;
        Assert.Equal(PersonTypes.Tester, revealed.Votes[late].PersonType);
        Assert.Equal("8", revealed.Votes[late].Mark);
    }

    [Fact]
    public void ForceShowVotes_VotersKeepJoiningWhileTheTypesAreRead_StillRevealsAndOnlyTheLastTypeIsUnknown()
    {
        var (roomId, people) = RoomWith(5, PersonTypes.Developer, PersonTypes.Developer);
        var interleaving = new InterleavingRoomRepository(_rooms);
        var service = RoomServiceFactory.Create(interleaving, _people);
        var joined = new List<Guid>();
        Action? hook = null;
        hook = () =>
        {
            var next = AddPerson();
            joined.Add(next);
            var connection = $"x{joined.Count}";
            Assert.True(_service.Join(next, connection, roomId).IsSuccess);
            Assert.True(_service.Vote(next, connection, "8").IsSuccess);
            // The reveal reads the room again afterwards, where voting is closed: stop interfering after three rounds.
            interleaving.AfterNextGet = joined.Count < RoomConstraints.TypeResolutionRounds ? hook : null;
        };
        interleaving.AfterNextGet = hook;

        var response = service.ForceShowVotes(people[0], "c1");

        Assert.True(response.IsSuccess);
        Assert.NotNull(response.Data!.VoteStatus);
        var revealed = _rooms.Get(roomId)!.Revealed!;
        Assert.Equal(PersonTypes.Developer, revealed.Votes[joined[0]].PersonType);
        Assert.Equal(string.Empty, revealed.Votes[joined[^1]].PersonType);
    }

    [Fact]
    public void ForceShowVotes_TwoRevealsAtOnce_FreezeOnceAndAnnounceOnce()
    {
        var (roomId, people) = RoomWith(5, PersonTypes.Developer, PersonTypes.Developer);
        _service.Vote(people[0], "c1", "3");
        var interleaving = new InterleavingRoomRepository(_rooms);
        var service = RoomServiceFactory.Create(interleaving, _people);
        RevealResult? other = null;
        // The other member's reveal runs between this reveal asking for the types and applying the change.
        interleaving.AfterNextUpdate = () => other = _service.ForceShowVotes(people[1], "c2").Data;

        var first = service.ForceShowVotes(people[0], "c1").Data!;

        Assert.Equal(1, new[] { first.VoteStatus, other!.VoteStatus }.Count(status => status is not null));
        Assert.Single(_rooms.Get(roomId)!.Revealed!.Votes);
    }
}
