using PlanningPoker.Repository.Models;
using PlanningPoker.Service.Mappers;
using PlanningPoker.Service.Models;

namespace PlanningPoker.UnitTests.Service;

public class RoomConfigurationMappersTests
{
    [Fact]
    public void RoomConfigurationEntityMapper_CopiesAllFields()
    {
        var configuration = new RoomConfiguration { CardSet = "linear", CountdownSeconds = 9, ShowFireworks = true, MaxParticipants = 4 };

        var entity = new RoomConfigurationEntityMapper().Map(configuration);

        Assert.Equal("linear", entity.CardSet);
        Assert.Equal(9, entity.CountdownSeconds);
        Assert.True(entity.ShowFireworks);
        Assert.Equal(4, entity.MaxParticipants);
    }

    [Fact]
    public void RoomConfigurationMapper_CopiesAllFields()
    {
        var entity = new RoomConfigurationEntity { CardSet = "linear", CountdownSeconds = 9, ShowFireworks = true, MaxParticipants = 4 };

        var configuration = new RoomConfigurationMapper().Map(entity);

        Assert.Equal("linear", configuration.CardSet);
        Assert.Equal(9, configuration.CountdownSeconds);
        Assert.True(configuration.ShowFireworks);
        Assert.Equal(4, configuration.MaxParticipants);
    }
}
