using PlanningPoker.Repository.Models;

namespace PlanningPoker.Service.Mappers;

public interface IRevealedVotesEntityMapper
{
    /// <summary>Freezes the room's votes, each tagged with its voter's role in the room; observers are left out.</summary>
    RevealedVotesEntity Map(RoomEntity room);
}
