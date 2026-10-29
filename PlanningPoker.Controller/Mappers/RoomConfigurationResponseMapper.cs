using PlanningPoker.Controller.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Controller.Mappers;

public class RoomConfigurationResponseMapper : IRoomConfigurationResponseMapper
{
    public RoomConfigurationResponse Map(RoomConfiguration configuration)
    {
        return new RoomConfigurationResponse
        {
            CardSet = configuration.CardSet,
            CountdownSeconds = configuration.CountdownSeconds,
            ShowFireworks = configuration.ShowFireworks,
            MaxParticipants = configuration.MaxParticipants
        };
    }
}
