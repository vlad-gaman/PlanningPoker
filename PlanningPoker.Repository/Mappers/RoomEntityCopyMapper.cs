using PlanningPoker.Repository.Models;

namespace PlanningPoker.Repository.Mappers;

public class RoomEntityCopyMapper(IRoomConfigurationEntityCopyMapper configurationMapper) : IRoomEntityCopyMapper
{
    public RoomEntity Map(RoomEntity source)
    {
        return new RoomEntity
        {
            Id = source.Id,
            OwnerGuid = source.OwnerGuid,
            MemberGuids = [.. source.MemberGuids],
            Votes = new Dictionary<Guid, string>(source.Votes),
            VotingEnabled = source.VotingEnabled,
            IsDisposed = source.IsDisposed,
            Configuration = configurationMapper.Map(source.Configuration)
        };
    }
}
