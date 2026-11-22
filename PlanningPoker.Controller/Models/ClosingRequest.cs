namespace PlanningPoker.Controller.Models;

public class ClosingRequest
{
    /// <summary>The hub connection of the page that is closing.</summary>
    public string ConnectionId { get; init; } = default!;
}
