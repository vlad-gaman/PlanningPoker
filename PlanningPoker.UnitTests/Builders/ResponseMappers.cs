using PlanningPoker.Controller.Mappers;

namespace PlanningPoker.UnitTests.Builders;

/// <summary>Builds the response mappers with their real collaborators.</summary>
public static class ResponseMappers
{
    public static VoteStatusResponseMapper VoteStatus()
    {
        return new VoteStatusResponseMapper(new VoteStatisticsResponseMapper(new GroupStatisticsResponseMapper(new MarkShareResponseMapper())));
    }
}
