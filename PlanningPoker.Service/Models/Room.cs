namespace PlanningPoker.Service.Models;

public class Room
{
    public string Id { get; init; } = default!;

    public Guid OwnerGuid { get; init; }

    public bool VotingEnabled { get; init; }

    public RoomConfiguration Configuration { get; init; } = default!;
}
