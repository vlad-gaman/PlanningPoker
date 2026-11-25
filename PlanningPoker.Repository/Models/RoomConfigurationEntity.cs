using PlanningPoker.Common.Constants;

namespace PlanningPoker.Repository.Models;

public class RoomConfigurationEntity
{
    public string CardSet { get; set; } = default!;

    public int CountdownSeconds { get; set; }

    public bool ShowFireworks { get; set; }

    public int MaxParticipants { get; set; }

    /// <summary>True when <paramref name="other"/> differs from this configuration in nothing but the Alarm settings.</summary>
    public bool DiffersOnlyInAlarmFrom(RoomConfigurationEntity other)
    {
        return CardSet == other.CardSet
            && CountdownSeconds == other.CountdownSeconds
            && ShowFireworks == other.ShowFireworks
            && MaxParticipants == other.MaxParticipants;
    }

    /// <summary>Whether the room's Alarm can be started. A stored room from before the Alarm existed reads as on.</summary>
    public bool AlarmEnabled { get; set; } = true;

    /// <summary>How many minutes the Alarm runs once started.</summary>
    public int AlarmMinutes { get; set; } = RoomConstraints.AlarmMinutesDefault;
}
