namespace PlanningPoker.Controller.Models;

public class JoinRoomResponse
{
    public IReadOnlyList<PersonResponse> People { get; init; } = default!;

    public VoteStatusResponse VoteStatus { get; init; } = default!;

    public Guid OwnerGuid { get; init; }

    public RoomConfigurationResponse Configuration { get; init; } = default!;
}
