namespace PlanningPoker.Controller.Models;

public class AdminStoreWriteResponse
{
    public DateTimeOffset? At { get; init; }

    public bool? Succeeded { get; init; }

    public int FailedRooms { get; init; }
}
