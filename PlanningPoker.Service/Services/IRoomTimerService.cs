namespace PlanningPoker.Service.Services;

/// <summary>Ticks a room reveal countdown once a second. It holds no room state: every tick is decided by <see cref="IRoomService.Tick"/>.</summary>
public interface IRoomTimerService
{
    /// <summary>Starts ticking the countdown that began at <paramref name="revision"/>, replacing an older one. Ignored when a newer one is already ticking.</summary>
    void Start(string roomId, long revision);
}
