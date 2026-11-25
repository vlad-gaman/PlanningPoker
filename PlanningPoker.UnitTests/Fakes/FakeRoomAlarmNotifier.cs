using PlanningPoker.Service.Models;
using PlanningPoker.Service.Services;

namespace PlanningPoker.UnitTests.Fakes;

/// <summary>Records what the Alarm timer announced.</summary>
public sealed class FakeRoomAlarmNotifier : IRoomAlarmNotifier
{
    public List<AlarmChangeResult> Changes { get; } = [];

    public Task AlarmChangedAsync(AlarmChangeResult result)
    {
        lock (Changes)
        {
            Changes.Add(result);
        }

        return Task.CompletedTask;
    }
}
