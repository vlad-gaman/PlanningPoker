namespace PlanningPoker.Controller.Models;

public class AdminPersonResponse
{
    public Guid Id { get; init; }

    public string Name { get; init; } = default!;

    public string PersonType { get; init; } = default!;

    public bool IsOwner { get; init; }

    /// <summary>"online" or "offgrid".</summary>
    public string Status { get; init; } = default!;

    public DateTimeOffset? LastSeenAt { get; init; }
}
