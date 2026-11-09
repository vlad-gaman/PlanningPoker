namespace PlanningPoker.Controller.Models;

public class MetricsOptions
{
    /// <summary>The Bearer token a scraper must send to /metrics. Never stored in appsettings files.</summary>
    public string? ScrapeToken { get; set; }
}
