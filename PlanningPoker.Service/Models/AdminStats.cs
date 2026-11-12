namespace PlanningPoker.Service.Models;

public class AdminStats
{
    public int Rooms { get; init; }

    public int VotingRooms { get; init; }

    public int RevealedRooms { get; init; }

    public int RestoredRooms { get; init; }

    public int PeopleOnline { get; init; }

    public int PeopleOffgrid { get; init; }

    public TimeSpan Uptime { get; init; }

    public int RoomsRestoredAtStart { get; init; }

    /// <summary>Abandoned rooms the sweep removed in the last 24 hours of this run of the server.</summary>
    public int AbandonedRoomsSweptLast24Hours { get; init; }

    public AdminStoreWrite LastStoreWrite { get; init; } = new();

    public int FailedLoginsLast24Hours { get; init; }
}
