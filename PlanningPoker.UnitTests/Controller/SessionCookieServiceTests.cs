using Microsoft.AspNetCore.Http;
using PlanningPoker.Controller.Services;

namespace PlanningPoker.UnitTests.Controller;

public class SessionCookieServiceTests
{
    private const string Token = "kq3n0aQ9rZ4wYb7xP2cLmT8vF1sJ6dH5gE0uRtWyXzA";

    private readonly SessionCookieService _service = new();

    [Fact]
    public void Write_SetsHttpOnlySecureLaxSessionCookieHoldingTheToken()
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = "https";

        _service.Write(context.Response, Token);

        var raw = context.Response.Headers.SetCookie.ToString();
        Assert.StartsWith($"pp_session={Token};", raw);
        var header = raw.ToLowerInvariant();
        Assert.Contains("httponly", header);
        Assert.Contains("secure", header);
        Assert.Contains("samesite=lax", header);
        Assert.Contains("path=/", header);
        Assert.DoesNotContain("expires", header);
        Assert.DoesNotContain("max-age", header);
    }

    [Fact]
    public void Write_OverPlainHttp_OmitsSecureSoTheBrowserKeepsTheCookie()
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = "http";

        _service.Write(context.Response, Token);

        Assert.DoesNotContain("secure", context.Response.Headers.SetCookie.ToString().ToLowerInvariant());
    }

    [Fact]
    public void Read_SessionCookie_ReturnsTheToken()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Cookie = $"pp_session={Token}";

        Assert.Equal(Token, _service.Read(context.Request));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("pp_session=")]
    [InlineData("other=1")]
    public void Read_MissingOrEmpty_ReturnsNull(string? cookie)
    {
        var context = new DefaultHttpContext();
        if (cookie is not null)
        {
            context.Request.Headers.Cookie = cookie;
        }

        Assert.Null(_service.Read(context.Request));
    }
}
