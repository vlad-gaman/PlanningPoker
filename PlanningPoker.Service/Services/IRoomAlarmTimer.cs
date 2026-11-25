namespace PlanningPoker.Service.Services;

/// <summary>Wakes up at a room's Alarm end time to elapse it and tell the room. It holds no room state: the room decides whether the wake-up is stale.</summary>
public interface IRoomAlarmTimer
{
    /// <summary>Waits until <paramref name="endsAt"/>, replacing any older wake-up of the room.</summary>
    void Schedule(string roomId, DateTimeOffset endsAt);

    /// <summary>Drops the room's pending wake-up, if any.</summary>
    void Cancel(string roomId);
}
