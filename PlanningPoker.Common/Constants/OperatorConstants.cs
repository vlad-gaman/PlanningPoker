namespace PlanningPoker.Common.Constants;

public static class OperatorConstants
{
    public const string ConfigurationSection = "Operator";
    public const string AuthenticationScheme = "Operator";
    public const string PolicyName = "Operator";
    public const string CookieName = "pp-operator";
    public const string XsrfCookieName = "XSRF-TOKEN";
    public const string XsrfHeaderName = "X-XSRF-TOKEN";
    public const int MaxFailedLogins = 5;
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan FailureDelay = TimeSpan.FromMilliseconds(500);
    public static readonly TimeSpan SessionLifetime = TimeSpan.FromMinutes(30);
}
