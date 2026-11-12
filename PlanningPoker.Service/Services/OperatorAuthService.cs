using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PlanningPoker.Common.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Service.Services;

/// <summary>Checks the Operator's credentials against the configured username and password hash, behind a lockout.</summary>
public class OperatorAuthService : IOperatorAuthService
{
    private readonly IOptions<OperatorOptions> _options;
    private readonly IPasswordHashingService _hashing;
    private readonly ILoginLockoutService _lockout;
    private readonly ILogger<OperatorAuthService> _logger;

    // Verified when nothing is configured, so every failure costs about the same time.
    private readonly string _decoyHash;

    public OperatorAuthService(
        IOptions<OperatorOptions> options,
        IPasswordHashingService hashing,
        ILoginLockoutService lockout,
        ILogger<OperatorAuthService> logger)
    {
        _options = options;
        _hashing = hashing;
        _lockout = lockout;
        _logger = logger;
        _decoyHash = hashing.Hash(Guid.NewGuid().ToString("N"));
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_options.Value.Username) && !string.IsNullOrWhiteSpace(_options.Value.PasswordHash);

    public async Task<ServiceResponse<OperatorIdentity>> AuthenticateAsync(string? username, string? password, string? ipAddress)
    {
        if (_lockout.IsLocked(ipAddress))
        {
            _logger.LogWarning("Operator sign-in refused for {IpAddress}: locked out.", ipAddress);
            return ServiceResponse<OperatorIdentity>.Failure(ServiceError.AdminLoginLocked());
        }

        var settings = _options.Value;
        var configured = IsConfigured;
        var passwordMatches = _hashing.Verify(configured ? settings.PasswordHash! : _decoyHash, password ?? string.Empty);
        var usernameMatches = configured && FixedTimeEquals(username ?? string.Empty, settings.Username!);
        if (configured && passwordMatches && usernameMatches)
        {
            _lockout.RecordSuccess(ipAddress);
            _logger.LogInformation("Operator signed in from {IpAddress}.", ipAddress);
            return ServiceResponse<OperatorIdentity>.Success(new OperatorIdentity { Username = settings.Username! });
        }

        _lockout.RecordFailure(ipAddress);
        _logger.LogWarning("Operator sign-in failed from {IpAddress}.", ipAddress);
        if (settings.FailureDelay > TimeSpan.Zero)
        {
            await Task.Delay(settings.FailureDelay);
        }

        return ServiceResponse<OperatorIdentity>.Failure(ServiceError.AdminInvalidCredentials());
    }

    private static bool FixedTimeEquals(string presented, string expected)
    {
        var presentedHash = SHA256.HashData(Encoding.UTF8.GetBytes(presented));
        var expectedHash = SHA256.HashData(Encoding.UTF8.GetBytes(expected));
        return CryptographicOperations.FixedTimeEquals(presentedHash, expectedHash);
    }
}
