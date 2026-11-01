using PlanningPoker.Common.Models;
using PlanningPoker.Controller.Mappers;
using PlanningPoker.Service.Models;
using PlanningPoker.UnitTests.Builders;

namespace PlanningPoker.UnitTests.Controller;

public class RoomManagementEventMappersTests
{
    private static readonly Person Ann = new() { Guid = Guid.NewGuid(), Name = "Ann", PersonType = "obs" };

    private static VoteStatus Status()
    {
        return new VoteStatus { Votes = new Dictionary<Guid, string> { [Ann.Guid] = "hide" }, HasEveryoneVoted = true, VotingFinished = false, Countdown = null };
    }

    [Fact]
    public void PersonTypeChangedEventMapper_CarriesThePersonAndTheStatus()
    {
        var result = new PersonTypeChangeResult { RoomId = "room", Person = Ann, Changed = true, VoteStatus = Status() };

        var mapped = new PersonTypeChangedEventMapper(new PersonResponseMapper(), ResponseMappers.VoteStatus()).Map(result);

        Assert.Equal((Ann.Guid, "Ann", "obs"), (mapped.Person.Guid, mapped.Person.Name, mapped.Person.PersonType));
        Assert.True(mapped.VoteStatus.HasEveryoneVoted);
        Assert.Null(mapped.VoteStatus.Countdown);
        Assert.Equal("hide", mapped.VoteStatus.Votes[Ann.Guid]);
    }

    [Fact]
    public void RoomConfigurationUpdatedEventMapper_CarriesConfigurationCardsUpdaterAndStatus()
    {
        var result = new RoomConfigurationChangeResult
        {
            RoomId = "room",
            Configuration = new RoomConfiguration { CardSet = "t-shirt-sizes", CountdownSeconds = 8, ShowFireworks = true, MaxParticipants = 3 },
            Cards = [new CardOption { Value = "XS" }, new CardOption { Value = "coffee", Display = "coffee-display" }],
            UpdatedBy = "Ann",
            VotesCleared = true,
            VoteStatus = Status()
        };

        var mapped = new RoomConfigurationUpdatedEventMapper(new RoomConfigurationResponseMapper(), new CardResponseMapper(), ResponseMappers.VoteStatus()).Map(result);

        Assert.Equal(("t-shirt-sizes", 8, true, 3), (mapped.Configuration.CardSet, mapped.Configuration.CountdownSeconds, mapped.Configuration.ShowFireworks, mapped.Configuration.MaxParticipants));
        Assert.Equal(["XS", "coffee"], mapped.Cards.Select(card => card.Value));
        Assert.Equal("coffee-display", mapped.Cards[1].Display);
        Assert.Equal("Ann", mapped.UpdatedBy);
        Assert.True(mapped.VoteStatus.HasEveryoneVoted);
    }
}
