namespace PlanningPoker.Controller.Models;

public class PersonJoinedEvent
{
    public PersonResponse Person { get; init; } = default!;

    public string? Vote { get; init; }

    public string PersonType { get; init; } = default!;
}
