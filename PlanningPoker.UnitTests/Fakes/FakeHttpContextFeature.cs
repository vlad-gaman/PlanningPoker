using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Connections.Features;

namespace PlanningPoker.UnitTests.Fakes;

/// <summary>Exposes a test HTTP context the way SignalR does for <c>HubCallerContext.GetHttpContext()</c>.</summary>
public sealed class FakeHttpContextFeature(HttpContext? httpContext) : IHttpContextFeature
{
    public HttpContext? HttpContext { get; set; } = httpContext;
}
