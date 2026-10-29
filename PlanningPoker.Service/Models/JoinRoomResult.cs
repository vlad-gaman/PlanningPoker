namespace PlanningPoker.Service.Models;

public class JoinRoomResult
{
    public Room Room { get; init; } = default!;

    public Person Person { get; init; } = default!;

    public IReadOnlyList<Person> People { get; init; } = default!;

    public VoteStatus VoteStatus { get; init; } = default!;

    public bool ResetCountdown { get; init; }
}
