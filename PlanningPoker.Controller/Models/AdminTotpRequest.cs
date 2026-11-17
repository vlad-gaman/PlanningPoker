namespace PlanningPoker.Controller.Models;

public class AdminTotpRequest
{
    /// <summary>The challenge returned with <c>admin_totp_required</c>.</summary>
    public string Challenge { get; init; } = default!;

    /// <summary>The 6-digit code from the authenticator app.</summary>
    public string Code { get; init; } = default!;
}
