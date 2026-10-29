using PlanningPoker.Repository.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Service.Mappers;

public interface IRoomMapper
{
    Room Map(RoomEntity entity);
}
