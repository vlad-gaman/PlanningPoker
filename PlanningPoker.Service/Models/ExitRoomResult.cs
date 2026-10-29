namespace PlanningPoker.Service.Models;

public class ExitRoomResult
{
    public string RoomId { get; init; } = default!;

    public Person Person { get; init; } = default!;

    /// <summary>False when the person still has another connection to the room, so nobody else needs to hear about it.</summary>
    public bool PersonLeft { get; init; }

    public bool RoomDisposed { get; init; }

    public Person? NewOwner { get; init; }
}
