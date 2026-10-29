namespace PlanningPoker.Controller.Models;

public class RoomExistsResponse
{
    public string Id { get; init; } = default!;

    public bool Exists { get; init; }
}
