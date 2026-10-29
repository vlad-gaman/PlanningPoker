using PlanningPoker.Service.Services;

namespace PlanningPoker.UnitTests.Fakes;

/// <summary>Records the countdowns the hub asked to start instead of ticking them.</summary>
public sealed class FakeRoomTimerService : IRoomTimerService
{
    public List<(string RoomId, long Revision)> Started { get; } = [];

    public void Start(string roomId, long revision) => Started.Add((roomId, revision));
}
