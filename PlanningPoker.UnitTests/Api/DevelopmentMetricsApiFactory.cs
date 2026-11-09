using Microsoft.Extensions.Hosting;

namespace PlanningPoker.UnitTests.Api;

/// <summary>Development without a scrape token: /metrics is open.</summary>
public class DevelopmentMetricsApiFactory() : MetricsApiFactory(null, Environments.Development);
