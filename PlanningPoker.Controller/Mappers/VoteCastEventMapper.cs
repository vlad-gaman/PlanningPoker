using PlanningPoker.Controller.Models;

namespace PlanningPoker.Controller.Mappers;

public class VoteCastEventMapper : IVoteCastEventMapper
{
    public VoteCastEvent Map(Guid personGuid)
    {
        return new VoteCastEvent { PersonGuid = personGuid };
    }
}
