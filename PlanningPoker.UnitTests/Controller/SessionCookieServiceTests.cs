using Microsoft.AspNetCore.Http;
using PlanningPoker.Controller.Services;

namespace PlanningPoker.UnitTests.Controller;

public class SessionCookieServiceTests
{
    private readonly SessionCookieService _service = new();

    [Fact]
    public void Write_SetsHttpOnlySecureLaxSessionCookie()
    {
        var context = new DefaultHttpContext();
        var guid = Guid.NewGuid();

        _service.Write(context.Response, guid);

        var header = context.Response.Headers.SetCookie.ToString().ToLowerInvariant();
        Assert.StartsWith($"pp_session={guid}", header);
        Assert.Contains("httponly", header);
        Assert.Contains("secure", header);
        Assert.Contains("samesite=lax", header);
        Assert.Contains("path=/", header);
        Assert.DoesNotContain("expires", header);
        Assert.DoesNotContain("max-age", header);
    }

    [Fact]
    public void Read_ValidCookie_ReturnsGuid()
    {
        var guid = Guid.NewGuid();
        var context = new DefaultHttpContext();
        context.Request.Headers.Cookie = $"pp_session={guid}";

        Assert.Equal(guid, _service.Read(context.Request));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("pp_session=not-a-guid")]
    [InlineData("other=1")]
    public void Read_MissingOrMalformed_ReturnsNull(string? cookie)
    {
        var context = new DefaultHttpContext();
        if (cookie is not null)
        {
            context.Request.Headers.Cookie = cookie;
        }

        Assert.Null(_service.Read(context.Request));
    }
}
