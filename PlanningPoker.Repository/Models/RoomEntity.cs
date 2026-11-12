using System.Text.Json.Serialization;

namespace PlanningPoker.Repository.Models;

public class RoomEntity
{
    public string Id { get; set; } = default!;

    public Guid OwnerGuid { get; set; }

    public List<Guid> MemberGuids { get; set; } = [];

    /// <summary>
    /// Open connections per member, in this room only. A member whose set empties is dropped (see <see cref="DroppedAt"/>)
    /// and only leaves when the sweep removes them; a connection-less member has no entry here. The creator is not a member
    /// until their first join.
    /// </summary>
    public Dictionary<Guid, HashSet<string>> MemberConnections { get; set; } = [];

    /// <summary>
    /// When each member without a connection lost their last one, in UTC. The first drop wins and a join clears it. A
    /// member is online for the grace period after it, offgrid after that, and removed once the removal window has passed.
    /// </summary>
    public Dictionary<Guid, DateTimeOffset> DroppedAt { get; set; } = [];

    /// <summary>
    /// Members whose grace period lapsed and who were announced as offgrid. They keep their vote, but the room no longer
    /// waits for them: "everyone voted" is decided over the members not listed here.
    /// </summary>
    public HashSet<Guid> OffgridGuids { get; set; } = [];

    public Dictionary<Guid, string> Votes { get; set; } = [];

    /// <summary>
    /// The votes of members who left while voting was open, kept so their return restores them. Only a join reads them:
    /// they are never shown, counted or revealed. A clear drops them, and so does a join after the reveal.
    /// </summary>
    public Dictionary<Guid, string> DepartedVotes { get; set; } = [];

    /// <summary>True while votes can be cast; false once they are revealed, until they are cleared.</summary>
    public bool VotingEnabled { get; set; }

    /// <summary>The votes frozen at the reveal; null while voting is open.</summary>
    public RevealedVotesEntity? Revealed { get; set; }

    /// <summary>
    /// Bumped, inside the room lock, by every change that cancels a running countdown: a stored vote, clearing, a reveal,
    /// a voter joining while voting is open, a person leaving, and (while voting is open) a person type or settings change.
    /// </summary>
    public long Revision { get; set; }

    /// <summary>The <see cref="Revision"/> the countdown was started at; null when none was.</summary>
    public long? CountdownRevision { get; set; }

    public int CountdownRemaining { get; set; }

    /// <summary>A countdown runs until a later change bumps the revision past the one it started at.</summary>
    public bool CountdownRunning => CountdownRevision == Revision;

    /// <summary>Set when the last member leaves. A disposed room accepts no new members.</summary>
    public bool IsDisposed { get; set; }

    /// <summary>
    /// When the current round began, in UTC: set at creation and reset by every reveal and clear. Votes, joins, exits, person
    /// type changes and settings changes leave it alone.
    /// </summary>
    public DateTimeOffset RoundStartedAt { get; set; }

    /// <summary>When the room was created, in UTC. Never changes; the sweep uses it to find rooms nobody ever joined.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    public RoomConfigurationEntity Configuration { get; set; } = new();

    /// <summary>
    /// When the room last changed, in UTC: any change to the stored state counts, a connection coming or going included. The
    /// abandoned sweep uses it to expire rooms nobody has touched for a long time.
    /// </summary>
    public DateTimeOffset LastActivityAt { get; set; }

    /// <summary>
    /// Set when the room was restored after a restart, until someone is connected again. The room is then not deleted for having
    /// nobody online: its people get the whole removal window to come back. Never stored.
    /// </summary>
    [JsonIgnore]
    public bool AwaitingReturn { get; set; }
}
