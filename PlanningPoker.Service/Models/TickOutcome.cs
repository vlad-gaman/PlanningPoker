namespace PlanningPoker.Service.Models;

public enum TickOutcome
{
    /// <summary>The countdown no longer runs: a later change cancelled it.</summary>
    Stale,
    Ticked,
    Revealed
}
