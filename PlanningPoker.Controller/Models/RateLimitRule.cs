using PlanningPoker.Common.Constants;

namespace PlanningPoker.Controller.Models;

/// <summary>One fixed-window limit: how many requests one client address may make in each window.</summary>
public class RateLimitRule
{
    public int PermitLimit { get; set; }

    public TimeSpan Window { get; set; } = RateLimitingConstants.Window;
}
