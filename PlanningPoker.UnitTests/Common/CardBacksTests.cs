using PlanningPoker.Common.Constants;

namespace PlanningPoker.UnitTests.Common;

public class CardBacksTests
{
    [Theory]
    [InlineData(CardBacks.Classic)]
    [InlineData(CardBacks.Stripes)]
    [InlineData(CardBacks.Dots)]
    [InlineData(CardBacks.Geometric)]
    [InlineData(CardBacks.Waves)]
    [InlineData(CardBacks.Checker)]
    [InlineData(CardBacks.Diamonds)]
    [InlineData(CardBacks.Zigzag)]
    [InlineData(CardBacks.Grid)]
    [InlineData(CardBacks.Rings)]
    [InlineData(CardBacks.Sunburst)]
    [InlineData(CardBacks.Plaid)]
    public void IsValid_KnownDesign_IsTrue(string design)
    {
        Assert.True(CardBacks.IsValid(design));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Classic")]
    [InlineData("sparkles")]
    public void IsValid_AnythingElse_IsFalse(string? design)
    {
        Assert.False(CardBacks.IsValid(design));
    }

    [Fact]
    public void Default_IsAKnownDesign()
    {
        Assert.True(CardBacks.IsValid(CardBacks.Default));
    }
}
