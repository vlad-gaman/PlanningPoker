using PlanningPoker.Common.Constants;
using PlanningPoker.Repository.Models;

namespace PlanningPoker.Service.Mappers;

public class RevealedVotesEntityMapper : IRevealedVotesEntityMapper
{
    public RevealedVotesEntity Map(RoomEntity room, IReadOnlyDictionary<Guid, string> personTypes)
    {
        var voters = room.MemberGuids.Where(guid => TypeOf(personTypes, guid) != PersonTypes.Observer).ToList();
        return new RevealedVotesEntity
        {
            EveryoneVoted = voters.Count > 0 && voters.All(room.Votes.ContainsKey),
            Votes = room.Votes
                .Where(vote => TypeOf(personTypes, vote.Key) != PersonTypes.Observer)
                .ToDictionary(vote => vote.Key, vote => new RevealedVoteEntity { Mark = vote.Value, PersonType = TypeOf(personTypes, vote.Key) })
        };
    }

    private static string TypeOf(IReadOnlyDictionary<Guid, string> personTypes, Guid guid)
    {
        return personTypes.GetValueOrDefault(guid, string.Empty);
    }
}
