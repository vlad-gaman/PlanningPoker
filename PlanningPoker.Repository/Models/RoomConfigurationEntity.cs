namespace PlanningPoker.Repository.Models;

public class RoomConfigurationEntity
{
    public string CardSet { get; set; } = default!;

    public int CountdownSeconds { get; set; }

    public bool ShowFireworks { get; set; }

    public int MaxParticipants { get; set; }
}
