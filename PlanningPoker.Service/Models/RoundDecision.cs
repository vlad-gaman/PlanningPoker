namespace PlanningPoker.Service.Models;

/// <summary>What the round decided after a change: start a countdown, reveal at once, or do nothing.</summary>
/// <param name="Countdown">The countdown the caller must now tick; null when none started.</param>
/// <param name="Revealed">True when the change revealed the votes at once (a countdown of 0).</param>
public sealed record RoundDecision(CountdownStart? Countdown, bool Revealed)
{
    public static readonly RoundDecision None = new(null, false);

    public bool IsNone => Countdown is null && !Revealed;
}
