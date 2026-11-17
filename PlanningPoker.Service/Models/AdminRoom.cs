namespace PlanningPoker.Service.Models;

/// <summary>A live room as the Operator sees it. Never carries votes.</summary>
public class AdminRoom
{
    public string Id { get; init; } = default!;

    public AdminRoomState State { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset LastActivityAt { get; init; }

    public IReadOnlyList<AdminPerson> People { get; init; } = [];
}
