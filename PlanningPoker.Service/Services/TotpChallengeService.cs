using System.Security.Cryptography;
using System.Text;
using System.Buffers.Text;
using Microsoft.Extensions.Options;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Service.Services;

/// <summary>
/// Signs "username and expiry" with a key derived from the Operator's configured secrets, so a challenge needs no server state,
/// survives a restart and stops working when the password hash or TOTP secret changes. It proves the password step only; the code step still needs a valid code.
/// </summary>
public class TotpChallengeService(IOptions<OperatorOptions> options, TimeProvider time) : ITotpChallengeService
{
    public string Issue(string username)
    {
        var expiry = time.GetUtcNow().Add(options.Value.ChallengeLifetime).ToUnixTimeSeconds();
        var payload = Base64Url.EncodeToString(Encoding.UTF8.GetBytes($"{expiry}|{username}"));
        return $"{payload}.{Sign(payload)}";
    }

    public string? Read(string? challenge)
    {
        var parts = (challenge ?? string.Empty).Split('.');
        if (parts.Length != 2 || !FixedTimeEquals(Sign(parts[0]), parts[1]))
        {
            return null;
        }

        try
        {
            var text = Encoding.UTF8.GetString(Base64Url.DecodeFromChars(parts[0]));
            var separator = text.IndexOf('|');
            if (separator < 0 || !long.TryParse(text[..separator], out var expiry) || time.GetUtcNow().ToUnixTimeSeconds() >= expiry)
            {
                return null;
            }

            return text[(separator + 1)..];
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private string Sign(string payload)
    {
        var settings = options.Value;
        var key = SHA256.HashData(Encoding.UTF8.GetBytes($"pp-totp-challenge|{settings.PasswordHash}|{settings.TotpSecret}"));
        return Base64Url.EncodeToString(HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(payload)));
    }

    private static bool FixedTimeEquals(string left, string right)
    {
        return CryptographicOperations.FixedTimeEquals(SHA256.HashData(Encoding.UTF8.GetBytes(left)), SHA256.HashData(Encoding.UTF8.GetBytes(right)));
    }
}
