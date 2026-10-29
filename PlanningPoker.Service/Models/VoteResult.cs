namespace PlanningPoker.Service.Models;

public class VoteResult
{
    public string RoomId { get; init; } = default!;

    public Guid PersonGuid { get; init; }

    /// <summary>The masked status after the vote.</summary>
    public VoteStatus VoteStatus { get; init; } = default!;

    public VotingFollowUp FollowUp { get; init; } = VotingFollowUp.None;
}
