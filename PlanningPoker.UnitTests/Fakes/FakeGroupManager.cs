using Microsoft.AspNetCore.SignalR;

namespace PlanningPoker.UnitTests.Fakes;

public sealed class FakeGroupManager : IGroupManager
{
    public List<(string ConnectionId, string GroupName)> Added { get; } = [];

    public Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
    {
        Added.Add((connectionId, groupName));
        return Task.CompletedTask;
    }

    public Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
