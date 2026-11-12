using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Time.Testing;

namespace PlanningPoker.UnitTests.Api;

public sealed class RoundHistoryEndpointTests : IDisposable
{
    private static readonly DateTimeOffset Start = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
    private readonly string _store = Path.Combine(Path.GetTempPath(), "pp-history-" + Guid.NewGuid() + ".db");

    public void Dispose()
    {
        foreach (var file in Directory.GetFiles(Path.GetTempPath(), Path.GetFileName(_store) + "*"))
        {
            File.Delete(file);
        }
    }

    private RestartApiFactory Boot() => new(_store, new FakeTimeProvider(Start));

    private static async Task<JsonElement> Invoke(HubConnection connection, string method, params object[] args)
    {
        var response = connection.InvokeCoreAsync<JsonElement>(method, args);
        return await response;
    }

    private static async Task<JsonElement> Join(HubConnection connection, string roomId)
    {
        var response = await Invoke(connection, "JoinRoom", roomId);
        Assert.Equal(JsonValueKind.Null, response.GetProperty("errors").ValueKind);
        return response.GetProperty("data");
    }

    [Fact]
    public async Task AReveal_TellsEveryoneTheHistoryWithTheNewRoundOnTop()
    {
        await using var factory = Boot();
        var room = await factory.CreateTestRoomAsync();
        await Invoke(room.Owner, "Vote", "5");
        await Invoke(room.Guest, "Vote", "3");
        var shown = room.Guest.NextEvent("VotesShown");

        await Invoke(room.Owner, "ForceShowVotes");

        var history = (await shown).GetProperty("roundHistory").EnumerateArray().ToList();
        var entry = Assert.Single(history);
        Assert.Equal(1, entry.GetProperty("round").GetInt32());
        Assert.Equal(4m, entry.GetProperty("averageMark").GetDecimal());
    }

    [Fact]
    public async Task AJoinSnapshot_CarriesTheHistory_AndASecondRoundIsNumberedTwo()
    {
        await using var factory = Boot();
        var room = await factory.CreateTestRoomAsync();
        await Invoke(room.Owner, "Vote", "5");
        await Invoke(room.Owner, "ForceShowVotes");
        await Invoke(room.Owner, "ClearVotes");
        await Invoke(room.Owner, "Vote", "8");
        await Invoke(room.Owner, "ForceShowVotes");

        var late = factory.Connect(await factory.MintCookie("Cy"));
        await late.StartAsync();
        var joined = await Join(late, room.RoomId);

        var history = joined.GetProperty("roundHistory").EnumerateArray().ToList();
        Assert.Equal([2, 1], history.Select(entry => entry.GetProperty("round").GetInt32()));
        Assert.Equal([8m, 5m], history.Select(entry => entry.GetProperty("averageMark").GetDecimal()));
    }

    [Fact]
    public async Task ARoundWithOnlyAQuestionMark_HasAnAverageThatIsNull()
    {
        await using var factory = Boot();
        var room = await factory.CreateTestRoomAsync();
        await Invoke(room.Owner, "ForceShowVotes");

        var late = factory.Connect(await factory.MintCookie("Cy"));
        await late.StartAsync();
        var joined = await Join(late, room.RoomId);

        var entry = Assert.Single(joined.GetProperty("roundHistory").EnumerateArray());
        Assert.Equal(JsonValueKind.Null, entry.GetProperty("averageMark").ValueKind);
    }

    [Fact]
    public async Task TheHistory_SurvivesAServerRestart()
    {
        string roomId;
        string annCookie;
        await using (var first = Boot())
        {
            annCookie = await first.MintCookie("Ann");
            roomId = await first.CreateRoom(annCookie);
            var ann = first.Connect(annCookie);
            await ann.StartAsync();
            await Join(ann, roomId);
            await Invoke(ann, "Vote", "5");
            await Invoke(ann, "ForceShowVotes");
            await Invoke(ann, "ClearVotes");
            await Invoke(ann, "Vote", "3");
            await Invoke(ann, "ForceShowVotes");
        }

        await using var second = Boot();
        var back = second.Connect(annCookie);
        await back.StartAsync();
        var joined = await Join(back, roomId);

        var history = joined.GetProperty("roundHistory").EnumerateArray().ToList();
        Assert.Equal([2, 1], history.Select(entry => entry.GetProperty("round").GetInt32()));
        Assert.Equal([3m, 5m], history.Select(entry => entry.GetProperty("averageMark").GetDecimal()));
    }
}
