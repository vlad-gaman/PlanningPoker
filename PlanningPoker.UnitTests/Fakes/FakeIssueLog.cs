using PlanningPoker.Common.Models;
using PlanningPoker.Repository.Models;
using PlanningPoker.Service.Services;

namespace PlanningPoker.UnitTests.Fakes;

/// <summary>An <see cref="IIssueLog"/> that remembers what was recorded.</summary>
public sealed class FakeIssueLog : IIssueLog
{
    public List<(IssueLevel Level, string Source, string Message)> Recorded { get; } = [];

    public List<(string? Address, bool Locked)> FailedLogins { get; } = [];

    /// <summary>The browser data of each failed login, in the same order as <see cref="FailedLogins"/>.</summary>
    public List<string?> FailedLoginBrowserData { get; } = [];

    public int Prunes { get; private set; }

    public void Record(IssueLevel level, string source, string message, Exception? exception = null)
    {
        Recorded.Add((level, source, message));
    }

    public void RecordFailedLogin(string? ipAddress, bool locked, string? browserData = null)
    {
        FailedLogins.Add((ipAddress, locked));
        FailedLoginBrowserData.Add(browserData);
    }

    public IReadOnlyList<IssueEntry> Recent(int limit) => [];

    public int FailedLoginCount { get; set; }

    public int CountFailedLogins(DateTimeOffset since) => FailedLoginCount;

    public void Prune()
    {
        Prunes++;
    }
}
