namespace PlanningPoker.Repository.Models;

public class PersonEntity
{
    public Guid Guid { get; set; }

    public string Name { get; set; } = default!;

    public string PersonType { get; set; } = default!;

    /// <summary>Open hub connections as connectionId to roomId. The person is in a room while any entry points at it.</summary>
    public Dictionary<string, string> Connections { get; set; } = [];
}
