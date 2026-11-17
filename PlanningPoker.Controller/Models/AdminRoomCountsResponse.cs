namespace PlanningPoker.Controller.Models;

public class AdminRoomCountsResponse
{
    public int Total { get; init; }

    public int Voting { get; init; }

    public int Revealed { get; init; }

    public int Restored { get; init; }
}
