using PlanningPoker.Common.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Service.Services;

public interface IOperatorAuthService
{
    /// <summary>False when no username or password hash is configured, so nobody can sign in.</summary>
    bool IsConfigured { get; }

    /// <summary>
    /// The password step. With a TOTP secret configured a correct password does not sign in: it fails with
    /// <c>admin_totp_required</c> and a challenge for <see cref="CompleteTotpAsync"/>. Failures and lockout are as for any sign-in.
    /// </summary>
    Task<ServiceResponse<OperatorIdentity>> AuthenticateAsync(string? username, string? password, string? ipAddress);

    /// <summary>The code step: a genuine challenge and a valid, unused code sign the Operator in. A wrong code counts toward the lockout like a wrong password.</summary>
    Task<ServiceResponse<OperatorIdentity>> CompleteTotpAsync(string? challenge, string? code, string? ipAddress);
}
