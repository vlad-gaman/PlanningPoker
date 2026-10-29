namespace PlanningPoker.Controller.Models;

public class VoteStatusResponse
{
    public IReadOnlyDictionary<Guid, string> Votes { get; init; } = default!;

    public bool HasEveryoneVoted { get; init; }
}
