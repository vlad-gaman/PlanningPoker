using PlanningPoker.Common.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Service.Services;

public interface IOperatorAuthService
{
    /// <summary>False when no username or password hash is configured, so nobody can sign in.</summary>
    bool IsConfigured { get; }

    Task<ServiceResponse<OperatorIdentity>> AuthenticateAsync(string? username, string? password, string? ipAddress);
}
