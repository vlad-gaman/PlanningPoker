using PlanningPoker.Repository.Models;

namespace PlanningPoker.Repository.Mappers;

public class RoomEntityCopyMapper(IRoomConfigurationEntityCopyMapper configurationMapper, IRevealedVotesEntityCopyMapper revealedMapper) : IRoomEntityCopyMapper
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
            Revealed = source.Revealed is null ? null : revealedMapper.Map(source.Revealed),
            Revision = source.Revision,
            CountdownRevision = source.CountdownRevision,
            CountdownRemaining = source.CountdownRemaining,
            IsDisposed = source.IsDisposed,
            Configuration = configurationMapper.Map(source.Configuration)
        };
    }
}
