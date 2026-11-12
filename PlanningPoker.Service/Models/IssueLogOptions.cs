using PlanningPoker.Common.Constants;

namespace PlanningPoker.Service.Models;

public class IssueLogOptions
{
    public int MaxEntries { get; set; } = IssueLogConstants.MaxEntries;

    public TimeSpan MaxAge { get; set; } = IssueLogConstants.MaxAge;

    public int MaxMessageLength { get; set; } = IssueLogConstants.MaxMessageLength;
}
