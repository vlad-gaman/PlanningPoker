using PlanningPoker.Repository.Models;
using PlanningPoker.Repository.Repositories;

namespace PlanningPoker.Repository.Mappers;

public interface IStoredRoomMapper
{
    /// <summary>The room with copies of its owner and members, as the store keeps them.</summary>
    StoredRoom Map(RoomEntity room, IPersonRepository people, DateTimeOffset savedAt);
}
