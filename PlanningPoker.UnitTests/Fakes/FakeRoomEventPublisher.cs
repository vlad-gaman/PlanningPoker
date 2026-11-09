using PlanningPoker.Controller.Services;
using PlanningPoker.Service.Models;

namespace PlanningPoker.UnitTests.Fakes;

/// <summary>Records what the hub asked to publish instead of sending it.</summary>
public sealed class FakeRoomEventPublisher : IRoomEventPublisher
{
    public List<(JoinRoomResult Result, string JoinerConnectionId)> Joined { get; } = [];

    public List<ExitRoomResult> Exited { get; } = [];

    public List<VoteResult> VoteCasts { get; } = [];

    public List<ClearVotesResult> Cleared { get; } = [];

    public List<(string RoomId, VoteStatus Status)> Shown { get; } = [];

    public List<(string RoomId, int Countdown)> CountdownUpdates { get; } = [];

    public List<PersonTypeChangeResult> TypeChanges { get; } = [];

    public List<RoomConfigurationChangeResult> ConfigurationUpdates { get; } = [];

    public List<OwnershipTransferResult> OwnershipTransfers { get; } = [];

    /// <summary>Every publish call in order, by name, so a test can check the order guarantees of the hub.</summary>
    public List<string> Order { get; } = [];

    public Task PublishPersonTypeChangedAsync(PersonTypeChangeResult result)
    {
        TypeChanges.Add(result);
        Order.Add("PersonTypeChanged");
        return Task.CompletedTask;
    }

    public Task PublishRoomConfigurationUpdatedAsync(RoomConfigurationChangeResult result)
    {
        ConfigurationUpdates.Add(result);
        Order.Add("RoomConfigurationUpdated");
        return Task.CompletedTask;
    }

    public Task PublishOwnershipTransferredAsync(OwnershipTransferResult result)
    {
        OwnershipTransfers.Add(result);
        Order.Add("OwnershipTransferred");
        return Task.CompletedTask;
    }

    public Task PublishVoteCastAsync(VoteResult result)
    {
        VoteCasts.Add(result);
        return Task.CompletedTask;
    }

    public Task PublishVotesClearedAsync(ClearVotesResult result)
    {
        Cleared.Add(result);
        return Task.CompletedTask;
    }

    public Task PublishVotesShownAsync(string roomId, VoteStatus status)
    {
        Shown.Add((roomId, status));
        Order.Add("VotesShown");
        return Task.CompletedTask;
    }

    public Task PublishCountdownUpdateAsync(string roomId, int countdown)
    {
        CountdownUpdates.Add((roomId, countdown));
        Order.Add("CountdownUpdate");
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
