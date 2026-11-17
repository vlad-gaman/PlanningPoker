namespace PlanningPoker.Service.Models;

public class TickResult
{
    public TickOutcome Outcome { get; init; }

    public int Remaining { get; init; }

    /// <summary>Set when <see cref="Outcome"/> is <see cref="TickOutcome.Revealed"/>.</summary>
    public VoteStatus? Revealed { get; init; }
}
