using PlanningPoker.Repository.Mappers;
using PlanningPoker.Repository.Models;

namespace PlanningPoker.UnitTests.Repository;

public class RoomEntityCopyMapperTests
{
    [Fact]
    public void Map_CopiesAllFieldsWithoutSharingCollections()
    {
        var owner = Guid.NewGuid();
        var source = new RoomEntity
        {
            Id = "room",
            OwnerGuid = owner,
            VotingEnabled = true,
            IsDisposed = true,
            MemberGuids = [owner],
            Votes = new Dictionary<Guid, string> { [owner] = "5" },
            Configuration = new RoomConfigurationEntity { CardSet = "linear", CountdownSeconds = 9, ShowFireworks = false, MaxParticipants = 7 }
        };

        var copy = new RoomEntityCopyMapper(new RoomConfigurationEntityCopyMapper()).Map(source);

        Assert.Equal("room", copy.Id);
        Assert.Equal(owner, copy.OwnerGuid);
        Assert.True(copy.VotingEnabled);
        Assert.True(copy.IsDisposed);
        Assert.Equal(source.MemberGuids, copy.MemberGuids);
        Assert.Equal("5", copy.Votes[owner]);
        Assert.Equal(9, copy.Configuration.CountdownSeconds);
        Assert.NotSame(source.MemberGuids, copy.MemberGuids);
        Assert.NotSame(source.Votes, copy.Votes);
        Assert.NotSame(source.Configuration, copy.Configuration);
    }
}
