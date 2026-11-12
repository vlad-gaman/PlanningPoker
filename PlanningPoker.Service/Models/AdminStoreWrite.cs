namespace PlanningPoker.Service.Models;

public class AdminStoreWrite
{
    public DateTimeOffset? At { get; init; }

    public bool? Succeeded { get; init; }

    public int FailedRooms { get; init; }
}
