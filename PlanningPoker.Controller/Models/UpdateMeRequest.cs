namespace PlanningPoker.Controller.Models;

public class UpdateMeRequest
{
    public string Name { get; init; } = default!;

    public string PersonType { get; init; } = default!;

    /// <summary>The design for the person's face-down card. Absent keeps the current one (the default for a new person).</summary>
    public string? CardBack { get; init; }
}
