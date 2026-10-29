namespace PlanningPoker.Controller.Models;

public class OwnershipTransferredEvent
{
    public PersonResponse NewOwner { get; init; } = default!;
}
