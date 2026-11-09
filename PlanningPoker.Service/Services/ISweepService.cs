using PlanningPoker.Service.Models;

namespace PlanningPoker.Service.Services;

public interface ISweepService
{
    /// <summary>
    /// Removes abandoned rooms, then abandoned people. A room is abandoned when it has no open connections and was created
    /// longer ago than the configured time. A person is abandoned when they have no open connections, no surviving room
    /// references them, and they were last seen longer ago than the configured time. Returns how many of each were removed.
    /// </summary>
    SweepResult Sweep();
}
