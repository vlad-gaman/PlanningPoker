namespace PlanningPoker.Controller.Models;

public class RoomDisposedEvent
{
    public string RoomId { get; init; } = default!;

    /// <summary>Null for an ordinary disposal; <c>closedByOperator</c> when the Operator closed the room.</summary>
    public string? Reason { get; init; }
}
