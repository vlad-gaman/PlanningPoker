namespace PlanningPoker.Service.Models;

public class VoteStatistics
{
    public GroupStatistics All { get; init; } = default!;

    public GroupStatistics Developers { get; init; } = default!;

    public GroupStatistics Testers { get; init; } = default!;

    /// <summary>At least two votes, all the same mark, and everyone had voted: the fireworks case.</summary>
    public bool Consensus { get; init; }
}
