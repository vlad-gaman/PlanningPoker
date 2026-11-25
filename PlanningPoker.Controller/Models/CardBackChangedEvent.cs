namespace PlanningPoker.Controller.Models;

public class CardBackChangedEvent
{
    public Guid PersonGuid { get; init; }

    public string CardBack { get; init; } = default!;
}
