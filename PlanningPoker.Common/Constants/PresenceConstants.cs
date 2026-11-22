namespace PlanningPoker.Common.Constants;

public static class PresenceConstants
{
    /// <summary>The appsettings section that overrides the presence options.</summary>
    public const string ConfigurationSection = "Presence";

    /// <summary>How long a person who lost their connection still counts as online, so a refresh goes unnoticed.</summary>
    public static readonly TimeSpan GracePeriod = TimeSpan.FromSeconds(10);

    /// <summary>
    /// How long a person whose page said it was closing keeps their place before they are removed, unless they join again first.
    /// Short, but longer than a reload takes, because a reload says the same thing a closed tab does.
    /// </summary>
    public static readonly TimeSpan ClosingWindow = TimeSpan.FromSeconds(5);

    /// <summary>How long after the drop an offgrid person keeps their place and vote before they are removed.</summary>
    public static readonly TimeSpan RemovalWindow = TimeSpan.FromMinutes(10);

    /// <summary>How long a room may have nobody online (people in their grace period count as online) before it is deleted.</summary>
    public static readonly TimeSpan EmptyRoomLifetime = TimeSpan.FromSeconds(10);

    /// <summary>
    /// How long after its creation a room that nobody has joined yet is deleted. Short, because creating rooms needs no sign-in and
    /// a room nobody joined would otherwise live for the abandoned-room sweep (24 hours); long enough for the creator's browser to open the room.
    /// </summary>
    public static readonly TimeSpan NeverJoinedRoomLifetime = TimeSpan.FromMinutes(1);

    /// <summary>How often the background sweep looks for people to turn offgrid or remove and for empty rooms.</summary>
    public static readonly TimeSpan SweepInterval = TimeSpan.FromSeconds(1);
}
