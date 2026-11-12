using PlanningPoker.Repository.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Service.Mappers;

public interface IAdminOverviewMapper
{
    AdminOverview Map(
        IReadOnlyList<AdminRoom> rooms,
        IReadOnlyList<IssueEntry> issues,
        TimeSpan uptime,
        int roomsRestoredAtStart,
        int roomsSweptLast24Hours,
        PersistenceStatusSnapshot storeWrite,
        int failedLoginsLast24Hours);
}
