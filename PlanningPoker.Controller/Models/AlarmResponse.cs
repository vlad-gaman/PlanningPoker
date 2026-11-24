namespace PlanningPoker.Controller.Models;

public class AlarmResponse
{
    /// <summary><c>idle</c>, <c>running</c> or <c>elapsed</c>.</summary>
    public string Status { get; init; } = default!;

    /// <summary>When the Alarm elapses or elapsed, in UTC; null while it is idle.</summary>
    public DateTimeOffset? EndsAt { get; init; }
}
