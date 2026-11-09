using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using PlanningPoker.Common.Constants;
using PlanningPoker.Common.Models;
using PlanningPoker.Controller.Models;

namespace PlanningPoker.Controller.Validations;

/// <summary>Decides whether a request may read /metrics: a Bearer token compared in constant time.</summary>
public class MetricsTokenValidation(IOptions<MetricsOptions> options, IHostEnvironment environment)
{
    private const string BearerPrefix = "Bearer ";

    /// <summary>True when no token is configured outside Development, so every request is refused.</summary>
    public bool IsFailClosed => !HasToken && !environment.IsDevelopment();

    private bool HasToken => !string.IsNullOrWhiteSpace(options.Value.ScrapeToken);

    public ServiceResponse Validate(string? authorizationHeader)
    {
        if (!HasToken)
        {
            return environment.IsDevelopment() ? ServiceResponse.Success() : Unauthorized();
        }

        if (authorizationHeader is null || !authorizationHeader.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return Unauthorized();
        }

        var presented = authorizationHeader[BearerPrefix.Length..].Trim();
        return Matches(presented, options.Value.ScrapeToken!) ? ServiceResponse.Success() : Unauthorized();
    }

    private static bool Matches(string presented, string expected)
    {
        var presentedHash = SHA256.HashData(Encoding.UTF8.GetBytes(presented));
        var expectedHash = SHA256.HashData(Encoding.UTF8.GetBytes(expected));
        return CryptographicOperations.FixedTimeEquals(presentedHash, expectedHash);
    }

    private static ServiceResponse Unauthorized()
    {
        return ServiceResponse.Failure(ServiceError.Create(ErrorCodes.Unauthorized, "A valid Bearer token is required."));
    }
}
