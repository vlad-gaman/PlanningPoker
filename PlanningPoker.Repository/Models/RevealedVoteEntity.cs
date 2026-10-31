namespace PlanningPoker.Repository.Models;

/// <summary>A vote as it stood at the reveal, with the person type of its voter at that moment.</summary>
public class RevealedVoteEntity
{
    public string Mark { get; set; } = default!;

    /// <summary>Empty when the voter's type could not be read at the reveal; such a vote only counts in the overall group.</summary>
    public string PersonType { get; set; } = default!;
}
