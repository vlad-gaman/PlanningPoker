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
            MemberConnections = source.MemberConnections.ToDictionary(pair => pair.Key, pair => new HashSet<string>(pair.Value)),
            Votes = new Dictionary<Guid, string>(source.Votes),
            VotingEnabled = source.VotingEnabled,
            IsDisposed = source.IsDisposed,
            Configuration = configurationMapper.Map(source.Configuration)
        };
    }
}
