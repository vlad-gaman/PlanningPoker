namespace PlanningPoker.Controller.Models;

public class PersonResponse
{
    public Guid Guid { get; init; }

    public string Name { get; init; } = default!;

    public string PersonType { get; init; } = default!;

    /// <summary>True when the person is still in the room but has had no connection for longer than the grace period.</summary>
    public bool IsOffgrid { get; init; }
}
