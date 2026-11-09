using System.Net;

namespace PlanningPoker.UnitTests.Api;

[Collection("Metrics host")]
public class MetricsFailClosedTests(NoTokenMetricsApiFactory factory) : IClassFixture<NoTokenMetricsApiFactory>
{
    [Theory]
    [InlineData(null)]
    [InlineData("Bearer anything")]
    [InlineData("Bearer ")]
    public async Task Metrics_NoTokenConfiguredInProduction_Returns401(string? authorization)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/metrics");
        if (authorization is not null)
        {
            request.Headers.TryAddWithoutValidation("Authorization", authorization);
        }

        var response = await factory.CreateClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Metrics_NoTokenConfiguredInProduction_TheAppStillServesOtherRoutes()
    {
        var response = await factory.CreateClient().GetAsync("/api/card-sets");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
