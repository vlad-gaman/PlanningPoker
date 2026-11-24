using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;

namespace PlanningPoker.UnitTests.Api;

public class RoomHubAlarmIntegrationTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static string ErrorCode(JsonElement response)
    {
        return response.GetProperty("errors")[0].GetProperty("code").GetString()!;
    }

    [Fact]
    public async Task StartAlarm_ByTheGuest_TellsTheWholeRoomItIsRunning()
    {
        await using var room = await factory.CreateTestRoomAsync();
        var ownerHears = room.Owner.NextEvent("AlarmChanged");

        var started = await room.Guest.InvokeAsync<JsonElement>("StartAlarm");

        Assert.Equal(JsonValueKind.Null, started.GetProperty("errors").ValueKind);
        var alarm = await ownerHears;
        Assert.Equal("running", alarm.GetProperty("status").GetString());
        Assert.True(alarm.GetProperty("endsAt").GetDateTimeOffset() > DateTimeOffset.UtcNow.AddMinutes(4));
    }

    [Fact]
    public async Task ALateJoiner_SeesTheRunningAlarmInTheJoinSnapshot()
    {
        await using var room = await factory.CreateTestRoomAsync();
        await room.Guest.InvokeAsync<JsonElement>("StartAlarm");
        await using var late = factory.Connect(await factory.MintCookie("Cara"));
        await late.StartAsync();

        var joined = await late.InvokeAsync<JsonElement>("JoinRoom", room.RoomId);

        Assert.Equal("running", joined.GetProperty("data").GetProperty("alarm").GetProperty("status").GetString());
    }

    [Fact]
    public async Task CancelAlarm_AnnouncesIdle_AndStartingTwiceIsRefused()
    {
        await using var room = await factory.CreateTestRoomAsync();
        await room.Owner.InvokeAsync<JsonElement>("StartAlarm");

        Assert.Equal("alarm_not_idle", ErrorCode(await room.Guest.InvokeAsync<JsonElement>("StartAlarm")));
        var guestHears = room.Guest.NextEvent("AlarmChanged");
        await room.Owner.InvokeAsync<JsonElement>("CancelAlarm");

        Assert.Equal("idle", (await guestHears).GetProperty("status").GetString());
    }

    [Fact]
    public async Task ResetAlarm_WhileRunning_IsRefused()
    {
        await using var room = await factory.CreateTestRoomAsync();
        await room.Owner.InvokeAsync<JsonElement>("StartAlarm");

        Assert.Equal("alarm_not_elapsed", ErrorCode(await room.Guest.InvokeAsync<JsonElement>("ResetAlarm")));
    }

    [Fact]
    public async Task OnlyTheOwner_CanChangeTheAlarmLength()
    {
        await using var room = await factory.CreateTestRoomAsync();
        var configuration = new { cardSet = "linear", countdownSeconds = 5, showFireworks = true, maxParticipants = 50, alarmEnabled = true, alarmMinutes = 12 };

        Assert.Equal("not_room_owner", ErrorCode(await room.Guest.InvokeAsync<JsonElement>("UpdateRoomConfiguration", configuration)));
        var guestHears = room.Guest.NextEvent("RoomConfigurationUpdated");
        await room.Owner.InvokeAsync<JsonElement>("UpdateRoomConfiguration", configuration);

        Assert.Equal(12, (await guestHears).GetProperty("configuration").GetProperty("alarmMinutes").GetInt32());
    }

    [Fact]
    public async Task AnAlarmLengthOutOfRange_IsRejectedWithItsField()
    {
        await using var room = await factory.CreateTestRoomAsync();
        var configuration = new { cardSet = "linear", countdownSeconds = 5, showFireworks = true, maxParticipants = 50, alarmEnabled = true, alarmMinutes = 61 };

        var response = await room.Owner.InvokeAsync<JsonElement>("UpdateRoomConfiguration", configuration);

        Assert.Equal("validation_failed", ErrorCode(response));
        Assert.Equal("alarmMinutes", response.GetProperty("errors")[0].GetProperty("metadata").GetProperty("field").GetString());
    }
}
