using PlanningPoker.Controller.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Controller.Mappers;

public interface IRoundResultResponseMapper
{
    RoundResultResponse Map(RoundResult result);
}
