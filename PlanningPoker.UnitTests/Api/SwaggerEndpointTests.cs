using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace PlanningPoker.UnitTests.Api;

public class SwaggerEndpointTests
{
    private const string SwaggerDocumentPath = "/swagger/v1/swagger.json";

    [Fact]
    public async Task SwaggerDocument_InProduction_IsNotFound()
    {
        using var factory = new ApiFactory();

        var response = await factory.CreateClient().GetAsync(SwaggerDocumentPath);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SwaggerDocument_InDevelopment_IsServed()
    {
        using var factory = new DevelopmentApiFactory();

        var response = await factory.CreateClient().GetAsync(SwaggerDocumentPath);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"openapi\"", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task SwaggerDocument_InDevelopment_ListsMeEndpointWithResponses()
    {
        using var factory = new DevelopmentApiFactory();

        var response = await factory.CreateClient().GetAsync(SwaggerDocumentPath);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var doc = await response.Content.ReadFromJsonAsync<JsonElement>();
        var me = doc.GetProperty("paths").GetProperty("/api/me");
        var put = me.GetProperty("put").GetProperty("responses");
        Assert.True(put.TryGetProperty("200", out _));
        Assert.True(put.TryGetProperty("400", out _));
        Assert.True(put.TryGetProperty("429", out _));
        var get = me.GetProperty("get").GetProperty("responses");
        Assert.True(get.TryGetProperty("200", out _));
        Assert.True(get.TryGetProperty("401", out _));
    }

    [Fact]
    public async Task SwaggerDocument_InDevelopment_DescribesTheRateLimitedResponseOfEveryLimitedEndpoint()
    {
        using var factory = new DevelopmentApiFactory();

        var doc = await (await factory.CreateClient().GetAsync(SwaggerDocumentPath)).Content.ReadFromJsonAsync<JsonElement>();

        var paths = doc.GetProperty("paths");
        Assert.True(paths.GetProperty("/api/me").GetProperty("put").GetProperty("responses").TryGetProperty("429", out _));
        Assert.True(paths.GetProperty("/api/rooms").GetProperty("post").GetProperty("responses").TryGetProperty("429", out _));
        Assert.True(paths.GetProperty("/api/rooms/{id}").GetProperty("get").GetProperty("responses").TryGetProperty("429", out _));
    }

    [Fact]
    public async Task SwaggerUi_InDevelopment_IsNotShadowedBySpaFallback()
    {
        using var factory = new DevelopmentApiFactory();

        var response = await factory.CreateClient().GetAsync("/swagger/index.html");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("spa-index", await response.Content.ReadAsStringAsync());
    }
}
