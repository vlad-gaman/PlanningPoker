using PlanningPoker.Repository.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Service.Mappers;

public interface IRoomConfigurationMapper
{
    RoomConfiguration Map(RoomConfigurationEntity entity);
}
