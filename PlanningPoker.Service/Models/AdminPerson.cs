namespace PlanningPoker.Service.Models;

/// <summary>A member of a room as the Operator sees them. Never carries a vote, nor whether the person has voted.</summary>
public class AdminPerson
{
    public Guid Id { get; init; }

    public string Name { get; init; } = default!;

    public string PersonType { get; init; } = default!;

    public bool IsOwner { get; init; }

    public AdminPersonStatus Status { get; init; }

    /// <summary>When the person lost their last connection to the room; null while they are connected.</summary>
    public DateTimeOffset? LastSeenAt { get; init; }
}
