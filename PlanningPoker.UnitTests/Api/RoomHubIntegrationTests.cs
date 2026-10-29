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
}
