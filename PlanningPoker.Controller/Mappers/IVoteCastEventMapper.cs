using PlanningPoker.Controller.Models;

namespace PlanningPoker.Controller.Mappers;

public interface IVoteCastEventMapper
{
    VoteCastEvent Map(Guid personGuid);
}
