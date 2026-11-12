using PlanningPoker.Repository.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Service.Mappers;

public class RoundResultMapper : IRoundResultMapper
{
    public IReadOnlyList<RoundResult> MapNewestFirst(IReadOnlyList<RoundResultEntity> history)
    {
        return [.. history.Reverse().Select(entry => new RoundResult { Round = entry.Round, AverageMark = entry.AverageMark, RevealedAt = entry.RevealedAt })];
    }
}
