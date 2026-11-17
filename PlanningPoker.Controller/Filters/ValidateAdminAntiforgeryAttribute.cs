using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using PlanningPoker.Common.Models;
using PlanningPoker.Controller.Services;

namespace PlanningPoker.Controller.Filters;

/// <summary>Requires the anti-forgery token (X-XSRF-TOKEN header) on a state-changing admin action, answering with an ApiResponse when it is missing or invalid.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class ValidateAdminAntiforgeryAttribute : Attribute, IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var antiforgery = context.HttpContext.RequestServices.GetRequiredService<IAntiforgery>();
        if (await antiforgery.IsRequestValidAsync(context.HttpContext))
        {
            return;
        }

        var writer = context.HttpContext.RequestServices.GetRequiredService<IApiResponseWriter>();
        await writer.WriteAsync(context.HttpContext, ServiceResponse.Failure(ServiceError.AdminAntiforgeryInvalid()));
        context.Result = new EmptyResult();
    }
}
