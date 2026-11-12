namespace PlanningPoker.Service.Services;

/// <summary>The short-lived proof that the password step succeeded, carried by the client to the code step.</summary>
public interface ITotpChallengeService
{
    string Issue(string username);

    /// <summary>The username in a challenge that is genuine and not expired, otherwise null.</summary>
    string? Read(string? challenge);
}
