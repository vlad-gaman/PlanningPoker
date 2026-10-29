using PlanningPoker.Controller.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Controller.Mappers;

public interface IVoteStatisticsResponseMapper
{
    VoteStatisticsResponse Map(VoteStatistics statistics);
}
