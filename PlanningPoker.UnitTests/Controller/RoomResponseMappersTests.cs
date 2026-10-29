using PlanningPoker.Common.Models;
using PlanningPoker.Controller.Mappers;
using PlanningPoker.Controller.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.UnitTests.Controller;

public class RoomResponseMappersTests
{
    private static readonly IPersonResponseMapper PersonMapper = new PersonResponseMapper();
    private static readonly Person Ann = new() { Guid = Guid.NewGuid(), Name = "Ann", PersonType = "dev" };
    private static readonly Person Bob = new() { Guid = Guid.NewGuid(), Name = "Bob", PersonType = "obs" };
    private static readonly RoomConfiguration Configuration = new() { CardSet = "linear", CountdownSeconds = 9, ShowFireworks = false, MaxParticipants = 4 };
    private static readonly Room Room = new() { Id = "room", OwnerGuid = Ann.Guid, Configuration = Configuration };

    private static JoinRoomResult JoinResult(Dictionary<Guid, string>? votes = null)
    {
        return new JoinRoomResult
        {
            Room = Room,
            Person = Bob,
            People = [Ann, Bob],
            VoteStatus = new VoteStatus { Votes = votes ?? [], HasEveryoneVoted = true }
        };
    }

    [Fact]
    public void CardSetResponseMapper_CopiesValueAndDisplay()
    {
        var response = new CardSetResponseMapper().Map(new CardSetOption { Value = "linear", Display = "Linear (1-10)" });

        Assert.Equal("linear", response.Value);
        Assert.Equal("Linear (1-10)", response.Display);
    }

    [Fact]
    public void RoomSettingsMapper_CopiesRequest()
    {
        var request = new CreateRoomRequest
        {
            UseFunRoomName = true,
            Configuration = new RoomConfigurationRequest { CardSet = "fibonacci", CountdownSeconds = 12, ShowFireworks = true, MaxParticipants = 3 }
        };

        var settings = new RoomSettingsMapper().Map(request);

        Assert.True(settings.UseFunRoomName);
        Assert.Equal("fibonacci", settings.Configuration.CardSet);
        Assert.Equal(12, settings.Configuration.CountdownSeconds);
        Assert.True(settings.Configuration.ShowFireworks);
        Assert.Equal(3, settings.Configuration.MaxParticipants);
    }

    [Fact]
    public void RoomCreatedResponseMapper_ReturnsId()
    {
        Assert.Equal("room", new RoomCreatedResponseMapper().Map(Room).Id);
    }

    [Fact]
    public void RoomExistsResponseMapper_ReturnsIdAndExistsTrue()
    {
        var response = new RoomExistsResponseMapper().Map("room");

        Assert.Equal("room", response.Id);
        Assert.True(response.Exists);
    }

    [Fact]
    public void RoomConfigurationResponseMapper_CopiesAllFields()
    {
        var response = new RoomConfigurationResponseMapper().Map(Configuration);

        Assert.Equal("linear", response.CardSet);
        Assert.Equal(9, response.CountdownSeconds);
        Assert.False(response.ShowFireworks);
        Assert.Equal(4, response.MaxParticipants);
    }

    [Fact]
    public void VoteStatusResponseMapper_CopiesVotesAndFlag()
    {
        var votes = new Dictionary<Guid, string> { [Ann.Guid] = "hide" };

        var response = new VoteStatusResponseMapper().Map(new VoteStatus { Votes = votes, HasEveryoneVoted = true });

        Assert.Equal("hide", response.Votes[Ann.Guid]);
        Assert.True(response.HasEveryoneVoted);
    }

    [Fact]
    public void JoinRoomResponseMapper_MapsPeopleOwnerStatusAndConfiguration()
    {
        var mapper = new JoinRoomResponseMapper(PersonMapper, new VoteStatusResponseMapper(), new RoomConfigurationResponseMapper());

        var response = mapper.Map(JoinResult());

        Assert.Equal(["Ann", "Bob"], response.People.Select(person => person.Name));
        Assert.Equal(Ann.Guid, response.OwnerGuid);
        Assert.True(response.VoteStatus.HasEveryoneVoted);
        Assert.Equal("linear", response.Configuration.CardSet);
    }

    [Fact]
    public void PersonJoinedEventMapper_IncludesPersonTypeAndMaskedVote()
    {
        var mapper = new PersonJoinedEventMapper(PersonMapper);

        var voted = mapper.Map(JoinResult(new Dictionary<Guid, string> { [Bob.Guid] = "hide" }));
        var notVoted = mapper.Map(JoinResult());

        Assert.Equal(Bob.Guid, voted.Person.Guid);
        Assert.Equal("obs", voted.PersonType);
        Assert.Equal("hide", voted.Vote);
        Assert.Null(notVoted.Vote);
    }

    [Fact]
    public void PersonExitedEventMapper_MapsPerson()
    {
        var response = new PersonExitedEventMapper(PersonMapper).Map(new ExitRoomResult { RoomId = "room", Person = Ann });

        Assert.Equal(Ann.Guid, response.Person.Guid);
    }

    [Fact]
    public void OwnershipTransferredEventMapper_MapsNewOwner()
    {
        Assert.Equal(Bob.Guid, new OwnershipTransferredEventMapper(PersonMapper).Map(Bob).NewOwner.Guid);
    }

    [Fact]
    public void RoomDisposedEventMapper_ReturnsRoomId()
    {
        Assert.Equal("room", new RoomDisposedEventMapper().Map("room").RoomId);
    }

    [Fact]
    public void CountdownResetEventMapper_ReturnsConfiguredCountdownAndResetFlag()
    {
        var response = new CountdownResetEventMapper().Map(Room);

        Assert.Equal(9, response.Countdown);
        Assert.True(response.Reset);
    }
}
