using PlanningPoker.Service.Models;
using PlanningPoker.Service.Services;

namespace PlanningPoker.UnitTests.Fakes;

/// <summary>Records what the presence sweep announced.</summary>
public sealed class FakeRoomPresenceNotifier : IRoomPresenceNotifier
{
    public List<PersonOffgridResult> Offgrid { get; } = [];

    public List<ExitRoomResult> Removed { get; } = [];

    public Task PersonOffgridAsync(PersonOffgridResult result)
    {
        Offgrid.Add(result);
        return Task.CompletedTask;
    }

    public Task PersonRemovedAsync(ExitRoomResult result)
    {
        Removed.Add(result);
        return Task.CompletedTask;
    }
}
