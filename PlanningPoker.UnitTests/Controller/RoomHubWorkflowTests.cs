using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using PlanningPoker.Common.Constants;
using PlanningPoker.Common.Models;
using PlanningPoker.Controller.Constants;
using PlanningPoker.Controller.Extensions;
using PlanningPoker.Controller.Hubs;
using PlanningPoker.Controller.Mappers;
using PlanningPoker.Controller.Models;
using PlanningPoker.Controller.Validations;
using PlanningPoker.Service.Models;
using PlanningPoker.Service.Validations;
using PlanningPoker.UnitTests.Builders;
using PlanningPoker.UnitTests.Fakes;

namespace PlanningPoker.UnitTests.Controller;

public class RoomHubWorkflowTests
{
    private const string ConnectionId = "conn-1";
    private static readonly Person Ann = new() { Guid = Guid.NewGuid(), Name = "Ann", PersonType = PersonTypes.Developer };

    private readonly FakeRoomService _rooms = new();
    private readonly FakeRoomEventPublisher _publisher = new();
    private readonly FakeRoomTimerService _timer = new();
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
            _timer,
            new RoomIdentifierValidation(),
            new PersonTypeValidation(),
            new RoomConfigurationValidation(),
            new TransferTargetValidation(),
            new RoomConfigurationRequestMapper(),
            new JoinRoomResponseMapper(new PersonResponseMapper(), ResponseMappers.VoteStatus(), new RoomConfigurationResponseMapper(), new CardResponseMapper()),
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

