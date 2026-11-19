namespace PlanningPoker.Controller.Models;

public class RoomExistsResponse
{
    public string Id { get; init; } = default!;

    public bool Exists { get; init; }

    /// <summary>The words of a fun room name, in order; null when the name is not a fun room name.</summary>
    public IReadOnlyList<RoomNameWordResponse>? NameWords { get; init; }
}
