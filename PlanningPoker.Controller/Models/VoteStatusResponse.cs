namespace PlanningPoker.Controller.Models;

public class VoteStatusResponse
{
    /// <summary>Person guid to mark; every mark is "hide" until <see cref="VotingFinished"/>.</summary>
    public IReadOnlyDictionary<Guid, string> Votes { get; init; } = default!;

    public bool HasEveryoneVoted { get; init; }

    public bool VotingFinished { get; init; }

    /// <summary>Seconds left while a countdown runs; null otherwise.</summary>
    public int? Countdown { get; init; }

    /// <summary>The statistics of the revealed votes; null while voting is open.</summary>
    public VoteStatisticsResponse? Statistics { get; init; }
}
