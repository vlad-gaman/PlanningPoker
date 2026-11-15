using System.Net;
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
        services.AddOptions<ForwardedHeadersTrustOptions>().BindConfiguration(ForwardedHeadersConstants.ConfigurationSection);
        services.AddOptions<ForwardedHeadersOptions>().Configure<IOptions<ForwardedHeadersTrustOptions>>((options, trust) =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

            // Only the last hop is read (ForwardLimit 1), which is the one the front end appended.
            options.ForwardLimit = 1;

            // Loopback stays trusted (the ASP.NET default). Trusting any sender is an explicit setting for a host that is reached
            // through its front end alone (App Service, whose address is not known ahead of time).
            if (trust.Value.TrustAnySender)
            {
                options.KnownIPNetworks.Clear();
                options.KnownProxies.Clear();
                return;
            }

            foreach (var proxy in trust.Value.KnownProxies)
            {
                options.KnownProxies.Add(IPAddress.Parse(proxy));
            }

            foreach (var network in trust.Value.KnownNetworks)
            {
                options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network));
            }
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
