namespace PlanningPoker.Controller.Models;

/// <summary>
/// Which senders may set <c>X-Forwarded-For</c> and <c>X-Forwarded-Proto</c>. Unset means loopback only (the ASP.NET default), so a direct
/// client cannot spoof its address or scheme. A deployment behind a front end whose address is not known (App Service) sets
/// <see cref="TrustAnySender"/> explicitly.
/// </summary>
public class ForwardedHeadersTrustOptions
{
    /// <summary>Trust the forwarded headers from any sender. Only for a host that is reached through its front end alone.</summary>
    public bool TrustAnySender { get; set; }

    /// <summary>Addresses of proxies trusted in addition to loopback, for example <c>10.0.0.4</c>.</summary>
    public string[] KnownProxies { get; set; } = [];

    /// <summary>Networks in CIDR notation trusted in addition to loopback, for example <c>10.0.0.0/8</c>.</summary>
    public string[] KnownNetworks { get; set; } = [];
}
