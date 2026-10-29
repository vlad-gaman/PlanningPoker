using PlanningPoker.Repository.Models;

namespace PlanningPoker.Repository.Mappers;

public interface IRoomEntityCopyMapper
{
    RoomEntity Map(RoomEntity source);
}
