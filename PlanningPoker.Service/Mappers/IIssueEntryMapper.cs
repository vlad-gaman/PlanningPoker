using PlanningPoker.Common.Models;
using PlanningPoker.Repository.Models;

namespace PlanningPoker.Service.Mappers;

public interface IIssueEntryMapper
{
    IssueEntry Map(DateTimeOffset time, IssueLevel level, string source, string message, string? ipAddress);
}
