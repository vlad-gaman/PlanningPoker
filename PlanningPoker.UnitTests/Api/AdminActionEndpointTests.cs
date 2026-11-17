using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using PlanningPoker.Common.Constants;
using PlanningPoker.Common.Models;
using PlanningPoker.Service.Services;
using PlanningPoker.UnitTests.Service;

namespace PlanningPoker.UnitTests.Api;

/// <summary>An <see cref="AdminApiFactory"/>-like host on a given store file, so a test can close a room and restart.</summary>
public class AdminRestartApiFactory(string storePath, FakeTimeProvider time) : RestartApiFactory(storePath, time)
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseSetting($"{OperatorConstants.ConfigurationSection}:FailureDelay", "00:00:00");
        builder.UseSetting($"{OperatorConstants.ConfigurationSection}:Username", AdminApiFactory.Username);
        builder.UseSetting($"{OperatorConstants.ConfigurationSection}:PasswordHash", new PasswordHashingService().Hash(AdminApiFactory.Password));
    }

    public HttpClient CreateBrowser()
    {
        return CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), HandleCookies = true });
    }
}

public sealed class AdminActionEndpointTests : IDisposable
{
    private static readonly DateTimeOffset Start = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
    private readonly string _store = Path.Combine(Path.GetTempPath(), "pp-admin-" + Guid.NewGuid() + ".db");

    public void Dispose()
    {
        foreach (var file in Directory.GetFiles(Path.GetTempPath(), Path.GetFileName(_store) + "*"))
        {
            File.Delete(file);
        }
    }

    private AdminRestartApiFactory Boot() => new(_store, new FakeTimeProvider(Start));

    private static async Task<(HttpClient Client, string Token)> SignIn(AdminRestartApiFactory factory)
    {
        var client = factory.CreateBrowser();
        var login = await client.PostAsJsonAsync("/api/admin/login", new { username = AdminApiFactory.Username, password = AdminApiFactory.Password });
        login.EnsureSuccessStatusCode();
        var token = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data").GetProperty("csrfToken").GetString()!;
        return (client, token);
    }

