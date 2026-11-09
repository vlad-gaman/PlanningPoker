using PlanningPoker.Controller.Models;

namespace PlanningPoker.Controller.Mappers;

public interface IVotesClearedEventMapper
{
    VotesClearedEvent Map(DateTimeOffset roundStartedAt);
}
