using PlanningPoker.Controller.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Controller.Mappers;

public class RoomSettingsMapper : IRoomSettingsMapper
{
    public RoomSettings Map(CreateRoomRequest request)
    {
        return new RoomSettings
        {
            UseFunRoomName = request.UseFunRoomName,
            Configuration = new RoomConfiguration
            {
                CardSet = request.Configuration!.CardSet,
                CountdownSeconds = request.Configuration.CountdownSeconds,
                ShowFireworks = request.Configuration.ShowFireworks,
                MaxParticipants = request.Configuration.MaxParticipants
            }
        };
    }
}
