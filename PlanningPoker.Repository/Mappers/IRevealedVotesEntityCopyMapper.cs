using PlanningPoker.Repository.Models;

namespace PlanningPoker.Repository.Mappers;

public interface IRevealedVotesEntityCopyMapper
{
    RevealedVotesEntity Map(RevealedVotesEntity source);
}
