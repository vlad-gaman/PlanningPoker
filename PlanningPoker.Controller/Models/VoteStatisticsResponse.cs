namespace PlanningPoker.Controller.Models;

public class VoteStatisticsResponse
{
    public GroupStatisticsResponse All { get; init; } = default!;

    public GroupStatisticsResponse Developers { get; init; } = default!;

    public GroupStatisticsResponse Testers { get; init; } = default!;

    public GroupStatisticsResponse Sres { get; init; } = default!;

    /// <summary>How many votes were counted when the round was revealed; later joins and role changes do not move it.</summary>
    public int VoteCount { get; init; }

    /// <summary>At least two votes, all the same mark, and everyone had voted.</summary>
    public bool Consensus { get; init; }
}
