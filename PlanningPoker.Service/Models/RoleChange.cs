namespace PlanningPoker.Service.Models;

/// <summary>What changing a member's role did to the round.</summary>
/// <param name="Changed">False when the member already had that role, in which case nothing changed.</param>
/// <param name="Decision">What the round decided once the role changed.</param>
public sealed record RoleChange(bool Changed, RoundDecision Decision);
