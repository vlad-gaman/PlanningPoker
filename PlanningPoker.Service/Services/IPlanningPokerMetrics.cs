namespace PlanningPoker.Service.Services;

public interface IPlanningPokerMetrics
{
    /// <summary>Counts one successfully created room.</summary>
    void RoomCreated();

    /// <summary>Counts one successfully cast vote for a room that uses <paramref name="cardSet"/>.</summary>
    void VoteCast(string cardSet);
}
