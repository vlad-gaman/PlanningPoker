namespace PlanningPoker.Controller.Models;

public class CreateRoomRequest
{
    public bool UseFunRoomName { get; init; }

    public RoomConfigurationRequest? Configuration { get; init; }
}
