using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Time.Testing;

namespace PlanningPoker.UnitTests.Api;

/// <summary>A dropped connection through the real hub and background sweep, with the server clock moved by the test.</summary>
public class RoomHubPresenceIntegrationTests(FakeClockApiFactory factory) : IClassFixture<FakeClockApiFactory>
{
    private static readonly TimeSpan PastGrace = TimeSpan.FromSeconds(11);
    private static readonly TimeSpan PastRemovalWindow = TimeSpan.FromMinutes(11);

    private static string? PersonName(JsonElement payload)
    {
        return payload.GetProperty("person").GetProperty("name").GetString();
    }

    /// <summary>Moves the server clock by <paramref name="step"/> until the awaited event arrives; the sweep reacts to each move.</summary>
    private async Task<T> AdvanceUntil<T>(Task<T> awaited, TimeSpan step)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(10);
        while (!awaited.IsCompleted)
        {
            Assert.True(DateTime.UtcNow < deadline, "The event did not arrive in time.");
            factory.Time.Advance(step);
            await Task.Delay(20);
        }

        return await awaited;
    }

    private async Task<bool> RoomExists(string roomId)
    {
        return (await factory.CreateClient().GetAsync($"/api/rooms/{roomId}")).StatusCode != HttpStatusCode.NotFound;
    }

    [Fact]
    public async Task Disconnect_Member_IsAnnouncedOffgridAfterTheGracePeriodAndNotExited()
    {
        await using var room = await factory.CreateTestRoomAsync();
        var offgrid = room.Owner.NextEvent("PersonOffgrid", payload => PersonName(payload) == "Bob");
        var exited = false;
        room.Owner.On<JsonElement>("PersonExited", _ => exited = true);

        await room.Guest.StopAsync();
        var payload = await AdvanceUntil(offgrid, PastGrace);

        Assert.True(payload.GetProperty("person").GetProperty("isOffgrid").GetBoolean());
        Assert.False(exited);
    }

    [Fact]
    public async Task Disconnect_Member_IsRemovedOnceTheRemovalWindowHasPassed()
    {
        await using var room = await factory.CreateTestRoomAsync();
        var exited = room.Owner.NextEvent("PersonExited", payload => PersonName(payload) == "Bob");

        await room.Guest.StopAsync();

        Assert.Equal("Bob", PersonName(await AdvanceUntil(exited, PastRemovalWindow)));
    }

    [Fact]
    public async Task Reconnect_AfterBeingOffgrid_ReturnsTheVoteWithTheJoinAndTellsTheRoomTheyAreBack()
    {
        var ownerCookie = await factory.MintCookie("Ann");
        var bobCookie = await factory.MintCookie("Bob");
        var roomId = await factory.CreateRoom(ownerCookie);
        await using var owner = factory.Connect(ownerCookie);
        await owner.StartAsync();
        await owner.InvokeAsync<JsonElement>("JoinRoom", roomId);
        await using var bob = factory.Connect(bobCookie);
        await bob.StartAsync();
        await bob.InvokeAsync<JsonElement>("JoinRoom", roomId);
        await bob.InvokeAsync<JsonElement>("Vote", "5");
        var offgrid = owner.NextEvent("PersonOffgrid", payload => PersonName(payload) == "Bob");

        await bob.StopAsync();
        await AdvanceUntil(offgrid, PastGrace);
        var back = owner.NextEvent("PersonBack", payload => PersonName(payload) == "Bob");
        await using var bobAgain = factory.Connect(bobCookie);
        await bobAgain.StartAsync();
        var joined = await bobAgain.InvokeAsync<JsonElement>("JoinRoom", roomId);

        var data = joined.GetProperty("data");
        Assert.Equal("5", data.GetProperty("myMark").GetString());
        Assert.False(data.GetProperty("people").EnumerateArray().Single(person => person.GetProperty("name").GetString() == "Bob").GetProperty("isOffgrid").GetBoolean());
        Assert.False((await back).GetProperty("person").GetProperty("isOffgrid").GetBoolean());
    }

    [Fact]
    public async Task Disconnect_Owner_TransfersOwnershipToTheRemainingMemberWhenTheOwnerIsRemoved()
    {
        await using var room = await factory.CreateTestRoomAsync();
        var transferred = room.Guest.NextEvent("OwnershipTransferred");

        await room.Owner.StopAsync();
        var payload = await AdvanceUntil(transferred, PastRemovalWindow);

        Assert.Equal("Bob", payload.GetProperty("newOwner").GetProperty("name").GetString());
    }

    [Fact]
    public async Task Disconnect_LastMember_KeepsTheRoomForAWhileAndThenDeletesIt()
    {
        var cookie = await factory.MintCookie();
        var roomId = await factory.CreateRoom(cookie);
        await using var connection = factory.Connect(cookie);
        await connection.StartAsync();
        await connection.InvokeAsync<JsonElement>("JoinRoom", roomId);

        await connection.StopAsync();
        Assert.True(await RoomExists(roomId));

        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(10);
        while (await RoomExists(roomId))
        {
            Assert.True(DateTime.UtcNow < deadline, "The room was not deleted in time.");
            factory.Time.Advance(PastGrace);
            await Task.Delay(20);
        }
    }

    [Fact]
    public async Task JoinRoom_AfterTheRoomWasDeleted_ReturnsRoomNotFound()
    {
        var cookie = await factory.MintCookie();
        var roomId = await factory.CreateRoom(cookie);
        await using var first = factory.Connect(cookie);
        await first.StartAsync();
        await first.InvokeAsync<JsonElement>("JoinRoom", roomId);
        await first.StopAsync();
        while (await RoomExists(roomId))
        {
            factory.Time.Advance(PastGrace);
            await Task.Delay(20);
        }
        await using var again = factory.Connect(cookie);
        await again.StartAsync();

        var response = await again.InvokeAsync<JsonElement>("JoinRoom", roomId);

        Assert.Equal("room_not_found", response.GetProperty("errors")[0].GetProperty("code").GetString());
    }
}
