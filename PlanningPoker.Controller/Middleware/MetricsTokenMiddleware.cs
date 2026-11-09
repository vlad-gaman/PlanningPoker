using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using PlanningPoker.Controller.Services;
using PlanningPoker.Controller.Validations;

namespace PlanningPoker.Controller.Middleware;

public class MetricsTokenMiddleware(RequestDelegate next, MetricsTokenValidation validation, IApiResponseWriter writer)
{
    private const string BearerScheme = "Bearer";

    public async Task InvokeAsync(HttpContext context)
    {
        var result = validation.Validate(context.Request.Headers.Authorization.ToString());
        if (!result.IsSuccess)
        {
            context.Response.Headers[HeaderNames.WWWAuthenticate] = BearerScheme;
            await writer.WriteAsync(context, result);
            return;
        }

        await next(context);
    }
}
