namespace PlanningPoker.Controller.Models;

public class RoomConfigurationResponse
{
    public string CardSet { get; init; } = default!;

    public int CountdownSeconds { get; init; }

    public bool ShowFireworks { get; init; }

    public int MaxParticipants { get; init; }
}
