namespace PlanningPoker.Repository.Models;

/// <summary>How the last write of rooms to the store went.</summary>
public class PersistenceStatusSnapshot
{
    /// <summary>When rooms were last written (or tried), in UTC; null until the first write.</summary>
    public DateTimeOffset? LastWriteAt { get; init; }

    /// <summary>True when every room in the last write was saved; null until the first write.</summary>
    public bool? LastWriteSucceeded { get; init; }

    /// <summary>How many rooms of the last write could not be saved.</summary>
    public int FailedRooms { get; init; }
}
