using PlanningPoker.Repository.Models;
using PlanningPoker.Repository.Repositories;

namespace PlanningPoker.UnitTests.Fakes;

/// <summary>An in-memory <see cref="IIssueStore"/> that can be told to fail.</summary>
public sealed class FakeIssueStore : IIssueStore
{
    private long _nextId = 1;

    public List<IssueEntry> Entries { get; } = [];

    public bool Failing { get; set; }

    public DateTimeOffset? PrunedBefore { get; private set; }

    public int? PrunedToMax { get; private set; }

    public void Add(IssueEntry entry)
    {
        if (Failing)
        {
            throw new IOException("The store is down.");
        }

        entry.Id = _nextId++;
        Entries.Add(entry);
    }

    public IReadOnlyList<IssueEntry> Recent(int limit)
    {
        if (Failing)
        {
            throw new IOException("The store is down.");
        }

        return [.. Entries.OrderByDescending(entry => entry.Id).Take(limit)];
    }

    public int CountFailedLogins(DateTimeOffset since)
    {
        if (Failing)
        {
            throw new IOException("The store is down.");
        }

        return Entries.Count(entry => entry.IpAddress is not null && entry.Time >= since);
    }

    public void Prune(DateTimeOffset olderThan, int maxEntries)
    {
        if (Failing)
        {
            throw new IOException("The store is down.");
        }

        PrunedBefore = olderThan;
        PrunedToMax = maxEntries;
        Entries.RemoveAll(entry => entry.Time < olderThan);
        while (Entries.Count > maxEntries)
        {
            Entries.RemoveAt(0);
        }
    }
}
