namespace PlanningPoker.Repository.Models;

public class PersonEntity
{
    public Guid Guid { get; set; }

    public string Name { get; set; } = default!;

    public string PersonType { get; set; } = default!;

    public string? ConnectionId { get; set; }

    public bool IsConnected { get; set; }
}
