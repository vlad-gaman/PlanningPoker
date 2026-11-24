namespace PlanningPoker.Service.Models;

public class AlarmState
{
    public AlarmStatus Status { get; init; }

    /// <summary>When the Alarm elapses or elapsed, in UTC; null while it is idle.</summary>
    public DateTimeOffset? EndsAt { get; init; }
}
