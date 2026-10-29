using PlanningPoker.Common.Models;
using PlanningPoker.Controller.Mappers;
using PlanningPoker.Controller.Models;
using PlanningPoker.Service.Models;
using PlanningPoker.UnitTests.Builders;

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
            Cards = [new CardOption { Value = "1" }, new CardOption { Value = "coffee", Display = "Coffee" }],
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
    public void RoomSettingsMapper_CopiesRequestAndDelegatesConfiguration()
    {
        var request = new CreateRoomRequest
        {
            UseFunRoomName = true,
            Configuration = new RoomConfigurationRequest { CardSet = "fibonacci", CountdownSeconds = 12, ShowFireworks = true, MaxParticipants = 3 }
        };

        var settings = new RoomSettingsMapper(new RoomConfigurationRequestMapper()).Map(request);

        Assert.True(settings.UseFunRoomName);
        Assert.Equal(12, settings.Configuration.CountdownSeconds);
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

        var response = ResponseMappers.VoteStatus().Map(new VoteStatus { Votes = votes, HasEveryoneVoted = true });

        Assert.Equal("hide", response.Votes[Ann.Guid]);
        Assert.True(response.HasEveryoneVoted);
    }

    [Fact]
    public void JoinRoomResponseMapper_MapsPeopleOwnerStatusAndConfiguration()
    {
        var mapper = new JoinRoomResponseMapper(PersonMapper, ResponseMappers.VoteStatus(), new RoomConfigurationResponseMapper(), new CardResponseMapper());

        var response = mapper.Map(JoinResult());

        Assert.Equal(["Ann", "Bob"], response.People.Select(person => person.Name));
        Assert.Equal(Ann.Guid, response.OwnerGuid);
        Assert.True(response.VoteStatus.HasEveryoneVoted);
        Assert.Equal("linear", response.Configuration.CardSet);
        Assert.Equal(["1", "coffee"], response.Cards.Select(card => card.Value));
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
    public void RoomCountdownEventMapper_CarriesCountdownAndResetFlag()
    {
        var reset = new RoomCountdownEventMapper().Map(9, reset: true);
        var tick = new RoomCountdownEventMapper().Map(3, reset: false);

        Assert.Equal(9, reset.Countdown);
        Assert.True(reset.Reset);
        Assert.Equal(3, tick.Countdown);
        Assert.False(tick.Reset);
    }

    [Fact]
    public void CardResponseMapper_CopiesValueAndDisplay()
    {
        var response = new CardResponseMapper().Map(new CardOption { Value = "coffee", Display = "Coffee" });

        Assert.Equal("coffee", response.Value);
        Assert.Equal("Coffee", response.Display);
    }

    [Fact]
    public void VoteCastEventMapper_CarriesPersonGuid()
    {
        Assert.Equal(Ann.Guid, new VoteCastEventMapper().Map(Ann.Guid).PersonGuid);
    }

    [Fact]
    public void VoteStatusResponseMapper_CarriesFinishedAndCountdown()
    {
        var response = ResponseMappers.VoteStatus().Map(new VoteStatus { Votes = new Dictionary<Guid, string>(), VotingFinished = true, Countdown = 4 });

        Assert.True(response.VotingFinished);
        Assert.Equal(4, response.Countdown);
    }

    private static VoteStatistics Statistics()
    {
        return new VoteStatistics
        {
            All = new GroupStatistics { Marks = [new MarkShare { Mark = "3", Percentage = 66.7m }, new MarkShare { Mark = "5", Percentage = 33.3m }], HighestMark = "3", AverageMark = 3.67m },
            Developers = new GroupStatistics { Marks = [new MarkShare { Mark = "3", Percentage = 100m }], HighestMark = "3", AverageMark = 3m },
            Testers = new GroupStatistics { Marks = [], HighestMark = null, AverageMark = null },
            Consensus = true
        };
    }

    [Fact]
    public void VoteStatusResponseMapper_CarriesTheStatistics()
    {
        var response = ResponseMappers.VoteStatus().Map(new VoteStatus { Votes = new Dictionary<Guid, string>(), VotingFinished = true, Statistics = Statistics() });

        var statistics = response.Statistics!;
        Assert.Equal([("3", 66.7m), ("5", 33.3m)], statistics.All.Marks.Select(share => (share.Mark, share.Percentage)));
        Assert.Equal("3", statistics.All.HighestMark);
        Assert.Equal(3.67m, statistics.All.AverageMark);
        Assert.Equal(3m, statistics.Developers.AverageMark);
        Assert.Empty(statistics.Testers.Marks);
        Assert.Null(statistics.Testers.HighestMark);
        Assert.Null(statistics.Testers.AverageMark);
        Assert.True(statistics.Consensus);
    }

    [Fact]
    public void VoteStatusResponseMapper_WithoutStatistics_HasNone()
    {
        var response = ResponseMappers.VoteStatus().Map(new VoteStatus { Votes = new Dictionary<Guid, string>() });

        Assert.Null(response.Statistics);
    }

    [Fact]
    public void MarkShareResponseMapper_CopiesMarkAndPercentage()
    {
        var response = new MarkShareResponseMapper().Map(new MarkShare { Mark = "?", Percentage = 12.5m });

        Assert.Equal("?", response.Mark);
        Assert.Equal(12.5m, response.Percentage);
    }
}
