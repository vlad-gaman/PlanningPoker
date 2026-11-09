using System.Net;

namespace PlanningPoker.UnitTests.Api;

public class MetricsDevelopmentTests(DevelopmentMetricsApiFactory factory) : IClassFixture<DevelopmentMetricsApiFactory>
{
    [Fact]
    public async Task Metrics_NoTokenConfiguredInDevelopment_IsOpen()
    {
        var response = await factory.CreateClient().GetAsync("/metrics");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("planningpoker_rooms_in_memory", await response.Content.ReadAsStringAsync());
    }
}
