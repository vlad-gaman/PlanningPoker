namespace PlanningPoker.Service.Models;

public class PersonTypeChangeResult
{
    public string RoomId { get; init; } = default!;

    /// <summary>The person with the new type.</summary>
    public Person Person { get; init; } = default!;

    /// <summary>False when the person already had that type, so nobody needs to hear about it.</summary>
    public bool Changed { get; init; }

    /// <summary>The status after the change, before any countdown it starts: a cancelled countdown shows as null.</summary>
    public VoteStatus VoteStatus { get; init; } = default!;

    /// <summary>A countdown or reveal the change caused: everyone left to vote had voted.</summary>
    public VotingFollowUp FollowUp { get; init; } = VotingFollowUp.None;
}
