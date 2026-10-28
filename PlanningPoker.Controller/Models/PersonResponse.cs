namespace PlanningPoker.Controller.Models;

public class PersonResponse
{
    public Guid Guid { get; init; }

    public string Name { get; init; } = default!;

    public string PersonType { get; init; } = default!;
}
