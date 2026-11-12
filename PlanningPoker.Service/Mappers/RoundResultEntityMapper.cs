using PlanningPoker.Common.Constants;
using PlanningPoker.Common.Models;
using PlanningPoker.Repository.Models;

namespace PlanningPoker.Service.Mappers;

public class RoundResultEntityMapper : IRoundResultEntityMapper
{
    public RoundResultEntity Map(int round, RevealedVotesEntity revealed, DateTimeOffset revealedAt)
    {
        // The same figure as the "All" statistics: every voter, observers excluded (they are not in the frozen votes).
        var numeric = revealed.Votes.Values.Select(vote => MarkValue.TryParse(vote.Mark)).OfType<decimal>().ToList();
        return new RoundResultEntity
        {
            Round = round,
            AverageMark = numeric.Count == 0 ? null : Math.Round(numeric.Average(), StatisticsConstants.AverageDecimals, MidpointRounding.AwayFromZero),
            RevealedAt = revealedAt
        };
    }
}
