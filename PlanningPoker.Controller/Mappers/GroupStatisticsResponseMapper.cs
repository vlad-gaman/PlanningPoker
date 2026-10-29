using PlanningPoker.Controller.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Controller.Mappers;

public class GroupStatisticsResponseMapper(IMarkShareResponseMapper shareMapper) : IGroupStatisticsResponseMapper
{
    public GroupStatisticsResponse Map(GroupStatistics group)
    {
        return new GroupStatisticsResponse
        {
            Marks = group.Marks.Select(shareMapper.Map).ToList(),
            HighestMark = group.HighestMark,
            AverageMark = group.AverageMark
        };
    }
}
