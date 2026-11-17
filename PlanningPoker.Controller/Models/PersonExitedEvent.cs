namespace PlanningPoker.Controller.Models;

public class PersonExitedEvent
{
    public PersonResponse Person { get; init; } = default!;

    /// <summary>Null for an ordinary exit or removal; <c>removedByOperator</c> when the Operator removed the person.</summary>
    public string? Reason { get; init; }
}
