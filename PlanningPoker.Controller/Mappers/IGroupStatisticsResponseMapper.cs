using PlanningPoker.Controller.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Controller.Mappers;

public interface IGroupStatisticsResponseMapper
{
    GroupStatisticsResponse Map(GroupStatistics group);
}
