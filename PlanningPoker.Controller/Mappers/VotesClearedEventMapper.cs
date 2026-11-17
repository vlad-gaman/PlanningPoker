using PlanningPoker.Controller.Models;

namespace PlanningPoker.Controller.Mappers;

public class VotesClearedEventMapper : IVotesClearedEventMapper
{
    public VotesClearedEvent Map(DateTimeOffset roundStartedAt)
    {
        return new VotesClearedEvent { RoundStartedAt = roundStartedAt };
    }
}
