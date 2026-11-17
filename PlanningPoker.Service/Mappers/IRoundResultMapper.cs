using PlanningPoker.Repository.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Service.Mappers;

public interface IRoundResultMapper
{
    /// <summary>The stored history (oldest first) as the list clients show: newest first.</summary>
    IReadOnlyList<RoundResult> MapNewestFirst(IReadOnlyList<RoundResultEntity> history);
}
