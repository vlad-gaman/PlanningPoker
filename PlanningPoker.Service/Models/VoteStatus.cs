namespace PlanningPoker.Service.Models;

public class VoteStatus
{
    /// <summary>Marks of everyone who has voted, masked until the votes are revealed.</summary>
    public IReadOnlyDictionary<Guid, string> Votes { get; init; } = default!;

    public bool HasEveryoneVoted { get; init; }

    /// <summary>True from the reveal until the votes are cleared; <see cref="Votes"/> then holds the real marks of the voters.</summary>
    public bool VotingFinished { get; init; }

    /// <summary>Seconds left while a countdown runs; null otherwise.</summary>
    public int? Countdown { get; init; }

    /// <summary>When the current round began, in UTC: the room's creation, or its last reveal or clear.</summary>
    public DateTimeOffset RoundStartedAt { get; init; }

    /// <summary>Derived from the votes frozen at the reveal; null while voting is open.</summary>
    public VoteStatistics? Statistics { get; init; }

    /// <summary>The room's revealed rounds, newest first, including the one just revealed; null while voting is open (the join snapshot carries it then).</summary>
    public IReadOnlyList<RoundResult>? RoundHistory { get; init; }
}
