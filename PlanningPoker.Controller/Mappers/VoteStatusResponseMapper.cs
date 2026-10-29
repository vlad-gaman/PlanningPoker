using PlanningPoker.Controller.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Controller.Mappers;

public class VoteStatusResponseMapper : IVoteStatusResponseMapper
{
    public VoteStatusResponse Map(VoteStatus voteStatus)
    {
        return new VoteStatusResponse { Votes = voteStatus.Votes, HasEveryoneVoted = voteStatus.HasEveryoneVoted };
    }
}
