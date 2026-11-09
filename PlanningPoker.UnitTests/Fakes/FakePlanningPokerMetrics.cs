using PlanningPoker.Service.Services;

namespace PlanningPoker.UnitTests.Fakes;

/// <summary>Records the metric calls a service makes.</summary>
public class FakePlanningPokerMetrics : IPlanningPokerMetrics
{
    public int RoomsCreated { get; private set; }

    public List<string> VoteCardSets { get; } = [];

    public void RoomCreated()
    {
        RoomsCreated++;
    }

    public void VoteCast(string cardSet)
    {
        VoteCardSets.Add(cardSet);
    }
}
