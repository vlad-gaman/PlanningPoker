namespace PlanningPoker.Service.Models;

public class Person
{
    public Guid Guid { get; init; }

    public string Name { get; init; } = default!;

    public string PersonType { get; init; } = default!;
}
