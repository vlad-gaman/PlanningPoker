using PlanningPoker.Common.Constants;
using PlanningPoker.Controller.Constants;
using PlanningPoker.Controller.Mappers;
using PlanningPoker.Controller.Models;
using PlanningPoker.Controller.Services;
using PlanningPoker.Service.Mappers;
using PlanningPoker.Service.Models;
using PlanningPoker.Service.Services;
using PlanningPoker.UnitTests.Builders;
using PlanningPoker.UnitTests.Fakes;

namespace PlanningPoker.UnitTests.Controller;

public class OperatorEventReasonTests
{
    private static readonly PersonResponseMapper PersonMapper = new();
    private static readonly Person Ann = new() { Guid = Guid.NewGuid(), Name = "Ann", PersonType = "dev" };
    private static readonly Person Bob = new() { Guid = Guid.NewGuid(), Name = "Bob", PersonType = "test" };

    private readonly FakeHubContext _hub = new();
    private readonly RoomEventPublisher _publisher;

    public OperatorEventReasonTests()
    {
        _publisher = new RoomEventPublisher(
            _hub,
            new PersonJoinedEventMapper(PersonMapper),
            new RoomCountdownEventMapper(),
            new PersonExitedEventMapper(PersonMapper),
            new PersonOffgridEventMapper(PersonMapper),
            new PersonBackEventMapper(PersonMapper),
            new OwnershipTransferredEventMapper(PersonMapper),
            new RoomDisposedEventMapper(),
            new VoteCastEventMapper(),
            ResponseMappers.VoteStatus(),
            new PersonTypeChangedEventMapper(PersonMapper, ResponseMappers.VoteStatus()),
            new RoomConfigurationUpdatedEventMapper(new RoomConfigurationResponseMapper(), new CardResponseMapper(), ResponseMappers.VoteStatus()),
            new VotesClearedEventMapper());
    }

    [Fact]
    public void RoomDisposedEventMapper_NoReason_IsNull()
    {
        Assert.Null(new RoomDisposedEventMapper().Map("blue-fox").Reason);
    }

    [Fact]
    public void RoomDisposedEventMapper_CarriesTheReason()
    {
        var mapped = new RoomDisposedEventMapper().Map("blue-fox", RoomEventReasons.ClosedByOperator);

        Assert.Equal("blue-fox", mapped.RoomId);
        Assert.Equal("closedByOperator", mapped.Reason);
    }

    [Fact]
    public void PersonExitedEventMapper_OrdinaryExit_HasNoReason()
    {
        Assert.Null(new PersonExitedEventMapper(PersonMapper).Map(RoomResults.Exit(Ann)).Reason);
    }

    [Fact]
    public void PersonExitedEventMapper_CarriesTheReasonAndThePersonsId()
    {
        var result = new ExitRoomMapperHelper().Removed(Bob);

        var mapped = new PersonExitedEventMapper(PersonMapper).Map(result);

        Assert.Equal("removedByOperator", mapped.Reason);
        Assert.Equal(Bob.Guid, mapped.Person.Guid);
    }

    [Fact]
    public void ExitRoomResultMapper_DefaultsToNoReasonAndNoConnections()
    {
        var result = new ExitRoomResultMapper().Map("room", Ann, true, false, null, VotingFollowUp.None);

        Assert.Null(result.Reason);
        Assert.Empty(result.ConnectionIds);
    }

    [Fact]
    public void ExitRoomResultMapper_CarriesTheReasonAndConnections()
    {
        var result = new ExitRoomResultMapper().Map("room", Ann, true, false, null, VotingFollowUp.None, RoomEventReasons.RemovedByOperator, ["c1", "c2"]);

        Assert.Equal("removedByOperator", result.Reason);
        Assert.Equal(["c1", "c2"], result.ConnectionIds);
    }

    [Fact]
    public async Task PublishPersonExited_ByTheOperator_SendsTheReasonThenTakesTheConnectionsOutOfTheGroup()
    {
        var result = new ExitRoomResultMapper().Map("room", Bob, true, false, null, VotingFollowUp.None, RoomEventReasons.RemovedByOperator, ["bob-1", "bob-2"]);

        await _publisher.PublishPersonExitedAsync(result);

        var exited = Assert.Single(_hub.Clients.Sent, message => message.Method == HubEvents.PersonExited);
        var payload = Assert.IsType<PersonExitedEvent>(exited.Payload);
        Assert.Equal("removedByOperator", payload.Reason);
        Assert.Equal(Bob.Guid, payload.Person.Guid);
        Assert.Equal([("bob-1", "room"), ("bob-2", "room")], _hub.Groups.Removed);
    }

    [Fact]
    public async Task PublishPersonExited_OrdinaryExit_LeavesTheGroupsAlone()
    {
        await _publisher.PublishPersonExitedAsync(RoomResults.Exit(Bob));

        Assert.Empty(_hub.Groups.Removed);
        Assert.Null(Assert.IsType<PersonExitedEvent>(Assert.Single(_hub.Clients.Sent).Payload).Reason);
    }

    [Fact]
    public async Task RoomClosed_SendsRoomDisposedWithTheReasonThenTakesEveryoneOutOfTheGroup()
    {
        await ((IRoomAdminNotifier)_publisher).RoomClosedAsync("room", ["a", "b", "c"]);

        var disposed = Assert.Single(_hub.Clients.Sent);
        Assert.Equal(HubEvents.RoomDisposed, disposed.Method);
        var payload = Assert.IsType<RoomDisposedEvent>(disposed.Payload);
        Assert.Equal("room", payload.RoomId);
        Assert.Equal("closedByOperator", payload.Reason);
        Assert.Equal(3, _hub.Groups.Removed.Count);
        Assert.All(_hub.Groups.Removed, removed => Assert.Equal("room", removed.GroupName));
    }

    private sealed class ExitRoomMapperHelper
    {
        public ExitRoomResult Removed(Person person)
        {
            return new ExitRoomResultMapper().Map("room", person, true, false, null, VotingFollowUp.None, RoomEventReasons.RemovedByOperator);
        }
    }
}
