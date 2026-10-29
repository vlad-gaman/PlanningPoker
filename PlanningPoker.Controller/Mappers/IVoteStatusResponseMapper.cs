using PlanningPoker.Controller.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Controller.Mappers;

public interface IVoteStatusResponseMapper
{
    VoteStatusResponse Map(VoteStatus voteStatus);
}
