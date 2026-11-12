namespace PlanningPoker.Controller.Models;

/// <summary>One revealed round in the room's history.</summary>
public class RoundResultResponse
{
    public int Round { get; init; }

    /// <summary>Null (shown as a dash) when no vote was numeric.</summary>
    public decimal? AverageMark { get; init; }

    public DateTimeOffset RevealedAt { get; init; }
}
