namespace PlanningPoker.Repository.Models;

/// <summary>One revealed round in a room's history. Never holds a person's vote.</summary>
public class RoundResultEntity
{
    /// <summary>Which revealed round of the room this was: 1 for the first reveal, 2 for the second, and so on.</summary>
    public int Round { get; set; }

    /// <summary>Mean of the numeric marks of everyone who voted, rounded to two decimals; null when no mark was numeric.</summary>
    public decimal? AverageMark { get; set; }

    public DateTimeOffset RevealedAt { get; set; }
}
