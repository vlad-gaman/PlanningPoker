using PlanningPoker.Controller.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Controller.Mappers;

public class VoteStatisticsResponseMapper(IGroupStatisticsResponseMapper groupMapper) : IVoteStatisticsResponseMapper
{
    public VoteStatisticsResponse Map(VoteStatistics statistics)
    {
        return new VoteStatisticsResponse
        {
            All = groupMapper.Map(statistics.All),
            Developers = groupMapper.Map(statistics.Developers),
            Testers = groupMapper.Map(statistics.Testers),
            Consensus = statistics.Consensus
        };
    }
}
