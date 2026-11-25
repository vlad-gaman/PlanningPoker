using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using PlanningPoker.Common.Models;
using PlanningPoker.Service.Models;
using PlanningPoker.Service.Services;
using PlanningPoker.UnitTests.Fakes;

namespace PlanningPoker.UnitTests.Service;

/// <summary>The wake-up that elapses a room's Alarm at its end time and tells the room.</summary>
public class RoomAlarmTimerServiceTests
{
    private readonly FakeRoomService _rooms = new();
    private readonly FakeRoomAlarmNotifier _notifier = new();
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
    private readonly RoomAlarmTimerService _timer;

    public RoomAlarmTimerServiceTests()
    {
        _timer = new RoomAlarmTimerService(_rooms, _notifier, _time, NullLogger<RoomAlarmTimerService>.Instance);
        _rooms.AlarmResult = ServiceResponse<AlarmChangeResult>.Success(new AlarmChangeResult { RoomId = "room", Alarm = new AlarmState { Status = AlarmStatus.Elapsed } });
    }

    private int Announced()
    {
        lock (_notifier.Changes)
        {
            return _notifier.Changes.Count;
        }
    }

    private async Task WaitForAsync(Func<bool> done)
    {
        for (var attempt = 0; attempt < 200 && !done(); attempt++)
        {
            await Task.Delay(5);
        }
    }

    [Fact]
    public async Task Schedule_AnnouncesTheElapsedAlarmWhenTheEndTimeComes_AndNotBefore()
    {
        _timer.Schedule("room", _time.GetUtcNow().AddMinutes(5));

        _time.Advance(TimeSpan.FromMinutes(4));
        await Task.Delay(50);
        Assert.Equal(0, Announced());

        _time.Advance(TimeSpan.FromMinutes(1));
        await WaitForAsync(() => Announced() == 1);

        Assert.Equal(AlarmStatus.Elapsed, Assert.Single(_notifier.Changes).Alarm.Status);
        Assert.Equal(["elapse"], _rooms.AlarmCalls);
    }

    [Fact]
    public async Task Schedule_AnnouncesNothingWhenTheRoomSaysTheRunIsStale()
    {
        _rooms.AlarmResult = ServiceResponse<AlarmChangeResult>.Failure(ServiceError.AlarmNotRunning());
        _timer.Schedule("room", _time.GetUtcNow().AddMinutes(1));

        _time.Advance(TimeSpan.FromMinutes(1));
        await WaitForAsync(() => _rooms.AlarmCalls.Count == 1);
        await Task.Delay(50);

        Assert.Equal(0, Announced());
    }

    [Fact]
    public async Task Cancel_StopsThePendingWakeUp()
    {
        _timer.Schedule("room", _time.GetUtcNow().AddMinutes(1));
        _timer.Cancel("room");

        _time.Advance(TimeSpan.FromMinutes(2));
        await Task.Delay(50);

        Assert.Empty(_rooms.AlarmCalls);
        Assert.Equal(0, Announced());
    }

    [Fact]
    public async Task Schedule_AgainForTheSameRoom_ReplacesTheOlderWakeUp()
    {
        _timer.Schedule("room", _time.GetUtcNow().AddMinutes(1));
        _timer.Schedule("room", _time.GetUtcNow().AddMinutes(3));

        _time.Advance(TimeSpan.FromMinutes(1));
        await Task.Delay(50);
        Assert.Empty(_rooms.AlarmCalls);

        _time.Advance(TimeSpan.FromMinutes(2));
        await WaitForAsync(() => Announced() == 1);
        Assert.Equal(1, Announced());
    }
}
