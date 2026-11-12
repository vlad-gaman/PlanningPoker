using PlanningPoker.Repository.Models;

namespace PlanningPoker.Repository.Repositories;

/// <summary>Durable storage for the issues log.</summary>
public interface IIssueStore
{
    void Add(IssueEntry entry);

    /// <summary>The newest entries first.</summary>
    IReadOnlyList<IssueEntry> Recent(int limit);

    /// <summary>How many failed-login entries (the ones that carry an address) are at or after the given time.</summary>
    int CountFailedLogins(DateTimeOffset since);

    /// <summary>Removes entries older than the cutoff, then the oldest ones beyond <paramref name="maxEntries"/>.</summary>
    void Prune(DateTimeOffset olderThan, int maxEntries);
}
