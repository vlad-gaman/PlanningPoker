namespace PlanningPoker.Service.Services;

public interface ISessionTokenGenerator
{
    /// <summary>A new secret session token: cryptographically random bytes, base64url-encoded.</summary>
    string Generate();
}
