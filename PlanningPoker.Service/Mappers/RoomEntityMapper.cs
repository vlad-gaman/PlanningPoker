using PlanningPoker.Repository.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Service.Mappers;

public class RoomEntityMapper(IRoomConfigurationEntityMapper configurationMapper, TimeProvider time) : IRoomEntityMapper
{
    public RoomEntity Map(string id, Guid ownerGuid, RoomConfiguration configuration)
    {
        // The creator owns the room but is not a member until their first successful join; the first person admitted to a memberless room becomes owner. A room nobody ever joins is removed by the sweep.
        return new RoomEntity
        {
            Id = id,
            OwnerGuid = ownerGuid,
            MemberGuids = [],
            VotingEnabled = true,
            CreatedAt = time.GetUtcNow(),
            RoundStartedAt = time.GetUtcNow(),
            Configuration = configurationMapper.Map(configuration)
        };
    }
}
