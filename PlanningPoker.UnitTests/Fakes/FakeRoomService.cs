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

    public ServiceResponse<Room> Create(Guid ownerGuid, RoomSettings settings) => throw new NotSupportedException();

    public ServiceResponse<string> Exists(string roomId) => throw new NotSupportedException();
}
