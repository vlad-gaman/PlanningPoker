namespace PlanningPoker.Service.Models;

public class RoomConfiguration
{
    public string CardSet { get; init; } = default!;

    public int CountdownSeconds { get; init; }

    public bool ShowFireworks { get; init; }

    public int MaxParticipants { get; init; }
}
