using PlanningPoker.Common.Constants;
using PlanningPoker.Repository.Models;
using PlanningPoker.Service.Mappers;
using PlanningPoker.Service.Models;

namespace PlanningPoker.UnitTests.Service;

public class RoomMappersTests
{
    private static readonly RoomConfiguration Configuration = new() { CardSet = "linear", CountdownSeconds = 9, ShowFireworks = false, MaxParticipants = 4 };

    [Fact]
    public void RoomEntityMapper_BuildsRoomWithOwnerAsOnlyMember()
    {
        var owner = Guid.NewGuid();

        var entity = new RoomEntityMapper().Map("room", owner, Configuration);

        Assert.Equal("room", entity.Id);
        Assert.Equal(owner, entity.OwnerGuid);
        Assert.Equal([owner], entity.MemberGuids);
        Assert.Empty(entity.Votes);
        Assert.False(entity.VotingEnabled);
        Assert.Equal("linear", entity.Configuration.CardSet);
        Assert.Equal(9, entity.Configuration.CountdownSeconds);
        Assert.False(entity.Configuration.ShowFireworks);
        Assert.Equal(4, entity.Configuration.MaxParticipants);
    }

    [Fact]
    public void RoomMapper_CopiesRoomFields()
    {
        var owner = Guid.NewGuid();
        var entity = new RoomEntityMapper().Map("room", owner, Configuration);
        entity.VotingEnabled = true;

        var room = new RoomMapper().Map(entity);

        Assert.Equal("room", room.Id);
        Assert.Equal(owner, room.OwnerGuid);
        Assert.True(room.VotingEnabled);
        Assert.Equal("linear", room.Configuration.CardSet);
        Assert.Equal(9, room.Configuration.CountdownSeconds);
        Assert.False(room.Configuration.ShowFireworks);
        Assert.Equal(4, room.Configuration.MaxParticipants);
    }

    private static PersonEntity Member(string type = PersonTypes.Developer)
    {
        return new PersonEntity { Guid = Guid.NewGuid(), Name = "n", PersonType = type };
    }

    [Fact]
    public void VoteStatusMapper_MasksEveryMark()
    {
        var voter = Member();
        var room = new RoomEntity { Votes = new Dictionary<Guid, string> { [voter.Guid] = "8" } };

        var status = new VoteStatusMapper().Map(room, [voter]);

        Assert.Equal(RoomConstraints.HiddenVote, status.Votes[voter.Guid]);
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

        var result = new JoinRoomResultMapper().Map(room, person, [person], status);

        Assert.Same(room, result.Room);
        Assert.Same(person, result.Person);
        Assert.Same(status, result.VoteStatus);
        Assert.Equal([person], result.People);
        Assert.True(result.ResetCountdown);
    }

    [Fact]
    public void ExitRoomResultMapper_CarriesInputs()
    {
        var person = new Person { Guid = Guid.NewGuid(), Name = "Ann", PersonType = PersonTypes.Tester };
        var newOwner = new Person { Guid = Guid.NewGuid(), Name = "Bob", PersonType = PersonTypes.Tester };

        var result = new ExitRoomResultMapper().Map("r", person, false, newOwner);

        Assert.Equal("r", result.RoomId);
        Assert.Same(person, result.Person);
        Assert.False(result.RoomDisposed);
        Assert.Same(newOwner, result.NewOwner);
    }
}
