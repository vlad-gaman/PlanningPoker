namespace PlanningPoker.UnitTests.Api;

/// <summary>Production without a scrape token: /metrics fails closed.</summary>
public class NoTokenMetricsApiFactory() : MetricsApiFactory(null);
