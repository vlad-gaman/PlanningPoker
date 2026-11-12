using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using PlanningPoker.Common.Constants;
using PlanningPoker.Controller.Models;

namespace PlanningPoker.Controller.Extensions;

/// <summary>
/// HTTPS-only behind App Service, which ends TLS at its front end and passes plain HTTP on with <c>X-Forwarded-Proto</c>: the forwarded
/// headers are applied first so the app sees the real scheme and client address, then HSTS and the redirect to HTTPS (both off in
/// Development). The Operator cookie is Secure whenever this is not Development (see OperatorAuthenticationExtensions).
/// </summary>
public static class HttpsEnforcementExtensions
{
    public static IServiceCollection AddHttpsEnforcement(this IServiceCollection services)
    {
        services.AddOptions<HttpsOptions>().BindConfiguration(HttpsConstants.ConfigurationSection);
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

            // The front end's address is not known ahead of time, so any sender is trusted; only the last hop is read (ForwardLimit 1),
            // which is the one the front end appended. The app is reached through the front end only.
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
            options.ForwardLimit = 1;
        });
        services.AddOptions<HstsOptions>().Configure<IOptions<HttpsOptions>>((hsts, https) =>
        {
            hsts.MaxAge = https.Value.HstsMaxAge;
            hsts.IncludeSubDomains = false;
            hsts.Preload = false;
        });
        services.AddOptions<HttpsRedirectionOptions>().Configure<IOptions<HttpsOptions>>((redirection, https) =>
        {
            redirection.HttpsPort = https.Value.RedirectPort;
        });
        return services;
    }

    /// <summary>Forwarded headers first, so everything after sees the real scheme and address; then HSTS and the redirect where they apply. Call before everything else.</summary>
    public static IApplicationBuilder UseHttpsEnforcement(this IApplicationBuilder app)
    {
        var environment = app.ApplicationServices.GetRequiredService<IHostEnvironment>();
        var options = app.ApplicationServices.GetRequiredService<IOptions<HttpsOptions>>().Value;

        app.UseForwardedHeaders();
        if (!environment.IsDevelopment())
        {
            app.UseHsts();
        }

        if (ShouldRedirect(options, environment))
        {
            app.UseHttpsRedirection();
        }

        return app;
    }

    /// <summary>The redirect is on unless configured off, and off in Development unless configured on.</summary>
    public static bool ShouldRedirect(HttpsOptions options, IHostEnvironment environment)
    {
        return options.Redirect ?? !environment.IsDevelopment();
    }
}
