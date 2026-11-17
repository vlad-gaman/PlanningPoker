using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Time.Testing;

namespace PlanningPoker.UnitTests.Api;

/// <summary>Rooms across a server restart: one host is stopped and the next boots on the same store file, as the real server does.</summary>
public sealed class RoomPersistenceTests : IDisposable
{
    private static readonly DateTimeOffset Start = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
    private readonly string _store = Path.Combine(Path.GetTempPath(), "pp-restart-" + Guid.NewGuid() + ".db");

    public void Dispose()
    {
        foreach (var file in Directory.GetFiles(Path.GetTempPath(), Path.GetFileName(_store) + "*"))
        {
            File.Delete(file);
        }
    }

    private RestartApiFactory Boot(TimeSpan? downtime = null) => new(_store, new FakeTimeProvider(Start + (downtime ?? TimeSpan.Zero)));

    private static async Task<bool> RoomExists(ApiFactory factory, string roomId)
    {
        return (await factory.CreateClient().GetAsync($"/api/rooms/{roomId}")).StatusCode != HttpStatusCode.NotFound;
    }

    /// <summary>Ann (owner, dev) and Bob (test) in one room, both joined. The cookies are returned so a later host can be joined as the same people.</summary>
    private static async Task<(string RoomId, string AnnCookie, HubConnection Ann, HubConnection Bob)> OpenRoom(ApiFactory factory)
    {
        var annCookie = await factory.MintCookie("Ann");
        var bobCookie = await factory.MintCookie("Bob", "test");
        var roomId = await factory.CreateRoom(annCookie);
        var ann = factory.Connect(annCookie);
        var bob = factory.Connect(bobCookie);
        await ann.StartAsync();
        await bob.StartAsync();
        await ann.InvokeAsync<JsonElement>("JoinRoom", roomId);
        var bobJoined = ann.NextEvent("PersonJoined", payload => payload.GetProperty("person").GetProperty("name").GetString() == "Bob");
        await bob.InvokeAsync<JsonElement>("JoinRoom", roomId);
        await bobJoined;
        return (roomId, annCookie, ann, bob);
    }

    private static async Task<JsonElement> Rejoin(ApiFactory factory, string cookie, string roomId)
    {
        var connection = factory.Connect(cookie);
        await connection.StartAsync();
        var response = await connection.InvokeAsync<JsonElement>("JoinRoom", roomId);
        Assert.Equal(JsonValueKind.Null, response.GetProperty("errors").ValueKind);
        return response.GetProperty("data");
    }

    private static JsonElement Person(JsonElement joined, string name)
    {
        return joined.GetProperty("people").EnumerateArray().Single(person => person.GetProperty("name").GetString() == name);
    }

    [Fact]
    public async Task Restart_KeepsTheRoomItsPeopleAndTheHiddenVote()
    {
        var first = Boot();
        var (roomId, annCookie, ann, bob) = await OpenRoom(first);
        await ann.InvokeAsync<JsonElement>("Vote", "5");
        await ann.DisposeAsync();
        await bob.DisposeAsync();
        await first.DisposeAsync();

        await using var second = Boot();
        Assert.True(await RoomExists(second, roomId));
        var joined = await Rejoin(second, annCookie, roomId);

        Assert.True(Person(joined, "Bob").GetProperty("isOffgrid").GetBoolean());
        Assert.Equal("test", Person(joined, "Bob").GetProperty("personType").GetString());
        Assert.False(Person(joined, "Ann").GetProperty("isOffgrid").GetBoolean());
        Assert.Equal("5", joined.GetProperty("myMark").GetString());
        var status = joined.GetProperty("voteStatus");
        Assert.False(status.GetProperty("votingFinished").GetBoolean());
        var marks = status.GetProperty("votes").EnumerateObject().Select(vote => vote.Value.GetString()).ToList();
        Assert.Equal(["hide"], marks);
    }

