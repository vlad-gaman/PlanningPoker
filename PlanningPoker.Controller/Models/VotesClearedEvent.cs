namespace PlanningPoker.Controller.Models;

public class VotesClearedEvent
{
    /// <summary>When the current round began, in UTC: the moment of the clear, or the unchanged start when a settings change dropped the votes.</summary>
    public DateTimeOffset RoundStartedAt { get; init; }
}
