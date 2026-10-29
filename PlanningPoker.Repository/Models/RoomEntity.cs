namespace PlanningPoker.Repository.Models;

public class RoomEntity
{
    public string Id { get; set; } = default!;

    public Guid OwnerGuid { get; set; }

    public List<Guid> MemberGuids { get; set; } = [];

    public Dictionary<Guid, string> Votes { get; set; } = [];

    public RoomConfigurationEntity Configuration { get; set; } = new();
}
