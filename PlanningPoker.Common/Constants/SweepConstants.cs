namespace PlanningPoker.Common.Constants;

public static class SweepConstants
{
    /// <summary>The appsettings section that overrides the sweep options.</summary>
    public const string ConfigurationSection = "Sweep";

    /// <summary>How long a room or person must be untouched before the sweep removes it.</summary>
    public static readonly TimeSpan AbandonedAfter = TimeSpan.FromHours(24);

    /// <summary>How often the background sweep runs.</summary>
    public static readonly TimeSpan Interval = TimeSpan.FromMinutes(10);
}
