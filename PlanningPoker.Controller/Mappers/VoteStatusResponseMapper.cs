using PlanningPoker.Controller.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Controller.Mappers;

public class VoteStatusResponseMapper(IVoteStatisticsResponseMapper statisticsMapper, IRoundResultResponseMapper roundResultMapper) : IVoteStatusResponseMapper
{
    public VoteStatusResponse Map(VoteStatus voteStatus)
    {
        return new VoteStatusResponse
        {
            Votes = voteStatus.Votes,
            HasEveryoneVoted = voteStatus.HasEveryoneVoted,
            VotingFinished = voteStatus.VotingFinished,
            Countdown = voteStatus.Countdown,
            RoundStartedAt = voteStatus.RoundStartedAt,
            Statistics = voteStatus.Statistics is null ? null : statisticsMapper.Map(voteStatus.Statistics),
            RoundHistory = voteStatus.RoundHistory?.Select(roundResultMapper.Map).ToList()
        };
    }
}
