using PlanningPoker.Service.Models;

namespace PlanningPoker.Service.Services;

/// <summary>Carries out what a change led to and ticks a room reveal countdown once a second. It holds no room state: every tick is decided by <see cref="IRoomService.Tick"/>.</summary>
public interface IRoomTimerService
{
    /// <summary>
    /// Announces an immediate reveal, or starts ticking the countdown (replacing an older one, ignored when a newer one is already ticking)
    /// and sends its first second. Does nothing for <see cref="VotingFollowUp.None"/>.
    /// </summary>
    Task ApplyAsync(string roomId, VotingFollowUp followUp);
}
