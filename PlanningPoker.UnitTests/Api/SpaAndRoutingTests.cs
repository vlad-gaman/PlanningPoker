using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace PlanningPoker.UnitTests.Api;

public class SpaAndRoutingTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public SpaAndRoutingTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/room/xyz")]
    [InlineData("/Room/xyz")]
    [InlineData("/credits")]
    [InlineData("/Credits")]
    public async Task SpaRoutes_ServeIndexHtml(string path)
    {
        var response = await _client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("spa-index", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("/api/unknown")]
    [InlineData("/api/unknown/deeper")]
    public async Task UnknownBackendRoutes_ReturnJsonNotFound_NotIndexHtml(string path)
    {
        var response = await _client.GetAsync(path);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.StartsWith("application/json", response.Content.Headers.ContentType!.MediaType);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("not_found", body.GetProperty("errors")[0].GetProperty("code").GetString());
    }

    [Fact]
    public async Task UnknownHubRoute_WithSession_ReturnsJsonNotFound()
    {
        var put = await _client.PutAsJsonAsync("/api/me", new { name = "Ann", personType = "dev" });
        var request = new HttpRequestMessage(HttpMethod.Get, "/hubs/unknown");
        request.Headers.Add("Cookie", put.Headers.GetValues("Set-Cookie").Single().Split(';')[0]);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("not_found", body.GetProperty("errors")[0].GetProperty("code").GetString());
    }

    [Fact]
    public async Task Swagger_ListsMeEndpointWithResponses()
    {
        var response = await _client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var doc = await response.Content.ReadFromJsonAsync<JsonElement>();
        var me = doc.GetProperty("paths").GetProperty("/api/me");
        var put = me.GetProperty("put").GetProperty("responses");
        Assert.True(put.TryGetProperty("200", out _));
        Assert.True(put.TryGetProperty("400", out _));
        var get = me.GetProperty("get").GetProperty("responses");
        Assert.True(get.TryGetProperty("200", out _));
        Assert.True(get.TryGetProperty("401", out _));
    }

    [Fact]
    public async Task SwaggerUi_IsNotShadowedBySpaFallback()
    {
        var response = await _client.GetAsync("/swagger/index.html");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("spa-index", await response.Content.ReadAsStringAsync());
    }
}
