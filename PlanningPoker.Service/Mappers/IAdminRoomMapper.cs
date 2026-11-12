using PlanningPoker.Repository.Models;
using PlanningPoker.Repository.Repositories;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Service.Mappers;

public interface IAdminRoomMapper
{
    /// <summary>The Operator's view of a room copy and its members. Votes are not read.</summary>
    AdminRoom Map(RoomEntity room, IPersonRepository people);
}
