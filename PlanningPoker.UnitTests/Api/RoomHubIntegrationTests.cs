using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;

namespace PlanningPoker.UnitTests.Api;

public class RoomHubIntegrationTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static JsonElement Data(JsonElement response)
    {
        return response.GetProperty("data");
    }

    private static string ErrorCode(JsonElement response)
    {
        return response.GetProperty("errors")[0].GetProperty("code").GetString()!;
    }

    private static string? PersonName(JsonElement payload)
    {
        return payload.GetProperty("person").GetProperty("name").GetString();
    }

    [Fact]
    public async Task JoinRoom_Owner_GetsPeopleAndMaskedVoteStatus()
    {
        var cookie = await factory.MintCookie("Ann");
        var roomId = await factory.CreateRoom(cookie);
        await using var connection = factory.Connect(cookie);
        await connection.StartAsync();

        var response = await connection.InvokeAsync<JsonElement>("JoinRoom", roomId);

        var data = Data(response);
        var person = Assert.Single(data.GetProperty("people").EnumerateArray());
        Assert.Equal("Ann", person.GetProperty("name").GetString());
        Assert.Equal("dev", person.GetProperty("personType").GetString());
        Assert.False(data.GetProperty("voteStatus").GetProperty("hasEveryoneVoted").GetBoolean());
        Assert.Equal(person.GetProperty("guid").GetGuid(), data.GetProperty("ownerGuid").GetGuid());
        Assert.Equal("linear", data.GetProperty("configuration").GetProperty("cardSet").GetString());
    }

    [Fact]
    public async Task JoinRoom_LaterPerson_GetsEveryoneAndOthersGetPersonJoined()
    {
        await using var room = await factory.CreateTestRoomAsync();
        var laterCookie = await factory.MintCookie("Cara", "obs");
        await using var later = factory.Connect(laterCookie);
        var joined = room.Owner.NextEvent("PersonJoined", payload => PersonName(payload) == "Cara");
        await later.StartAsync();

        var response = await later.InvokeAsync<JsonElement>("JoinRoom", room.RoomId);

        Assert.Equal(["Ann", "Bob", "Cara"], Data(response).GetProperty("people").EnumerateArray().Select(p => p.GetProperty("name").GetString()));
        var payload = await joined;
        Assert.Equal("Cara", PersonName(payload));
        Assert.Equal("obs", payload.GetProperty("personType").GetString());
    }

    [Fact]
    public async Task JoinRoom_UnknownRoom_ReturnsRoomNotFound()
    {
        await using var connection = factory.Connect(await factory.MintCookie());
        await connection.StartAsync();

        var response = await connection.InvokeAsync<JsonElement>("JoinRoom", "missing");

        Assert.Equal("room_not_found", ErrorCode(response));
    }

    [Theory]
    [InlineData("")]
    [InlineData("bad id")]
    [InlineData("a<b>")]
    public async Task JoinRoom_InvalidIdentifier_ReturnsValidationFailed(string roomId)
    {
        await using var connection = factory.Connect(await factory.MintCookie());
        await connection.StartAsync();

        var response = await connection.InvokeAsync<JsonElement>("JoinRoom", roomId);

        Assert.Equal("validation_failed", ErrorCode(response));
    }

    [Fact]
    public async Task Disconnect_Member_BroadcastsPersonExited()
    {
        await using var room = await factory.CreateTestRoomAsync();
        var exited = room.Owner.NextEvent("PersonExited", payload => PersonName(payload) == "Bob");

        await room.Guest.StopAsync();

        Assert.Equal("Bob", PersonName(await exited));
    }

    [Fact]
    public async Task Disconnect_Owner_TransfersOwnershipToRemainingMember()
    {
        await using var room = await factory.CreateTestRoomAsync();
        var transferred = room.Guest.NextEvent("OwnershipTransferred");

        await room.Owner.StopAsync();

        Assert.Equal("Bob", (await transferred).GetProperty("newOwner").GetProperty("name").GetString());
    }

    [Fact]
    public async Task Disconnect_LastMember_RemovesTheRoom()
    {
        var cookie = await factory.MintCookie();
        var roomId = await factory.CreateRoom(cookie);
        await using var connection = factory.Connect(cookie);
        await connection.StartAsync();
        await connection.InvokeAsync<JsonElement>("JoinRoom", roomId);

        await connection.StopAsync();

        var client = factory.CreateClient();
        await TestSession.WaitUntilAsync(async () =>
            (await client.GetAsync($"/api/rooms/{roomId}")).StatusCode == System.Net.HttpStatusCode.NotFound,
            TimeSpan.FromSeconds(5));
    }

    private static bool HasErrors(JsonElement response)
    {
        return response.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Array;
    }

    [Fact]
    public async Task Vote_BroadcastsVoteCastAndMaskedStatusToTheWholeRoom()
    {
        await using var room = await factory.CreateTestRoomAsync();
        var cast = room.Guest.NextEvent("VoteCast");
        var status = room.Owner.NextEvent("VoteStatusUpdate");

        var response = await room.Owner.InvokeAsync<JsonElement>("Vote", "5");

        Assert.False(HasErrors(response));
        Assert.NotEqual(Guid.Empty, (await cast).GetProperty("personGuid").GetGuid());
        Assert.All((await status).GetProperty("votes").EnumerateObject(), vote => Assert.Equal("hide", vote.Value.GetString()));
    }

    [Fact]
    public async Task Vote_InvalidMark_ReturnsValidationFailed()
    {
        await using var room = await factory.CreateTestRoomAsync();

        var response = await room.Owner.InvokeAsync<JsonElement>("Vote", "not-a-card");

        Assert.Equal("validation_failed", ErrorCode(response));
    }

    [Fact]
    public async Task Vote_Observer_ReturnsObserverCannotVote()
    {
        await using var room = await factory.CreateTestRoomAsync();
        var observerCookie = await factory.MintCookie("Cara", "obs");
        await using var observer = factory.Connect(observerCookie);
        await observer.StartAsync();
        await observer.InvokeAsync<JsonElement>("JoinRoom", room.RoomId);

        var response = await observer.InvokeAsync<JsonElement>("Vote", "5");

        Assert.Equal("observer_cannot_vote", ErrorCode(response));
    }

    [Fact]
    public async Task ForceShowVotes_RevealsRealMarksToEveryone()
    {
        await using var room = await factory.CreateTestRoomAsync();
        await room.Owner.InvokeAsync<JsonElement>("Vote", "8");
        var shown = room.Guest.NextEvent("VotesShown");

        await room.Owner.InvokeAsync<JsonElement>("ForceShowVotes");

        var payload = await shown;
        Assert.True(payload.GetProperty("votingFinished").GetBoolean());
        Assert.Contains(payload.GetProperty("votes").EnumerateObject(), vote => vote.Value.GetString() == "8");
    }

    [Fact]
    public async Task Vote_AfterReveal_ReturnsVotingClosed()
    {
        await using var room = await factory.CreateTestRoomAsync();
        await room.Owner.InvokeAsync<JsonElement>("ForceShowVotes");

        var response = await room.Guest.InvokeAsync<JsonElement>("Vote", "3");

        Assert.Equal("voting_closed", ErrorCode(response));
    }

    [Fact]
    public async Task ClearVotes_BroadcastsVotesClearedAndReopensVoting()
    {
        await using var room = await factory.CreateTestRoomAsync();
        await room.Owner.InvokeAsync<JsonElement>("ForceShowVotes");
        var cleared = new TaskCompletionSource();
        room.Guest.On("VotesCleared", () => cleared.TrySetResult());

        await room.Owner.InvokeAsync<JsonElement>("ClearVotes");
        await cleared.Task.WaitAsync(TimeSpan.FromSeconds(10));
        var response = await room.Guest.InvokeAsync<JsonElement>("Vote", "3");

        Assert.False(HasErrors(response));
    }

    [Fact]
    public async Task VotesShown_CarriesTheStatisticsOfTheVotes()
    {
        await using var room = await factory.CreateTestRoomAsync();
        await room.Owner.InvokeAsync<JsonElement>("Vote", "3");
        await room.Guest.InvokeAsync<JsonElement>("Vote", "5");
        var shown = room.Guest.NextEvent("VotesShown");

        await room.Owner.InvokeAsync<JsonElement>("ForceShowVotes");

        var statistics = (await shown).GetProperty("statistics");
        var all = statistics.GetProperty("all");
        Assert.Equal(["3", "5"], all.GetProperty("marks").EnumerateArray().Select(share => share.GetProperty("mark").GetString()));
        Assert.Equal(50m, all.GetProperty("marks")[0].GetProperty("percentage").GetDecimal());
        Assert.Equal(JsonValueKind.Null, all.GetProperty("highestMark").ValueKind);
        Assert.Equal(4m, all.GetProperty("averageMark").GetDecimal());
        Assert.False(statistics.GetProperty("consensus").GetBoolean());
    }

    [Fact]
    public async Task JoinRoom_AfterTheReveal_GetsTheSameStatistics_AndAfterClearingNone()
    {
        await using var room = await factory.CreateTestRoomAsync();
        await room.Owner.InvokeAsync<JsonElement>("Vote", "3");
        await room.Guest.InvokeAsync<JsonElement>("Vote", "3");
        await room.Owner.InvokeAsync<JsonElement>("ForceShowVotes");
        await using var late = factory.Connect(await factory.MintCookie("Cara", "dev"));
        await late.StartAsync();

        var joined = await late.InvokeAsync<JsonElement>("JoinRoom", room.RoomId);

        var statistics = Data(joined).GetProperty("voteStatus").GetProperty("statistics");
        Assert.True(statistics.GetProperty("consensus").GetBoolean());
        Assert.Equal("3", statistics.GetProperty("all").GetProperty("highestMark").GetString());
        Assert.Equal(JsonValueKind.Null, Data(await room.Owner.InvokeAsync<JsonElement>("ClearVotes")).ValueKind);
        await using var after = factory.Connect(await factory.MintCookie("Dan", "dev"));
        await after.StartAsync();
        var fresh = await after.InvokeAsync<JsonElement>("JoinRoom", room.RoomId);
        Assert.Equal(JsonValueKind.Null, Data(fresh).GetProperty("voteStatus").GetProperty("statistics").ValueKind);
    }
}
