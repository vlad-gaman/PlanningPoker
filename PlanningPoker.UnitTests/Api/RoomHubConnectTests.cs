using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;

namespace PlanningPoker.UnitTests.Api;

public class RoomHubConnectTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public RoomHubConnectTests(ApiFactory factory)
    {
        _factory = factory;
    }

    private async Task<string> MintCookie()
    {
        var client = _factory.CreateClient();
        var response = await client.PutAsJsonAsync("/api/me", new { name = "Ann", personType = "dev" });
        return response.Headers.GetValues("Set-Cookie").Single().Split(';')[0];
    }

    private HubConnection BuildConnection(string? cookie)
    {
        return new HubConnectionBuilder()
            .WithUrl(new Uri(_factory.Server.BaseAddress, "/hubs/room"), options =>
            {
                options.Transports = HttpTransportType.LongPolling;
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                if (cookie is not null)
                {
                    options.Headers["Cookie"] = cookie;
                }
            })
            .Build();
    }

    [Fact]
    public async Task Negotiate_WithoutSession_Returns401()
    {
        var response = await _factory.CreateClient().PostAsync("/hubs/room/negotiate?negotiateVersion=1", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Connect_WithoutSession_IsRejected()
    {
        await using var connection = BuildConnection(null);

        await Assert.ThrowsAnyAsync<Exception>(() => connection.StartAsync());
    }

    [Fact]
    public async Task Connect_WithStaleSession_IsRejected()
    {
        await using var connection = BuildConnection($"pp_session={Guid.NewGuid()}");

        await Assert.ThrowsAnyAsync<Exception>(() => connection.StartAsync());
    }

    [Fact]
    public async Task Connect_WithCookieHoldingPublicId_IsRejected()
    {
        var guid = await PublicIdOf(await MintCookie());

        var response = await NegotiateWith($"pp_session={guid}");
        await using var connection = BuildConnection($"pp_session={guid}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("no_session", (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors")[0].GetProperty("code").GetString());
        await Assert.ThrowsAnyAsync<Exception>(() => connection.StartAsync());
    }

    [Fact]
    public async Task Negotiate_WithSessionToken_Succeeds()
    {
        var response = await NegotiateWith(await MintCookie());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task JoinRoomAndPersonJoined_DoNotCarryTheSessionToken()
    {
        var ownerCookie = await _factory.MintCookie("Ann");
        var guestCookie = await _factory.MintCookie("Bob", "test");
        var roomId = await _factory.CreateRoom(ownerCookie);
        await using var owner = BuildConnection(ownerCookie);
        await using var guest = BuildConnection(guestCookie);
        await owner.StartAsync();
        await guest.StartAsync();
        var ownerJoin = await owner.InvokeAsync<JsonElement>("JoinRoom", roomId);
        var guestJoinedEvent = owner.NextEvent("PersonJoined");

        var guestJoin = await guest.InvokeAsync<JsonElement>("JoinRoom", roomId);
        var guestJoined = await guestJoinedEvent;

        var payloads = string.Join(" ", ownerJoin.GetRawText(), guestJoin.GetRawText(), guestJoined.GetRawText());
        Assert.Contains(await PublicIdOf(guestCookie), payloads);
        Assert.DoesNotContain(TokenOf(ownerCookie), payloads);
        Assert.DoesNotContain(TokenOf(guestCookie), payloads);
    }

    private static string TokenOf(string cookie) => cookie["pp_session=".Length..];

    private async Task<string> PublicIdOf(string cookie)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/me");
        request.Headers.Add("Cookie", cookie);
        var body = await (await _factory.CreateClient().SendAsync(request)).Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("data").GetProperty("guid").GetString()!;
    }

    private async Task<HttpResponseMessage> NegotiateWith(string cookie)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/hubs/room/negotiate?negotiateVersion=1");
        request.Headers.Add("Cookie", cookie);
        return await _factory.CreateClient().SendAsync(request);
    }

    [Fact]
    public async Task Connect_WithSession_ConnectsAndDisconnects()
    {
        await using var connection = BuildConnection(await MintCookie());

        await connection.StartAsync();
        Assert.Equal(HubConnectionState.Connected, connection.State);

        await connection.StopAsync();
        Assert.Equal(HubConnectionState.Disconnected, connection.State);
    }
}
