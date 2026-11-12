namespace PlanningPoker.Controller.Models;

public class AdminStatsResponse
{
    public AdminRoomCountsResponse Rooms { get; init; } = default!;

    public AdminPeopleCountsResponse People { get; init; } = default!;

    public long UptimeSeconds { get; init; }

    public int RoomsRestoredAtStart { get; init; }

    public int AbandonedRoomsSweptLast24Hours { get; init; }

    public AdminStoreWriteResponse LastStoreWrite { get; init; } = default!;

    public int FailedLoginsLast24Hours { get; init; }
}
