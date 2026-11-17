namespace PlanningPoker.Common.Constants;

public static class HttpsConstants
{
    /// <summary>The appsettings section that overrides the HTTPS options.</summary>
    public const string ConfigurationSection = "Https";

    /// <summary>The port plain-HTTP requests are redirected to.</summary>
    public const int RedirectPort = 443;

    /// <summary>
    /// How long a browser that has seen the site over HTTPS refuses plain HTTP for it. Short on purpose: the setting is hard to take
    /// back once a browser has cached it, and the site shares a host name with other apps on App Service.
    /// </summary>
    public static readonly TimeSpan HstsMaxAge = TimeSpan.FromDays(7);
}
