namespace PlanningPoker.Repository.Models;

public class RoomEntity
{
    public string Id { get; set; } = default!;

    public Guid OwnerGuid { get; set; }

    public List<Guid> MemberGuids { get; set; } = [];

    public Dictionary<Guid, string> Votes { get; set; } = [];

    public bool VotingEnabled { get; set; }

    /// <summary>Set when the last member leaves. A disposed room accepts no new members.</summary>
    public bool IsDisposed { get; set; }

    public RoomConfigurationEntity Configuration { get; set; } = new();
}
