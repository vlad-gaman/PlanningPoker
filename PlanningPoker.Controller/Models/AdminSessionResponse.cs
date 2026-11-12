namespace PlanningPoker.Controller.Models;

public class AdminSessionResponse
{
    public bool Authenticated { get; init; }

    public string? Username { get; init; }

    /// <summary>The anti-forgery request token to send as the X-XSRF-TOKEN header on state-changing admin requests. Null when not signed in.</summary>
    public string? CsrfToken { get; init; }
}
