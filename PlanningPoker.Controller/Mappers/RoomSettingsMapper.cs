using PlanningPoker.Controller.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Controller.Mappers;

public class RoomSettingsMapper(IRoomConfigurationRequestMapper configurationMapper) : IRoomSettingsMapper
{
    public RoomSettings Map(CreateRoomRequest request)
    {
        return new RoomSettings
        {
            UseFunRoomName = request.UseFunRoomName,
            Configuration = configurationMapper.Map(request.Configuration!)
        };
    }
}
