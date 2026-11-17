namespace PlanningPoker.Common.Models;

public class CardOption
{
    public string Value { get; init; } = default!;

    /// <summary>What to show instead of the value (an emoji or a PNG file name); null when the value is shown as is.</summary>
    public string? Display { get; init; }
}
