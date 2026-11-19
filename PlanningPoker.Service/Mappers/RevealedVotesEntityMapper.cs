using PlanningPoker.Common.Constants;
using PlanningPoker.Repository.Models;

namespace PlanningPoker.Service.Mappers;

public class RevealedVotesEntityMapper : IRevealedVotesEntityMapper
{
    public RevealedVotesEntity Map(RoomEntity room)
    {
        return new RevealedVotesEntity
        {
            EveryoneVoted = room.EveryoneVoted,
            Votes = room.Votes
                .Where(vote => room.RoleOf(vote.Key) != PersonTypes.Observer)
                .ToDictionary(vote => vote.Key, vote => new RevealedVoteEntity { Mark = vote.Value, PersonType = room.RoleOf(vote.Key) })
        };
    }
}
