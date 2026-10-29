using Microsoft.AspNetCore.Http;
using PlanningPoker.Controller.Constants;

namespace PlanningPoker.Controller.Services;

public class SessionCookieService : ISessionCookieService
{
    public Guid? Read(HttpRequest request)
    {
        return request.Cookies.TryGetValue(SessionConstants.CookieName, out var value) && Guid.TryParse(value, out var guid)
            ? guid
            : null;
    }

    public void Write(HttpResponse response, Guid personGuid)
    {
        response.Cookies.Append(SessionConstants.CookieName, personGuid.ToString(), new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = "/"
        });
    }
}
