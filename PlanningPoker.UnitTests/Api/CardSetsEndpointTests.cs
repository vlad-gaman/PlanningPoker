using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace PlanningPoker.UnitTests.Api;

public class CardSetsEndpointTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Get_ReturnsTheSevenCardSets()
    {
        var response = await factory.CreateClient().GetAsync("/api/card-sets");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var data = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");
        Assert.Equal(7, data.GetArrayLength());
        Assert.Equal("fibonacci", data[0].GetProperty("value").GetString());
        Assert.Equal("Fibonacci", data[0].GetProperty("display").GetString());
    }
}
