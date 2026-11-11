using PlanningPoker.Common.Constants;

namespace PlanningPoker.Service.Models;

public class PresenceOptions
{
    /// <summary>How long after losing their connection a person still counts as online.</summary>
    public TimeSpan GracePeriod { get; set; } = PresenceConstants.GracePeriod;

    /// <summary>How long after the drop a person who has not returned is removed from the room, with their vote.</summary>
    public TimeSpan RemovalWindow { get; set; } = PresenceConstants.RemovalWindow;

    /// <summary>How long a room may have nobody online before it is deleted.</summary>
    public TimeSpan EmptyRoomLifetime { get; set; } = PresenceConstants.EmptyRoomLifetime;

    /// <summary>How often the background sweep runs.</summary>
    public TimeSpan SweepInterval { get; set; } = PresenceConstants.SweepInterval;
}
