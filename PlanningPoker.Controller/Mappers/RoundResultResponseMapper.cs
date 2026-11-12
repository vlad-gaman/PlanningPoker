using PlanningPoker.Controller.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Controller.Mappers;

public class RoundResultResponseMapper : IRoundResultResponseMapper
{
    public RoundResultResponse Map(RoundResult result)
    {
        return new RoundResultResponse { Round = result.Round, AverageMark = result.AverageMark, RevealedAt = result.RevealedAt };
    }
}
