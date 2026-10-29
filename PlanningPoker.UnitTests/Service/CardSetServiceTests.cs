using PlanningPoker.Service.Services;

namespace PlanningPoker.UnitTests.Service;

public class CardSetServiceTests
{
    [Fact]
    public void GetAll_ReturnsTheSevenCardSetsWithDisplayNames()
    {
        var response = new CardSetService().GetAll();

        Assert.True(response.IsSuccess);
        Assert.Equal(
            ["fibonacci", "modified-fibonacci", "linear", "powers-of-2", "t-shirt-sizes", "traffic-lights", "fist-of-five"],
            response.Data!.Select(option => option.Value));
        Assert.Equal("Linear (1-10)", response.Data!.Single(option => option.Value == "linear").Display);
        Assert.All(response.Data!, option => Assert.False(string.IsNullOrWhiteSpace(option.Display)));
    }

    [Theory]
    [InlineData("fibonacci", 14)]
    [InlineData("modified-fibonacci", 13)]
    [InlineData("linear", 12)]
    [InlineData("powers-of-2", 11)]
    [InlineData("t-shirt-sizes", 8)]
    [InlineData("traffic-lights", 3)]
    [InlineData("fist-of-five", 6)]
    public void GetCards_ReturnsTheCardsOfEverySet(string set, int count)
    {
        var response = new CardSetService().GetCards(set);

        Assert.True(response.IsSuccess);
        Assert.Equal(count, response.Data!.Count);
        Assert.Equal(response.Data!.Count, response.Data!.Select(card => card.Value).Distinct().Count());
    }

    [Fact]
    public void GetCards_KeepsDisplaysOfSpecialCards()
    {
        var cards = new CardSetService().GetCards("modified-fibonacci").Data!;

        Assert.Equal("½", cards.Single(card => card.Value == "0.5").Display);
        Assert.Equal("☕", cards.Single(card => card.Value == "coffee").Display);
        Assert.Null(cards.Single(card => card.Value == "13").Display);
        Assert.Equal("zero-finger-512x512.png", new CardSetService().GetCards("fist-of-five").Data![0].Display);
    }

    [Fact]
    public void GetCards_UnknownSet_FailsValidation()
    {
        var response = new CardSetService().GetCards("tarot");

        Assert.False(response.IsSuccess);
        Assert.Equal("validation_failed", response.Errors[0].Code);
        Assert.Equal("cardSet", response.Errors[0].Metadata["field"]);
    }
}
