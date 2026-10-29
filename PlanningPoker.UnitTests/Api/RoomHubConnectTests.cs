using System.Net;
using System.Net.Http.Json;
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
    public async Task Connect_WithSession_ConnectsAndDisconnects()
    {
        await using var connection = BuildConnection(await MintCookie());

        await connection.StartAsync();
        Assert.Equal(HubConnectionState.Connected, connection.State);

        await connection.StopAsync();
        Assert.Equal(HubConnectionState.Disconnected, connection.State);
    }
}
