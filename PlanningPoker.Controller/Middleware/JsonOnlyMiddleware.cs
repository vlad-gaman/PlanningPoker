using Microsoft.AspNetCore.Http;
using PlanningPoker.Common.Constants;
using PlanningPoker.Common.Models;
using PlanningPoker.Controller.Constants;
using PlanningPoker.Controller.Services;

namespace PlanningPoker.Controller.Middleware;

public class JsonOnlyMiddleware(RequestDelegate next, IApiResponseWriter writer)
{
    private static readonly string[] StateChangingMethods = ["POST", "PUT", "PATCH", "DELETE"];

    public async Task InvokeAsync(HttpContext context)
    {
        var isStateChanging = StateChangingMethods.Contains(context.Request.Method, StringComparer.OrdinalIgnoreCase);
        if (isStateChanging && context.Request.Path.StartsWithSegments(RoutePrefixes.Api) && !context.Request.HasJsonContentType())
        {
            await writer.WriteAsync(context, ServiceResponse.Failure(
                ServiceError.Create(ErrorCodes.UnsupportedMediaType, "Request bodies must be application/json.")));
            return;
        }

        await next(context);
    }
}
