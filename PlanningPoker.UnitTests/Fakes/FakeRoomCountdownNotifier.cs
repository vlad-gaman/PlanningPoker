using PlanningPoker.Service.Models;
using PlanningPoker.Service.Services;

namespace PlanningPoker.UnitTests.Fakes;

/// <summary>Records what the countdown ticker announced.</summary>
public sealed class FakeRoomCountdownNotifier : IRoomCountdownNotifier
{
    public List<(string RoomId, int Remaining)> Ticks { get; } = [];

    public List<(string RoomId, VoteStatus Status)> Reveals { get; } = [];

    public Task CountdownTickedAsync(string roomId, int remaining)
    {
        lock (Ticks)
        {
            Ticks.Add((roomId, remaining));
        }

        return Task.CompletedTask;
    }

    public Task VotesRevealedAsync(string roomId, VoteStatus status)
    {
        lock (Ticks)
        {
            Reveals.Add((roomId, status));
        }

        return Task.CompletedTask;
    }
}
