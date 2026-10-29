using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;

namespace PlanningPoker.UnitTests.Api;

/// <summary>Helpers that act as a browser: mint a session cookie, create rooms and open hub connections.</summary>
public static class TestSession
{
    public static readonly object DefaultConfiguration = new { cardSet = "linear", countdownSeconds = 5, showFireworks = true, maxParticipants = 50 };

    public static async Task<string> MintCookie(this ApiFactory factory, string name = "Ann", string personType = "dev")
    {
        var response = await factory.CreateClient().PutAsJsonAsync("/api/me", new { name, personType });
        return response.Headers.GetValues("Set-Cookie").Single().Split(';')[0];
    }

    public static async Task<string> CreateRoom(this ApiFactory factory, string cookie, bool fun = false)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/rooms")
        {
            Content = JsonContent.Create(new { useFunRoomName = fun, configuration = DefaultConfiguration })
        };
        request.Headers.Add("Cookie", cookie);
        var response = await factory.CreateClient().SendAsync(request);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("data").GetProperty("id").GetString()!;
    }

    public static HubConnection Connect(this ApiFactory factory, string cookie)
    {
        return new HubConnectionBuilder()
            .WithUrl(new Uri(factory.Server.BaseAddress, "/hubs/room"), options =>
            {
                options.Transports = HttpTransportType.LongPolling;
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
                options.Headers["Cookie"] = cookie;
            })
            .Build();
    }

    /// <summary>Creates a room owned by "Ann" with "Bob" as a second member. Both are connected and joined before this returns.</summary>
    public static async Task<TestRoom> CreateTestRoomAsync(this ApiFactory factory)
    {
        var ownerCookie = await factory.MintCookie("Ann");
        var guestCookie = await factory.MintCookie("Bob", "test");
        var roomId = await factory.CreateRoom(ownerCookie);
        var owner = factory.Connect(ownerCookie);
        var guest = factory.Connect(guestCookie);
        await owner.StartAsync();
        await guest.StartAsync();
        await JoinSuccessfully(owner, roomId);
        await JoinSuccessfully(guest, roomId);
        return new TestRoom(roomId, owner, guest);
    }

    /// <summary>Resolves with the first payload of <paramref name="eventName"/> received on the connection.</summary>
    public static Task<JsonElement> NextEvent(this HubConnection connection, string eventName)
    {
        var received = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        connection.On<JsonElement>(eventName, payload => received.TrySetResult(payload));
        return received.Task.WaitAsync(TimeSpan.FromSeconds(10));
    }

    /// <summary>Polls <paramref name="condition"/> every 50 ms until it holds, failing with a timeout after <paramref name="timeout"/>.</summary>
    public static async Task WaitUntilAsync(Func<Task<bool>> condition, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (!await condition())
        {
            if (DateTime.UtcNow > deadline)
            {
                throw new TimeoutException("The condition was not met before the timeout.");
            }

            await Task.Delay(50);
        }
    }

    private static async Task JoinSuccessfully(HubConnection connection, string roomId)
    {
        var response = await connection.InvokeAsync<JsonElement>("JoinRoom", roomId);
        Assert.Equal(JsonValueKind.Null, response.GetProperty("errors").ValueKind);
    }
}
