using PlanningPoker.Controller.Services;
using PlanningPoker.Service.Models;

namespace PlanningPoker.UnitTests.Fakes;

/// <summary>Records what the hub asked to publish instead of sending it.</summary>
public sealed class FakeRoomEventPublisher : IRoomEventPublisher
{
    public List<(JoinRoomResult Result, string JoinerConnectionId)> Joined { get; } = [];

    public List<ExitRoomResult> Exited { get; } = [];

    public List<VoteResult> VoteCasts { get; } = [];

    public List<string> Cleared { get; } = [];

    public List<(string RoomId, VoteStatus Status)> Shown { get; } = [];

    public List<(string RoomId, int Countdown)> CountdownUpdates { get; } = [];

    public Task PublishVoteCastAsync(VoteResult result)
    {
        VoteCasts.Add(result);
        return Task.CompletedTask;
    }

    public Task PublishVotesClearedAsync(string roomId)
    {
        Cleared.Add(roomId);
        return Task.CompletedTask;
    }

    public Task PublishVotesShownAsync(string roomId, VoteStatus status)
    {
        Shown.Add((roomId, status));
        return Task.CompletedTask;
    }

    public Task PublishCountdownUpdateAsync(string roomId, int countdown)
    {
        CountdownUpdates.Add((roomId, countdown));
        return Task.CompletedTask;
    }

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
