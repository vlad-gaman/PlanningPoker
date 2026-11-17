namespace PlanningPoker.Repository.Models;

/// <summary>What the store keeps of one live room: the room and the people who belong to it. Connections and timers are not kept.</summary>
public class StoredRoom
{
    public RoomEntity Room { get; set; } = default!;

    /// <summary>The owner and every member of the room.</summary>
    public List<PersonEntity> People { get; set; } = [];

    /// <summary>When the room was last written. For a member who was connected at that moment this is the last time they were seen online.</summary>
    public DateTimeOffset SavedAt { get; set; }
}
