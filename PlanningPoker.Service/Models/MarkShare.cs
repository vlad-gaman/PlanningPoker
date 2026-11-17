namespace PlanningPoker.Service.Models;

public class MarkShare
{
    public string Mark { get; init; } = default!;

    /// <summary>Share of the group's votes, 0 to 100, rounded to one decimal.</summary>
    public decimal Percentage { get; init; }
}
