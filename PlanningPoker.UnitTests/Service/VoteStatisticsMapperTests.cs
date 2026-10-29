using System.Globalization;
using PlanningPoker.Common.Constants;
using PlanningPoker.Repository.Models;
using PlanningPoker.Service.Mappers;
using PlanningPoker.Service.Models;

namespace PlanningPoker.UnitTests.Service;

public class VoteStatisticsMapperTests
{
    private static readonly string[] ModifiedFibonacci = ["0", "0.5", "1", "2", "3", "5", "8", "13", "20", "40", "100", "?", "coffee"];
    private static readonly string[] TShirts = ["XS", "S", "M", "L", "XL", "XXL", "?", "coffee"];
    private static readonly string[] TrafficLights = ["green", "yellow", "red"];

    private static RevealedVotesEntity Revealed(bool everyoneVoted, params (string Type, string Mark)[] votes)
    {
        return new RevealedVotesEntity
        {
            EveryoneVoted = everyoneVoted,
            Votes = votes.ToDictionary(_ => Guid.NewGuid(), vote => new RevealedVoteEntity { Mark = vote.Mark, PersonType = vote.Type })
        };
    }

    private static VoteStatistics Map(RevealedVotesEntity revealed, string[]? order = null)
    {
        return new VoteStatisticsMapper().Map(revealed, order ?? ModifiedFibonacci);
    }

    private static (string Mark, decimal Percentage)[] Shares(GroupStatistics group)
    {
        return group.Marks.Select(share => (share.Mark, share.Percentage)).ToArray();
    }

    [Fact]
    public void Map_OneMark_IsOneHundredPercentAndTheHighest()
    {
        var statistics = Map(Revealed(true, (PersonTypes.Developer, "5"), (PersonTypes.Tester, "5")));

        Assert.Equal([("5", 100m)], Shares(statistics.All));
        Assert.Equal("5", statistics.All.HighestMark);
    }

    [Fact]
    public void Map_PercentagesAreRoundedToOneDecimal()
    {
        var statistics = Map(Revealed(true, (PersonTypes.Developer, "3"), (PersonTypes.Developer, "3"), (PersonTypes.Developer, "5")));

        Assert.Equal([("3", 66.7m), ("5", 33.3m)], Shares(statistics.All));
    }

    [Fact]
    public void Map_MarksFollowTheCardSetOrder_NotTheOrderOfTheVotes()
    {
        var statistics = Map(Revealed(true,
            (PersonTypes.Developer, "coffee"), (PersonTypes.Developer, "?"), (PersonTypes.Developer, "13"),
            (PersonTypes.Developer, "0.5"), (PersonTypes.Developer, "2")));

        Assert.Equal(["0.5", "2", "13", "?", "coffee"], statistics.All.Marks.Select(share => share.Mark));
    }

    [Fact]
    public void Map_NonNumericSets_FollowTheCardSetOrder()
    {
        var sizes = Map(Revealed(true, (PersonTypes.Developer, "XL"), (PersonTypes.Developer, "S"), (PersonTypes.Developer, "XS")), TShirts);
        var lights = Map(Revealed(true, (PersonTypes.Developer, "red"), (PersonTypes.Developer, "green")), TrafficLights);

        Assert.Equal(["XS", "S", "XL"], sizes.All.Marks.Select(share => share.Mark));
        Assert.Equal(["green", "red"], lights.All.Marks.Select(share => share.Mark));
    }

    [Fact]
    public void Map_AMarkOutsideTheCardSet_GoesLast()
    {
        var statistics = Map(Revealed(true, (PersonTypes.Developer, "odd"), (PersonTypes.Developer, "coffee"), (PersonTypes.Developer, "1")));

        Assert.Equal(["1", "coffee", "odd"], statistics.All.Marks.Select(share => share.Mark));
    }

    [Fact]
    public void Map_MarksNobodyPicked_HaveNoEntry()
    {
        var statistics = Map(Revealed(true, (PersonTypes.Developer, "5")));

        Assert.Single(statistics.All.Marks);
    }

    [Fact]
    public void Map_HighestMark_IsNullOnATieAtTheTop()
    {
        var statistics = Map(Revealed(true, (PersonTypes.Developer, "3"), (PersonTypes.Developer, "5"), (PersonTypes.Developer, "8"), (PersonTypes.Developer, "8")));
        var tied = Map(Revealed(true, (PersonTypes.Developer, "3"), (PersonTypes.Developer, "5")));

        Assert.Equal("8", statistics.All.HighestMark);
        Assert.Null(tied.All.HighestMark);
    }

