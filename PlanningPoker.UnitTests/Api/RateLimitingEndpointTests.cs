using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using PlanningPoker.Common.Constants;

namespace PlanningPoker.UnitTests.Api;

/// <summary>
/// The real default limits, on a host that has not overridden any of them. Every test works from its own client address
/// (sent as <c>X-Forwarded-For</c>, which the trusted forwarded headers turn into the address the limiter partitions on), so the
/// tests share one host without sharing a window.
/// </summary>
public class RateLimitingEndpointTests : IClassFixture<RateLimitedApiFactory>
{
    private static int _nextHost;
    private readonly RateLimitedApiFactory _factory;
    private readonly HttpClient _client;

    public RateLimitingEndpointTests(RateLimitedApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
    }

    private static string NewAddress()
    {
        return $"198.51.100.{Interlocked.Increment(ref _nextHost)}";
    }

    private static HttpRequestMessage Request(HttpMethod method, string url, string address, object? body = null, string? cookie = null)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add("X-Forwarded-For", address);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        if (cookie is not null)
        {
            request.Headers.Add("Cookie", cookie);
        }

        return request;
    }

    private Task<HttpResponseMessage> PutMe(string address)
    {
        return _client.SendAsync(Request(HttpMethod.Put, "/api/me", address, new { name = "Ann", personType = "dev" }));
    }

    private Task<HttpResponseMessage> PostRoom(string address, string cookie)
    {
        return _client.SendAsync(Request(HttpMethod.Post, "/api/rooms", address, new { useFunRoomName = false, configuration = TestSession.DefaultConfiguration }, cookie));
    }

    private async Task<string> Cookie()
    {
        var response = await PutMe(NewAddress());
        return response.Headers.GetValues("Set-Cookie").Single().Split(';')[0];
    }

    private static async Task AssertRateLimited(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.True(response.Headers.Contains("Retry-After"));
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Null, body.GetProperty("data").ValueKind);
        var error = Assert.Single(body.GetProperty("errors").EnumerateArray());
        Assert.Equal(ErrorCodes.RateLimited, error.GetProperty("code").GetString());
        Assert.False(string.IsNullOrWhiteSpace(error.GetProperty("message").GetString()));
    }

    [Fact]
    public async Task PutMe_AfterTenRequestsInAMinute_Returns429RateLimited()
    {
        var address = NewAddress();
        for (var i = 0; i < RateLimitingConstants.MeUpdateLimit; i++)
        {
            Assert.Equal(HttpStatusCode.OK, (await PutMe(address)).StatusCode);
        }

        await AssertRateLimited(await PutMe(address));
    }

    [Fact]
    public async Task PostRooms_AfterTenRequestsInAMinute_Returns429RateLimited()
    {
        var cookie = await Cookie();
        var address = NewAddress();
        for (var i = 0; i < RateLimitingConstants.RoomCreateLimit; i++)
        {
            Assert.Equal(HttpStatusCode.Created, (await PostRoom(address, cookie)).StatusCode);
        }

        await AssertRateLimited(await PostRoom(address, cookie));
    }

    [Fact]
    public async Task GetRoom_AfterThirtyRequestsInAMinute_Returns429RateLimited()
    {
        var address = NewAddress();
        for (var i = 0; i < RateLimitingConstants.RoomLookupLimit; i++)
        {
            Assert.Equal(HttpStatusCode.NotFound, (await _client.SendAsync(Request(HttpMethod.Get, "/api/rooms/no-such-room", address))).StatusCode);
        }

        await AssertRateLimited(await _client.SendAsync(Request(HttpMethod.Get, "/api/rooms/no-such-room", address)));
    }

    [Fact]
    public async Task PostClosing_AfterSixtyRequestsInAMinute_Returns429RateLimited()
    {
        var cookie = await Cookie();
        var address = NewAddress();
        Task<HttpResponseMessage> Closing() => _client.SendAsync(Request(HttpMethod.Post, "/api/presence/closing", address, new { connectionId = "nope" }, cookie));
        for (var i = 0; i < RateLimitingConstants.PresenceClosingLimit; i++)
        {
            // The unknown connection is refused, but each call counts against the limit.
            Assert.NotEqual(HttpStatusCode.TooManyRequests, (await Closing()).StatusCode);
        }

        await AssertRateLimited(await Closing());
    }

    [Fact]
    public async Task Limits_ArePerClientAddress_AfterTheForwardedHeadersAreApplied()
    {
        var exhausted = NewAddress();
        for (var i = 0; i < RateLimitingConstants.MeUpdateLimit; i++)
        {
            await PutMe(exhausted);
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, (await PutMe(exhausted)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await PutMe(NewAddress())).StatusCode);
    }

    [Fact]
    public async Task Limits_AreSeparatePerEndpoint()
    {
        var address = NewAddress();
        for (var i = 0; i < RateLimitingConstants.MeUpdateLimit; i++)
        {
            await PutMe(address);
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, (await PutMe(address)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.SendAsync(Request(HttpMethod.Get, "/api/rooms/no-such-room", address))).StatusCode);
    }

    [Fact]
    public async Task OtherEndpoints_AreNotLimited()
    {
        var address = NewAddress();
        for (var i = 0; i < RateLimitingConstants.RoomLookupLimit * 2; i++)
        {
            Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(Request(HttpMethod.Get, "/api/card-sets", address))).StatusCode);
        }
    }

    [Fact]
    public async Task JoinRoom_AfterTenFailedJoinsInAMinute_ReturnsRateLimited()
    {
        var address = NewAddress();
        var cookie = await Cookie();
        var knownRoom = await _factory.CreateRoom(cookie);
        await using var connection = Connect(cookie, address);
        await connection.StartAsync();

        for (var i = 0; i < RateLimitingConstants.FailedJoinLimit; i++)
        {
            Assert.Equal(ErrorCodes.RoomNotFound, await JoinErrorCode(connection, "no-such-room"));
        }

        Assert.Equal(ErrorCodes.RateLimited, await JoinErrorCode(connection, knownRoom));
    }

    [Fact]
    public async Task JoinRoom_SuccessfulJoins_AreNeverLimited()
    {
        var address = NewAddress();
        var cookie = await Cookie();
        var room = await _factory.CreateRoom(cookie);
        await using var connection = Connect(cookie, address);
        await connection.StartAsync();

        for (var i = 0; i < RateLimitingConstants.FailedJoinLimit * 2; i++)
        {
            Assert.Null(await JoinErrorCode(connection, room));
        }
    }

    private HubConnection Connect(string cookie, string address)
    {
        return new HubConnectionBuilder()
            .WithUrl(new Uri(_factory.Server.BaseAddress, "/hubs/room"), options =>
            {
                options.Transports = HttpTransportType.LongPolling;
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                options.Headers["Cookie"] = cookie;
                options.Headers["X-Forwarded-For"] = address;
            })
            .Build();
    }

    private static async Task<string?> JoinErrorCode(HubConnection connection, string roomId)
    {
        var response = await connection.InvokeAsync<JsonElement>("JoinRoom", roomId);
        var errors = response.GetProperty("errors");
        return errors.ValueKind == JsonValueKind.Array ? errors[0].GetProperty("code").GetString() : null;
    }
}
