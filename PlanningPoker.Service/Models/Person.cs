namespace PlanningPoker.Service.Models;

public class Person
{
    public Guid Guid { get; init; }

    public string Name { get; init; } = default!;

    public string PersonType { get; init; } = default!;

    /// <summary>True once the person has been without a connection for longer than the grace period. They are still a member.</summary>
    public bool IsOffgrid { get; init; }
}
