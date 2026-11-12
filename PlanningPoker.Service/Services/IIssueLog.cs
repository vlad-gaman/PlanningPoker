using PlanningPoker.Common.Models;
using PlanningPoker.Repository.Models;

namespace PlanningPoker.Service.Services;

/// <summary>
/// The issues log: unhandled exceptions, warnings and errors, failed persistence writes, startup restores, sweep removals and Operator
/// actions. Recording never throws, so a fault in the log cannot break what is being logged.
/// </summary>
public interface IIssueLog
{
    void Record(IssueLevel level, string source, string message, Exception? exception = null);

    /// <summary>Records a failed or refused Operator sign-in. The only entry that keeps the client address.</summary>
    void RecordFailedLogin(string? ipAddress, bool locked);

    /// <summary>The newest entries first; none when the store cannot be read.</summary>
    IReadOnlyList<IssueEntry> Recent(int limit);

    /// <summary>Applies the retention limits: nothing older than the maximum age, at most the maximum number of entries.</summary>
    void Prune();
}
