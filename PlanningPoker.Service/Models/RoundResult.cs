namespace PlanningPoker.Service.Models;

/// <summary>One revealed round of a room: its number and the average of its votes.</summary>
public class RoundResult
{
    public int Round { get; init; }

    /// <summary>Null (shown as a dash) when no vote was numeric.</summary>
    public decimal? AverageMark { get; init; }

    public DateTimeOffset RevealedAt { get; init; }
}
