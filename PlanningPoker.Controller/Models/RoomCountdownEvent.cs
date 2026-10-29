namespace PlanningPoker.Controller.Models;

/// <summary>Payload of both CountdownReset (reset true) and CountdownUpdate (reset false).</summary>
public class RoomCountdownEvent
{
    public int Countdown { get; init; }

    public bool Reset { get; init; }
}
