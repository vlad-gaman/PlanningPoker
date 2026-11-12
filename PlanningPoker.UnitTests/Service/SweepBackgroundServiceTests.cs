using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using PlanningPoker.Service.Models;
using PlanningPoker.Service.Services;
using PlanningPoker.UnitTests.Fakes;

namespace PlanningPoker.UnitTests.Service;

public class SweepBackgroundServiceTests
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(10);

    private sealed class CountingSweep(Func<SweepResult>? behaviour = null) : ISweepService
    {
        private int _calls;

        public int Calls => Volatile.Read(ref _calls);

        public SweepResult Sweep()
        {
            Interlocked.Increment(ref _calls);
            return behaviour?.Invoke() ?? new SweepResult();
        }
    }

    private sealed class RecordingLogger : ILogger<SweepBackgroundService>
    {
        public List<(LogLevel Level, string Message, Exception? Exception)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            lock (Entries)
            {
                Entries.Add((logLevel, formatter(state, exception), exception));
            }
        }
    }

    private static SweepBackgroundService Create(ISweepService sweep, FakeTimeProvider time, ILogger<SweepBackgroundService>? logger = null, IIssueLog? issues = null, IServerStatistics? statistics = null)
    {
        return new SweepBackgroundService(sweep, Options.Create(new SweepOptions { Interval = Interval }), time, issues ?? new FakeIssueLog(), statistics ?? new ServerStatistics(time), logger ?? NullLogger<SweepBackgroundService>.Instance);
    }

    /// <summary>
    /// The host may start the loop on another thread, so the timer may not exist yet when the clock first moves. Moves the
    /// clock by one interval at a time until the condition holds.
    /// </summary>
    private static async Task AdvanceUntil(FakeTimeProvider time, Func<bool> condition)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(10);
        while (!condition())
        {
            Assert.True(DateTime.UtcNow < deadline, "The condition was not met in time.");
            time.Advance(Interval);
            await Task.Delay(10);
        }
    }

    [Fact]
    public async Task ExecuteAsync_RunsSweepEachTimeTheClockPassesTheInterval()
    {
        var time = new FakeTimeProvider();
        var sweep = new CountingSweep();
        var service = Create(sweep, time);
        await service.StartAsync(CancellationToken.None);

        Assert.Equal(0, sweep.Calls);

        await AdvanceUntil(time, () => sweep.Calls >= 1);
        var afterFirst = sweep.Calls;
        await AdvanceUntil(time, () => sweep.Calls > afterFirst);

        await service.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ExecuteAsync_ClockStaysBeforeTheInterval_DoesNotSweep()
    {
        var time = new FakeTimeProvider();
        var sweep = new CountingSweep();
        var service = Create(sweep, time);
        await service.StartAsync(CancellationToken.None);

        time.Advance(Interval - TimeSpan.FromSeconds(1));
        await Task.Delay(100);

        Assert.Equal(0, sweep.Calls);
        await service.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ExecuteAsync_StopRequested_EndsTheLoopWithoutError()
    {
        var time = new FakeTimeProvider();
        var sweep = new CountingSweep();
        var service = Create(sweep, time);
        await service.StartAsync(CancellationToken.None);
        await AdvanceUntil(time, () => sweep.Calls >= 1);

        await service.StopAsync(CancellationToken.None);

        Assert.True(service.ExecuteTask!.IsCompletedSuccessfully);
    }

    [Fact]
    public void RunOnce_SomethingRemoved_LogsInformation()
    {
        var logger = new RecordingLogger();
        var service = Create(new CountingSweep(() => new SweepResult { Rooms = 2, People = 3 }), new FakeTimeProvider(), logger);

        service.RunOnce();

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Information, entry.Level);
        Assert.Equal("Swept 2 rooms and 3 people", entry.Message);
    }

    [Fact]
    public void RunOnce_NothingRemoved_LogsNothing()
    {
        var logger = new RecordingLogger();
        var service = Create(new CountingSweep(), new FakeTimeProvider(), logger);

        service.RunOnce();

        Assert.Empty(logger.Entries);
    }

    [Fact]
    public void RunOnce_SweepThrows_LogsErrorAndDoesNotThrow()
    {
        var logger = new RecordingLogger();
        var failure = new InvalidOperationException("boom");
        var service = Create(new CountingSweep(() => throw failure), new FakeTimeProvider(), logger);

        service.RunOnce();

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Same(failure, entry.Exception);
    }

    [Fact]
    public async Task ExecuteAsync_SweepThrowsOnce_KeepsRunningOnTheNextTick()
    {
        var time = new FakeTimeProvider();
        var first = true;
        var throwing = new CountingSweep(() =>
        {
            if (first)
            {
                first = false;
                throw new InvalidOperationException("boom");
            }

            return new SweepResult();
        });
        var service = Create(throwing, time);
        await service.StartAsync(CancellationToken.None);

        await AdvanceUntil(time, () => throwing.Calls >= 1);
        await AdvanceUntil(time, () => throwing.Calls >= 2);

        await service.StopAsync(CancellationToken.None);
    }

    [Fact]
    public void RunOnce_ASweepThatRemovedSomething_RecordsItAndPrunesTheLog()
    {
        var issues = new FakeIssueLog();
        var service = Create(new CountingSweep(() => new SweepResult { Rooms = 2, People = 3 }), new FakeTimeProvider(), issues: issues);

        service.RunOnce();

        Assert.Equal([(PlanningPoker.Common.Models.IssueLevel.Info, "Sweep", "Swept 2 rooms and 3 people")], issues.Recorded);
        Assert.Equal(1, issues.Prunes);
    }

    [Fact]
    public void RunOnce_ASweepThatRemovedNothing_RecordsNothingButStillPrunes()
    {
        var issues = new FakeIssueLog();
        var service = Create(new CountingSweep(), new FakeTimeProvider(), issues: issues);

        service.RunOnce();

        Assert.Empty(issues.Recorded);
        Assert.Equal(1, issues.Prunes);
    }

    [Fact]
    public void RunOnce_FeedsTheServerStatistics()
    {
        var time = new FakeTimeProvider();
        var statistics = new ServerStatistics(time);
        var service = Create(new CountingSweep(() => new SweepResult { Rooms = 4, People = 1 }), time, statistics: statistics);

        service.RunOnce();

        Assert.Equal(4, statistics.RoomsSweptSince(DateTimeOffset.MinValue));
    }
}
