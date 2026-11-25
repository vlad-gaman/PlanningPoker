namespace PlanningPoker.Service.Models;

/// <summary>Where a room's Alarm is: not started, counting down, or run out and waiting for a reset.</summary>
public enum AlarmStatus
{
    Idle,
    Running,
    Elapsed
}
