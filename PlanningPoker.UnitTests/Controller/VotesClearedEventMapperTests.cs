using PlanningPoker.Controller.Mappers;

namespace PlanningPoker.UnitTests.Controller;

public class VotesClearedEventMapperTests
{
    [Fact]
    public void Map_CarriesTheRoundStart()
    {
        var roundStartedAt = new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);

        Assert.Equal(roundStartedAt, new VotesClearedEventMapper().Map(roundStartedAt).RoundStartedAt);
    }
}