    [Fact]
    public async Task Restart_KeepsARevealedRoundWithItsResults()
    {
        var first = Boot();
        var (roomId, annCookie, ann, bob) = await OpenRoom(first);
        await ann.InvokeAsync<JsonElement>("Vote", "8");
        await ann.InvokeAsync<JsonElement>("ForceShowVotes");
        await ann.DisposeAsync();
        await bob.DisposeAsync();
        await first.DisposeAsync();

        await using var second = Boot();
        var status = (await Rejoin(second, annCookie, roomId)).GetProperty("voteStatus");

        Assert.True(status.GetProperty("votingFinished").GetBoolean());
        Assert.Contains("8", status.GetProperty("votes").EnumerateObject().Select(vote => vote.Value.GetString()));
        Assert.NotEqual(JsonValueKind.Null, status.GetProperty("statistics").ValueKind);
    }

    [Fact]
    public async Task Restart_DoesNotRestoreARoomThatWasDeleted()
    {
        var first = Boot();
        var (roomId, _, ann, bob) = await OpenRoom(first);
        await ann.InvokeAsync<JsonElement>("LeaveRoom");
        await bob.InvokeAsync<JsonElement>("LeaveRoom");
        Assert.False(await RoomExists(first, roomId));
        await ann.DisposeAsync();
        await bob.DisposeAsync();
        await first.DisposeAsync();

        await using var second = Boot();
        Assert.False(await RoomExists(second, roomId));
    }

    [Fact]
    public async Task Restart_RemovesPeopleWhoWereGoneLongerThanTheRemovalWindowIncludingTheDowntime()
    {
        var first = Boot();
        var (roomId, _, ann, bob) = await OpenRoom(first);
        await ann.DisposeAsync();
        await bob.DisposeAsync();
        await first.DisposeAsync();

        await using var second = Boot(TimeSpan.FromMinutes(30));
        await TestSession.WaitUntilAsync(async () =>
        {
            second.Time.Advance(TimeSpan.FromSeconds(2));
            return !await RoomExists(second, roomId);
        }, TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task Restart_KeepsARoomWhosePeopleAreStillInsideTheRemovalWindow()
    {
        var first = Boot();
        var (roomId, _, ann, bob) = await OpenRoom(first);
        await ann.DisposeAsync();
        await bob.DisposeAsync();
        await first.DisposeAsync();

        await using var second = Boot(TimeSpan.FromMinutes(2));
        second.Time.Advance(TimeSpan.FromSeconds(30));
        await Task.Delay(300);

        Assert.True(await RoomExists(second, roomId));
    }

    [Fact]
    public async Task Restart_SamePersonJoiningFromTwoTabs_StaysOnePersonInTheRoom()
    {
        var first = Boot();
        var (roomId, annCookie, ann, bob) = await OpenRoom(first);
        await ann.DisposeAsync();
        await bob.DisposeAsync();
        await first.DisposeAsync();

        await using var second = Boot();
        await Rejoin(second, annCookie, roomId);
        var joinedAgain = await Rejoin(second, annCookie, roomId);

        Assert.Equal(2, joinedAgain.GetProperty("people").GetArrayLength());
        Assert.False(Person(joinedAgain, "Ann").GetProperty("isOffgrid").GetBoolean());
    }

    [Fact]
    public async Task Restart_RoomNotTouchedForLongerThanTheAbandonedTime_IsNotKept()
    {
        var first = Boot();
        var (roomId, _, ann, bob) = await OpenRoom(first);
        await ann.DisposeAsync();
        await bob.DisposeAsync();
        await first.DisposeAsync();

        await using var second = Boot(TimeSpan.FromDays(2));
        await TestSession.WaitUntilAsync(async () =>
        {
            second.Time.Advance(TimeSpan.FromSeconds(2));
            return !await RoomExists(second, roomId);
        }, TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task CorruptStore_StartsCleanWithNoRoomsAndKeepsWorking()
    {
        await File.WriteAllTextAsync(_store, "this is not a sqlite database");

        await using var factory = Boot();
        var cookie = await factory.MintCookie();
        var roomId = await factory.CreateRoom(cookie);

        Assert.False(await RoomExists(factory, "some-room-from-before"));
        Assert.True(await RoomExists(factory, roomId));
    }
}
