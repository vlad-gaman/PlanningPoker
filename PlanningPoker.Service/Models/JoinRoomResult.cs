namespace PlanningPoker.Service.Models;

public class JoinRoomResult
{
    public Room Room { get; init; } = default!;

    public Person Person { get; init; } = default!;

    public IReadOnlyList<Person> People { get; init; } = default!;

    public VoteStatus VoteStatus { get; init; } = default!;

    /// <summary>False when the person already had a connection to the room (another tab or a reconnect), so nobody else needs to hear about it.</summary>
    public bool PersonAdded { get; init; }

    public bool ResetCountdown { get; init; }
}