    [Theory]
    [InlineData("", "required")]
    [InlineData("has space", "invalid_characters")]
    public async Task JoinRoom_InvalidIdentifier_IsRejectedBeforeAnyGroupOrJoin(string roomId, string reason)
    {
        var response = await NewHub(Ann).JoinRoom(roomId);

        var error = Assert.Single(response.Errors!);
        Assert.Equal(ErrorCodes.ValidationFailed, error.Code);
        Assert.Equal(reason, error.Metadata["reason"]);
        Assert.Empty(_groups.Added);
        Assert.Empty(_groups.Removed);
        Assert.Empty(_rooms.JoinCalls);
        Assert.Empty(_publisher.Joined);
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
    public async Task JoinRoom_SecondTab_PassesThePersonAddedFalseResultToThePublisher()
    {
        _rooms.JoinResult = ServiceResponse<JoinRoomResult>.Success(RoomResults.Join(Ann, personAdded: false));

        await NewHub(Ann).JoinRoom("room");

        Assert.False(Assert.Single(_publisher.Joined).Result.PersonAdded);
    }

    [Fact]
    public async Task OnDisconnected_UnknownConnection_PublishesNothing()
    {
        _rooms.ExitResult = ServiceResponse<ExitRoomResult>.Failure(ServiceError.NotInRoom());

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

    private static PersonTypeChangeResult TypeChange(VotingFollowUp? followUp = null, bool changed = true)
    {
        return new PersonTypeChangeResult
        {
            RoomId = "room",
            Person = Ann,
            Changed = changed,
            VoteStatus = new VoteStatus { Votes = new Dictionary<Guid, string>(), HasEveryoneVoted = true },
            FollowUp = followUp ?? VotingFollowUp.None
        };
    }

    private static RoomConfigurationChangeResult ConfigurationChange(VotingFollowUp? followUp = null)
    {
        return new RoomConfigurationChangeResult
        {
            RoomId = "room",
            Configuration = new RoomConfiguration { CardSet = "linear", CountdownSeconds = 5, ShowFireworks = false, MaxParticipants = 9 },
            Cards = [new CardOption { Value = "1" }],
            UpdatedBy = "Ann",
            VoteStatus = new VoteStatus { Votes = new Dictionary<Guid, string>(), HasEveryoneVoted = true },
            FollowUp = followUp ?? VotingFollowUp.None
        };
    }

    private static RoomConfigurationRequest ConfigurationRequest(string cardSet = "linear", int countdown = 5)
    {
        return new RoomConfigurationRequest { CardSet = cardSet, CountdownSeconds = countdown, ShowFireworks = true, MaxParticipants = 9 };
    }

    [Fact]
    public async Task ChangePersonType_Success_PublishesTheChangeThenTheCountdownItCaused()
    {
        _rooms.ChangeTypeResult = ServiceResponse<PersonTypeChangeResult>.Success(TypeChange(new VotingFollowUp { Countdown = new CountdownStart { Revision = 7, Seconds = 5 } }));

        var response = await NewHub(Ann).ChangePersonType("obs");

        Assert.Null(response.Errors);
        Assert.Equal((Ann.Guid, ConnectionId, "obs"), Assert.Single(_rooms.ChangeTypeCalls));
        Assert.Equal(["PersonTypeChanged", "CountdownUpdate"], _publisher.Order);
        Assert.Equal(("room", 7L), Assert.Single(_timer.Started));
    }

    [Fact]
    public async Task ChangePersonType_ThatRevealsAtOnce_PublishesTheChangeThenVotesShown()
    {
        var revealed = new VoteStatus { Votes = new Dictionary<Guid, string>(), HasEveryoneVoted = true, VotingFinished = true };
        _rooms.ChangeTypeResult = ServiceResponse<PersonTypeChangeResult>.Success(TypeChange(new VotingFollowUp { Revealed = revealed }));

        await NewHub(Ann).ChangePersonType("obs");

        Assert.Equal(["PersonTypeChanged", "VotesShown"], _publisher.Order);
    }

    [Fact]
    public async Task ChangePersonType_Unchanged_IsASuccessThatStartsNothing()
    {
        _rooms.ChangeTypeResult = ServiceResponse<PersonTypeChangeResult>.Success(TypeChange(changed: false));

        var response = await NewHub(Ann).ChangePersonType("dev");

        Assert.Null(response.Errors);
        Assert.False(Assert.Single(_publisher.TypeChanges).Changed);
        Assert.Empty(_timer.Started);
    }

    [Fact]
    public async Task ChangePersonType_InvalidType_IsRejectedBeforeTheService()
    {
        var response = await NewHub(Ann).ChangePersonType("admin");

        var error = Assert.Single(response.Errors!);
        Assert.Equal(ErrorCodes.ValidationFailed, error.Code);
        Assert.Equal("personType", error.Metadata["field"]);
        Assert.Empty(_rooms.ChangeTypeCalls);
        Assert.Empty(_publisher.TypeChanges);
    }

    [Fact]
    public async Task ChangePersonType_NotInRoom_ReturnsTheErrorAndPublishesNothing()
    {
        _rooms.ChangeTypeResult = ServiceResponse<PersonTypeChangeResult>.Failure(ServiceError.NotInRoom());

        var response = await NewHub(Ann).ChangePersonType("obs");

        Assert.Equal(ErrorCodes.NotInRoom, ErrorCode(response));
        Assert.Empty(_publisher.TypeChanges);
    }

    [Fact]
    public async Task ChangePersonType_NoSession_ReturnsNoSession()
    {
        var response = await NewHub(null).ChangePersonType("obs");

        Assert.Equal(ErrorCodes.NoSession, ErrorCode(response));
        Assert.Empty(_rooms.ChangeTypeCalls);
    }

    [Fact]
    public async Task UpdateRoomConfiguration_Success_PublishesTheUpdateThenTheCountdownItRestarted()
    {
        _rooms.UpdateConfigurationResult = ServiceResponse<RoomConfigurationChangeResult>.Success(ConfigurationChange(new VotingFollowUp { Countdown = new CountdownStart { Revision = 3, Seconds = 8 } }));

        var response = await NewHub(Ann).UpdateRoomConfiguration(ConfigurationRequest(countdown: 8));

        Assert.Null(response.Errors);
        var call = Assert.Single(_rooms.UpdateConfigurationCalls);
        Assert.Equal((Ann.Guid, ConnectionId, "linear", 8), (call.PersonGuid, call.ConnectionId, call.Configuration.CardSet, call.Configuration.CountdownSeconds));
        Assert.Equal(["RoomConfigurationUpdated", "CountdownUpdate"], _publisher.Order);
        Assert.Equal(("room", 3L), Assert.Single(_timer.Started));
    }

    [Fact]
    public async Task UpdateRoomConfiguration_NotTheOwner_ReturnsTheErrorAndPublishesNothing()
    {
        _rooms.UpdateConfigurationResult = ServiceResponse<RoomConfigurationChangeResult>.Failure(ServiceError.NotRoomOwner());

        var response = await NewHub(Ann).UpdateRoomConfiguration(ConfigurationRequest());

        Assert.Equal(ErrorCodes.NotRoomOwner, ErrorCode(response));
        Assert.Empty(_publisher.ConfigurationUpdates);
    }

    [Theory]
    [InlineData(null, "configuration")]
    [InlineData("tarot", "cardSet")]
    public async Task UpdateRoomConfiguration_Invalid_IsRejectedBeforeTheService(string? cardSet, string field)
    {
        var request = cardSet is null ? null : ConfigurationRequest(cardSet);

        var response = await NewHub(Ann).UpdateRoomConfiguration(request);

        var error = Assert.Single(response.Errors!);
        Assert.Equal(ErrorCodes.ValidationFailed, error.Code);
        Assert.Equal(field, error.Metadata["field"]);
        Assert.Empty(_rooms.UpdateConfigurationCalls);
    }

    [Fact]
    public async Task UpdateRoomConfiguration_NoSession_ReturnsNoSession()
    {
        var response = await NewHub(null).UpdateRoomConfiguration(ConfigurationRequest());

        Assert.Equal(ErrorCodes.NoSession, ErrorCode(response));
        Assert.Empty(_rooms.UpdateConfigurationCalls);
    }

    [Fact]
    public async Task TransferOwnership_Success_PublishesTheTransfer()
    {
        var bob = Guid.NewGuid();
        _rooms.TransferResult = ServiceResponse<OwnershipTransferResult>.Success(new OwnershipTransferResult { RoomId = "room", NewOwner = Ann });

        var response = await NewHub(Ann).TransferOwnership(bob.ToString());

        Assert.Null(response.Errors);
        Assert.Equal((Ann.Guid, ConnectionId, bob), Assert.Single(_rooms.TransferCalls));
        Assert.Same(Ann, Assert.Single(_publisher.OwnershipTransfers).NewOwner);
    }

    [Fact]
    public async Task TransferOwnership_NotAGuid_IsRejectedBeforeTheService()
    {
        var response = await NewHub(Ann).TransferOwnership("nope");

        var error = Assert.Single(response.Errors!);
        Assert.Equal("targetPersonGuid", error.Metadata["field"]);
        Assert.Equal("invalid_guid", error.Metadata["reason"]);
        Assert.Empty(_rooms.TransferCalls);
    }

    [Fact]
    public async Task TransferOwnership_ServiceRefuses_ReturnsTheErrorAndPublishesNothing()
    {
        _rooms.TransferResult = ServiceResponse<OwnershipTransferResult>.Failure(ServiceError.NotRoomOwner());

        var response = await NewHub(Ann).TransferOwnership(Guid.NewGuid().ToString());

        Assert.Equal(ErrorCodes.NotRoomOwner, ErrorCode(response));
        Assert.Empty(_publisher.OwnershipTransfers);
    }

    [Fact]
    public async Task TransferOwnership_NoSession_ReturnsNoSession()
    {
        var response = await NewHub(null).TransferOwnership(Guid.NewGuid().ToString());

        Assert.Equal(ErrorCodes.NoSession, ErrorCode(response));
        Assert.Empty(_rooms.TransferCalls);
    }

    private static VoteResult VoteOutcome(VotingFollowUp? followUp = null)
    {
        return new VoteResult
        {
            RoomId = "room",
            PersonGuid = Ann.Guid,
            VoteStatus = new VoteStatus { Votes = new Dictionary<Guid, string> { [Ann.Guid] = "hide" } },
            FollowUp = followUp ?? VotingFollowUp.None
        };
    }

    [Fact]
    public async Task Vote_NoSession_FailsWithoutCallingTheService()
    {
        var response = await NewHub(null).Vote("5");

        Assert.Equal(ErrorCodes.NoSession, ErrorCode(response));
        Assert.Empty(_rooms.VoteCalls);
    }

    [Fact]
    public async Task Vote_ServiceFailure_ReturnsItsErrorAndPublishesNothing()
    {
        _rooms.VoteResult = ServiceResponse<VoteResult>.Failure(ServiceError.ObserverCannotVote());

        var response = await NewHub(Ann).Vote("5");

        Assert.Equal(ErrorCodes.ObserverCannotVote, ErrorCode(response));
        Assert.Empty(_publisher.VoteCasts);
        Assert.Empty(_timer.Started);
    }

    [Fact]
    public async Task Vote_Success_PublishesTheVoteAndStartsNothingWithoutAFollowUp()
    {
        _rooms.VoteResult = ServiceResponse<VoteResult>.Success(VoteOutcome());

        var response = await NewHub(Ann).Vote("5");

        Assert.Null(response.Errors);
        Assert.Equal((Ann.Guid, ConnectionId, "5"), Assert.Single(_rooms.VoteCalls));
        Assert.Single(_publisher.VoteCasts);
        Assert.Empty(_timer.Started);
        Assert.Empty(_publisher.CountdownUpdates);
    }

    [Fact]
    public async Task Vote_CountdownFollowUp_StartsTheTickerAndSendsTheFirstSecond()
    {
        var followUp = new VotingFollowUp { Countdown = new CountdownStart { Revision = 4, Seconds = 5 } };
        _rooms.VoteResult = ServiceResponse<VoteResult>.Success(VoteOutcome(followUp));

        await NewHub(Ann).Vote("5");

        Assert.Equal(("room", 4L), Assert.Single(_timer.Started));
        Assert.Equal(("room", 5), Assert.Single(_publisher.CountdownUpdates));
    }

    [Fact]
    public async Task Vote_ImmediateRevealFollowUp_AnnouncesTheVotesAndStartsNoTicker()
    {
        var status = new VoteStatus { Votes = new Dictionary<Guid, string> { [Ann.Guid] = "5" }, VotingFinished = true };
        _rooms.VoteResult = ServiceResponse<VoteResult>.Success(VoteOutcome(new VotingFollowUp { Revealed = status }));

        await NewHub(Ann).Vote("5");

        Assert.Same(status, Assert.Single(_publisher.Shown).Status);
        Assert.Empty(_timer.Started);
    }

    [Fact]
    public async Task ClearVotes_Success_PublishesClearedForTheRoom()
    {
        _rooms.ClearResult = ServiceResponse<string>.Success("room");

        var response = await NewHub(Ann).ClearVotes();

        Assert.Null(response.Errors);
        Assert.Equal("room", Assert.Single(_publisher.Cleared));
    }

    [Fact]
    public async Task ClearVotes_NoSession_Fails()
    {
        var response = await NewHub(null).ClearVotes();

        Assert.Equal(ErrorCodes.NoSession, ErrorCode(response));
        Assert.Empty(_rooms.ClearCalls);
    }

    [Fact]
    public async Task ClearVotes_ServiceFailure_PublishesNothing()
    {
        _rooms.ClearResult = ServiceResponse<string>.Failure(ServiceError.NotInRoom());

        var response = await NewHub(Ann).ClearVotes();

        Assert.Equal(ErrorCodes.NotInRoom, ErrorCode(response));
        Assert.Empty(_publisher.Cleared);
    }

    [Fact]
    public async Task ForceShowVotes_Revealed_PublishesTheVotes()
    {
        var status = new VoteStatus { Votes = new Dictionary<Guid, string>(), VotingFinished = true };
        _rooms.RevealResult = ServiceResponse<RevealResult>.Success(new RevealResult { RoomId = "room", VoteStatus = status });

        var response = await NewHub(Ann).ForceShowVotes();

        Assert.Null(response.Errors);
        Assert.Equal("room", Assert.Single(_publisher.Shown).RoomId);
    }

    [Fact]
    public async Task ForceShowVotes_AlreadyRevealed_AnnouncesNothing()
    {
        _rooms.RevealResult = ServiceResponse<RevealResult>.Success(new RevealResult { RoomId = "room", VoteStatus = null });

        var response = await NewHub(Ann).ForceShowVotes();

        Assert.Null(response.Errors);
        Assert.Empty(_publisher.Shown);
    }

    [Fact]
    public async Task ForceShowVotes_NoSession_Fails()
    {
        var response = await NewHub(null).ForceShowVotes();

        Assert.Equal(ErrorCodes.NoSession, ErrorCode(response));
        Assert.Empty(_rooms.RevealCalls);
    }

    [Fact]
    public async Task ForceShowVotes_ServiceFailure_PublishesNothing()
    {
        _rooms.RevealResult = ServiceResponse<RevealResult>.Failure(ServiceError.NotInRoom());

        await NewHub(Ann).ForceShowVotes();

        Assert.Empty(_publisher.Shown);
    }

    [Fact]
    public async Task OnDisconnected_ExitWithCountdownFollowUp_RestartsTheTicker()
    {
        var exit = RoomResults.Exit(Ann, followUp: new VotingFollowUp { Countdown = new CountdownStart { Revision = 9, Seconds = 3 } });
        _rooms.ExitResult = ServiceResponse<ExitRoomResult>.Success(exit);

        await NewHub(Ann).OnDisconnectedAsync(null);

        Assert.Equal(("room", 9L), Assert.Single(_timer.Started));
        Assert.Equal(("room", 3), Assert.Single(_publisher.CountdownUpdates));
    }
}