    [Fact]
    public void Map_SplitsDevelopersAndTesters_AndLeavesObserversOut()
    {
        var statistics = Map(Revealed(true,
            (PersonTypes.Developer, "3"), (PersonTypes.Developer, "5"), (PersonTypes.Tester, "8"), (PersonTypes.Observer, "13")));

        Assert.Equal([("3", 50m), ("5", 50m)], Shares(statistics.Developers));
        Assert.Equal([("8", 100m)], Shares(statistics.Testers));
        Assert.Equal(["3", "5", "8"], statistics.All.Marks.Select(share => share.Mark));
        Assert.Equal(33.3m, statistics.All.Marks[0].Percentage);
    }

    [Fact]
    public void Map_AVoteWithAnUnknownType_CountsOnlyInTheOverallGroup()
    {
        var statistics = Map(Revealed(true, (string.Empty, "3"), (PersonTypes.Developer, "5")));

        Assert.Equal(2, statistics.All.Marks.Count);
        Assert.Equal([("5", 100m)], Shares(statistics.Developers));
        Assert.Empty(statistics.Testers.Marks);
    }

    [Fact]
    public void Map_AGroupWithoutVotes_IsEmpty()
    {
        var statistics = Map(Revealed(true, (PersonTypes.Developer, "5")));

        Assert.Empty(statistics.Testers.Marks);
        Assert.Null(statistics.Testers.HighestMark);
        Assert.Null(statistics.Testers.AverageMark);
    }

    [Fact]
    public void Map_NoVotesAtAll_IsEmptyEverywhere()
    {
        var statistics = Map(Revealed(false));

        Assert.Empty(statistics.All.Marks);
        Assert.Null(statistics.All.AverageMark);
        Assert.False(statistics.Consensus);
    }

    [Fact]
    public void Map_Average_UsesTheNumericMarksOnly()
    {
        var statistics = Map(Revealed(true, (PersonTypes.Developer, "3"), (PersonTypes.Developer, "8"), (PersonTypes.Tester, "?"), (PersonTypes.Tester, "coffee")));

        Assert.Equal(5.5m, statistics.All.AverageMark);
        Assert.Equal(5.5m, statistics.Developers.AverageMark);
        Assert.Null(statistics.Testers.AverageMark);
    }

    [Fact]
    public void Map_Average_IsRoundedToTwoDecimals()
    {
        var statistics = Map(Revealed(true, (PersonTypes.Developer, "1"), (PersonTypes.Developer, "1"), (PersonTypes.Developer, "2")));

        Assert.Equal(1.33m, statistics.All.AverageMark);
    }

    [Fact]
    public void Map_Average_OfZeroIsZeroNotNull()
    {
        var statistics = Map(Revealed(true, (PersonTypes.Developer, "0"), (PersonTypes.Developer, "0")));

        Assert.Equal(0m, statistics.All.AverageMark);
    }

    [Fact]
    public void Map_Average_UnderTheHalfCard()
    {
        var statistics = Map(Revealed(true, (PersonTypes.Developer, "0.5"), (PersonTypes.Developer, "1")));

        Assert.Equal(0.75m, statistics.All.AverageMark);
    }

    [Fact]
    public void Map_Average_NonNumericSetsHaveNone()
    {
        var statistics = Map(Revealed(true, (PersonTypes.Developer, "XL"), (PersonTypes.Developer, "S")), TShirts);

        Assert.Null(statistics.All.AverageMark);
    }

    [Fact]
    public void Map_Numbers_ParseAndRoundIndependentlyOfTheCurrentCulture()
    {
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");
        try
        {
            var statistics = Map(Revealed(true, (PersonTypes.Developer, "0.5"), (PersonTypes.Developer, "1")));

            Assert.Equal(0.75m, statistics.All.AverageMark);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void Map_Consensus_NeedsTwoVotesOneMarkAndEveryoneVoted()
    {
        Assert.True(Map(Revealed(true, (PersonTypes.Developer, "5"), (PersonTypes.Tester, "5"))).Consensus);
        Assert.False(Map(Revealed(true, (PersonTypes.Developer, "5"))).Consensus);
        Assert.False(Map(Revealed(false, (PersonTypes.Developer, "5"), (PersonTypes.Tester, "5"))).Consensus);
        Assert.False(Map(Revealed(true, (PersonTypes.Developer, "5"), (PersonTypes.Tester, "8"))).Consensus);
    }
}
