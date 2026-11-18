namespace PlanningPoker.Service.Models;

/// <summary>What one tick of a countdown did to the round.</summary>
/// <param name="Outcome">Stale when the countdown no longer runs, Ticked while it counts down, Revealed on its last tick.</param>
/// <param name="Remaining">The seconds left; 0 unless the countdown ticked.</param>
public sealed record RoundTick(TickOutcome Outcome, int Remaining);
