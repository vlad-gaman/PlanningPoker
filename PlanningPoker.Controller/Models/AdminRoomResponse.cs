namespace PlanningPoker.Controller.Models;

public class AdminRoomResponse
{
    public string Id { get; init; } = default!;

    /// <summary>"voting", "revealed" or "restored".</summary>
    public string State { get; init; } = default!;

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset LastActivityAt { get; init; }

    public List<AdminPersonResponse> People { get; init; } = [];
}
