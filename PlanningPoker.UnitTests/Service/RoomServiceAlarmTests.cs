using Microsoft.Extensions.Time.Testing;
using PlanningPoker.Common.Constants;
using PlanningPoker.Repository.Mappers;
using PlanningPoker.Repository.Models;
using PlanningPoker.Repository.Repositories;
using PlanningPoker.Repository.Services;
using PlanningPoker.Service.Models;
using PlanningPoker.Service.Services;

namespace PlanningPoker.UnitTests.Service;

/// <summary>The Alarm of a room: who may start, cancel and reset it, what each change leaves behind, and what leaves it alone.</summary>
public class RoomServiceAlarmTests
{
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
    private readonly PersonRepository _people = new();
    private readonly RoomRepository _rooms = new(new RoomEntityCopyMapper(), new RoomChangeLog());
    private readonly RoomService _service;

    public RoomServiceAlarmTests()
    {
        _service = RoomServiceFactory.Create(_rooms, _people, time: _time);
    }

    private Guid AddPerson()
    {
        var entity = new PersonEntity { Guid = Guid.NewGuid(), Name = "p", PersonType = PersonTypes.Developer };
        _people.TryAdd(entity);
        return entity.Guid;
    }

    private static RoomConfiguration Configuration(bool alarmEnabled = true, int alarmMinutes = 5, string cardSet = "linear")
    {
        return new RoomConfiguration { CardSet = cardSet, CountdownSeconds = 0, ShowFireworks = false, MaxParticipants = 9, AlarmEnabled = alarmEnabled, AlarmMinutes = alarmMinutes };
    }

    private (string RoomId, Guid Owner, Guid Member) Room(RoomConfiguration? configuration = null)
    {
        var owner = AddPerson();
        var member = AddPerson();
        var roomId = _service.Create(owner, new RoomSettings { UseFunRoomName = false, Configuration = configuration ?? Configuration() }).Data!.Id;
        Assert.True(_service.Join(owner, "owner", roomId).IsSuccess);
        Assert.True(_service.Join(member, "member", roomId).IsSuccess);
        return (roomId, owner, member);
    }

    [Fact]
    public void StartAlarm_ByAnyMember_RunsForTheConfiguredMinutes()
    {
        var (roomId, _, member) = Room(Configuration(alarmMinutes: 7));

        var started = _service.StartAlarm(member, "member");

        Assert.True(started.IsSuccess);
        Assert.Equal(roomId, started.Data!.RoomId);
        Assert.Equal(AlarmStatus.Running, started.Data.Alarm.Status);
        Assert.Equal(_time.GetUtcNow().AddMinutes(7), started.Data.Alarm.EndsAt);
    }

    [Fact]
    public void ALateJoiner_SeesTheRunningAlarmWithItsEndTime()
    {
        var (roomId, _, member) = Room();
        var endsAt = _service.StartAlarm(member, "member").Data!.Alarm.EndsAt;
        _time.Advance(TimeSpan.FromMinutes(2));

        var joined = _service.Join(AddPerson(), "late", roomId);

        Assert.Equal(AlarmStatus.Running, joined.Data!.Room.Alarm.Status);
        Assert.Equal(endsAt, joined.Data.Room.Alarm.EndsAt);
    }

    [Fact]
    public void StartAlarm_FailsWhenTheAlarmIsOff()
    {
        var (_, _, member) = Room(Configuration(alarmEnabled: false));

        var started = _service.StartAlarm(member, "member");

        Assert.Equal(ErrorCodes.AlarmDisabled, Assert.Single(started.Errors).Code);
    }

    [Fact]
    public void StartAlarm_FailsWhenItIsRunningOrElapsed()
    {
        var (roomId, _, member) = Room();
        var running = _service.StartAlarm(member, "member").Data!.Alarm.EndsAt!.Value;

        Assert.Equal(ErrorCodes.AlarmNotIdle, Assert.Single(_service.StartAlarm(member, "member").Errors).Code);

        _service.ElapseAlarm(roomId, running);
        Assert.Equal(ErrorCodes.AlarmNotIdle, Assert.Single(_service.StartAlarm(member, "member").Errors).Code);
    }

    [Fact]
    public void StartAlarm_FromAConnectionNotInARoom_Fails()
    {
        var (_, _, member) = Room();

        var started = _service.StartAlarm(member, "elsewhere");

        Assert.Equal(ErrorCodes.NotInRoom, Assert.Single(started.Errors).Code);
    }

    [Fact]
    public void CancelAlarm_ReturnsARunningAlarmToIdle_AndFailsOtherwise()
    {
        var (_, owner, member) = Room();

        Assert.Equal(ErrorCodes.AlarmNotRunning, Assert.Single(_service.CancelAlarm(owner, "owner").Errors).Code);

        _service.StartAlarm(member, "member");
        var cancelled = _service.CancelAlarm(owner, "owner");

        Assert.Equal(AlarmStatus.Idle, cancelled.Data!.Alarm.Status);
        Assert.Null(cancelled.Data.Alarm.EndsAt);
    }

