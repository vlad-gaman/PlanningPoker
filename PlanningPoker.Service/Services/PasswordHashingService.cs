using Microsoft.AspNetCore.Identity;

namespace PlanningPoker.Service.Services;

/// <summary>Hashes passwords with the ASP.NET Core Identity hasher (PBKDF2, random salt, versioned format).</summary>
public class PasswordHashingService : IPasswordHashingService
{
    private readonly PasswordHasher<string> _hasher = new();

    public string Hash(string password)
    {
        return _hasher.HashPassword(string.Empty, password);
    }

    public bool Verify(string hash, string password)
    {
        return _hasher.VerifyHashedPassword(string.Empty, hash, password) != PasswordVerificationResult.Failed;
    }
}
