using Microsoft.AspNetCore.Http;
using PlanningPoker.Controller.Constants;
using PlanningPoker.Controller.Extensions;
using PlanningPoker.Controller.Services;
using PlanningPoker.Service.Services;

namespace PlanningPoker.Controller.Middleware;

public class SessionPersonMiddleware(RequestDelegate next, ISessionCookieService cookies, IPersonService persons, IApiResponseWriter writer)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var result = persons.Get(cookies.Read(context.Request));
        if (result.IsSuccess)
        {
            context.SetSessionPerson(result.Data!);
        }
        else if (context.Request.Path.StartsWithSegments(RoutePrefixes.Hubs))
        {
            await writer.WriteAsync(context, result);
            return;
        }

        await next(context);
    }
}
