using System.Security.Cryptography;
using OtpNet;

namespace PlanningPoker.Service.Services;

/// <summary>The <c>--generate-totp-secret</c> switch: prints a new random secret to put in <c>Operator:TotpSecret</c>, and the otpauth address an authenticator app can import.</summary>
public static class TotpSecretCommand
{
    public const string Switch = "--generate-totp-secret";

    public static int Run(string accountName, TextWriter output)
    {
        var secret = Base32Encoding.ToString(RandomNumberGenerator.GetBytes(20));
        output.WriteLine($"Operator:TotpSecret = {secret}");
        output.WriteLine($"otpauth://totp/PlanningPoker:{Uri.EscapeDataString(accountName)}?secret={secret}&issuer=PlanningPoker");
        return 0;
    }
}
