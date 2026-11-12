using PlanningPoker.Repository.Models;

namespace PlanningPoker.Service.Mappers;

public interface IRoundResultEntityMapper
{
    /// <summary>The history entry for a round whose votes were just frozen: its number, the average of the numeric marks, and when it was revealed.</summary>
    RoundResultEntity Map(int round, RevealedVotesEntity revealed, DateTimeOffset revealedAt);
}
