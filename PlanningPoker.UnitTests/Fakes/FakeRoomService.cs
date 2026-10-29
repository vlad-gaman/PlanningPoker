using PlanningPoker.Common.Models;
using PlanningPoker.Service.Models;
using PlanningPoker.Service.Services;

namespace PlanningPoker.UnitTests.Fakes;

/// <summary>Returns scripted results for Join and Exit and records the calls made to them.</summary>
public sealed class FakeRoomService : IRoomService
{
    public ServiceResponse<JoinRoomResult> JoinResult { get; set; } = ServiceResponse<JoinRoomResult>.Failure();

    public ServiceResponse<ExitRoomResult> ExitResult { get; set; } = ServiceResponse<ExitRoomResult>.Failure();

    /// <summary>Runs at the start of Join, so a test can observe what the hub did before it.</summary>
    public Action? BeforeJoin { get; set; }

    public List<(Guid PersonGuid, string ConnectionId, string RoomId)> JoinCalls { get; } = [];

    public List<(Guid PersonGuid, string ConnectionId)> ExitCalls { get; } = [];

    public ServiceResponse<JoinRoomResult> Join(Guid personGuid, string connectionId, string roomId)
    {
        BeforeJoin?.Invoke();
        JoinCalls.Add((personGuid, connectionId, roomId));
        return JoinResult;
    }

    public ServiceResponse<ExitRoomResult> Exit(Guid personGuid, string connectionId)
    {
        ExitCalls.Add((personGuid, connectionId));
        return ExitResult;
    }

    public ServiceResponse<VoteResult> VoteResult { get; set; } = ServiceResponse<VoteResult>.Failure();

    public ServiceResponse<string> ClearResult { get; set; } = ServiceResponse<string>.Failure();

    public ServiceResponse<RevealResult> RevealResult { get; set; } = ServiceResponse<RevealResult>.Failure();

    public List<(Guid PersonGuid, string ConnectionId, string Mark)> VoteCalls { get; } = [];

    public List<(Guid PersonGuid, string ConnectionId)> ClearCalls { get; } = [];

    public List<(Guid PersonGuid, string ConnectionId)> RevealCalls { get; } = [];

    public ServiceResponse<VoteResult> Vote(Guid personGuid, string connectionId, string mark)
    {
        VoteCalls.Add((personGuid, connectionId, mark));
        return VoteResult;
    }

    public ServiceResponse<string> ClearVotes(Guid personGuid, string connectionId)
    {
        ClearCalls.Add((personGuid, connectionId));
        return ClearResult;
    }

    public ServiceResponse<RevealResult> ForceShowVotes(Guid personGuid, string connectionId)
    {
        RevealCalls.Add((personGuid, connectionId));
        return RevealResult;
    }

    /// <summary>Answers each Tick call; a test scripts the countdown by queueing its results.</summary>
    public Queue<TickResult> TickResults { get; } = new();

    public List<(string RoomId, long Revision)> TickCalls { get; } = [];

    public TickResult Tick(string roomId, long revision)
    {
        lock (TickCalls)
        {
            TickCalls.Add((roomId, revision));
            return TickResults.Count > 0 ? TickResults.Dequeue() : new TickResult { Outcome = TickOutcome.Stale };
        }
    }

    public ServiceResponse<Room> Create(Guid ownerGuid, RoomSettings settings) => throw new NotSupportedException();

    public ServiceResponse<string> Exists(string roomId) => throw new NotSupportedException();
}
