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
using PlanningPoker.UnitTests.Fakes;

namespace PlanningPoker.UnitTests.Controller;

public class RoomHubWorkflowTests
{
    private const string ConnectionId = "conn-1";
    private static readonly Guid AnnGuid = Guid.NewGuid();
    private static readonly Person Ann = new() { Guid = AnnGuid, Name = "Ann", PersonType = PersonTypes.Developer };
    private static readonly RoomConfiguration Configuration = new() { CardSet = "linear", CountdownSeconds = 5, ShowFireworks = false, MaxParticipants = 9 };
    private static readonly Room Room = new() { Id = "room", OwnerGuid = AnnGuid, VotingEnabled = true, Configuration = Configuration };

    private readonly FakeRoomService _rooms = new();
    private readonly FakeRoomEventPublisher _publisher = new();

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
            Groups = new FakeGroupManager()
        };
    }

    private static JoinRoomResult JoinResult(bool resetCountdown = true)
    {
        return new JoinRoomResult
        {
            Room = Room,
            Person = Ann,
            People = [Ann],
            VoteStatus = new VoteStatus { Votes = new Dictionary<Guid, string>(), HasEveryoneVoted = false },
            ResetCountdown = resetCountdown
        };
    }

    private static ExitRoomResult ExitResult(bool disposed, Person? newOwner = null)
    {
        return new ExitRoomResult { RoomId = "room", Person = Ann, RoomDisposed = disposed, NewOwner = newOwner };
    }

    private static string ErrorCode<T>(ApiResponse<T> response) => response.Errors![0].Code;

    [Fact]
    public async Task JoinRoom_Success_JoinsGroupPublishesAndReturnsPeople()
    {
        _rooms.JoinResult = ServiceResponse<JoinRoomResult>.Success(JoinResult());
        var hub = NewHub(Ann);

        var response = await hub.JoinRoom("room");

        Assert.Null(response.Errors);
        Assert.Equal("Ann", response.Data!.People.Single().Name);
        Assert.Equal((AnnGuid, ConnectionId, "room"), Assert.Single(_rooms.JoinCalls));
        Assert.Equal(("conn-1", "room"), Assert.Single(((FakeGroupManager)hub.Groups).Added));
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
        Assert.Empty(_rooms.JoinCalls);
        Assert.Empty(_publisher.Joined);
    }

    [Fact]
    public async Task JoinRoom_UnknownRoom_ReturnsRoomNotFoundAndPublishesNothing()
    {
        _rooms.JoinResult = ServiceResponse<JoinRoomResult>.Failure(ServiceError.Create(ErrorCodes.RoomNotFound, "The room does not exist."));
        var hub = NewHub(Ann);

        var response = await hub.JoinRoom("room");

        Assert.Equal(ErrorCodes.RoomNotFound, ErrorCode(response));
        Assert.Empty(((FakeGroupManager)hub.Groups).Added);
        Assert.Empty(_publisher.Joined);
    }

    [Fact]
    public async Task JoinRoom_ConnectionClosedDuringJoin_ExitsAndDoesNotAnnounce()
    {
        _rooms.JoinResult = ServiceResponse<JoinRoomResult>.Success(JoinResult());
        _rooms.ExitResult = ServiceResponse<ExitRoomResult>.Success(ExitResult(disposed: true));
        using var aborted = new CancellationTokenSource();
        await aborted.CancelAsync();

        var response = await NewHub(Ann, aborted.Token).JoinRoom("room");

        Assert.Equal(ErrorCodes.ConnectionClosed, ErrorCode(response));
        var exit = Assert.Single(_rooms.ExitCalls);
        Assert.Equal((AnnGuid, ConnectionId), exit);
        Assert.Empty(_publisher.Joined);
        Assert.True(Assert.Single(_publisher.Exited).RoomDisposed);
    }

    [Fact]
    public async Task OnDisconnected_ExitSucceeds_PublishesTheExitResult()
    {
        _rooms.ExitResult = ServiceResponse<ExitRoomResult>.Success(ExitResult(disposed: false, newOwner: Ann));
        var hub = NewHub(Ann);

        await hub.OnDisconnectedAsync(null);

        Assert.Equal((AnnGuid, ConnectionId), Assert.Single(_rooms.ExitCalls));
        var exited = Assert.Single(_publisher.Exited);
        Assert.Same(Ann, exited.NewOwner);
        Assert.False(exited.RoomDisposed);
    }

    [Fact]
    public async Task OnDisconnected_LastMember_PublishesRoomDisposed()
    {
        _rooms.ExitResult = ServiceResponse<ExitRoomResult>.Success(ExitResult(disposed: true));

        await NewHub(Ann).OnDisconnectedAsync(null);

        Assert.True(Assert.Single(_publisher.Exited).RoomDisposed);
    }

    [Fact]
    public async Task OnDisconnected_ExitFails_PublishesNothing()
    {
        _rooms.ExitResult = ServiceResponse<ExitRoomResult>.Failure(ServiceError.Create(ErrorCodes.PersonStillInRoom, "still in room"));

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
