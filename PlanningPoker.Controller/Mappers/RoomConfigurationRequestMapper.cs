using PlanningPoker.Controller.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Controller.Mappers;

public class RoomConfigurationRequestMapper : IRoomConfigurationRequestMapper
{
    public RoomConfiguration Map(RoomConfigurationRequest request)
    {
        return new RoomConfiguration
        {
            CardSet = request.CardSet,
            CountdownSeconds = request.CountdownSeconds,
            ShowFireworks = request.ShowFireworks,
            MaxParticipants = request.MaxParticipants
        };
    }
}
