namespace PlanningPoker.Service.Models;

public class VoteStatus
{
    /// <summary>Marks of everyone who has voted, masked until the votes are revealed.</summary>
    public IReadOnlyDictionary<Guid, string> Votes { get; init; } = default!;

    public bool HasEveryoneVoted { get; init; }
}
