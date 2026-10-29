using PlanningPoker.Repository.Models;

namespace PlanningPoker.Repository.Mappers;

public class RevealedVotesEntityCopyMapper : IRevealedVotesEntityCopyMapper
{
    public RevealedVotesEntity Map(RevealedVotesEntity source)
    {
        return new RevealedVotesEntity
        {
            EveryoneVoted = source.EveryoneVoted,
            Votes = source.Votes.ToDictionary(pair => pair.Key, pair => new RevealedVoteEntity { Mark = pair.Value.Mark, PersonType = pair.Value.PersonType })
        };
    }
}
