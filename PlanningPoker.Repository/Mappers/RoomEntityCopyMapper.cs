using PlanningPoker.Repository.Models;

namespace PlanningPoker.Repository.Mappers;

public class RoomEntityCopyMapper : IRoomEntityCopyMapper
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
            ClosingAt = new Dictionary<Guid, DateTimeOffset>(source.ClosingAt),
            OffgridGuids = [.. source.OffgridGuids],
            Roles = new Dictionary<Guid, string>(source.Roles),
            Votes = new Dictionary<Guid, string>(source.Votes),
            DepartedVotes = new Dictionary<Guid, string>(source.DepartedVotes),
            VotingEnabled = source.VotingEnabled,
            Revealed = source.Revealed is null ? null : MapRevealed(source.Revealed),
            Revision = source.Revision,
            CountdownRevision = source.CountdownRevision,
            CountdownRemaining = source.CountdownRemaining,
            IsDisposed = source.IsDisposed,
            RoundStartedAt = source.RoundStartedAt,
            CreatedAt = source.CreatedAt,
            LastActivityAt = source.LastActivityAt,
            AwaitingReturn = source.AwaitingReturn,
            Configuration = MapConfiguration(source.Configuration),
            RoundsRevealed = source.RoundsRevealed,
            RoundHistory = [.. source.RoundHistory.Select(result => new RoundResultEntity { Round = result.Round, AverageMark = result.AverageMark, RevealedAt = result.RevealedAt })]
        };
    }

    private static RoomConfigurationEntity MapConfiguration(RoomConfigurationEntity source)
    {
        return new RoomConfigurationEntity
        {
            CardSet = source.CardSet,
            CountdownSeconds = source.CountdownSeconds,
            ShowFireworks = source.ShowFireworks,
            MaxParticipants = source.MaxParticipants
        };
    }

    private static RevealedVotesEntity MapRevealed(RevealedVotesEntity source)
    {
        return new RevealedVotesEntity
        {
            EveryoneVoted = source.EveryoneVoted,
            Votes = source.Votes.ToDictionary(pair => pair.Key, pair => new RevealedVoteEntity { Mark = pair.Value.Mark, PersonType = pair.Value.PersonType })
        };
    }
}
