using System.Net;
using System.Threading.RateLimiting;
using Microsoft.Extensions.Options;
using PlanningPoker.Controller.Models;

namespace PlanningPoker.Controller.Services;

/// <summary>
/// A fixed window per client address. A check takes no permit (<c>AttemptAcquire(key, 0)</c> only asks whether any are left)
/// and a failure takes one, which is what makes only failures count.
/// </summary>
public sealed class FailedJoinLimiter : IFailedJoinLimiter, IDisposable
{
    private readonly IOptions<RateLimitingOptions> _options;
    private readonly PartitionedRateLimiter<string> _limiter;

    public FailedJoinLimiter(IOptions<RateLimitingOptions> options)
    {
        _options = options;
        _limiter = PartitionedRateLimiter.Create<string, string>(key =>
        {
            var rule = _options.Value.FailedJoin;
            return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = rule.PermitLimit,
                Window = rule.Window,
                QueueLimit = 0
            });
        });
    }

    public bool IsLimited(IPAddress? client)
    {
        if (!_options.Value.Enabled)
        {
            return false;
        }

        using var lease = _limiter.AttemptAcquire(ClientAddressPartition.KeyOf(client), 0);
        return !lease.IsAcquired;
    }

    public void RecordFailure(IPAddress? client)
    {
        if (_options.Value.Enabled)
        {
            using var lease = _limiter.AttemptAcquire(ClientAddressPartition.KeyOf(client));
        }
    }

    public void Dispose()
    {
        _limiter.Dispose();
    }
}
