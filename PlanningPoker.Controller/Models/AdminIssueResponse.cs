namespace PlanningPoker.Controller.Models;

public class AdminIssueResponse
{
    public long Id { get; init; }

    public DateTimeOffset Time { get; init; }

    /// <summary>"info", "warning" or "error".</summary>
    public string Level { get; init; } = default!;

    public string Source { get; init; } = default!;

    public string Message { get; init; } = default!;
}
