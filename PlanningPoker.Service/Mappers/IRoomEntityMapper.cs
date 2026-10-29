using PlanningPoker.Repository.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Service.Mappers;

public interface IRoomEntityMapper
{
    RoomEntity Map(string id, Guid ownerGuid, RoomConfiguration configuration);
}
