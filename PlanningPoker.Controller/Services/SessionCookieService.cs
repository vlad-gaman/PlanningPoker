using Microsoft.AspNetCore.Http;
using PlanningPoker.Controller.Constants;

namespace PlanningPoker.Controller.Services;

public class SessionCookieService : ISessionCookieService
{
    public string? Read(HttpRequest request)
    {
        return request.Cookies.TryGetValue(SessionConstants.CookieName, out var value) && !string.IsNullOrEmpty(value)
            ? value
            : null;
    }

    public void Write(HttpResponse response, string sessionToken)
    {
        response.Cookies.Append(SessionConstants.CookieName, sessionToken, new CookieOptions
        {
            HttpOnly = true,
            // Browsers drop a Secure cookie over plain HTTP, which would break the app on the local network.
            Secure = response.HttpContext.Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Path = "/"
        });
    }
}
