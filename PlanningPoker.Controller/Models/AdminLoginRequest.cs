namespace PlanningPoker.Controller.Models;

public class AdminLoginRequest
{
    public string Username { get; init; } = default!;

    public string Password { get; init; } = default!;

    /// <summary>What the browser has saved about voting (name, role, room settings), as the browser reports it. Optional and never trusted.</summary>
    public string? BrowserData { get; init; }
}
