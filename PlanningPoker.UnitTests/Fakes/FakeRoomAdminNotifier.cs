using PlanningPoker.Service.Services;

namespace PlanningPoker.UnitTests.Fakes;

/// <summary>Records the rooms the Operator closed.</summary>
public sealed class FakeRoomAdminNotifier : IRoomAdminNotifier
{
    public List<(string RoomId, IReadOnlyList<string> ConnectionIds)> Closed { get; } = [];

    public Task RoomClosedAsync(string roomId, IReadOnlyList<string> connectionIds)
    {
        Closed.Add((roomId, connectionIds));
        return Task.CompletedTask;
    }
}
