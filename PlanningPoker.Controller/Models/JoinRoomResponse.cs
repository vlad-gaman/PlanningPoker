namespace PlanningPoker.Controller.Models;

public class JoinRoomResponse
{
    public IReadOnlyList<PersonResponse> People { get; init; } = default!;

    public VoteStatusResponse VoteStatus { get; init; } = default!;

    /// <summary>The cards of the room card set, in display order.</summary>
    public IReadOnlyList<CardResponse> Cards { get; init; } = default!;

    /// <summary>The caller's own pick, or null when they have not voted. Lets a second tab show the selected card.</summary>
    public string? MyMark { get; init; }

    public Guid OwnerGuid { get; init; }

    public RoomConfigurationResponse Configuration { get; init; } = default!;
}
