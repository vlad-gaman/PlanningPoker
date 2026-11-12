namespace PlanningPoker.Service.Services;

public interface IPasswordHashingService
{
    string Hash(string password);

    /// <summary>True when the password matches the hash. A malformed hash never matches.</summary>
    bool Verify(string hash, string password);
}
