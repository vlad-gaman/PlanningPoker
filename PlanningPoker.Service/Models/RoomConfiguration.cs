using PlanningPoker.Common.Constants;

namespace PlanningPoker.Service.Models;

public class RoomConfiguration
{
    public string CardSet { get; init; } = default!;

    public int CountdownSeconds { get; init; }

    public bool ShowFireworks { get; init; }

    public int MaxParticipants { get; init; }

    /// <summary>Whether the room's Alarm can be started. A stored room from before the Alarm existed reads as on.</summary>
    public bool AlarmEnabled { get; init; } = true;

    /// <summary>How many minutes the Alarm runs once started.</summary>
    public int AlarmMinutes { get; init; } = RoomConstraints.AlarmMinutesDefault;
}
