using PlanningPoker.Repository.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Service.Mappers;

public class RoomEntityMapper(IRoomConfigurationEntityMapper configurationMapper) : IRoomEntityMapper
{
    public RoomEntity Map(string id, Guid ownerGuid, RoomConfiguration configuration)
    {
        return new RoomEntity
        {
            Id = id,
            OwnerGuid = ownerGuid,
            MemberGuids = [ownerGuid],
            Configuration = configurationMapper.Map(configuration)
        };
    }
}
