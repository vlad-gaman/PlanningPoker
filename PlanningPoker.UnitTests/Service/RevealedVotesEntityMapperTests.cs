using PlanningPoker.Common.Constants;
using PlanningPoker.Repository.Models;
using PlanningPoker.Service.Mappers;

namespace PlanningPoker.UnitTests.Service;

public class RevealedVotesEntityMapperTests
{
    private static readonly Guid Dev = Guid.NewGuid();
    private static readonly Guid Tester = Guid.NewGuid();
    private static readonly Guid Observer = Guid.NewGuid();

    private static readonly Dictionary<Guid, string> Types = new()
    {
        [Dev] = PersonTypes.Developer,
        [Tester] = PersonTypes.Tester,
        [Observer] = PersonTypes.Observer
    };

    private static RoomEntity Room(params (Guid Guid, string? Mark)[] members)
    {
        return new RoomEntity
        {
            MemberGuids = members.Select(member => member.Guid).ToList(),
            Votes = members.Where(member => member.Mark is not null).ToDictionary(member => member.Guid, member => member.Mark!)
        };
    }

    [Fact]
    public void Map_CopiesTheMarksWithTheTypeOfEachVoter()
    {
        var revealed = new RevealedVotesEntityMapper().Map(Room((Dev, "5"), (Tester, "8")), Types);

        Assert.Equal("5", revealed.Votes[Dev].Mark);
        Assert.Equal(PersonTypes.Developer, revealed.Votes[Dev].PersonType);
        Assert.Equal(PersonTypes.Tester, revealed.Votes[Tester].PersonType);
        Assert.True(revealed.EveryoneVoted);
    }

    [Fact]
    public void Map_LeavesObserversOut_AndDoesNotWaitForThem()
    {
        var revealed = new RevealedVotesEntityMapper().Map(Room((Dev, "5"), (Observer, null)), Types);

        Assert.Equal([Dev], revealed.Votes.Keys);
        Assert.True(revealed.EveryoneVoted);
    }

    [Fact]
    public void Map_MemberWhoHasNotVoted_MeansNotEveryoneVoted()
    {
        var revealed = new RevealedVotesEntityMapper().Map(Room((Dev, "5"), (Tester, null)), Types);

        Assert.False(revealed.EveryoneVoted);
    }

    [Fact]
    public void Map_OnlyObservers_MeansNotEveryoneVoted()
    {
        var revealed = new RevealedVotesEntityMapper().Map(Room((Observer, null)), Types);

        Assert.Empty(revealed.Votes);
        Assert.False(revealed.EveryoneVoted);
    }

    [Fact]
    public void Map_VoterOfUnknownType_KeepsTheVoteWithAnEmptyType()
    {
        var stranger = Guid.NewGuid();

        var revealed = new RevealedVotesEntityMapper().Map(Room((Dev, "5"), (stranger, "8")), Types);

        Assert.Equal(string.Empty, revealed.Votes[stranger].PersonType);
        Assert.True(revealed.EveryoneVoted);
    }

    [Fact]
    public void Map_MemberOfUnknownTypeWhoHasNotVoted_MeansNotEveryoneVoted()
    {
        var revealed = new RevealedVotesEntityMapper().Map(Room((Dev, "5"), (Guid.NewGuid(), null)), Types);

        Assert.False(revealed.EveryoneVoted);
    }
}
