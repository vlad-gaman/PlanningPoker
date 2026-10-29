using PlanningPoker.Controller.Mappers;
using PlanningPoker.Controller.Models;

namespace PlanningPoker.UnitTests.Controller;

public class RoomConfigurationMapperTests
{
    [Fact]
    public void Map_CopiesAllRequestFields()
    {
        var request = new RoomConfigurationRequest { CardSet = "fibonacci", CountdownSeconds = 12, ShowFireworks = true, MaxParticipants = 3 };

        var configuration = new RoomConfigurationMapper().Map(request);

        Assert.Equal("fibonacci", configuration.CardSet);
        Assert.Equal(12, configuration.CountdownSeconds);
        Assert.True(configuration.ShowFireworks);
        Assert.Equal(3, configuration.MaxParticipants);
    }
}
