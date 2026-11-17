namespace PlanningPoker.Service.Models;

public class VoteStatistics
{
    public GroupStatistics All { get; init; } = default!;

    public GroupStatistics Developers { get; init; } = default!;

    public GroupStatistics Testers { get; init; } = default!;

    public GroupStatistics Sres { get; init; } = default!;

    /// <summary>How many votes were counted when the round was revealed; later joins and role changes do not move it.</summary>
    public int VoteCount { get; init; }

    /// <summary>At least two votes, all the same mark, and everyone had voted: the fireworks case.</summary>
    public bool Consensus { get; init; }
}
