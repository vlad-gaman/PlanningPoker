using PlanningPoker.Common.Constants;
using PlanningPoker.Repository.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Service.Mappers;

public class VoteStatusMapper : IVoteStatusMapper
{
    public VoteStatus Map(RoomEntity room, IReadOnlyCollection<Person> members)
    {
        var voters = members.Where(member => member.PersonType != PersonTypes.Observer).ToList();
        return new VoteStatus
        {
            Votes = room.Votes.ToDictionary(vote => vote.Key, _ => RoomConstraints.HiddenVote),
            HasEveryoneVoted = voters.Count > 0 && voters.All(voter => room.Votes.ContainsKey(voter.Guid))
        };
    }
}
