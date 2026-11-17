using Microsoft.Extensions.Options;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Service.Services;

/// <summary>Counts failed sign-ins in memory, per IP address and for the single Operator account. A restart clears the counters.</summary>
public class LoginLockoutService(IOptions<OperatorOptions> options, TimeProvider time) : ILoginLockoutService
{
    private const string AccountKey = "account";
    private const string UnknownAddress = "unknown";

    private readonly Dictionary<string, Counter> _counters = [];
    private readonly Lock _gate = new();

    public bool IsLocked(string? ipAddress)
    {
        lock (_gate)
        {
            var now = time.GetUtcNow();
            return IsLockedAt(AccountKey, now) || IsLockedAt(AddressKey(ipAddress), now);
        }
    }

    public void RecordFailure(string? ipAddress)
    {
        lock (_gate)
        {
            var now = time.GetUtcNow();
            Prune(now);
            Fail(AccountKey, now);
            Fail(AddressKey(ipAddress), now);
        }
    }

    public void RecordSuccess(string? ipAddress)
    {
        lock (_gate)
        {
            _counters.Remove(AccountKey);
            _counters.Remove(AddressKey(ipAddress));
        }
    }

    private static string AddressKey(string? ipAddress)
    {
        return "ip:" + (string.IsNullOrWhiteSpace(ipAddress) ? UnknownAddress : ipAddress);
    }

    private bool IsLockedAt(string key, DateTimeOffset now)
    {
        return _counters.TryGetValue(key, out var counter) && counter.LockedUntil > now;
    }

    private void Fail(string key, DateTimeOffset now)
    {
        var settings = options.Value;
        var counter = _counters.GetValueOrDefault(key) ?? new Counter();
        if (counter.LockedUntil <= now && now - counter.LastFailure >= settings.LockoutDuration)
        {
            counter.Failures = 0;
        }

        counter.Failures++;
        counter.LastFailure = now;
        if (counter.Failures >= settings.MaxFailures)
        {
            counter.LockedUntil = now + settings.LockoutDuration;
            counter.Failures = 0;
        }

        _counters[key] = counter;
    }

    private void Prune(DateTimeOffset now)
    {
        var memory = options.Value.LockoutDuration;
        var expired = _counters.Where(pair => pair.Value.LockedUntil <= now && now - pair.Value.LastFailure >= memory).Select(pair => pair.Key).ToList();
        foreach (var key in expired)
        {
            _counters.Remove(key);
        }
    }

    private sealed class Counter
    {
        public int Failures { get; set; }

        public DateTimeOffset LastFailure { get; set; }

        public DateTimeOffset LockedUntil { get; set; }
    }
}
