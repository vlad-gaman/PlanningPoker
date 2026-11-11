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
            DroppedAt = new Dictionary<Guid, DateTimeOffset>(source.DroppedAt),
            OffgridGuids = [.. source.OffgridGuids],
            Votes = new Dictionary<Guid, string>(source.Votes),
            DepartedVotes = new Dictionary<Guid, string>(source.DepartedVotes),
            VotingEnabled = source.VotingEnabled,
            Revealed = source.Revealed is null ? null : revealedMapper.Map(source.Revealed),
            Revision = source.Revision,
            CountdownRevision = source.CountdownRevision,
            CountdownRemaining = source.CountdownRemaining,
            IsDisposed = source.IsDisposed,
            RoundStartedAt = source.RoundStartedAt,
            CreatedAt = source.CreatedAt,
            Configuration = configurationMapper.Map(source.Configuration)
        };
    }
}
