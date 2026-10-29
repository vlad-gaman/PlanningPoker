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
}
