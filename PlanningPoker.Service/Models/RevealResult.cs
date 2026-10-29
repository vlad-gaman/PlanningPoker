namespace PlanningPoker.Service.Models;

public class RevealResult
{
    public string RoomId { get; init; } = default!;

    /// <summary>The revealed votes; null when they were already revealed, so there is nothing to announce.</summary>
    public VoteStatus? VoteStatus { get; init; }
}
