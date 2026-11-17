using PlanningPoker.Common.Constants;

namespace PlanningPoker.Controller.Models;

public class HttpsOptions
{
    /// <summary>
    /// Whether plain-HTTP requests are redirected to HTTPS. Unset means yes everywhere except Development, so local work over
    /// plain HTTP (including from another device on the network) keeps working. Set it to false only for a host that is not reached through TLS at all.
    /// </summary>
    public bool? Redirect { get; set; }

    /// <summary>The port plain-HTTP requests are redirected to.</summary>
    public int RedirectPort { get; set; } = HttpsConstants.RedirectPort;

    /// <summary>The Strict-Transport-Security max-age, sent outside Development on HTTPS responses.</summary>
    public TimeSpan HstsMaxAge { get; set; } = HttpsConstants.HstsMaxAge;
}
