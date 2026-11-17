namespace PlanningPoker.Service.Models;

public class ClearVotesResult
{
    public string RoomId { get; init; } = default!;

    /// <summary>When the new round began, in UTC.</summary>
    public DateTimeOffset RoundStartedAt { get; init; }
}
