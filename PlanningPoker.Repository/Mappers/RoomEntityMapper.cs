using PlanningPoker.Repository.Models;

namespace PlanningPoker.Repository.Mappers;

public class RoomEntityMapper : IRoomEntityMapper
{
    public RoomEntity Map(RoomEntity source)
    {
        return new RoomEntity
        {
            Id = source.Id,
            OwnerGuid = source.OwnerGuid,
            MemberGuids = [.. source.MemberGuids],
            Votes = new Dictionary<Guid, string>(source.Votes),
            Configuration = new RoomConfigurationEntity
            {
                CardSet = source.Configuration.CardSet,
                CountdownSeconds = source.Configuration.CountdownSeconds,
                ShowFireworks = source.Configuration.ShowFireworks,
                MaxParticipants = source.Configuration.MaxParticipants
            }
        };
    }
}
