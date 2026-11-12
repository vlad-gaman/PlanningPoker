namespace PlanningPoker.Common.Constants;

public static class IssueLogConstants
{
    /// <summary>The appsettings section that overrides the issues log options.</summary>
    public const string ConfigurationSection = "IssueLog";

    /// <summary>The log keeps at most this many entries; the oldest go first.</summary>
    public const int MaxEntries = 500;

    /// <summary>An entry older than this is removed.</summary>
    public static readonly TimeSpan MaxAge = TimeSpan.FromDays(7);

    /// <summary>A message is cut to this many characters.</summary>
    public const int MaxMessageLength = 500;

    /// <summary>The source shown for entries about the Operator.</summary>
    public const string AdminSource = "Admin";
}
