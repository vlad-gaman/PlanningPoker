namespace PlanningPoker.Service.Models;

public class RoomSettings
{
    public bool UseFunRoomName { get; init; }

    public RoomConfiguration Configuration { get; init; } = default!;
}
