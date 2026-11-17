namespace PlanningPoker.Service.Services;

public interface ITotpService
{
    /// <summary>True when a TOTP secret is configured, which makes the second step part of every sign-in.</summary>
    bool IsEnabled { get; }

    /// <summary>
    /// True when the code is valid for the current time step (with a small tolerance either side) and has not been used before.
    /// A valid code is accepted once: reusing it, or presenting an earlier one, fails. Fails closed when the secret cannot be read.
    /// </summary>
    bool Verify(string? code);
}
