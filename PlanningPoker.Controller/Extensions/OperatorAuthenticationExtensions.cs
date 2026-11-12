using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using PlanningPoker.Common.Constants;
using PlanningPoker.Common.Models;
using PlanningPoker.Controller.Services;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Controller.Extensions;

public static class OperatorAuthenticationExtensions
{
    /// <summary>
    /// Adds the Operator cookie scheme (its own named scheme, so the person-cookie endpoints are untouched), the policy that
    /// requires it, and the anti-forgery header. The cookie is Secure outside Development, HttpOnly, SameSite=Strict and slides.
    /// </summary>
    public static IServiceCollection AddOperatorAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication().AddCookie(OperatorConstants.AuthenticationScheme);
        services.AddAuthorizationBuilder().AddPolicy(OperatorConstants.PolicyName, policy => policy
            .AddAuthenticationSchemes(OperatorConstants.AuthenticationScheme)
            .RequireAuthenticatedUser());
        services.AddAntiforgery(options =>
        {
            options.HeaderName = OperatorConstants.XsrfHeaderName;
            options.Cookie.Name = "pp-operator-antiforgery";
            options.Cookie.SameSite = SameSiteMode.Strict;
        });
        services.AddOptions<CookieAuthenticationOptions>(OperatorConstants.AuthenticationScheme)
            .Configure<IOptions<OperatorOptions>, IHostEnvironment, IApiResponseWriter, TimeProvider>(
                (cookie, operatorOptions, environment, writer, time) =>
                {
                    cookie.TimeProvider = time;
                    cookie.Cookie.Name = OperatorConstants.CookieName;
                    cookie.Cookie.HttpOnly = true;
                    cookie.Cookie.SameSite = SameSiteMode.Strict;
                    cookie.Cookie.SecurePolicy = environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
                    cookie.ExpireTimeSpan = operatorOptions.Value.SessionLifetime;
                    cookie.SlidingExpiration = true;
                    cookie.Events.OnRedirectToLogin = context => Reply(writer, context.HttpContext, ServiceError.Unauthorized());
                    cookie.Events.OnRedirectToAccessDenied = context => Reply(writer, context.HttpContext, ServiceError.Forbidden());
                });
        return services;
    }

    /// <summary>Authentication and authorization, after the exception, JSON and session-person middleware.</summary>
    public static IApplicationBuilder UseOperatorAuthentication(this IApplicationBuilder app)
    {
        app.UseAuthentication();
        app.UseAuthorization();
        return app;
    }

    private static Task Reply(IApiResponseWriter writer, HttpContext context, ServiceError error)
    {
        return writer.WriteAsync(context, ServiceResponse.Failure(error));
    }
}
