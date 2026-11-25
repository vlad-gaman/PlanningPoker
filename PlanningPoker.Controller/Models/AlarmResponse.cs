namespace PlanningPoker.Controller.Models;

public class AlarmResponse
{
    /// <summary><c>idle</c>, <c>running</c> or <c>elapsed</c>.</summary>
    public string Status { get; init; } = default!;

    /// <summary>When the Alarm elapses or elapsed, in UTC; null while it is idle.</summary>
    public DateTimeOffset? EndsAt { get; init; }

    /// <summary>How long a running Alarm still has, in milliseconds as of this message; null otherwise. A client counts down from it, so its own clock never has to agree with the server's.</summary>
    public long? RemainingMilliseconds { get; init; }
}
