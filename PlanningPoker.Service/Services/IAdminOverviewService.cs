using PlanningPoker.Common.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Service.Services;

public interface IAdminOverviewService
{
    /// <summary>Stats, the live rooms with their people, and the newest issues-log entries. Read-only and free of vote data.</summary>
    ServiceResponse<AdminOverview> GetOverview();
}
