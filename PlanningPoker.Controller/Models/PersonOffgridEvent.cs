namespace PlanningPoker.Controller.Models;

/// <summary>A person's grace period lapsed: they are still in the room with their vote, but no longer waited for.</summary>
public class PersonOffgridEvent
{
    public PersonResponse Person { get; init; } = default!;
}
