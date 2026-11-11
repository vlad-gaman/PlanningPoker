namespace PlanningPoker.Controller.Models;

/// <summary>A person who was announced offgrid has a connection again.</summary>
public class PersonBackEvent
{
    public PersonResponse Person { get; init; } = default!;
}
