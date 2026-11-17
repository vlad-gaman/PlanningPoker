using System.Net;
using System.Net.Http.Json;
using PlanningPoker.Common.Constants;

namespace PlanningPoker.UnitTests.Api;

public class RateLimitingDisabledTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public RateLimitingDisabledTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task WithRateLimitingDisabled_AnAddressMayExceedEveryDefaultLimit()
    {
        var client = _factory.CreateClient();

        for (var i = 0; i < RateLimitingConstants.RoomLookupLimit * 2; i++)
        {
            Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync("/api/me", new { name = "Ann", personType = "dev" })).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/rooms/no-such-room")).StatusCode);
        }
    }
}
