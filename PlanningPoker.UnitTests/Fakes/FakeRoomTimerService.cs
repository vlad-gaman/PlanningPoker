using PlanningPoker.Service.Models;
using PlanningPoker.Service.Services;

namespace PlanningPoker.UnitTests.Fakes;

/// <summary>Records the follow-ups it was asked to carry out instead of ticking or announcing them.</summary>
public sealed class FakeRoomTimerService : IRoomTimerService
{
    public List<(string RoomId, VotingFollowUp FollowUp)> Applied { get; } = [];

    public Task ApplyAsync(string roomId, VotingFollowUp followUp)
    {
        Applied.Add((roomId, followUp));
        return Task.CompletedTask;
    }
}
