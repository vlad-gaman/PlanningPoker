using Microsoft.AspNetCore.Http;

namespace PlanningPoker.Controller.Services;

public interface ISessionCookieService
{
    Guid? Read(HttpRequest request);

    void Write(HttpResponse response, Guid personGuid);
}
