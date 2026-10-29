using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using PlanningPoker.Common.Constants;
using PlanningPoker.Common.Models;
using PlanningPoker.Controller.Constants;
using PlanningPoker.Controller.Extensions;
using PlanningPoker.Controller.Hubs;
using PlanningPoker.Controller.Mappers;
using PlanningPoker.Controller.Models;
using PlanningPoker.Service.Models;
using PlanningPoker.UnitTests.Builders;
using PlanningPoker.UnitTests.Fakes;

namespace PlanningPoker.UnitTests.Controller;

public class RoomHubWorkflowTests
{
    private const string ConnectionId = "conn-1";
    private static readonly Person Ann = new() { Guid = Guid.NewGuid(), Name = "Ann", PersonType = PersonTypes.Developer };

    private readonly FakeRoomService _rooms = new();
    private readonly FakeRoomEventPublisher _publisher = new();
    private readonly FakeGroupManager _groups = new();

    private RoomHub NewHub(Person? person, CancellationToken connectionAborted = default)
    {
        var http = new DefaultHttpContext();
        if (person is not null)
        {
            http.SetSessionPerson(person);
        }

        return new RoomHub(
            _rooms,
            _publisher,
            new JoinRoomResponseMapper(new PersonResponseMapper(), new VoteStatusResponseMapper(), new RoomConfigurationResponseMapper()),
            new ApiResponseMapper(new ApiErrorMapper()),
            NullLogger<RoomHub>.Instance)
        {
            Context = new FakeHubCallerContext(ConnectionId, http, connectionAborted),
            Groups = _groups
        };
    }

    private static string ErrorCode<T>(ApiResponse<T> response) => response.Errors![0].Code;

    [Fact]
    public async Task JoinRoom_Success_JoinsGroupBeforeJoiningTheRoomAndPublishesJoin()
    {
        _rooms.JoinResult = ServiceResponse<JoinRoomResult>.Success(RoomResults.Join(Ann));
        var groupsSeenByService = -1;
        _rooms.BeforeJoin = () => groupsSeenByService = _groups.Added.Count;

        var response = await NewHub(Ann).JoinRoom("room");

        Assert.Null(response.Errors);
        Assert.Equal("Ann", response.Data!.People.Single().Name);
        Assert.Equal(1, groupsSeenByService);
        Assert.Equal((ConnectionId, "room"), Assert.Single(_groups.Added));
        Assert.Empty(_groups.Removed);
        Assert.Equal((Ann.Guid, ConnectionId, "room"), Assert.Single(_rooms.JoinCalls));
        var joined = Assert.Single(_publisher.Joined);
        Assert.Equal(ConnectionId, joined.JoinerConnectionId);
        Assert.True(joined.Result.ResetCountdown);
        Assert.Empty(_publisher.Exited);
    }

    [Fact]
    public async Task JoinRoom_NoSession_ReturnsNoSessionWithoutTouchingTheRoom()
    {
        var response = await NewHub(null).JoinRoom("room");

        Assert.Equal(ErrorCodes.NoSession, ErrorCode(response));
        Assert.Empty(_groups.Added);
        Assert.Empty(_rooms.JoinCalls);
        Assert.Empty(_publisher.Joined);
    }

    [Fact]
    public async Task JoinRoom_UnknownRoom_LeavesTheGroupAgainAndPublishesNothing()
    {
        _rooms.JoinResult = ServiceResponse<JoinRoomResult>.Failure(ServiceError.RoomNotFound("room"));

        var response = await NewHub(Ann).JoinRoom("room");

        Assert.Equal(ErrorCodes.RoomNotFound, ErrorCode(response));
        Assert.Equal((ConnectionId, "room"), Assert.Single(_groups.Added));
        Assert.Equal((ConnectionId, "room"), Assert.Single(_groups.Removed));
        Assert.Empty(_publisher.Joined);
    }

    [Fact]
    public async Task JoinRoom_EmptyIdentifier_IsRejectedWithoutAGroup()
    {
        _rooms.JoinResult = ServiceResponse<JoinRoomResult>.Failure(ServiceError.ValidationFailed("roomId", "required"));

        var response = await NewHub(Ann).JoinRoom(string.Empty);

        Assert.Equal(ErrorCodes.ValidationFailed, ErrorCode(response));
        Assert.Empty(_groups.Added);
        Assert.Empty(_groups.Removed);
    }

    [Fact]
    public async Task JoinRoom_ConnectionClosedDuringJoin_ExitsKeepsTheGroupMembershipAndDoesNotAnnounce()
    {
        _rooms.JoinResult = ServiceResponse<JoinRoomResult>.Success(RoomResults.Join(Ann));
        _rooms.ExitResult = ServiceResponse<ExitRoomResult>.Success(RoomResults.Exit(Ann, roomDisposed: true));
        using var aborted = new CancellationTokenSource();
        await aborted.CancelAsync();

        var response = await NewHub(Ann, aborted.Token).JoinRoom("room");

        Assert.Equal(ErrorCodes.ConnectionClosed, ErrorCode(response));
        Assert.Equal((Ann.Guid, ConnectionId), Assert.Single(_rooms.ExitCalls));
        Assert.Empty(_publisher.Joined);
        Assert.True(Assert.Single(_publisher.Exited).RoomDisposed);
        Assert.Empty(_groups.Removed);
    }

    [Fact]
    public async Task OnDisconnected_ExitSucceeds_PublishesTheExitResult()
    {
        _rooms.ExitResult = ServiceResponse<ExitRoomResult>.Success(RoomResults.Exit(Ann, newOwner: Ann));

        await NewHub(Ann).OnDisconnectedAsync(null);

        Assert.Equal((Ann.Guid, ConnectionId), Assert.Single(_rooms.ExitCalls));
        var exited = Assert.Single(_publisher.Exited);
        Assert.Same(Ann, exited.NewOwner);
        Assert.False(exited.RoomDisposed);
    }

    [Fact]
    public async Task OnDisconnected_LastMember_PassesRoomDisposedToThePublisher()
    {
        _rooms.ExitResult = ServiceResponse<ExitRoomResult>.Success(RoomResults.Exit(Ann, roomDisposed: true));

        await NewHub(Ann).OnDisconnectedAsync(null);

        Assert.True(Assert.Single(_publisher.Exited).RoomDisposed);
    }

    [Fact]
    public async Task OnDisconnected_OtherTabStillOpen_PassesPersonLeftFalseToThePublisher()
    {
        _rooms.ExitResult = ServiceResponse<ExitRoomResult>.Success(RoomResults.Exit(Ann, personLeft: false));

        await NewHub(Ann).OnDisconnectedAsync(null);

        Assert.False(Assert.Single(_publisher.Exited).PersonLeft);
    }

    [Fact]
    public async Task OnDisconnected_UnknownConnection_PublishesNothing()
    {
        _rooms.ExitResult = ServiceResponse<ExitRoomResult>.Failure(ServiceError.Create(ErrorCodes.NotInRoom, "The connection is not in a room."));

        await NewHub(Ann).OnDisconnectedAsync(null);

        Assert.Empty(_publisher.Exited);
    }

    [Fact]
    public async Task OnDisconnected_NoSession_DoesNotCallExit()
    {
        await NewHub(null).OnDisconnectedAsync(null);

        Assert.Empty(_rooms.ExitCalls);
        Assert.Empty(_publisher.Exited);
    }
}
