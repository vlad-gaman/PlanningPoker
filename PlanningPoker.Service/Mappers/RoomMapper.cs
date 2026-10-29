using PlanningPoker.Repository.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Service.Mappers;

public class RoomMapper : IRoomMapper
{
    public Room Map(RoomEntity entity)
    {
        return new Room
        {
            Id = entity.Id,
            OwnerGuid = entity.OwnerGuid,
            VotingEnabled = entity.VotingEnabled,
            Configuration = new RoomConfiguration
            {
                CardSet = entity.Configuration.CardSet,
                CountdownSeconds = entity.Configuration.CountdownSeconds,
                ShowFireworks = entity.Configuration.ShowFireworks,
                MaxParticipants = entity.Configuration.MaxParticipants
            }
        };
    }
}
