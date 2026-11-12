using PlanningPoker.Common.Models;
using PlanningPoker.Repository.Models;

namespace PlanningPoker.Service.Mappers;

public class IssueEntryMapper : IIssueEntryMapper
{
    public IssueEntry Map(DateTimeOffset time, IssueLevel level, string source, string message, string? ipAddress)
    {
        return new IssueEntry { Time = time, Level = level, Source = source, Message = message, IpAddress = ipAddress };
    }
}
