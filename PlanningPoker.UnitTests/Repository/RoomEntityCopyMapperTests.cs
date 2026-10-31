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
            Revision = 4,
            CountdownRevision = 3,
            CountdownRemaining = 2,
            MemberGuids = [owner],
            MemberConnections = new Dictionary<Guid, HashSet<string>> { [owner] = ["c1"] },
            Votes = new Dictionary<Guid, string> { [owner] = "5" },
            Configuration = new RoomConfigurationEntity { CardSet = "linear", CountdownSeconds = 9, ShowFireworks = false, MaxParticipants = 7 }
        };

        var copy = new RoomEntityCopyMapper(new RoomConfigurationEntityCopyMapper(), new RevealedVotesEntityCopyMapper()).Map(source);

        Assert.Equal("room", copy.Id);
        Assert.Equal(owner, copy.OwnerGuid);
        Assert.True(copy.VotingEnabled);
        Assert.True(copy.IsDisposed);
        Assert.Equal(4, copy.Revision);
        Assert.Equal(3, copy.CountdownRevision);
        Assert.Equal(2, copy.CountdownRemaining);
        Assert.Equal(source.MemberGuids, copy.MemberGuids);
        Assert.Equal(["c1"], copy.MemberConnections[owner]);
        Assert.Equal("5", copy.Votes[owner]);
        Assert.Equal(9, copy.Configuration.CountdownSeconds);
        Assert.NotSame(source.MemberGuids, copy.MemberGuids);
        Assert.NotSame(source.MemberConnections, copy.MemberConnections);
        Assert.NotSame(source.MemberConnections[owner], copy.MemberConnections[owner]);
        Assert.NotSame(source.Votes, copy.Votes);
        Assert.NotSame(source.Configuration, copy.Configuration);
    }

    [Fact]
    public void Map_CopiesTheRevealedVotesWithoutSharingThem()
    {
        var voter = Guid.NewGuid();
        var source = new RoomEntity
        {
            Revealed = new RevealedVotesEntity
            {
                EveryoneVoted = true,
                Votes = new Dictionary<Guid, RevealedVoteEntity> { [voter] = new() { Mark = "5", PersonType = "dev" } }
            }
        };

        var copy = new RoomEntityCopyMapper(new RoomConfigurationEntityCopyMapper(), new RevealedVotesEntityCopyMapper()).Map(source);

        Assert.True(copy.Revealed!.EveryoneVoted);
        Assert.Equal("5", copy.Revealed.Votes[voter].Mark);
        Assert.Equal("dev", copy.Revealed.Votes[voter].PersonType);
        Assert.NotSame(source.Revealed, copy.Revealed);
        Assert.NotSame(source.Revealed.Votes, copy.Revealed.Votes);
        Assert.NotSame(source.Revealed.Votes[voter], copy.Revealed.Votes[voter]);
    }

    [Fact]
    public void Map_OfAnOpenRoom_HasNoRevealedVotes()
    {
        var copy = new RoomEntityCopyMapper(new RoomConfigurationEntityCopyMapper(), new RevealedVotesEntityCopyMapper()).Map(new RoomEntity());

        Assert.Null(copy.Revealed);
    }
}
