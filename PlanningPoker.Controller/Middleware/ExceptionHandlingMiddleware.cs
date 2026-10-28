using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using PlanningPoker.Common.Constants;
using PlanningPoker.Common.Models;
using PlanningPoker.Controller.Services;

namespace PlanningPoker.Controller.Middleware;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, IApiResponseWriter writer)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
            if (context.Response.HasStarted)
            {
                throw;
            }

            context.Response.Clear();
            await writer.WriteAsync(context, ServiceResponse.Failure(
                ServiceError.Create(ErrorCodes.InternalError, "An unexpected error occurred.")));
        }
    }
}
