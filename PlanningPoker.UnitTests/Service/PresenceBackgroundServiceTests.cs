using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using PlanningPoker.Common.Models;
using PlanningPoker.Service.Models;
using PlanningPoker.Service.Services;
using PlanningPoker.UnitTests.Builders;
using PlanningPoker.UnitTests.Fakes;

namespace PlanningPoker.UnitTests.Service;

public class PresenceBackgroundServiceTests
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(1);
    private static readonly Person Ann = new() { Guid = Guid.NewGuid(), Name = "Ann", PersonType = "dev" };

    private readonly FakeRoomService _rooms = new();
    private readonly FakeRoomPresenceNotifier _presence = new();
    private readonly FakeRoomCountdownNotifier _countdown = new();
    private readonly FakeRoomTimerService _timer = new();
    private readonly FakeTimeProvider _time = new();

    private PresenceBackgroundService Create(ILogger<PresenceBackgroundService>? logger = null)
    {
        return new PresenceBackgroundService(_rooms, _presence, _countdown, _timer, Options.Create(new PresenceOptions { SweepInterval = Interval }), _time, logger ?? NullLogger<PresenceBackgroundService>.Instance);
    }

    private static VotingFollowUp Countdown(long revision, int seconds)
    {
        return new VotingFollowUp { Countdown = new CountdownStart { Revision = revision, Seconds = seconds } };
    }

    [Fact]
    public async Task RunOnce_PersonWentOffgrid_AnnouncesThemAndStartsTheCountdownTheyCaused()
    {
        _rooms.PresenceSweepResult = new PresenceSweepResult
        {
            Offgrid = [new PersonOffgridResult { RoomId = "room", Person = Ann, FollowUp = Countdown(9, 3) }]
        };

        await Create().RunOnceAsync();

        Assert.Equal("room", Assert.Single(_presence.Offgrid).RoomId);
        Assert.Equal(("room", 9L), Assert.Single(_timer.Started));
        Assert.Equal(("room", 3), Assert.Single(_countdown.Ticks));
    }

    [Fact]
    public async Task RunOnce_PersonRemoved_AnnouncesThemAndRevealsWhenTheRestHadAllVoted()
    {
        var status = new VoteStatus { Votes = new Dictionary<Guid, string>() };
        _rooms.PresenceSweepResult = new PresenceSweepResult
        {
            Removed = [RoomResults.Exit(Ann, followUp: new VotingFollowUp { Revealed = status })]
        };

        await Create().RunOnceAsync();

        Assert.Equal(Ann.Guid, Assert.Single(_presence.Removed).Person.Guid);
        Assert.Same(status, Assert.Single(_countdown.Reveals).Status);
        Assert.Empty(_timer.Started);
    }

    [Fact]
    public async Task RunOnce_NothingChanged_AnnouncesNothing()
    {
        await Create().RunOnceAsync();

        Assert.Equal(1, _rooms.PresenceSweeps);
        Assert.Empty(_presence.Offgrid);
        Assert.Empty(_presence.Removed);
        Assert.Empty(_timer.Started);
    }

    [Fact]
    public async Task RunOnce_SweepThrows_LogsAndDoesNotThrow()
    {
        var logger = new RecordingLogger();
        _rooms.SweepPresenceException = new InvalidOperationException("boom");

        await Create(logger).RunOnceAsync();

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Same(_rooms.SweepPresenceException, entry.Exception);
    }

    [Fact]
    public async Task ExecuteAsync_SweepsEachTimeTheClockPassesTheInterval()
    {
        var service = Create();
        await service.StartAsync(CancellationToken.None);

        await AdvanceUntil(() => _rooms.PresenceSweeps >= 1);
        var afterFirst = _rooms.PresenceSweeps;
        await AdvanceUntil(() => _rooms.PresenceSweeps > afterFirst);

        await service.StopAsync(CancellationToken.None);
    }

    /// <summary>The host may start the loop on another thread, so the timer may not exist yet when the clock first moves.</summary>
    private async Task AdvanceUntil(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(10);
        while (!condition())
        {
            Assert.True(DateTime.UtcNow < deadline, "The condition was not met in time.");
            _time.Advance(Interval);
            await Task.Delay(10);
        }
    }

    private sealed class RecordingLogger : ILogger<PresenceBackgroundService>
    {
        public List<(LogLevel Level, string Message, Exception? Exception)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Entries.Add((logLevel, formatter(state, exception), exception));
        }
    }
}
