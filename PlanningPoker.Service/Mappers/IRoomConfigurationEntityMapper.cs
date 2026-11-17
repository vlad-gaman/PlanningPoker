using PlanningPoker.Repository.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Service.Mappers;

public interface IRoomConfigurationEntityMapper
{
    RoomConfigurationEntity Map(RoomConfiguration configuration);
}
