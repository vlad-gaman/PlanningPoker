using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using PlanningPoker.Common.Models;
using PlanningPoker.Service.Mappers;
using PlanningPoker.Service.Models;
using PlanningPoker.Service.Services;
using PlanningPoker.UnitTests.Fakes;

namespace PlanningPoker.UnitTests.Service;

public class IssueLogTests
{
    private readonly FakeIssueStore _store = new();
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 3, 1, 12, 0, 0, TimeSpan.Zero));
    private readonly IssueLogOptions _options = new() { MaxEntries = 3, MaxAge = TimeSpan.FromDays(7), MaxMessageLength = 200 };

    private IssueLog Create()
    {
        var redactor = new IssueRedactor(Options.Create(_options));
        return new IssueLog(_store, redactor, new IssueEntryMapper(), Options.Create(_options), _time, NullLogger<IssueLog>.Instance);
    }

    [Fact]
    public void Record_StoresTheEntryWithTheCurrentTime()
    {
        Create().Record(IssueLevel.Error, "Sweep", "It broke");

        var entry = Assert.Single(_store.Entries);
        Assert.Equal(_time.GetUtcNow(), entry.Time);
        Assert.Equal(IssueLevel.Error, entry.Level);
        Assert.Equal("Sweep", entry.Source);
        Assert.Equal("It broke", entry.Message);
        Assert.Null(entry.IpAddress);
    }

    [Fact]
    public void Record_AnException_AddsOnlyItsTypeAndFirstLine()
    {
        var exception = new InvalidOperationException("Bad thing\nat Some.Stack.Frame()\nat Another()");

        Create().Record(IssueLevel.Error, "RoomHub", "Vote failed", exception);

        var message = Assert.Single(_store.Entries).Message;
        Assert.Contains("InvalidOperationException: Bad thing", message);
        Assert.DoesNotContain("Stack", message);
    }

    [Fact]
    public void Record_AQueryStringOrVoteValue_IsRedacted()
    {
        Create().Record(IssueLevel.Warning, "Api", "Bad request /x?token=abc vote: 13");

        var message = Assert.Single(_store.Entries).Message;
        Assert.DoesNotContain("token", message);
        Assert.DoesNotContain("13", message);
    }

    [Fact]
    public void RecordFailedLogin_KeepsTheAddress_AndNothingElseDoes()
    {
        var log = Create();

        log.Record(IssueLevel.Info, "Admin", "Operator signed in");
        log.RecordFailedLogin("203.0.113.7", locked: false);
        log.RecordFailedLogin("203.0.113.7", locked: true);

        Assert.Null(_store.Entries[0].IpAddress);
        Assert.Equal("203.0.113.7", _store.Entries[1].IpAddress);
        Assert.Equal(IssueLevel.Warning, _store.Entries[1].Level);
        Assert.Contains("locked", _store.Entries[2].Message);
    }

    [Fact]
    public void RecordFailedLogin_NoAddress_StoresNone()
    {
        Create().RecordFailedLogin("  ", locked: false);

        Assert.Null(Assert.Single(_store.Entries).IpAddress);
    }

    [Fact]
    public void Record_TheStoreFails_DoesNotThrow()
    {
        _store.Failing = true;

        Create().Record(IssueLevel.Error, "Sweep", "It broke");

        Assert.Empty(_store.Entries);
    }

    [Fact]
    public void Recent_TheStoreFails_ReturnsNone()
    {
        var log = Create();
        log.Record(IssueLevel.Info, "Startup", "Up");
        _store.Failing = true;

        Assert.Empty(log.Recent(10));
    }

    [Fact]
    public void Recent_ReturnsTheNewestFirst()
    {
        var log = Create();
        log.Record(IssueLevel.Info, "A", "first");
        log.Record(IssueLevel.Info, "A", "second");

        Assert.Equal(["second", "first"], log.Recent(10).Select(entry => entry.Message));
    }

    [Fact]
    public void CountFailedLogins_CountsTheEntriesCarryingAnAddress()
    {
        var log = Create();
        log.RecordFailedLogin("203.0.113.7", locked: false);
        log.Record(IssueLevel.Info, "Admin", "Operator signed in");

        Assert.Equal(1, log.CountFailedLogins(_time.GetUtcNow().AddHours(-1)));
    }

    [Fact]
    public void CountFailedLogins_TheStoreFails_IsZero()
    {
        _store.Failing = true;

        Assert.Equal(0, Create().CountFailedLogins(_time.GetUtcNow()));
    }

    [Fact]
    public void Prune_PassesTheAgeAndCountLimitsToTheStore()
    {
        Create().Prune();

        Assert.Equal(_time.GetUtcNow() - TimeSpan.FromDays(7), _store.PrunedBefore);
        Assert.Equal(3, _store.PrunedToMax);
    }

    [Fact]
    public void Prune_TheStoreFails_DoesNotThrow()
    {
        _store.Failing = true;

        Create().Prune();
    }
}

