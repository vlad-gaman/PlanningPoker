namespace PlanningPoker.Controller.Models;

public class RoomConfigurationUpdatedEvent
{
    public RoomConfigurationResponse Configuration { get; init; } = default!;

    /// <summary>The cards of the room card set after the change, in display order.</summary>
    public IReadOnlyList<CardResponse> Cards { get; init; } = default!;

    public string UpdatedBy { get; init; } = default!;

    /// <summary>The status after the change; cleared votes and a cancelled countdown show here.</summary>
    public VoteStatusResponse VoteStatus { get; init; } = default!;
}
