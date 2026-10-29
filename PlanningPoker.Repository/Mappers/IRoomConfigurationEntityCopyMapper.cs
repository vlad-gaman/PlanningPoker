using PlanningPoker.Repository.Models;

namespace PlanningPoker.Repository.Mappers;

public interface IRoomConfigurationEntityCopyMapper
{
    RoomConfigurationEntity Map(RoomConfigurationEntity source);
}
