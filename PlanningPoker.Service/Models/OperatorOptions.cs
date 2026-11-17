using PlanningPoker.Common.Constants;

namespace PlanningPoker.Service.Models;

public class OperatorOptions
{
    /// <summary>The Operator's username. Never stored in appsettings files; supplied by the environment.</summary>
    public string? Username { get; set; }

    /// <summary>The Operator's password hash (ASP.NET Core Identity format). Never stored in appsettings files.</summary>
    public string? PasswordHash { get; set; }

    /// <summary>The Base32 secret of the Operator's authenticator app. Never stored in appsettings files. Unset means no second step.</summary>
    public string? TotpSecret { get; set; }

    /// <summary>How long the password step stays valid while the code is entered.</summary>
    public TimeSpan ChallengeLifetime { get; set; } = OperatorConstants.ChallengeLifetime;

    /// <summary>Failed sign-ins (per IP address, and for the Operator account) that trigger a lockout.</summary>
    public int MaxFailures { get; set; } = OperatorConstants.MaxFailedLogins;

    /// <summary>How long a lockout lasts, and how long failures are remembered.</summary>
    public TimeSpan LockoutDuration { get; set; } = OperatorConstants.LockoutDuration;

    /// <summary>A flat wait added to every failed sign-in to slow guessing down.</summary>
    public TimeSpan FailureDelay { get; set; } = OperatorConstants.FailureDelay;

    /// <summary>The sliding lifetime of the session cookie.</summary>
    public TimeSpan SessionLifetime { get; set; } = OperatorConstants.SessionLifetime;
}
