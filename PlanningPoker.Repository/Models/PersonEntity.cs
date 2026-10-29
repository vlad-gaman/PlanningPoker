namespace PlanningPoker.Repository.Models;

public class PersonEntity
{
    public Guid Guid { get; set; }

    public string Name { get; set; } = default!;

    public string PersonType { get; set; } = default!;

    /// <summary>
    /// Index from open hub connection to the room it joined: connectionId to roomId. It only tells an Exit which room to
    /// ask; whether the person is still in that room is decided by the room's <see cref="RoomEntity.MemberConnections"/>.
    /// </summary>
    public Dictionary<string, string> Connections { get; set; } = [];
}
