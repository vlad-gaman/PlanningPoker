using PlanningPoker.Repository.Mappers;
using PlanningPoker.Repository.Models;

namespace PlanningPoker.UnitTests.Repository;

public class RoomConfigurationEntityCopyMapperTests
{
    [Fact]
    public void Map_CopiesAllFields()
    {
        var source = new RoomConfigurationEntity { CardSet = "linear", CountdownSeconds = 9, ShowFireworks = true, MaxParticipants = 7 };

        var copy = new RoomConfigurationEntityCopyMapper().Map(source);

        Assert.Equal("linear", copy.CardSet);
        Assert.Equal(9, copy.CountdownSeconds);
        Assert.True(copy.ShowFireworks);
        Assert.Equal(7, copy.MaxParticipants);
        Assert.NotSame(source, copy);
    }
}
