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
}
