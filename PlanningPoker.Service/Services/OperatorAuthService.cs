using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PlanningPoker.Common.Constants;
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
    private readonly IIssueLog _issues;
    private readonly ITotpService _totp;
    private readonly ITotpChallengeService _challenges;

    // Verified when nothing is configured, so every failure costs about the same time.
    private readonly string _decoyHash;

    public OperatorAuthService(
        IOptions<OperatorOptions> options,
        IPasswordHashingService hashing,
        ILoginLockoutService lockout,
        ILogger<OperatorAuthService> logger,
        IIssueLog issues,
        ITotpService totp,
        ITotpChallengeService challenges)
    {
        _options = options;
        _hashing = hashing;
        _lockout = lockout;
        _logger = logger;
        _issues = issues;
        _totp = totp;
        _challenges = challenges;
        _decoyHash = hashing.Hash(Guid.NewGuid().ToString("N"));
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_options.Value.Username) && !string.IsNullOrWhiteSpace(_options.Value.PasswordHash);

    public async Task<ServiceResponse<OperatorIdentity>> AuthenticateAsync(string? username, string? password, string? ipAddress)
    {
        if (_lockout.IsLocked(ipAddress))
        {
            _logger.LogWarning("Operator sign-in refused for {IpAddress}: locked out.", ipAddress);
            _issues.RecordFailedLogin(ipAddress, locked: true);
            return ServiceResponse<OperatorIdentity>.Failure(ServiceError.AdminLoginLocked());
        }

        var settings = _options.Value;
        var configured = IsConfigured;
        var passwordMatches = _hashing.Verify(configured ? settings.PasswordHash! : _decoyHash, password ?? string.Empty);
        var usernameMatches = configured && FixedTimeEquals(username ?? string.Empty, settings.Username!);
        if (configured && passwordMatches && usernameMatches)
        {
            if (_totp.IsEnabled)
            {
                // Not signed in yet, and the lockout counters are left alone: only a finished sign-in clears them.
                return ServiceResponse<OperatorIdentity>.Failure(ServiceError.AdminTotpRequired(_challenges.Issue(settings.Username!)));
            }

            return SignedIn(settings.Username!, ipAddress);
        }

        return await FailAsync(ipAddress, ServiceError.AdminInvalidCredentials());
    }

    public async Task<ServiceResponse<OperatorIdentity>> CompleteTotpAsync(string? challenge, string? code, string? ipAddress)
    {
        if (_lockout.IsLocked(ipAddress))
        {
            _logger.LogWarning("Operator sign-in refused for {IpAddress}: locked out.", ipAddress);
            _issues.RecordFailedLogin(ipAddress, locked: true);
            return ServiceResponse<OperatorIdentity>.Failure(ServiceError.AdminLoginLocked());
        }

        var username = IsConfigured && _totp.IsEnabled ? _challenges.Read(challenge) : null;
        if (username is null)
        {
            return ServiceResponse<OperatorIdentity>.Failure(ServiceError.AdminLoginExpired());
        }

        return _totp.Verify(code)
            ? SignedIn(username, ipAddress)
            : await FailAsync(ipAddress, ServiceError.AdminTotpInvalid());
    }

    private ServiceResponse<OperatorIdentity> SignedIn(string username, string? ipAddress)
    {
        _lockout.RecordSuccess(ipAddress);
        _logger.LogInformation("Operator signed in from {IpAddress}.", ipAddress);
        _issues.Record(IssueLevel.Info, IssueLogConstants.AdminSource, "Operator signed in");
        return ServiceResponse<OperatorIdentity>.Success(new OperatorIdentity { Username = username });
    }

    private async Task<ServiceResponse<OperatorIdentity>> FailAsync(string? ipAddress, ServiceError error)
    {
        _lockout.RecordFailure(ipAddress);
        _logger.LogWarning("Operator sign-in failed from {IpAddress}.", ipAddress);
        _issues.RecordFailedLogin(ipAddress, locked: false);
        if (_options.Value.FailureDelay > TimeSpan.Zero)
        {
            await Task.Delay(_options.Value.FailureDelay);
        }

        return ServiceResponse<OperatorIdentity>.Failure(error);
    }

    private static bool FixedTimeEquals(string presented, string expected)
    {
        var presentedHash = SHA256.HashData(Encoding.UTF8.GetBytes(presented));
        var expectedHash = SHA256.HashData(Encoding.UTF8.GetBytes(expected));
        return CryptographicOperations.FixedTimeEquals(presentedHash, expectedHash);
    }
}
