using PlanningPoker.Repository.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Service.Mappers;

public interface IVoteStatisticsMapper
{
    /// <summary>Derives the statistics from the frozen votes; <paramref name="cardOrder"/> is the room's card values in display order.</summary>
    VoteStatistics Map(RevealedVotesEntity revealed, IReadOnlyList<string> cardOrder);
}
