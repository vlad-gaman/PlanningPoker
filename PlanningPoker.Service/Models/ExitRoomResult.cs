namespace PlanningPoker.Service.Models;

public class ExitRoomResult
{
    public string RoomId { get; init; } = default!;

    public Person Person { get; init; } = default!;

    public bool RoomDisposed { get; init; }

    public Person? NewOwner { get; init; }
}
