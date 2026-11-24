using PlanningPoker.Repository.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Service.Mappers;

public class RoomMapper(IRoomConfigurationMapper configurationMapper, IRoundResultMapper roundResultMapper, IAlarmStateMapper alarmStateMapper) : IRoomMapper
{
    public Room Map(RoomEntity entity)
    {
        return new Room
        {
            Id = entity.Id,
            OwnerGuid = entity.OwnerGuid,
            VotingEnabled = entity.VotingEnabled,
            Configuration = configurationMapper.Map(entity.Configuration),
            Alarm = alarmStateMapper.Map(entity),
            RoundHistory = roundResultMapper.MapNewestFirst(entity.RoundHistory)
        };
    }
}
