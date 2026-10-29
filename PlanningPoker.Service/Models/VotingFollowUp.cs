namespace PlanningPoker.Service.Models;

/// <summary>What a change to the votes or the voters led to: a countdown to tick, an immediate reveal, or nothing.</summary>
public class VotingFollowUp
{
    public static readonly VotingFollowUp None = new();

    public CountdownStart? Countdown { get; init; }

    /// <summary>The revealed votes when the change revealed them at once (a countdown of 0).</summary>
    public VoteStatus? Revealed { get; init; }
}