public class IssueRedactorTests
{
    private static IssueRedactor Create(int maxLength = 500)
    {
        return new IssueRedactor(Options.Create(new IssueLogOptions { MaxMessageLength = maxLength }));
    }

    [Fact]
    public void Redact_KeepsOnlyTheFirstLine()
    {
        Assert.Equal("first", Create().Redact("first\r\nsecond\nthird"));
    }

    [Fact]
    public void Redact_CutsAMessageToTheMaximumLength()
    {
        var result = Create(maxLength: 20).Redact(new string('x', 100));

        Assert.Equal(20, result.Length);
        Assert.EndsWith("...", result);
    }

    [Fact]
    public void Redact_ALongMessageAtFiveHundred_IsCappedAtFiveHundred()
    {
        Assert.Equal(500, Create().Redact(new string('y', 900)).Length);
    }

    [Theory]
    [InlineData("GET /api/rooms?token=abc&x=1 failed", "GET /api/rooms failed")]
    [InlineData("Redirect to /home?next=%2Fadmin", "Redirect to /home")]
    public void Redact_RemovesQueryStrings(string input, string expected)
    {
        Assert.Equal(expected, Create().Redact(input));
    }

    [Theory]
    [InlineData("Cast vote: 13 failed", "Cast vote: [redacted] failed")]
    [InlineData("votes=XL", "votes=[redacted]")]
    [InlineData("Card = 8.", "Card = [redacted]")]
    public void Redact_HidesVoteValues(string input, string expected)
    {
        Assert.Equal(expected, Create().Redact(input));
    }

    [Fact]
    public void Redact_ALabelFollowedByAWord_IsLeftAlone()
    {
        Assert.Equal("Unhandled exception in Vote: person not found", Create().Redact("Unhandled exception in Vote: person not found"));
    }

    [Fact]
    public void Describe_GivesTheTypeAndFirstLineOfTheMessage()
    {
        var text = Create().Describe(new IOException("disk gone\nat stack"));

        Assert.Equal("IOException: disk gone", text);
    }
}

public class IssueLoggerProviderTests
{
    private static (ILogger Logger, FakeIssueLog Log) Create(string category)
    {
        var log = new FakeIssueLog();
        var services = new ServiceCollection();
        services.AddSingleton<IIssueLog>(log);
        var provider = new IssueLoggerProvider(services.BuildServiceProvider());
        return (provider.CreateLogger(category), log);
    }

    [Fact]
    public void Warning_IsRecordedWithTheLastSegmentOfTheCategory()
    {
        var (logger, log) = Create("PlanningPoker.Repository.Services.RoomPersistenceWriter");

        logger.LogWarning("Write failed for {RoomId}", "abc");

        var entry = Assert.Single(log.Recorded);
        Assert.Equal(IssueLevel.Warning, entry.Level);
        Assert.Equal("RoomPersistenceWriter", entry.Source);
        Assert.Equal("Write failed for abc", entry.Message);
    }

    [Fact]
    public void Error_IsRecordedAsAnError()
    {
        var (logger, log) = Create("Some.Category");

        logger.LogError(new InvalidOperationException("boom"), "Unhandled");

        Assert.Equal(IssueLevel.Error, Assert.Single(log.Recorded).Level);
    }

    [Theory]
    [InlineData(LogLevel.Information)]
    [InlineData(LogLevel.Debug)]
    [InlineData(LogLevel.Trace)]
    public void BelowWarning_IsNotRecorded(LogLevel level)
    {
        var (logger, log) = Create("Some.Category");

        logger.Log(level, "routine");

        Assert.Empty(log.Recorded);
    }

    [Theory]
    [InlineData("PlanningPoker.Service.Services.IssueLog")]
    [InlineData("PlanningPoker.Repository.Repositories.SqliteIssueStore")]
    [InlineData("PlanningPoker.Service.Services.OperatorAuthService")]
    public void TheLogItselfAndTheSignInService_AreNotRecorded(string category)
    {
        var (logger, log) = Create(category);

        logger.LogError("would loop or duplicate");

        Assert.Empty(log.Recorded);
        Assert.False(logger.IsEnabled(LogLevel.Error));
    }

    [Fact]
    public void ALogThatIsNotRegistered_IsIgnoredWithoutThrowing()
    {
        var provider = new IssueLoggerProvider(new ServiceCollection().BuildServiceProvider());

        provider.CreateLogger("Some.Category").LogError("nobody is listening");
    }
}
