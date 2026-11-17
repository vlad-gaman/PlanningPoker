using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using PlanningPoker.Service.Models;
using PlanningPoker.Service.Services;

namespace PlanningPoker.UnitTests.Service;

public class LoginLockoutServiceTests
{
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero));

    private LoginLockoutService Create(int maxFailures = 3)
    {
        return new LoginLockoutService(Options.Create(new OperatorOptions { MaxFailures = maxFailures, LockoutDuration = TimeSpan.FromMinutes(15) }), _time);
    }

    [Fact]
    public void IsLocked_BelowTheLimit_IsFalse()
    {
        var service = Create();
        service.RecordFailure("a");
        service.RecordFailure("a");

        Assert.False(service.IsLocked("a"));
    }

    [Fact]
    public void IsLocked_AtTheLimit_LocksThatAddressAndTheAccount()
    {
        var service = Create();
        for (var i = 0; i < 3; i++)
        {
            service.RecordFailure("a");
        }

        Assert.True(service.IsLocked("a"));
        Assert.True(service.IsLocked("b"));
    }

    [Fact]
    public void IsLocked_FailuresSpreadOverAddresses_OnlyTheAccountLocks()
    {
        var service = Create(maxFailures: 3);
        service.RecordFailure("a");
        service.RecordFailure("b");
        service.RecordFailure("c");

        Assert.True(service.IsLocked("d"));
    }

    [Fact]
    public void IsLocked_AfterTheLockoutDuration_IsFalse()
    {
        var service = Create();
        for (var i = 0; i < 3; i++)
        {
            service.RecordFailure("a");
        }

        _time.Advance(TimeSpan.FromMinutes(15) + TimeSpan.FromSeconds(1));

        Assert.False(service.IsLocked("a"));
    }

    [Fact]
    public void RecordFailure_OldFailures_AreForgottenAfterTheLockoutDuration()
    {
        var service = Create();
        service.RecordFailure("a");
        service.RecordFailure("a");
        _time.Advance(TimeSpan.FromMinutes(16));
        service.RecordFailure("a");

        Assert.False(service.IsLocked("a"));
    }

    [Fact]
    public void RecordSuccess_ClearsTheCounters()
    {
        var service = Create();
        service.RecordFailure("a");
        service.RecordFailure("a");
        service.RecordSuccess("a");
        service.RecordFailure("a");
        service.RecordFailure("a");

        Assert.False(service.IsLocked("a"));
    }

    [Fact]
    public void IsLocked_NoAddress_StillCountsAgainstAnUnknownAddress()
    {
        var service = Create();
        for (var i = 0; i < 3; i++)
        {
            service.RecordFailure(null);
        }

        Assert.True(service.IsLocked(null));
    }
}
