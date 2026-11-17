namespace PlanningPoker.Controller.Models;

public class PersonTypeChangedEvent
{
    public PersonResponse Person { get; init; } = default!;

    /// <summary>The status after the change; a countdown the change cancelled shows as null.</summary>
    public VoteStatusResponse VoteStatus { get; init; } = default!;
}
