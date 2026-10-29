using PlanningPoker.Repository.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Service.Mappers;

public class RoomEntityMapper(IRoomConfigurationEntityMapper configurationMapper) : IRoomEntityMapper
{
    public RoomEntity Map(string id, Guid ownerGuid, RoomConfiguration configuration)
    {
        // The creator is a member with no connection yet. Rooms that nobody ever joins are not evicted in slice 1 (see the spec's eviction item).
        return new RoomEntity
        {
            Id = id,
            OwnerGuid = ownerGuid,
            MemberGuids = [ownerGuid],
            Configuration = configurationMapper.Map(configuration)
        };
    }
}
