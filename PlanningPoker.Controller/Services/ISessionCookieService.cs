using Microsoft.AspNetCore.Http;

namespace PlanningPoker.Controller.Services;

public interface ISessionCookieService
{
    /// <summary>The session token in the request's session cookie, or null when there is none.</summary>
    string? Read(HttpRequest request);

    void Write(HttpResponse response, string sessionToken);
}
