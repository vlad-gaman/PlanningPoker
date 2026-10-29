using PlanningPoker.Repository.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Service.Mappers;

public class RoomEntityMapper : IRoomEntityMapper
{
    public RoomEntity Map(string id, Guid ownerGuid, RoomConfiguration configuration)
    {
        return new RoomEntity
        {
            Id = id,
            OwnerGuid = ownerGuid,
            MemberGuids = [ownerGuid],
            Configuration = new RoomConfigurationEntity
            {
                CardSet = configuration.CardSet,
                CountdownSeconds = configuration.CountdownSeconds,
                ShowFireworks = configuration.ShowFireworks,
                MaxParticipants = configuration.MaxParticipants
            }
        };
    }
}
