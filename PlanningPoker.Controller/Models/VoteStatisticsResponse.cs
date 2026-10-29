namespace PlanningPoker.Controller.Models;

public class VoteStatisticsResponse
{
    public GroupStatisticsResponse All { get; init; } = default!;

    public GroupStatisticsResponse Developers { get; init; } = default!;

    public GroupStatisticsResponse Testers { get; init; } = default!;

    /// <summary>At least two votes, all the same mark, and everyone had voted.</summary>
    public bool Consensus { get; init; }
}