    [Fact]
    public void ResetAlarm_ReturnsAnElapsedAlarmToIdle_WithoutStartingIt()
    {
        var (roomId, owner, member) = Room();
        var endsAt = _service.StartAlarm(member, "member").Data!.Alarm.EndsAt!.Value;

        Assert.Equal(ErrorCodes.AlarmNotElapsed, Assert.Single(_service.ResetAlarm(owner, "owner").Errors).Code);

        _service.ElapseAlarm(roomId, endsAt);
        var reset = _service.ResetAlarm(owner, "owner");

        Assert.Equal(AlarmStatus.Idle, reset.Data!.Alarm.Status);
        Assert.Null(reset.Data.Alarm.EndsAt);
    }

    [Fact]
    public void ElapseAlarm_ElapsesTheRunAtItsEndTime_AndASecondCallIsStale()
    {
        var (roomId, _, member) = Room();
        var endsAt = _service.StartAlarm(member, "member").Data!.Alarm.EndsAt!.Value;

        var elapsed = _service.ElapseAlarm(roomId, endsAt);
        var again = _service.ElapseAlarm(roomId, endsAt);

        Assert.Equal(AlarmStatus.Elapsed, elapsed.Data!.Alarm.Status);
        Assert.False(again.IsSuccess);
    }

    [Fact]
    public void ElapseAlarm_IsStaleOnceTheRunWasCancelledOrReplaced()
    {
        var (roomId, owner, member) = Room();
        var first = _service.StartAlarm(member, "member").Data!.Alarm.EndsAt!.Value;
        _service.CancelAlarm(owner, "owner");
        _time.Advance(TimeSpan.FromMinutes(1));
        _service.StartAlarm(member, "member");

        Assert.False(_service.ElapseAlarm(roomId, first).IsSuccess);
        Assert.Equal(AlarmStatus.Running, _service.Join(AddPerson(), "late", roomId).Data!.Room.Alarm.Status);
    }

    [Fact]
    public void RevealClearAndRoleChanges_DoNotTouchARunningAlarm()
    {
        var (roomId, owner, member) = Room();
        var endsAt = _service.StartAlarm(member, "member").Data!.Alarm.EndsAt;

        _service.Vote(owner, "owner", "5");
        _service.ForceShowVotes(owner, "owner");
        _service.ClearVotes(member, "member");
        _service.ChangePersonType(member, "member", PersonTypes.Observer);

        var alarm = _service.Join(AddPerson(), "late", roomId).Data!.Room.Alarm;
        Assert.Equal(AlarmStatus.Running, alarm.Status);
        Assert.Equal(endsAt, alarm.EndsAt);
    }

    [Fact]
    public void ChangingTheLength_LeavesARunningAlarmAlone_AndAppliesFromTheNextStart()
    {
        var (roomId, owner, member) = Room();
        var endsAt = _service.StartAlarm(member, "member").Data!.Alarm.EndsAt!.Value;

        var changed = _service.UpdateConfiguration(owner, "owner", Configuration(alarmMinutes: 20));

        Assert.Equal(AlarmStatus.Running, changed.Data!.Alarm.Status);
        Assert.Equal(endsAt, changed.Data.Alarm.EndsAt);

        _service.ElapseAlarm(roomId, endsAt);
        _service.ResetAlarm(owner, "owner");
        Assert.Equal(_time.GetUtcNow().AddMinutes(20), _service.StartAlarm(member, "member").Data!.Alarm.EndsAt);
    }

    [Fact]
    public void ASaveThatOnlyChangesTheAlarm_KeepsShownVotesAndTheCountdown()
    {
        var (roomId, owner, member) = Room();
        _service.Vote(owner, "owner", "5");
        _service.Vote(member, "member", "8");
        Assert.NotNull(_rooms.Get(roomId)!.Revealed);

        var changed = _service.UpdateConfiguration(owner, "owner", Configuration(alarmMinutes: 12));

        Assert.False(changed.Data!.VotesCleared);
        Assert.NotNull(_rooms.Get(roomId)!.Revealed);
    }

    [Fact]
    public void TurningTheAlarmOff_ReturnsARunningAlarmToIdle()
    {
        var (_, owner, member) = Room();
        _service.StartAlarm(member, "member");

        var changed = _service.UpdateConfiguration(owner, "owner", Configuration(alarmEnabled: false));

        Assert.Equal(AlarmStatus.Idle, changed.Data!.Alarm.Status);
        Assert.Equal(ErrorCodes.AlarmDisabled, Assert.Single(_service.StartAlarm(member, "member").Errors).Code);
    }

    [Fact]
    public void ANonOwner_CannotChangeTheAlarmSetting()
    {
        var (_, _, member) = Room();

        var changed = _service.UpdateConfiguration(member, "member", Configuration(alarmMinutes: 30));

        Assert.Equal(ErrorCodes.NotRoomOwner, Assert.Single(changed.Errors).Code);
    }

    [Fact]
    public void ARestoredRoom_StartsIdle_WhateverStateItWasStoppedIn()
    {
        var (roomId, _, member) = Room();
        _service.StartAlarm(member, "member");

        var restored = System.Text.Json.JsonSerializer.Deserialize<RoomEntity>(System.Text.Json.JsonSerializer.Serialize(_rooms.Get(roomId)!))!;

        Assert.Null(restored.AlarmEndsAt);
        Assert.False(restored.AlarmElapsed);
    }
}
