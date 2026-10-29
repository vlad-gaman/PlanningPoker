using PlanningPoker.Repository.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Service.Mappers;

public class RoomConfigurationMapper : IRoomConfigurationMapper
{
    public RoomConfiguration Map(RoomConfigurationEntity entity)
    {
        return new RoomConfiguration
        {
            CardSet = entity.CardSet,
            CountdownSeconds = entity.CountdownSeconds,
            ShowFireworks = entity.ShowFireworks,
            MaxParticipants = entity.MaxParticipants
        };
    }
}
