namespace PlanningPoker.Service.Models;

public class OwnershipTransferResult
{
    public string RoomId { get; init; } = default!;

    /// <summary>The new owner; null when the target already owned the room, so nothing changed.</summary>
    public Person? NewOwner { get; init; }
}
