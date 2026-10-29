namespace PlanningPoker.Repository.Models;

public class RoomEntity
{
    public string Id { get; set; } = default!;

    public Guid OwnerGuid { get; set; }

    public List<Guid> MemberGuids { get; set; } = [];

    /// <summary>
    /// Open connections per member, in this room only. A member leaves when their set empties. A member
    /// without an entry (the creator before joining) has no connection yet.
    /// </summary>
    public Dictionary<Guid, HashSet<string>> MemberConnections { get; set; } = [];

    public Dictionary<Guid, string> Votes { get; set; } = [];

    /// <summary>True while votes can be cast; false once they are revealed, until they are cleared.</summary>
    public bool VotingEnabled { get; set; }

    /// <summary>
    /// Bumped, inside the room lock, by every change that cancels a running countdown: a stored vote, clearing, a reveal,
    /// a voter joining while voting is open and a person leaving.
    /// </summary>
    public long Revision { get; set; }

    /// <summary>The <see cref="Revision"/> the countdown was started at; null when none was.</summary>
    public long? CountdownRevision { get; set; }

    public int CountdownRemaining { get; set; }

    /// <summary>A countdown runs until a later change bumps the revision past the one it started at.</summary>
    public bool CountdownRunning => CountdownRevision == Revision;

    /// <summary>Set when the last member leaves. A disposed room accepts no new members.</summary>
    public bool IsDisposed { get; set; }

    public RoomConfigurationEntity Configuration { get; set; } = new();
}
