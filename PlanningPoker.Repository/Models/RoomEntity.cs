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

    public bool VotingEnabled { get; set; }

    /// <summary>Set when the last member leaves. A disposed room accepts no new members.</summary>
    public bool IsDisposed { get; set; }

    public RoomConfigurationEntity Configuration { get; set; } = new();
}
