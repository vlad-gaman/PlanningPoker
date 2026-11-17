using PlanningPoker.Common.Models;

namespace PlanningPoker.Service.Models;

public class RoomConfigurationChangeResult
{
    public string RoomId { get; init; } = default!;

    public RoomConfiguration Configuration { get; init; } = default!;

    /// <summary>The cards of the (possibly new) card set, in display order.</summary>
    public IReadOnlyList<CardOption> Cards { get; init; } = default!;

    /// <summary>The name of the owner who saved the settings.</summary>
    public string UpdatedBy { get; init; } = default!;

    /// <summary>True when the save also dropped the votes: they were shown, or the card set changed.</summary>
    public bool VotesCleared { get; init; }

    /// <summary>The status after the change, before any countdown it starts.</summary>
    public VoteStatus VoteStatus { get; init; } = default!;

    /// <summary>A countdown with the new length, or a reveal, when the round was complete and the votes were kept.</summary>
    public VotingFollowUp FollowUp { get; init; } = VotingFollowUp.None;
}
