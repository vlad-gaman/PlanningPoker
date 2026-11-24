using PlanningPoker.Common.Constants;

namespace PlanningPoker.Controller.Models;

public class RoomConfigurationRequest
{
    public string CardSet { get; init; } = default!;

    public int CountdownSeconds { get; init; }

    public bool ShowFireworks { get; init; }

    public int MaxParticipants { get; init; }

    /// <summary>Whether the room's Alarm can be started; on when a client leaves it out.</summary>
    public bool AlarmEnabled { get; init; } = true;

    /// <summary>How many minutes the Alarm runs; the default when a client leaves it out.</summary>
    public int AlarmMinutes { get; init; } = RoomConstraints.AlarmMinutesDefault;
}
