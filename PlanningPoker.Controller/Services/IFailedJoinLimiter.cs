using System.Net;

namespace PlanningPoker.Controller.Services;

/// <summary>Limits how many failed <c>JoinRoom</c> calls one client address may make, so room ids cannot be guessed at speed.</summary>
public interface IFailedJoinLimiter
{
    /// <summary>True when the address has already used up its failed joins for the current window.</summary>
    bool IsLimited(IPAddress? client);

    /// <summary>Counts one failed join against the address.</summary>
    void RecordFailure(IPAddress? client);
}
