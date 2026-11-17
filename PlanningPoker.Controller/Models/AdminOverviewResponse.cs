namespace PlanningPoker.Controller.Models;

public class AdminOverviewResponse
{
    public AdminStatsResponse Stats { get; init; } = default!;

    public List<AdminRoomResponse> Rooms { get; init; } = [];

    public List<AdminIssueResponse> Issues { get; init; } = [];
}
