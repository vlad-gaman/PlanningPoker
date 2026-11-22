using PlanningPoker.Common.Constants;

namespace PlanningPoker.Service.Models;

public class PresenceOptions
{
    /// <summary>How long after losing their connection a person still counts as online.</summary>
    public TimeSpan GracePeriod { get; set; } = PresenceConstants.GracePeriod;

    /// <summary>How long after their page said it was closing a person who has not joined again is removed from the room. They show as offgrid straight away.</summary>
    public TimeSpan ClosingWindow { get; set; } = PresenceConstants.ClosingWindow;

    /// <summary>How long after the drop a person who has not returned is removed from the room, with their vote.</summary>
    public TimeSpan RemovalWindow { get; set; } = PresenceConstants.RemovalWindow;

    /// <summary>How long a room may have nobody online before it is deleted.</summary>
    public TimeSpan EmptyRoomLifetime { get; set; } = PresenceConstants.EmptyRoomLifetime;

    /// <summary>How long after its creation a room that nobody has joined yet is deleted.</summary>
    public TimeSpan NeverJoinedRoomLifetime { get; set; } = PresenceConstants.NeverJoinedRoomLifetime;

    /// <summary>How often the background sweep runs.</summary>
    public TimeSpan SweepInterval { get; set; } = PresenceConstants.SweepInterval;
}
