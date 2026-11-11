namespace PlanningPoker.Service.Models;

/// <summary>A person whose grace period lapsed in the sweep: still a member with their vote, but no longer waited for.</summary>
public class PersonOffgridResult
{
    public string RoomId { get; init; } = default!;

    public Person Person { get; init; } = default!;

    /// <summary>Who the room was handed to because its owner went offgrid; null when the owner did not change.</summary>
    public Person? NewOwner { get; init; }

    /// <summary>A countdown or reveal the person going offgrid caused: the people still online had all voted.</summary>
    public VotingFollowUp FollowUp { get; init; } = VotingFollowUp.None;
}
