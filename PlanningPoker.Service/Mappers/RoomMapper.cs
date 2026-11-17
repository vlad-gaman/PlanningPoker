using PlanningPoker.Repository.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Service.Mappers;

public class RoomMapper(IRoomConfigurationMapper configurationMapper, IRoundResultMapper roundResultMapper) : IRoomMapper
{
    public Room Map(RoomEntity entity)
    {
        return new Room
        {
            Id = entity.Id,
            OwnerGuid = entity.OwnerGuid,
            VotingEnabled = entity.VotingEnabled,
            Configuration = configurationMapper.Map(entity.Configuration),
            RoundHistory = roundResultMapper.MapNewestFirst(entity.RoundHistory)
        };
    }
}
