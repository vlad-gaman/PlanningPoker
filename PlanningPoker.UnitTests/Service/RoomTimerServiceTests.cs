using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using PlanningPoker.Service.Models;
using PlanningPoker.Service.Services;
using PlanningPoker.UnitTests.Fakes;

namespace PlanningPoker.UnitTests.Service;

public class RoomTimerServiceTests
{
    private readonly FakeRoomService _rooms = new();
    private readonly FakeRoomCountdownNotifier _notifier = new();
    private readonly FakeTimeProvider _time = new();
    private readonly RoomTimerService _timer;

    public RoomTimerServiceTests()
    {
        _timer = new RoomTimerService(_rooms, _notifier, _time, NullLogger<RoomTimerService>.Instance);
    }

    private static TickResult Ticked(int remaining) => new() { Outcome = TickOutcome.Ticked, Remaining = remaining };

    private static TickResult Revealed() => new() { Outcome = TickOutcome.Revealed, Revealed = new VoteStatus { Votes = new Dictionary<Guid, string>(), VotingFinished = true } };

    /// <summary>Moves time on one second and waits for the ticker to handle it.</summary>
    private async Task AdvanceSecondAsync(Func<bool> handled)
    {
        _time.Advance(TimeSpan.FromSeconds(1));
        for (var attempt = 0; attempt < 200 && !handled(); attempt++)
        {
            await Task.Delay(5);
        }

        Assert.True(handled());
    }

    private bool TickCalled(int count)
    {
        lock (_rooms.TickCalls)
        {
            return _rooms.TickCalls.Count >= count;
        }
    }

    [Fact]
    public async Task Start_TicksEverySecondAndAnnouncesTheRevealAtTheEnd()
    {
        _rooms.TickResults.Enqueue(Ticked(2));
        _rooms.TickResults.Enqueue(Revealed());

        _timer.Start("room", 7);
        await AdvanceSecondAsync(() => TickCalled(1));
        await AdvanceSecondAsync(() => TickCalled(2));
        await Task.Delay(50);

        Assert.Equal([("room", 7L), ("room", 7L)], _rooms.TickCalls);
        Assert.Equal([("room", 2)], _notifier.Ticks);
        Assert.Equal("room", Assert.Single(_notifier.Reveals).RoomId);
    }

    [Fact]
    public async Task Start_StaleTick_StopsWithoutAnnouncing()
    {
        _timer.Start("room", 3);
        await AdvanceSecondAsync(() => TickCalled(1));
        await Task.Delay(50);
        _time.Advance(TimeSpan.FromSeconds(1));
        await Task.Delay(50);

        Assert.Single(_rooms.TickCalls);
        Assert.Empty(_notifier.Ticks);
        Assert.Empty(_notifier.Reveals);
    }

    [Fact]
    public async Task Start_NewerRevision_ReplacesTheRunningCountdown()
    {
        _rooms.TickResults.Enqueue(Ticked(4));
        _timer.Start("room", 1);
        _timer.Start("room", 2);

        await AdvanceSecondAsync(() => TickCalled(1));
        await Task.Delay(50);

        Assert.All(_rooms.TickCalls, call => Assert.Equal(2L, call.Revision));
    }

    [Fact]
    public async Task Start_OlderRevision_LeavesTheNewerCountdownRunning()
    {
        _rooms.TickResults.Enqueue(Ticked(4));
        _timer.Start("room", 5);
        _timer.Start("room", 4);

        await AdvanceSecondAsync(() => TickCalled(1));
        await Task.Delay(50);

        Assert.All(_rooms.TickCalls, call => Assert.Equal(5L, call.Revision));
    }
}
