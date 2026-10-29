using PlanningPoker.Repository.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Service.Mappers;

public class RoomConfigurationEntityMapper : IRoomConfigurationEntityMapper
{
    public RoomConfigurationEntity Map(RoomConfiguration configuration)
    {
        return new RoomConfigurationEntity
        {
            CardSet = configuration.CardSet,
            CountdownSeconds = configuration.CountdownSeconds,
            ShowFireworks = configuration.ShowFireworks,
            MaxParticipants = configuration.MaxParticipants
        };
    }
}
