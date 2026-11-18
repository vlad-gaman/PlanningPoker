namespace PlanningPoker.Controller.Models;

public class AdminTotpRequest
{
    /// <summary>The challenge returned with <c>admin_totp_required</c>.</summary>
    public string Challenge { get; init; } = default!;

    /// <summary>The 6-digit code from the authenticator app.</summary>
    public string Code { get; init; } = default!;

    /// <summary>What the browser has saved about voting (name, role, room settings), as the browser reports it. Optional and never trusted.</summary>
    public string? BrowserData { get; init; }
}
