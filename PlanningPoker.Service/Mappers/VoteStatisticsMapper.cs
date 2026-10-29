using System.Globalization;
using PlanningPoker.Common.Constants;
using PlanningPoker.Repository.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Service.Mappers;

public class VoteStatisticsMapper : IVoteStatisticsMapper
{
    private const NumberStyles NumericMark = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;

    public VoteStatistics Map(RevealedVotesEntity revealed, IReadOnlyList<string> cardOrder)
    {
        var votes = revealed.Votes.Values.Where(vote => vote.PersonType != PersonTypes.Observer).ToList();
        return new VoteStatistics
        {
            All = MapGroup(votes, cardOrder),
            Developers = MapGroup(votes.Where(vote => vote.PersonType == PersonTypes.Developer).ToList(), cardOrder),
            Testers = MapGroup(votes.Where(vote => vote.PersonType == PersonTypes.Tester).ToList(), cardOrder),
            Consensus = revealed.EveryoneVoted
                && votes.Count >= StatisticsConstants.MinimumConsensusVotes
                && votes.Select(vote => vote.Mark).Distinct().Count() == 1
        };
    }

    private static GroupStatistics MapGroup(IReadOnlyCollection<RevealedVoteEntity> votes, IReadOnlyList<string> cardOrder)
    {
        var counts = votes
            .GroupBy(vote => vote.Mark)
            .OrderBy(group => Position(cardOrder, group.Key))
            .ThenBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => (Mark: group.Key, Count: group.Count()))
            .ToList();
        var top = counts.Count == 0 ? 0 : counts.Max(entry => entry.Count);
        var numeric = votes.Select(vote => TryParse(vote.Mark)).OfType<decimal>().ToList();

        return new GroupStatistics
        {
            Marks = counts
                .Select(entry => new MarkShare { Mark = entry.Mark, Percentage = Math.Round(entry.Count * 100m / votes.Count, StatisticsConstants.PercentageDecimals, MidpointRounding.AwayFromZero) })
                .ToList(),
            HighestMark = counts.Count(entry => entry.Count == top) == 1 ? counts.First(entry => entry.Count == top).Mark : null,
            AverageMark = numeric.Count == 0 ? null : Math.Round(numeric.Average(), StatisticsConstants.AverageDecimals, MidpointRounding.AwayFromZero)
        };
    }

    /// <summary>Marks outside the card set (not expected) sort after all cards.</summary>
    private static int Position(IReadOnlyList<string> cardOrder, string mark)
    {
        for (var index = 0; index < cardOrder.Count; index++)
        {
            if (cardOrder[index] == mark)
            {
                return index;
            }
        }

        return int.MaxValue;
    }

    private static decimal? TryParse(string mark)
    {
        return decimal.TryParse(mark, NumericMark, CultureInfo.InvariantCulture, out var value) ? value : null;
    }
}
