namespace PlanningPoker.Service.Models;

/// <summary>What <see cref="Services.IRoomMembership.Join"/> decided.</summary>
/// <param name="Admitted">False when the room is disposed and refuses new members.</param>
/// <param name="PersonAdded">True when the person was new to the room.</param>
/// <param name="WasOffgrid">True when the person came back from being offgrid.</param>
/// <param name="ResetCountdown">True when a running countdown no longer applies to the group.</param>
public sealed record MembershipJoined(bool Admitted, bool PersonAdded, bool WasOffgrid, bool ResetCountdown);
