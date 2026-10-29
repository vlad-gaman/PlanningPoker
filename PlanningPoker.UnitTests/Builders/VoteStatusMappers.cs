using PlanningPoker.Service.Mappers;
using PlanningPoker.Service.Services;

namespace PlanningPoker.UnitTests.Builders;

/// <summary>Builds a <see cref="VoteStatusMapper"/> over the real statistics mapper and card sets.</summary>
public static class VoteStatusMappers
{
    public static VoteStatusMapper Create()
    {
        return new VoteStatusMapper(new VoteStatisticsMapper(), new CardSetService());
    }
}