    private static Task<HttpResponseMessage> Post(HttpClient client, string url, string? token)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(new { }) };
        if (token is not null)
        {
            request.Headers.Add(OperatorConstants.XsrfHeaderName, token);
        }

        return client.SendAsync(request);
    }

    private static async Task<string> ErrorCode(HttpResponseMessage response)
    {
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors")[0].GetProperty("code").GetString()!;
    }

    private static async Task<Guid> PersonId(ApiFactory factory, string cookie)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/me");
        request.Headers.Add("Cookie", cookie);
        var body = await (await factory.CreateClient().SendAsync(request)).Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("data").GetProperty("guid").GetGuid();
    }

    /// <summary>Ann (owner) and Bob in one room, both joined; also returns Bob's person id.</summary>
    private static async Task<(string RoomId, Guid BobId, HubConnection Owner, HubConnection Guest)> OpenRoom(ApiFactory factory)
    {
        var annCookie = await factory.MintCookie("Ann");
        var bobCookie = await factory.MintCookie("Bob", "test");
        var roomId = await factory.CreateRoom(annCookie);
        var owner = factory.Connect(annCookie);
        var guest = factory.Connect(bobCookie);
        await owner.StartAsync();
        await guest.StartAsync();
        await owner.InvokeAsync<JsonElement>("JoinRoom", roomId);
        var bobJoined = owner.NextEvent("PersonJoined", payload => payload.GetProperty("person").GetProperty("name").GetString() == "Bob");
        await guest.InvokeAsync<JsonElement>("JoinRoom", roomId);
        await bobJoined;
        return (roomId, await PersonId(factory, bobCookie), owner, guest);
    }

    private static async Task<bool> RoomExists(ApiFactory factory, string roomId)
    {
        return (await factory.CreateClient().GetAsync($"/api/rooms/{roomId}")).StatusCode != HttpStatusCode.NotFound;
    }

    [Fact]
    public async Task Close_NotSignedIn_Returns401()
    {
        await using var factory = Boot();

        var response = await Post(factory.CreateBrowser(), "/api/admin/rooms/anything/close", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Close_WithoutTheAntiforgeryToken_Returns403AndTheRoomStays()
    {
        await using var factory = Boot();
        var room = await factory.CreateTestRoomAsync();
        var (client, _) = await SignIn(factory);

        var response = await Post(client, $"/api/admin/rooms/{room.RoomId}/close", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(ErrorCodes.AdminAntiforgeryInvalid, await ErrorCode(response));
        Assert.True(await RoomExists(factory, room.RoomId));
    }

    [Fact]
    public async Task Close_UnknownRoom_Returns404()
    {
        await using var factory = Boot();
        var (client, token) = await SignIn(factory);

        var response = await Post(client, "/api/admin/rooms/no-such-room/close", token);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(ErrorCodes.RoomNotFound, await ErrorCode(response));
    }

    [Fact]
    public async Task Close_TellsTheRoomItWasClosedByTheOperator_AndTheRoomIsGone()
    {
        await using var factory = Boot();
        var room = await factory.CreateTestRoomAsync();
        var closed = room.Guest.NextEvent("RoomDisposed");
        var (client, token) = await SignIn(factory);

        var response = await Post(client, $"/api/admin/rooms/{room.RoomId}/close", token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await closed;
        Assert.Equal(room.RoomId, payload.GetProperty("roomId").GetString());
        Assert.Equal("closedByOperator", payload.GetProperty("reason").GetString());
        Assert.False(await RoomExists(factory, room.RoomId));
    }

    [Fact]
    public async Task Close_RemovesTheRoomFromTheStore_SoARestartDoesNotBringItBack()
    {
        string roomId;
        await using (var first = Boot())
        {
            var room = await first.CreateTestRoomAsync();
            roomId = room.RoomId;
            var (client, token) = await SignIn(first);
            Assert.Equal(HttpStatusCode.OK, (await Post(client, $"/api/admin/rooms/{roomId}/close", token)).StatusCode);
        }

        await using var second = Boot();

        Assert.False(await RoomExists(second, roomId));
    }

    [Fact]
    public async Task Remove_NotSignedIn_Returns401()
    {
        await using var factory = Boot();

        var response = await Post(factory.CreateBrowser(), $"/api/admin/rooms/anything/people/{Guid.NewGuid()}/remove", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Remove_TellsTheRoomWithTheReasonAndThePersonsId_AndOthersStayInTheRoom()
    {
        await using var factory = Boot();
        var room = await OpenRoom(factory);
        var exited = room.Owner.NextEvent("PersonExited", payload => payload.GetProperty("person").GetProperty("name").GetString() == "Bob");
        var (client, token) = await SignIn(factory);

        var response = await Post(client, $"/api/admin/rooms/{room.RoomId}/people/{room.BobId}/remove", token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await exited;
        Assert.Equal("removedByOperator", payload.GetProperty("reason").GetString());
        Assert.True(await RoomExists(factory, room.RoomId));
    }

    [Fact]
    public async Task Remove_UnknownPerson_Returns404()
    {
        await using var factory = Boot();
        var room = await factory.CreateTestRoomAsync();
        var (client, token) = await SignIn(factory);

        var response = await Post(client, $"/api/admin/rooms/{room.RoomId}/people/{Guid.NewGuid()}/remove", token);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(ErrorCodes.PersonNotFound, await ErrorCode(response));
    }

    [Fact]
    public async Task Remove_ARemovedPersonNoLongerHearsTheRoom()
    {
        await using var factory = Boot();
        var room = await OpenRoom(factory);
        var heardAfter = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var removedHeard = room.Guest.NextEvent("PersonExited");
        var (client, token) = await SignIn(factory);
        await Post(client, $"/api/admin/rooms/{room.RoomId}/people/{room.BobId}/remove", token);
        await removedHeard;

        room.Guest.On<JsonElement>("VotesCleared", _ => heardAfter.TrySetResult(true));
        await room.Owner.InvokeAsync<JsonElement>("ClearVotes");

        var heard = await Task.WhenAny(heardAfter.Task, Task.Delay(500)) == heardAfter.Task;
        Assert.False(heard);
    }
}
