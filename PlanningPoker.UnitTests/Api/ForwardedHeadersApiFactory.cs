using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using PlanningPoker.Common.Constants;
using PlanningPoker.Controller.Models;

namespace PlanningPoker.UnitTests.Api;

/// <summary>
/// An Operator-enabled Production host with the <c>ForwardedHeaders</c> trust settings chosen. The in-memory server has no client address,
/// so the sender is taken from the <see cref="SenderHeader"/> request header, set before the forwarded headers are applied.
/// </summary>
public class ForwardedHeadersApiFactory(bool trustAnySender, string[]? knownProxies = null, string[]? knownNetworks = null) : AdminApiFactory
{
    public const string SenderHeader = "X-Test-Sender";

    protected override bool TrustAnySender => trustAnySender;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        AddAll(builder, nameof(ForwardedHeadersTrustOptions.KnownProxies), knownProxies);
        AddAll(builder, nameof(ForwardedHeadersTrustOptions.KnownNetworks), knownNetworks);
        builder.ConfigureServices(services => services.AddTransient<IStartupFilter, SenderAddressStartupFilter>());
    }

    /// <summary>A client that follows nothing, keeps no cookies and speaks plain HTTP, like the App Service front end passing a request on.</summary>
    public HttpClient CreateRaw()
    {
        return CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = false });
    }

    private static void AddAll(IWebHostBuilder builder, string name, string[]? values)
    {
        for (var index = 0; index < (values?.Length ?? 0); index++)
        {
            builder.UseSetting($"{ForwardedHeadersConstants.ConfigurationSection}:{name}:{index}", values![index]);
        }
    }

    private sealed class SenderAddressStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return app =>
            {
                app.Use((context, pipeline) =>
                {
                    if (context.Request.Headers.TryGetValue(SenderHeader, out var sender))
                    {
                        context.Connection.RemoteIpAddress = IPAddress.Parse(sender.ToString());
                    }

                    return pipeline();
                });
                next(app);
            };
        }
    }
}
