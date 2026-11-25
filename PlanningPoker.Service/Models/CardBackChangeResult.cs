namespace PlanningPoker.Service.Models;

public class CardBackChangeResult
{
    public string RoomId { get; init; } = default!;

    public Guid PersonGuid { get; init; }

    /// <summary>The person's card back after the change.</summary>
    public string CardBack { get; init; } = default!;

    /// <summary>False when the person already had that card back, so nobody needs to hear about it.</summary>
    public bool Changed { get; init; }
}
