namespace PlanningPoker.Service.Models;

public class GroupStatistics
{
    /// <summary>The marks that were cast, in card-set order; empty for a group without votes.</summary>
    public IReadOnlyList<MarkShare> Marks { get; init; } = default!;

    /// <summary>The mark with the strictly highest share; null for a tie or an empty group.</summary>
    public string? HighestMark { get; init; }

    /// <summary>Mean of the numeric marks, rounded to two decimals; null when none is numeric.</summary>
    public decimal? AverageMark { get; init; }
}
