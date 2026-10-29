using PlanningPoker.Controller.Services;
using PlanningPoker.Service.Models;

namespace PlanningPoker.UnitTests.Fakes;

/// <summary>Records what the hub asked to publish instead of sending it.</summary>
public sealed class FakeRoomEventPublisher : IRoomEventPublisher
{
    public List<(JoinRoomResult Result, string JoinerConnectionId)> Joined { get; } = [];

    public List<ExitRoomResult> Exited { get; } = [];

    public Task PublishPersonJoinedAsync(JoinRoomResult result, string joinerConnectionId)
    {
        Joined.Add((result, joinerConnectionId));
        return Task.CompletedTask;
    }

    public Task PublishPersonExitedAsync(ExitRoomResult result)
    {
        Exited.Add(result);
        return Task.CompletedTask;
    }
}
