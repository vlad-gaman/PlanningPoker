using PlanningPoker.Repository.Models;

namespace PlanningPoker.Repository.Mappers;

public class RoomConfigurationEntityCopyMapper : IRoomConfigurationEntityCopyMapper
{
    public RoomConfigurationEntity Map(RoomConfigurationEntity source)
    {
        return new RoomConfigurationEntity
        {
            CardSet = source.CardSet,
            CountdownSeconds = source.CountdownSeconds,
            ShowFireworks = source.ShowFireworks,
            MaxParticipants = source.MaxParticipants
        };
    }
}
