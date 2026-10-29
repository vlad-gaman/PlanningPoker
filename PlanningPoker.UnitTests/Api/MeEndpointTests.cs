using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace PlanningPoker.UnitTests.Api;

public class MeEndpointTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public MeEndpointTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    private static HttpRequestMessage Put(object body, string? cookie = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, "/api/me") { Content = JsonContent.Create(body) };
        if (cookie is not null)
        {
            request.Headers.Add("Cookie", cookie);
        }
        return request;
    }

    private static string CookieValue(HttpResponseMessage response)
    {
        var header = Assert.Single(response.Headers.GetValues("Set-Cookie"));
        return header.Split(';')[0];
    }

    [Fact]
    public async Task Get_WithoutCookie_Returns401NoSession()
    {
        var response = await _client.GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("no_session", body.GetProperty("errors")[0].GetProperty("code").GetString());
    }

    [Fact]
    public async Task Get_StaleCookie_Returns401()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/me");
        request.Headers.Add("Cookie", $"pp_session={Guid.NewGuid()}");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Put_NoSession_MintsCookieAndReturnsPerson()
    {
        var response = await _client.SendAsync(Put(new { name = "Ann", personType = "dev" }));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var cookie = Assert.Single(response.Headers.GetValues("Set-Cookie")).ToLowerInvariant();
        Assert.Contains("pp_session=", cookie);
        Assert.Contains("httponly", cookie);
        Assert.Contains("secure", cookie);
        Assert.Contains("samesite=lax", cookie);
        var data = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");
        Assert.Equal("Ann", data.GetProperty("name").GetString());
        Assert.Equal("dev", data.GetProperty("personType").GetString());
        Assert.Contains(data.GetProperty("guid").GetString()!, cookie);
    }

    [Fact]
    public async Task Get_AfterPut_ReturnsSamePerson()
    {
        var put = await _client.SendAsync(Put(new { name = "Ann", personType = "test" }));
        var cookie = CookieValue(put);

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/me");
        request.Headers.Add("Cookie", cookie);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var data = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");
        Assert.Equal("Ann", data.GetProperty("name").GetString());
        Assert.Equal("test", data.GetProperty("personType").GetString());
    }

    [Fact]
    public async Task Put_ExistingSession_UpdatesWithoutNewCookie()
    {
        var first = await _client.SendAsync(Put(new { name = "Ann", personType = "dev" }));
        var cookie = CookieValue(first);

        var second = await _client.SendAsync(Put(new { name = "Bob", personType = "obs" }, cookie));

        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.False(second.Headers.Contains("Set-Cookie"));
        var data = (await second.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");
        Assert.Equal("Bob", data.GetProperty("name").GetString());
    }

    [Fact]
    public async Task Put_StaleCookie_ReplacesCookie()
    {
        var stale = $"pp_session={Guid.NewGuid()}";

        var response = await _client.SendAsync(Put(new { name = "Ann", personType = "dev" }, stale));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotEqual(stale, CookieValue(response));
    }

    [Fact]
    public async Task Put_InvalidName_Returns400ValidationFailed()
    {
        var response = await _client.SendAsync(Put(new { name = "A", personType = "dev" }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors")[0];
        Assert.Equal("validation_failed", error.GetProperty("code").GetString());
        Assert.Equal("name", error.GetProperty("metadata").GetProperty("field").GetString());
        Assert.False(response.Headers.Contains("Set-Cookie"));
    }

    [Fact]
    public async Task Put_MalformedJson_Returns400ApiResponse()
    {
        var request = new HttpRequestMessage(HttpMethod.Put, "/api/me") { Content = new StringContent("{not json", Encoding.UTF8, "application/json") };

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("validation_failed", body.GetProperty("errors")[0].GetProperty("code").GetString());
    }

    [Fact]
    public async Task Put_NonJsonBody_Returns415()
    {
        var request = new HttpRequestMessage(HttpMethod.Put, "/api/me") { Content = new StringContent("name=Ann", Encoding.UTF8, "application/x-www-form-urlencoded") };

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("unsupported_media_type", body.GetProperty("errors")[0].GetProperty("code").GetString());
    }
}
