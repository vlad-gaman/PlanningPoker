using PlanningPoker.Common.Models;

namespace PlanningPoker.Repository.Models;

/// <summary>One entry of the issues log. Never holds vote data.</summary>
public class IssueEntry
{
    /// <summary>Assigned by the store; entries are numbered in the order they were added.</summary>
    public long Id { get; set; }

    public DateTimeOffset Time { get; set; }

    public IssueLevel Level { get; set; }

    public string Source { get; set; } = default!;

    public string Message { get; set; } = default!;

    /// <summary>The client address; only ever set on failed-login entries.</summary>
    public string? IpAddress { get; set; }
}
