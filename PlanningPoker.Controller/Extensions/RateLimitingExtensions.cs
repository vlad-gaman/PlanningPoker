using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PlanningPoker.Common.Constants;
using PlanningPoker.Common.Models;
using PlanningPoker.Controller.Models;
using PlanningPoker.Controller.Services;

namespace PlanningPoker.Controller.Extensions;

/// <summary>
/// Per-address fixed-window limits on the endpoints open to anonymous callers. The address is <c>Connection.RemoteIpAddress</c>, which is
/// the real client only after the forwarded headers are applied, so <see cref="UseRateLimiting"/> must run after <c>UseForwardedHeaders</c>
/// (see <see cref="HttpsEnforcementExtensions.UseHttpsEnforcement"/>, which is called first).
/// </summary>
public static class RateLimitingExtensions
{
    public static IServiceCollection AddRateLimiting(this IServiceCollection services)
    {
        services.AddOptions<RateLimitingOptions>().BindConfiguration(RateLimitingConstants.ConfigurationSection);
        services.AddSingleton<IFailedJoinLimiter, FailedJoinLimiter>();
        services.AddRateLimiter(limiter =>
        {
            limiter.AddPolicy(RateLimitingConstants.MeUpdatePolicy, context => Partition(context, options => options.MeUpdate));
            limiter.AddPolicy(RateLimitingConstants.RoomCreatePolicy, context => Partition(context, options => options.RoomCreate));
            limiter.AddPolicy(RateLimitingConstants.RoomLookupPolicy, context => Partition(context, options => options.RoomLookup));
            limiter.AddPolicy(RateLimitingConstants.PresenceClosingPolicy, context => Partition(context, options => options.PresenceClosing));
            limiter.OnRejected = WriteRejection;
        });
        return services;
    }

    public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder app)
    {
        return app.UseRateLimiter();
    }

    private static RateLimitPartition<string> Partition(HttpContext context, Func<RateLimitingOptions, RateLimitRule> ruleOf)
    {
        var options = context.RequestServices.GetRequiredService<IOptions<RateLimitingOptions>>().Value;
        var key = ClientAddressPartition.KeyOf(context.Connection.RemoteIpAddress);
        if (!options.Enabled)
        {
            return RateLimitPartition.GetNoLimiter(key);
        }

        var rule = ruleOf(options);
        return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = rule.PermitLimit,
            Window = rule.Window,
            QueueLimit = 0
        });
    }

    private static ValueTask WriteRejection(OnRejectedContext context, CancellationToken cancellationToken)
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();
        }

        var writer = context.HttpContext.RequestServices.GetRequiredService<IApiResponseWriter>();
        return new ValueTask(writer.WriteAsync(context.HttpContext, ServiceResponse.Failure(ServiceError.RateLimited())));
    }
}
