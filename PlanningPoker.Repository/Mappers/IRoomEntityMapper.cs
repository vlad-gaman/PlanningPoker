using PlanningPoker.Repository.Models;

namespace PlanningPoker.Repository.Mappers;

public interface IRoomEntityMapper
{
    RoomEntity Map(RoomEntity source);
}
