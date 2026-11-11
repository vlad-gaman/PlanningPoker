namespace PlanningPoker.Common.Constants;

public static class PresenceConstants
{
    /// <summary>The appsettings section that overrides the presence options.</summary>
    public const string ConfigurationSection = "Presence";

    /// <summary>How long a person who lost their connection still counts as online, so a refresh goes unnoticed.</summary>
    public static readonly TimeSpan GracePeriod = TimeSpan.FromSeconds(10);

    /// <summary>How long after the drop an offgrid person keeps their place and vote before they are removed.</summary>
    public static readonly TimeSpan RemovalWindow = TimeSpan.FromMinutes(10);

    /// <summary>How long a room may have nobody online (people in their grace period count as online) before it is deleted.</summary>
    public static readonly TimeSpan EmptyRoomLifetime = TimeSpan.FromSeconds(10);

    /// <summary>How often the background sweep looks for people to turn offgrid or remove and for empty rooms.</summary>
    public static readonly TimeSpan SweepInterval = TimeSpan.FromSeconds(1);
}
