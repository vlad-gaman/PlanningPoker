namespace PlanningPoker.Repository.Models;

/// <summary>
/// The votes frozen at the reveal. The statistics are derived from this and never change until the votes are cleared,
/// whoever leaves or changes type afterwards.
/// </summary>
public class RevealedVotesEntity
{
    /// <summary>Person guid to vote; observers are not included.</summary>
    public Dictionary<Guid, RevealedVoteEntity> Votes { get; set; } = [];

    /// <summary>Whether every non-observer in the room had voted when the votes were revealed.</summary>
    public bool EveryoneVoted { get; set; }
}
