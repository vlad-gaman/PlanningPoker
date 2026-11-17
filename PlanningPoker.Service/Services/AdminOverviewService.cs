using PlanningPoker.Common.Models;
using PlanningPoker.Repository.Models;
using PlanningPoker.Repository.Repositories;
using PlanningPoker.Repository.Services;
using PlanningPoker.Service.Mappers;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Service.Services;

public class AdminOverviewService(
    IRoomRepository rooms,
    IPersonRepository people,
    IServerStatistics server,
    IPersistenceStatus persistence,
    IIssueLog issues,
    IAdminRoomMapper roomMapper,
    IAdminOverviewMapper overviewMapper,
    TimeProvider time) : IAdminOverviewService
{
    /// <summary>How many of the newest issues-log entries an overview carries.</summary>
    public const int IssueCount = 50;

    private static readonly TimeSpan Day = TimeSpan.FromHours(24);

    public ServiceResponse<AdminOverview> GetOverview()
    {
        var now = time.GetUtcNow();
        var liveRooms = rooms.Ids()
            .Select(rooms.Get)
            .OfType<RoomEntity>()
            .Select(room => roomMapper.Map(room, people))
            .OrderByDescending(room => room.LastActivityAt)
            .ThenBy(room => room.Id, StringComparer.Ordinal)
            .ToList();

        var overview = overviewMapper.Map(
            liveRooms,
            issues.Recent(IssueCount),
            now - server.StartedAt,
            server.RoomsRestoredAtStart,
            server.RoomsSweptSince(now - Day),
            persistence.Snapshot(),
            issues.CountFailedLogins(now - Day));
        return ServiceResponse<AdminOverview>.Success(overview);
    }
}
