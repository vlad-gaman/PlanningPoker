using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace PlanningPoker.UnitTests.Api;

public class PresenceEndpointTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public PresenceEndpointTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
    }

    private Task<HttpResponseMessage> PostClosing(object body, string? cookie)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/presence/closing") { Content = JsonContent.Create(body) };
        if (cookie is not null)
        {
            request.Headers.Add("Cookie", cookie);
        }

        return _client.SendAsync(request);
    }

    private static async Task<string?> FirstErrorCode(HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("errors")[0].GetProperty("code").GetString();
    }

    [Theory]
    [InlineData("text/plain")]
    [InlineData("application/x-www-form-urlencoded")]
    public async Task Closing_CrossSiteStyleContentType_Returns415AndChangesNothing(string contentType)
    {
        var cookie = await _factory.MintCookie();
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/presence/closing")
        {
            Content = new StringContent("{\"connectionId\":\"abc\"}", System.Text.Encoding.UTF8, contentType)
        };
        request.Headers.Add("Cookie", cookie);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
        Assert.Equal("unsupported_media_type", await FirstErrorCode(response));
    }

    [Fact]
    public async Task Closing_WithoutSession_Returns401NoSession()
    {
        var response = await PostClosing(new { connectionId = "abc" }, cookie: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("no_session", await FirstErrorCode(response));
    }

    [Fact]
    public async Task Closing_WithoutConnectionId_Returns400ValidationFailed()
    {
        var cookie = await _factory.MintCookie();

        var response = await PostClosing(new { connectionId = "" }, cookie);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("validation_failed", await FirstErrorCode(response));
    }

    [Fact]
    public async Task Closing_UnknownConnection_ReturnsNotInRoom()
    {
        var cookie = await _factory.MintCookie();

        var response = await PostClosing(new { connectionId = "nope" }, cookie);

        Assert.Equal("not_in_room", await FirstErrorCode(response));
    }
}
