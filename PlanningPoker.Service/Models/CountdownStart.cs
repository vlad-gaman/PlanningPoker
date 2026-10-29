namespace PlanningPoker.Service.Models;

/// <summary>A countdown the caller must now tick, tied to the room revision it started at.</summary>
public class CountdownStart
{
    public long Revision { get; init; }

    public int Seconds { get; init; }
}
