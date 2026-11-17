namespace PlanningPoker.Repository.Models;

public class PersonEntity
{
    /// <summary>The public id of the person. Other room members see it.</summary>
    public Guid Guid { get; set; }

    /// <summary>
    /// The secret the session cookie carries; it is never sent to anyone else. Null for a person stored before tokens
    /// existed: no session resolves to them, so their owner signs in again as a new person.
    /// </summary>
    public string? SessionToken { get; set; }

    public string Name { get; set; } = default!;

    public string PersonType { get; set; } = default!;

    /// <summary>
    /// Index from open hub connection to the room it joined: connectionId to roomId. It only tells an Exit which room to
    /// ask; whether the person is still in that room is decided by the room's <see cref="RoomEntity.MemberConnections"/>.
    /// </summary>
    public Dictionary<string, string> Connections { get; set; } = [];

    /// <summary>When the person was created, last upserted, or last had a connection removed, in UTC. Read paths never touch it.</summary>
    public DateTimeOffset LastSeenAt { get; set; }

    /// <summary>Set, under the person lock, when the sweep removes the person. A removed person accepts no further updates.</summary>
    public bool IsRemoved { get; set; }
}
