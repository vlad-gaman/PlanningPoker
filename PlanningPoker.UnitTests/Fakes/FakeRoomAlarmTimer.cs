using PlanningPoker.Service.Services;

namespace PlanningPoker.UnitTests.Fakes;

/// <summary>Records the Alarm wake-ups the hub asked for instead of waiting for them.</summary>
public sealed class FakeRoomAlarmTimer : IRoomAlarmTimer
{
    public List<(string RoomId, DateTimeOffset EndsAt)> Scheduled { get; } = [];

    public List<string> Cancelled { get; } = [];

    public void Schedule(string roomId, DateTimeOffset endsAt)
    {
        Scheduled.Add((roomId, endsAt));
    }

    public void Cancel(string roomId)
    {
        Cancelled.Add(roomId);
    }
}
