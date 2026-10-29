using PlanningPoker.Common.Constants;
using PlanningPoker.Repository.Models;
using PlanningPoker.Service.Mappers;
using PlanningPoker.Service.Models;

namespace PlanningPoker.UnitTests.Service;

public class RoomMappersTests
{
    private static readonly RoomConfiguration Configuration = new() { CardSet = "linear", CountdownSeconds = 9, ShowFireworks = false, MaxParticipants = 4 };

    [Fact]
    public void RoomEntityMapper_BuildsRoomWithOwnerAsOnlyMemberAndDelegatesConfiguration()
    {
        var owner = Guid.NewGuid();

        var entity = new RoomEntityMapper(new RoomConfigurationEntityMapper()).Map("room", owner, Configuration);

        Assert.Equal("room", entity.Id);
        Assert.Equal(owner, entity.OwnerGuid);
        Assert.Equal([owner], entity.MemberGuids);
        Assert.Empty(entity.MemberConnections);
        Assert.Empty(entity.Votes);
        Assert.True(entity.VotingEnabled);
        Assert.False(entity.IsDisposed);
        Assert.Equal(9, entity.Configuration.CountdownSeconds);
    }

    [Fact]
    public void RoomMapper_CopiesRoomFieldsAndDelegatesConfiguration()
    {
        var owner = Guid.NewGuid();
        var entity = new RoomEntityMapper(new RoomConfigurationEntityMapper()).Map("room", owner, Configuration);
        entity.VotingEnabled = true;

        var room = new RoomMapper(new RoomConfigurationMapper()).Map(entity);

        Assert.Equal("room", room.Id);
        Assert.Equal(owner, room.OwnerGuid);
        Assert.True(room.VotingEnabled);
        Assert.Equal(9, room.Configuration.CountdownSeconds);
    }

    private static Person Member(string type = PersonTypes.Developer)
    {
        return new Person { Guid = Guid.NewGuid(), Name = "n", PersonType = type };
    }

    [Fact]
    public void VoteStatusMapper_MasksEveryMark()
    {
        var voter = Member();
        var room = new RoomEntity { VotingEnabled = true, Votes = new Dictionary<Guid, string> { [voter.Guid] = "8" } };

        var status = new VoteStatusMapper().Map(room, [voter]);

        Assert.Equal(RoomConstraints.HiddenVote, status.Votes[voter.Guid]);
        Assert.False(status.VotingFinished);
        Assert.Null(status.Countdown);
    }

    [Fact]
    public void VoteStatusMapper_VotingFinished_ShowsRealMarksOfCurrentVotersOnly()
    {
        var voter = Member();
        var gone = Guid.NewGuid();
        var room = new RoomEntity { VotingEnabled = false, Votes = new Dictionary<Guid, string> { [voter.Guid] = "8", [gone] = "3" } };

        var status = new VoteStatusMapper().Map(room, [voter]);

        Assert.Equal("8", Assert.Single(status.Votes).Value);
        Assert.True(status.VotingFinished);
    }

    [Fact]
    public void VoteStatusMapper_RunningCountdown_ReportsRemainingSeconds()
    {
        var room = new RoomEntity { VotingEnabled = true, Revision = 2, CountdownRevision = 2, CountdownRemaining = 4 };

        Assert.Equal(4, new VoteStatusMapper().Map(room, [Member()]).Countdown);
    }

    [Fact]
    public void VoteStatusMapper_EveryoneVoted_IgnoresObservers()
    {
        var voter = Member();
        var observer = Member(PersonTypes.Observer);
        var room = new RoomEntity { Votes = new Dictionary<Guid, string> { [voter.Guid] = "8" } };

        Assert.True(new VoteStatusMapper().Map(room, [voter, observer]).HasEveryoneVoted);
    }

    [Fact]
    public void VoteStatusMapper_SomeoneHasNotVoted_IsFalse()
    {
        var voter = Member();
        var waiting = Member();
        var room = new RoomEntity { Votes = new Dictionary<Guid, string> { [voter.Guid] = "8" } };

        Assert.False(new VoteStatusMapper().Map(room, [voter, waiting]).HasEveryoneVoted);
    }

    [Fact]
    public void VoteStatusMapper_NoVoters_IsFalse()
    {
        Assert.False(new VoteStatusMapper().Map(new RoomEntity(), [Member(PersonTypes.Observer)]).HasEveryoneVoted);
    }

    [Fact]
    public void JoinRoomResultMapper_CarriesInputsAndDecidesCountdownReset()
    {
        var room = new Room { Id = "r", VotingEnabled = true, Configuration = Configuration };
        var person = new Person { Guid = Guid.NewGuid(), Name = "Ann", PersonType = PersonTypes.Tester };
        var status = new VoteStatus { Votes = new Dictionary<Guid, string>() };

        var result = new JoinRoomResultMapper().Map(room, person, [person], status, [], personAdded: true);

        Assert.Same(room, result.Room);
        Assert.Same(person, result.Person);
        Assert.Same(status, result.VoteStatus);
        Assert.Equal([person], result.People);
        Assert.True(result.PersonAdded);
        Assert.True(result.ResetCountdown);
    }

    [Fact]
    public void JoinRoomResultMapper_CarriesPersonAddedFalseForAnotherConnection()
    {
        var room = new Room { Id = "r", VotingEnabled = true, Configuration = Configuration };
        var person = new Person { Guid = Guid.NewGuid(), Name = "Ann", PersonType = PersonTypes.Tester };

        var result = new JoinRoomResultMapper().Map(room, person, [person], new VoteStatus { Votes = new Dictionary<Guid, string>() }, [], personAdded: false);

        Assert.False(result.PersonAdded);
    }

    [Fact]
    public void ExitRoomResultMapper_CarriesInputs()
    {
        var person = new Person { Guid = Guid.NewGuid(), Name = "Ann", PersonType = PersonTypes.Tester };
        var newOwner = new Person { Guid = Guid.NewGuid(), Name = "Bob", PersonType = PersonTypes.Tester };

        var result = new ExitRoomResultMapper().Map("r", person, personLeft: true, roomDisposed: false, newOwner, VotingFollowUp.None);

        Assert.Equal("r", result.RoomId);
        Assert.Same(person, result.Person);
        Assert.True(result.PersonLeft);
        Assert.False(result.RoomDisposed);
        Assert.Same(newOwner, result.NewOwner);
    }
}
