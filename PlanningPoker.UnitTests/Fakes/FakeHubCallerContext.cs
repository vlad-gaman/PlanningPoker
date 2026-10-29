using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;

namespace PlanningPoker.UnitTests.Fakes;

/// <summary>A hub caller whose HTTP context (for the session cookie) and abort token are set by the test.</summary>
public sealed class FakeHubCallerContext : HubCallerContext
{
    private readonly CancellationToken _connectionAborted;
    private readonly FeatureCollection _features = new();

    public FakeHubCallerContext(string connectionId, HttpContext? httpContext, CancellationToken connectionAborted)
    {
        ConnectionId = connectionId;
        _connectionAborted = connectionAborted;
        _features.Set<IHttpContextFeature>(new FakeHttpContextFeature(httpContext));
    }

    public override string ConnectionId { get; }

    public override string? UserIdentifier => null;

    public override ClaimsPrincipal? User => null;

    public override IDictionary<object, object?> Items { get; } = new Dictionary<object, object?>();

    public override IFeatureCollection Features => _features;

    public override CancellationToken ConnectionAborted => _connectionAborted;

    public override void Abort()
    {
    }
}
