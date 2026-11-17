using Microsoft.Extensions.Options;
using OtpNet;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Service.Services;

/// <summary>Verifies authenticator codes (RFC 6238, SHA-1, 6 digits, 30 seconds) and remembers the last accepted time step so a code works only once.</summary>
public class TotpService(IOptions<OperatorOptions> options, TimeProvider time) : ITotpService
{
    private const int Digits = 6;
    private const int PreviousSteps = 1;
    private const int FutureSteps = 1;

    private readonly Lock _gate = new();
    private long _lastAcceptedStep;

    public bool IsEnabled => !string.IsNullOrWhiteSpace(options.Value.TotpSecret);

    public bool Verify(string? code)
    {
        var digits = (code ?? string.Empty).Replace(" ", string.Empty);
        if (!IsEnabled || digits.Length != Digits || !digits.All(char.IsAsciiDigit))
        {
            return false;
        }

        byte[] key;
        try
        {
            key = Base32Encoding.ToBytes(options.Value.TotpSecret!.Trim().Replace(" ", string.Empty));
        }
        catch (ArgumentException)
        {
            return false;
        }

        if (key.Length == 0)
        {
            return false;
        }

        var totp = new Totp(key, totpSize: Digits);
        lock (_gate)
        {
            if (!totp.VerifyTotp(time.GetUtcNow().UtcDateTime, digits, out var matchedStep, new VerificationWindow(PreviousSteps, FutureSteps)) || matchedStep <= _lastAcceptedStep)
            {
                return false;
            }

            _lastAcceptedStep = matchedStep;
            return true;
        }
    }
}
