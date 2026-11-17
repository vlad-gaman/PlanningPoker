using PlanningPoker.Repository.Models;

namespace PlanningPoker.Service.Mappers;

public interface IRevealedVotesEntityMapper
{
    /// <summary>
    /// Freezes the room's votes. <paramref name="personTypes"/> maps member guids to their person type; a member missing
    /// from it counts as a voter of unknown type.
    /// </summary>
    RevealedVotesEntity Map(RoomEntity room, IReadOnlyDictionary<Guid, string> personTypes);
}
